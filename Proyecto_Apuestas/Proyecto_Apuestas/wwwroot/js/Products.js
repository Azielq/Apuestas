document.addEventListener("DOMContentLoaded", function () {
    const stripe = Stripe(stripePublicKey); 

    // Detectar todos los botones "Comprar"
    document.querySelectorAll(".btn-comprar").forEach(button => {
        button.addEventListener("click", async function () {
            const productId = this.getAttribute("data-product-id");

            try {
                // Llamar al backend para crear una sesión de pago con Stripe
                const response = await fetch("/payment/create-checkout-session", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({ productId: parseInt(productId) })
                });

                const result = await response.json();

                if (!result.success) {
                    alert(result.message || "Error al crear la sesión de pago");
                    return;
                }

                // Cargar Stripe Embedded Checkout
                const checkout = await stripe.initEmbeddedCheckout({
                    fetchClientSecret: () => result.data.clientSecret
                });

                // Montar el checkout en un contenedor (puede estar oculto al inicio)
                checkout.mount("#checkout");
            } catch (error) {
                console.error("Error al iniciar Stripe Checkout:", error);
                alert("Hubo un error al procesar el pago.");
            }
        });
    });
});
