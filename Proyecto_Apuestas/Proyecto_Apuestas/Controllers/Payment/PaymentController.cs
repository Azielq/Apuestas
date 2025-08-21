using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Proyecto_Apuestas.Services.Implementations;
using Proyecto_Apuestas.Models.Payment;
using Proyecto_Apuestas.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Controllers.Payment
{
    [Authorize]
    public class PaymentController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IStripeService _stripeService;
        private readonly IProductService _productService;
        private readonly IConfiguration _configuration;

        // bypass / persistencia
        private readonly IWebHostEnvironment _env;
        private readonly apuestasDbContext _context;

        public PaymentController(
           IPaymentService paymentService,
           IUserService userService,
           INotificationService notificationService,
           IStripeService stripeService,
           IProductService productService,
           IConfiguration configuration,
           IWebHostEnvironment env,
           apuestasDbContext context,
           ILogger<PaymentController> logger) : base(logger)
        {
            _paymentService = paymentService;
            _userService = userService;
            _notificationService = notificationService;
            _stripeService = stripeService;
            _productService = productService;
            _configuration = configuration;
            _env = env;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userService.GetCurrentUserId();
            var paymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);
            var user = await _userService.GetCurrentUserAsync();

            ViewBag.Balance = user?.CreditBalance ?? 0;
            return View(paymentMethods);
        }

        [HttpGet]
        public async Task<IActionResult> Deposit()
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetCurrentUserAsync();
            var paymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);

            var model = new DepositViewModel
            {
                AvailableMethods = paymentMethods,
                CurrentBalance = user?.CreditBalance ?? 0
            };

            if (!paymentMethods.Any())
            {
                AddErrorMessage("Debes agregar un método de pago antes de hacer un depósito");
                return RedirectToAction(nameof(AddPaymentMethod));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(DepositViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDepositViewData(model);
                return View(model);
            }

            var result = await _paymentService.ProcessDepositAsync(model);

            if (result.Success)
            {
                AddSuccessMessage($"Depósito de {model.Amount:C} procesado exitosamente");
                return RedirectToAction(nameof(TransactionDetails), new { id = result.TransactionId });
            }

            AddModelErrors(result.ErrorMessage ?? "Error al procesar el depósito");
            await LoadDepositViewData(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Withdraw()
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetCurrentUserAsync();
            var paymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);

            var model = new WithdrawViewModel
            {
                AvailableMethods = paymentMethods,
                CurrentBalance = user?.CreditBalance ?? 0,
                MinimumWithdrawal = await _paymentService.GetMinimumWithdrawalAmount(userId)
            };

            if (!paymentMethods.Any())
            {
                AddErrorMessage("Debes agregar un método de pago antes de hacer un retiro");
                return RedirectToAction(nameof(AddPaymentMethod));
            }

            if (model.CurrentBalance < model.MinimumWithdrawal)
            {
                AddErrorMessage($"Saldo insuficiente. El monto mínimo de retiro es {model.MinimumWithdrawal:C}");
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(WithdrawViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadWithdrawViewData(model);
                return View(model);
            }

            var result = await _paymentService.ProcessWithdrawalAsync(model);

            if (result.Success)
            {
                AddSuccessMessage($"Retiro de {model.Amount:C} procesado. Llegará a tu cuenta en 1-3 días hábiles.");
                return RedirectToAction(nameof(TransactionDetails), new { id = result.TransactionId });
            }

            AddModelErrors(result.ErrorMessage ?? "Error al procesar el retiro");
            await LoadWithdrawViewData(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> History(TransactionFilter? filter = null)
        {
            var userId = _userService.GetCurrentUserId();
            filter ??= new TransactionFilter();

            var history = await _paymentService.GetTransactionHistoryAsync(userId, filter);

            ViewBag.Filter = filter;
            return View(history);
        }

        [HttpGet]
        public async Task<IActionResult> TransactionDetails(int id)
        {
            var transaction = await _paymentService.GetTransactionAsync(id);
            if (transaction == null || transaction.UserId != _userService.GetCurrentUserId())
            {
                return NotFound();
            }

            return View(transaction);
        }

        [HttpGet]
        public IActionResult AddPaymentMethod()
        {
            ViewBag.Providers = new List<string> { "VISA", "MasterCard", "PayPal", "Skrill", "Transferencia" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPaymentMethod(AddPaymentMethodViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Providers = new List<string> { "VISA", "MasterCard", "PayPal", "Skrill", "Transferencia" };
                return View(model);
            }

            var userId = _userService.GetCurrentUserId();
            var success = await _paymentService.AddPaymentMethodAsync(userId, model);

            if (success)
            {
                AddSuccessMessage("Método de pago agregado exitosamente");
                return RedirectToAction(nameof(Index));
            }

            AddModelErrors("Error al agregar el método de pago. Verifica los datos ingresados.");
            ViewBag.Providers = new List<string> { "VISA", "MasterCard", "PayPal", "Skrill", "Transferencia" };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePaymentMethod(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _paymentService.RemovePaymentMethodAsync(userId, id);

            if (success) AddSuccessMessage("Método de pago eliminado");
            else AddErrorMessage("No se pudo eliminar el método de pago");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SetDefaultPaymentMethod(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _paymentService.SetDefaultPaymentMethodAsync(userId, id);

            if (success) return JsonSuccess(message: "Método de pago predeterminado actualizado");
            return JsonError("No se pudo actualizar el método de pago predeterminado");
        }

        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var userId = _userService.GetCurrentUserId();
            var stats = await _paymentService.GetPaymentStatisticsAsync(userId);
            return View(stats);
        }

        // ========================== Stripe / Productos ===============================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Products()
        {
            var productos = _productService.GetAvailableProducts();
            ViewBag.StripePublicKey = _configuration["Payment:Stripe:PublicKey"];

            // Flags bypass (solo Dev o si habilitas en config y/o rol Admin)
            var enableBypass = _configuration.GetValue<bool>("Payment:EnableBypass");
            var userIsAdmin = User?.IsInRole("Admin") ?? false;
            ViewBag.BypassEnabled = (_env.IsDevelopment() || userIsAdmin) && enableBypass;
            ViewBag.BypassCode = _configuration["Payment:BypassCode"] ?? string.Empty;

            return View(productos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("payment/create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] ProductPaymentRequest request)
        {
            var userId = _userService.GetCurrentUserId();
            if (userId <= 0) return Unauthorized();

            var product = _productService.GetById(request.ProductId);
            if (product == null) return JsonError("Producto no encontrado");

            try
            {
                var origin = $"{Request.Scheme}://{Request.Host}";
                var userIdStr = userId.ToString();
                string clientSecret;

                if (!string.IsNullOrWhiteSpace(product.StripePriceId) && product.StripePriceId.StartsWith("price_"))
                {
                    clientSecret = await _stripeService.CreateCheckoutSessionAsync(
                        product.StripePriceId, origin, userIdStr, product.Id.ToString());
                }
                else
                {
                    clientSecret = await _stripeService.CreateCheckoutSessionInlinePriceAsync(
                        unitAmount: product.PriceInCents, // céntimos (CRC)
                        currency: "crc",
                        productName: product.Name,
                        originBaseUrl: origin,
                        userId: userIdStr,
                        packageId: product.Id.ToString());
                }

                return JsonSuccess(new { clientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear CheckoutSession Embedded");
                return JsonError("No se pudo crear la sesión de Stripe");
            }
        }

        // =============================== BYPASS DEV / ADMIN =================================

        /// <summary>
        /// Confirma una compra "gratis" para acelerar pruebas.
        /// Requiere: ambiente Development O (EnableBypass=true y rol Admin).
        /// Si config trae Payment:BypassCode, debe enviarse header X-Bypass-Code.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("payment/dev/confirm")]
        public async Task<IActionResult> DevConfirm([FromBody] ProductPaymentRequest request,
            [FromHeader(Name = "X-Bypass-Code")] string? code)
        {
            var enableBypass = _configuration.GetValue<bool>("Payment:EnableBypass");
            var cfgCode = _configuration["Payment:BypassCode"];
            var userIsAdmin = User?.IsInRole("Admin") ?? false;

            if (!(_env.IsDevelopment() || (enableBypass && userIsAdmin)))
                return NotFound(); // oculta el endpoint si no cumple reglas de acceso

            if (!string.IsNullOrEmpty(cfgCode) && !string.Equals(cfgCode, code))
                return Forbid();

            var product = _productService.GetById(request.ProductId);
            if (product == null) return JsonError("Producto no encontrado");

            var userId = _userService.GetCurrentUserId();
            if (userId <= 0) return Unauthorized();

            // => Actualización robusta del saldo (evita cross-DbContext)
            await _context.UserAccounts
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(u => u.CreditBalance, u => u.CreditBalance + product.Chips));

            // Transacción para trazabilidad: monto = chips (CRC 1:1)
            await _paymentService.CreateTransactionAsync(new PaymentTransactionRequest
            {
                UserId = userId,
                Amount = product.Chips,                    // CRC
                TransactionType = "DEPOSIT",
                Description = $"DEV_BYPASS: {product.Name} (+{product.Chips} chips)"
            });

            // Devuelve nuevo balance
            var newBalance = await _context.UserAccounts
                .Where(u => u.UserId == userId)
                .Select(u => u.CreditBalance)
                .FirstAsync();

            return JsonSuccess(new
            {
                message = $"Se acreditaron {product.Chips} chips (DEV BYPASS).",
                data = new { newBalance, product = product.Name }
            });
        }

        // ============================== Métodos privados =====================================

        private async Task LoadDepositViewData(DepositViewModel model)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetCurrentUserAsync();
            var paymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);

            model.AvailableMethods = paymentMethods;
            model.CurrentBalance = user?.CreditBalance ?? 0;
        }

        private async Task LoadWithdrawViewData(WithdrawViewModel model)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetCurrentUserAsync();
            var paymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);

            model.AvailableMethods = paymentMethods;
            model.CurrentBalance = user?.CreditBalance ?? 0;
            model.MinimumWithdrawal = await _paymentService.GetMinimumWithdrawalAmount(userId);
        }
    }
}
