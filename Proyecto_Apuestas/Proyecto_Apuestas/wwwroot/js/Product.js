/* global bootstrap, Stripe */

// ================= Config =================
const STRIPE_PUBLIC_KEY = window.STRIPE_PUBLIC_KEY;
const CREATE_SESSION_URL = "/payment/create-checkout-session";

// ================= Estado =================
let stripe = null;
let embeddedCheckout = null;   // única instancia
let embeddedMounted = false;
let runId = 0;

let requestedProductId = null; // el producto del intento activo
let lastSecretAbort = null;    // AbortController del último fetch

// ================= Utilitarios =================
function ensureStripe() {
    if (!STRIPE_PUBLIC_KEY) throw new Error("NO_PUBLISHABLE_KEY");
    if (!window.Stripe) throw new Error("NO_STRIPE_GLOBAL");
    if (!stripe) stripe = window.Stripe(STRIPE_PUBLIC_KEY);
}

function getAntiForgeryToken() {
    const i = document.querySelector('input[name="__RequestVerificationToken"]');
    return i ? i.value : null;
}

function showOverlay() {
    document.getElementById("stripe-loading-spinner")?.classList.add("is-visible");
    const c = document.getElementById("checkout");
    if (c) c.style.display = "none";
}
function hideOverlay() {
    document.getElementById("stripe-loading-spinner")?.classList.remove("is-visible");
    const c = document.getElementById("checkout");
    if (c) c.style.display = "block";
}

async function unmountIfMounted() {
    if (embeddedCheckout && embeddedMounted) {
        try { await embeddedCheckout.unmount(); } catch (e) { console.warn("[Stripe] unmount falló:", e); }
        embeddedMounted = false;
    }
    const cont = document.getElementById("checkout");
    if (cont) cont.innerHTML = "";
    hideOverlay();
}

function closeStripeModal() {
    const el = document.getElementById("stripeModal");
    if (!el) return;
    try { (bootstrap.Modal.getInstance(el) || bootstrap.Modal.getOrCreateInstance(el)).hide(); } catch { }
}

// ================= Backend =================
async function createSession(productId, signal) {
    const headers = { "Content-Type": "application/json" };
    const anti = getAntiForgeryToken();
    if (anti) headers["RequestVerificationToken"] = anti;

    const resp = await fetch(CREATE_SESSION_URL, {
        method: "POST",
        headers,
        body: JSON.stringify({ productId: parseInt(productId, 10) }),
        signal
    });

    // Auth / CSRF / No-JSON
    if (resp.redirected && resp.url?.toLowerCase().includes("/account/login")) {
        const returnUrl = encodeURIComponent(location.pathname + location.search);
        location.href = `/Account/Login?returnUrl=${returnUrl}`;
        throw new Error("AUTH_REDIRECT");
    }
    if (resp.status === 401) {
        const returnUrl = encodeURIComponent(location.pathname + location.search);
        location.href = `/Account/Login?returnUrl=${returnUrl}`;
        throw new Error("UNAUTHORIZED");
    }
    if (resp.status === 400) {
        console.error("[Stripe] BadRequest/Antiforgery:", await resp.text());
        throw new Error("ANTIFORGERY");
    }

    const ct = resp.headers.get("content-type") || "";
    if (!ct.includes("application/json")) {
        console.error("[Stripe] Respuesta no JSON:", await resp.text());
        throw new Error("NON_JSON");
    }

    const json = await resp.json();
    if (!json.success) throw new Error(json.message || "API_ERROR");

    const clientSecret = (json.data && json.data.clientSecret) || json.clientSecret;
    if (!clientSecret) throw new Error("NO_CLIENT_SECRET");
    return clientSecret;
}

// fetchClientSecret llamado por Stripe **cada vez que montas**
async function fetchClientSecretImpl() {
    const myRun = runId;
    const pid = requestedProductId;
    console.log("[Stripe] fetchClientSecret para productId:", pid, "run:", myRun);
    if (!pid) throw new Error("NO_PRODUCT");

    // Cancela cualquier solicitud anterior
    if (lastSecretAbort) { try { lastSecretAbort.abort("NEW_ATTEMPT"); } catch { } }
    lastSecretAbort = new AbortController();

    // Timeout manual (Stripe si no recibe nada, hace ese “Timed out…”)
    const t = setTimeout(() => {
        try { lastSecretAbort.abort("TIMEOUT"); } catch { }
    }, 20000); // 20s

    try {
        const secret = await createSession(pid, lastSecretAbort.signal);
        if (myRun !== runId) throw new Error("CANCELLED");
        console.log("[Stripe] clientSecret OK");
        return secret;
    } finally {
        clearTimeout(t);
    }
}

