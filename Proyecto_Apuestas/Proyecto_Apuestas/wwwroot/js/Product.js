/* global bootstrap, Stripe */

const STRIPE_PUBLIC_KEY = window.STRIPE_PUBLIC_KEY;
const CREATE_SESSION_URL = "/payment/create-checkout-session";

let stripe = null;
let embeddedCheckout = null;     // instancia única
let embeddedMounted = false;

let checkoutObserver = null;
let runId = 0;

// deferred para el clientSecret
let secretDeferred = null;
function newDeferred() {
    let resolve, reject;
    const promise = new Promise((res, rej) => { resolve = res; reject = rej; });
    return { promise, resolve, reject };
}

function showOverlay() {
    const o = document.getElementById("stripe-loading-spinner");
    const c = document.getElementById("checkout");
    o?.classList.add("is-visible");
    if (c) c.style.display = "none";
}
function hideOverlay() {
    const o = document.getElementById("stripe-loading-spinner");
    const c = document.getElementById("checkout");
    o?.classList.remove("is-visible");
    if (c) c.style.display = "block";
}
function getAntiForgeryToken() {
    const i = document.querySelector('input[name="__RequestVerificationToken"]');
    return i ? i.value : null;
}

async function unmountIfMounted() {
    if (embeddedCheckout && embeddedMounted) {
        try { await embeddedCheckout.unmount(); } catch (e) { console.warn("unmount fallo:", e); }
        embeddedMounted = false;
    }
    if (checkoutObserver) { try { checkoutObserver.disconnect(); } catch { } checkoutObserver = null; }
    hideOverlay();
    const cont = document.getElementById("checkout");
    if (cont) cont.innerHTML = "";
}

// crea (una vez) el Embedded Checkout con fetchClientSecret basado en deferred
async function getEmbeddedCheckout() {
    if (!embeddedCheckout) {
        embeddedCheckout = await stripe.initEmbeddedCheckout({
            fetchClientSecret: async () => {
                if (!secretDeferred) throw new Error("No pending client secret");
                return await secretDeferred.promise;  // se resuelve cuando llegue del backend
            }
        });
    }
    return embeddedCheckout;
}

async function startEmbeddedCheckout(productId) {
    if (!STRIPE_PUBLIC_KEY) { alert("Stripe Public Key no configurada."); return; }
    if (!stripe) stripe = Stripe(STRIPE_PUBLIC_KEY);

    const modalEl = document.getElementById("stripeModal");
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl, { backdrop: "static", keyboard: false });
    const checkoutContainer = document.getElementById("checkout");

    const myRun = ++runId;

    await unmountIfMounted();
    showOverlay();
    modal.show();

    // Invalida si se cierra
    const onHiding = () => { runId++; };
    const onHidden = async () => {
        await unmountIfMounted();
        modalEl.removeEventListener("hide.bs.modal", onHiding);
        modalEl.removeEventListener("hidden.bs.modal", onHidden);
    };
    modalEl.addEventListener("hide.bs.modal", onHiding);
    modalEl.addEventListener("hidden.bs.modal", onHidden);

    const headers = { "Content-Type": "application/json" };
    const anti = getAntiForgeryToken();
    if (anti) headers["RequestVerificationToken"] = anti;

    try {
        // 1) Prepara el deferred ANTES de montar
        secretDeferred = newDeferred();

        // 2) En paralelo, pedimos el clientSecret al backend
        const fetchPromise = (async () => {
            const resp = await fetch(CREATE_SESSION_URL, {
                method: "POST",
                headers,
                body: JSON.stringify({ productId: parseInt(productId, 10) })
            });

            if (resp.redirected && resp.url?.toLowerCase().includes("/account/login")) {
                const returnUrl = encodeURIComponent(location.pathname + location.search);
                location.href = `/Account/Login?returnUrl=${returnUrl}`;
                return;
            }
            if (resp.status === 401) {
                const returnUrl = encodeURIComponent(location.pathname + location.search);
                location.href = `/Account/Login?returnUrl=${returnUrl}`;
                return;
            }
            if (resp.status === 400) {
                console.error("BadRequest/Antiforgery:", await resp.text());
                alert("Tu sesión o token CSRF expiró. Recarga la página e intenta de nuevo.");
                secretDeferred.reject(new Error("Antiforgery"));
                return;
            }

            const ct = resp.headers.get("content-type") || "";
            if (!ct.includes("application/json")) {
                console.error("Respuesta no-JSON:", await resp.text());
                secretDeferred.reject(new Error("Respuesta no JSON"));
                return;
            }

            const json = await resp.json();
            if (!json.success) {
                alert(json.message || "Error al crear la sesión de pago");
                secretDeferred.reject(new Error(json.message || "API error"));
                return;
            }

            const clientSecret = (json.data && json.data.clientSecret) || json.clientSecret;
            if (!clientSecret) {
                secretDeferred.reject(new Error("Sin clientSecret del servidor"));
                return;
            }

            // Resuelve el deferred (Stripe seguirá cuando esto se resuelva)
            secretDeferred.resolve(clientSecret);
        })();

        // 3) Crea (o reutiliza) la instancia y monta (esto llamará a fetchClientSecret -> deferred)
        const ec = await getEmbeddedCheckout();
        await ec.mount("#checkout");
        embeddedMounted = true;

        if (myRun !== runId) { await unmountIfMounted(); return; }

        hideOverlay();

        // Plan B: ocultar overlay cuando aparezca el iframe
        checkoutObserver = new MutationObserver(() => {
            const iframe = checkoutContainer?.querySelector("iframe");
            if (iframe) { hideOverlay(); checkoutObserver.disconnect(); checkoutObserver = null; }
        });
        if (checkoutContainer) {
            checkoutObserver.observe(checkoutContainer, { childList: true, subtree: true });
        }

        // Espera a que el fetch termine (para capturar errores y no dejar el deferred colgado)
        await fetchPromise;
    } catch (err) {
        console.error("Error al iniciar Stripe Checkout:", err);
        alert("Hubo un problema al iniciar el pago. Intenta nuevamente.");
        try { modal.hide(); } catch { }
    }
}

// Botones
function wireBuyButtons() {
    document.querySelectorAll(".btn-comprar").forEach((btn) => {
        btn.addEventListener("click", (e) => {
            e.preventDefault();
            const productId = btn.getAttribute("data-product-id");
            if (!productId) { alert("Producto inválido."); return; }
            startEmbeddedCheckout(productId);
        });
    });
}

// X blanca (fallback)
document.addEventListener("DOMContentLoaded", () => {
    const closeBtn = document.querySelector("#stripeModal .btn-close");
    if (closeBtn) closeBtn.classList.add("btn-close-white");
    wireBuyButtons();
});
