using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services;
using Proyecto_Apuestas.Services.Implementations;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Controllers
{
    [Authorize]
    public class PaymentController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IStripeService _stripeService;
        private readonly IProductService _productService;



        public PaymentController(
            IPaymentService paymentService,
            IUserService userService,
            INotificationService notificationService,
            IStripeService stripeService,
            IProductService productService,
            ILogger<PaymentController> logger) : base(logger)
        {
            _paymentService = paymentService;
            _userService = userService;
            _notificationService = notificationService;
            _stripeService = stripeService;
            _productService = productService; // ✅ Aquí lo guardás
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

            if (success)
            {
                AddSuccessMessage("Método de pago eliminado");
            }
            else
            {
                AddErrorMessage("No se pudo eliminar el método de pago");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SetDefaultPaymentMethod(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _paymentService.SetDefaultPaymentMethodAsync(userId, id);

            if (success)
            {
                return JsonSuccess(message: "Método de pago predeterminado actualizado");
            }

            return JsonError("No se pudo actualizar el método de pago predeterminado");
        }

        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var userId = _userService.GetCurrentUserId();
            var stats = await _paymentService.GetPaymentStatisticsAsync(userId);

            return View(stats);
        }

        //Seccion Stripe ------------------------------------------------------------------

        //PruebaStripe
        [HttpGet]
        [AllowAnonymous] 

        public async Task<IActionResult> TestStripe()
        {
            try
            {
                decimal montoDePrueba = 50m;
                var clientSecret = await _stripeService.CreatePaymentIntentAsync(montoDePrueba);

                return Content($"Stripe funciono correctamente. ClientSecret generado: {clientSecret}");
            }
            catch (Exception ex)
            {
                return Content($"Error al probar Stripe: {ex.Message}");
            }
        }

        //Stripe Payment Intent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStripePaymentIntent([FromBody] decimal amount)
        {
            if (amount <= 0)
                return JsonError("El monto debe ser mayor a 0");

            try
            {
                var clientSecret = await _stripeService.CreatePaymentIntentAsync(amount);
                return JsonSuccess(new { clientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el PaymentIntent de Stripe");
                return JsonError("No se pudo crear el pago con Stripe");
            }
        }

        public IActionResult Products()
        {
            var productos = _productService.GetAvailableProducts();
            return View(productos);
        }




        // Métodos privados
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