async function ensureEmbeddedCheckout() {
    ensureStripe();
    if (!embeddedCheckout) {
        embeddedCheckout = await stripe.initEmbeddedCheckout({
            fetchClientSecret: fetchClientSecretImpl
        });
    }
    return embeddedCheckout;
}

// ================= Flujo principal =================
async function startEmbeddedCheckout(productId) {
    const modalEl = document.getElementById("stripeModal");
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl, { backdrop: "static", keyboard: false });

    const thisRun = ++runId;
    requestedProductId = productId;

    // Limpia intentos previos y cancela fetches pendientes
    await unmountIfMounted();
    if (lastSecretAbort) { try { lastSecretAbort.abort("NEW_MODAL"); } catch { } }

    showOverlay();
    modal.show();

    // invalidar si el usuario cierra
    const onHiding = () => { runId++; requestedProductId = null; if (lastSecretAbort) try { lastSecretAbort.abort("MODAL_CLOSE"); } catch { } };
    const onHidden = async () => {
        modalEl.removeEventListener("hide.bs.modal", onHiding);
        modalEl.removeEventListener("hidden.bs.modal", onHidden);
        await unmountIfMounted();
    };
    modalEl.addEventListener("hide.bs.modal", onHiding);
    modalEl.addEventListener("hidden.bs.modal", onHidden);

    try {
        await ensureEmbeddedCheckout();      // una sola vez
        await embeddedCheckout.mount("#checkout");  // llama a fetchClientSecretImpl()
        embeddedMounted = true;

        if (thisRun !== runId) { await unmountIfMounted(); return; }
        hideOverlay();
    } catch (err) {
        console.error("Error al iniciar Stripe Checkout:", err);
        await unmountIfMounted();
        closeStripeModal();
        alert("Hubo un problema al iniciar el pago. Intenta nuevamente.");
    }
}

// ================= BYPASS DEV/ADMIN =================
async function confirmFreeBypass(productId) {
    if (window.BYPASS_ENABLED !== true) { alert("Bypass deshabilitado."); return; }

    const headers = { "Content-Type": "application/json" };
    const anti = getAntiForgeryToken();
    if (anti) headers["RequestVerificationToken"] = anti;
    if (window.BYPASS_CODE) headers["X-Bypass-Code"] = window.BYPASS_CODE;

    const resp = await fetch("/payment/dev/confirm", {
        method: "POST",
        headers,
        body: JSON.stringify({ productId: parseInt(productId, 10) })
    });

    const ct = resp.headers.get("content-type") || "";
    if (!ct.includes("application/json")) { alert("Respuesta inesperada del servidor."); return; }

    const json = await resp.json();
    if (json.success) {
        alert(json.message || "Compra DEV confirmada");
        // Resetea estado y cierra
        requestedProductId = null;
        runId++;
        if (lastSecretAbort) { try { lastSecretAbort.abort("BYPASS_DONE"); } catch { } }
        await unmountIfMounted();
        closeStripeModal();
    } else {
        alert(json.message || "No se pudo confirmar el bypass");
    }
}

// ================= Wiring =================
function wireBuyButtons() {
    document.querySelectorAll(".btn-comprar").forEach((btn) => {
        btn.addEventListener("click", (e) => {
            e.preventDefault(); // necesitamos que no navegue
            const productId = btn.getAttribute("data-product-id");
            if (e.altKey && window.BYPASS_ENABLED === true) {
                if (productId) confirmFreeBypass(productId);
                return;
            }
            if (!productId) { alert("Producto inválido."); return; }
            startEmbeddedCheckout(productId);
        }, { passive: false }); // ¡importante! así sí podemos preventDefault
    });
}

function wireDevButton() {
    const devBtn = document.getElementById("btn-dev-bypass");
    if (!devBtn) return;

    devBtn.classList.remove("d-none");
    let lastProductIdHover = null;

    document.querySelectorAll(".btn-comprar").forEach((b) => {
        const pid = () => b.getAttribute("data-product-id");
        ["mouseenter", "focus", "click"].forEach(evt => b.addEventListener(evt, () => lastProductIdHover = pid()));
    });

    devBtn.addEventListener("click", () => {
        if (window.BYPASS_ENABLED !== true) { alert("Bypass deshabilitado."); return; }
        const pid = devBtn.getAttribute("data-product-id") || lastProductIdHover;
        if (!pid) { alert("Selecciona un producto primero."); return; }
        confirmFreeBypass(pid);
    });
}

document.addEventListener("DOMContentLoaded", () => {
    wireBuyButtons();
    wireDevButton();
});
