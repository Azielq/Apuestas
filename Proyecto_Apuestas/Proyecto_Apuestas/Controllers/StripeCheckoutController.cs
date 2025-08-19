using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.Models;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Alias para no chocar con Stripe.PaymentMethod
using AppPaymentMethod = Proyecto_Apuestas.Models.PaymentMethod;

namespace Proyecto_Apuestas.Controllers
{
    [Authorize]
    public class StripeCheckoutController : BaseController
    {
        private readonly apuestasDbContext _context;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly ILogger<StripeCheckoutController> _logger;

        public StripeCheckoutController(
            apuestasDbContext context,
            IProductService productService,
            IUserService userService,
            ILogger<StripeCheckoutController> logger
        ) : base(logger)
        {
            _context = context;
            _productService = productService;
            _userService = userService;
            _logger = logger;
        }

        // ReturnUrl de Stripe llega aqui con ?session_id=
        [HttpGet]
        public async Task<IActionResult> Success([FromQuery(Name = "session_id")] string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                AddErrorMessage("Sesion de Stripe invalida.");
                return RedirectToAction("Products", "Payment");
            }

            Session session;
            try
            {
                var svc = new SessionService();
                // Pedimos la session expandida para leer line_items y price.id
                var getOpts = new SessionGetOptions
                {
                    Expand = new List<string> { "line_items", "line_items.data.price" }
                };
                session = svc.Get(sessionId, getOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo obtener la Checkout Session {SessionId}", sessionId);
                AddErrorMessage("No se pudo validar el pago.");
                return RedirectToAction("Products", "Payment");
            }

            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                AddErrorMessage("El pago no fue completado.");
                return RedirectToAction("Products", "Payment");
            }

            // Idempotencia por session.Id
            var already = await _context.PaymentMethods
                .AnyAsync(pm => pm.AccountReference == session.Id && pm.ProviderName == "Stripe Checkout");

            if (!already)
            {
                try
                {
                    var currentUserId = _userService.GetCurrentUserId();
                    await ApplySuccessfulCheckoutAsync(session, currentUserId > 0 ? currentUserId : (int?)null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error acreditando pago para Session {SessionId}", session.Id);
                    AddErrorMessage("Ocurrio un error al acreditar el pago.");
                    return RedirectToAction("Products", "Payment");
                }
            }

            AddSuccessMessage("Deposito acreditado exitosamente.");
            return RedirectToAction("Index", "Payment");
        }

        // Acreditacion: resuelve userId, determina producto, registra transaccion y suma chips
        private async Task ApplySuccessfulCheckoutAsync(Session session, int? userIdFromContext = null)
        {
            // 1) userId
            var userId = 0;
            if (session.Metadata?.TryGetValue("userId", out var uidStr) == true)
                int.TryParse(uidStr, out userId);

            if (userId == 0 && int.TryParse(session.ClientReferenceId, out var uidRef))
                userId = uidRef;

            if (userId == 0 && userIdFromContext.HasValue)
                userId = userIdFromContext.Value;

            if (userId <= 0)
                throw new InvalidOperationException("No se pudo inferir el usuario asociado al pago.");

            // 2) producto -> chips
            int productId = 0;
            if (session.Metadata?.TryGetValue("packageId", out var pkg) == true)
                int.TryParse(pkg, out productId);

            ChipProduct product = null;

            if (productId > 0)
                product = _productService.GetById(productId);

            // Si no vino packageId, usamos el priceId del primer line item (gracias a Expand)
            if (product == null)
            {
                var priceId = session.LineItems?.Data?.FirstOrDefault()?.Price?.Id;
                if (!string.IsNullOrWhiteSpace(priceId))
                {
                    product = _productService
                        .GetAvailableProducts()
                        .FirstOrDefault(p => string.Equals(p.StripePriceId, priceId, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (product == null)
                throw new InvalidOperationException("No se pudo determinar el paquete adquirido.");

            var chips = product.Chips;
            var amountPaid = (session.AmountTotal ?? 0) / 100m;

            // 3) marca idempotente por session.Id
            var pm = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.UserId == userId
                    && p.ProviderName == "Stripe Checkout"
                    && p.AccountReference == session.Id);

            if (pm == null)
            {
                pm = new AppPaymentMethod
                {
                    UserId = userId,
                    ProviderName = "Stripe Checkout",
                    AccountReference = session.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.PaymentMethods.Add(pm);
                await _context.SaveChangesAsync();
            }

            // 4) transaccion
            _context.PaymentTransactions.Add(new PaymentTransaction
            {
                UserId = userId,
                PaymentMethodId = pm.PaymentMethodId,
                Amount = amountPaid,
                TransactionType = "DEPOSIT",
                Status = "COMPLETED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // 5) acreditar chips
            var user = await _context.UserAccounts.FirstAsync(u => u.UserId == userId);
            user.CreditBalance += chips;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}
