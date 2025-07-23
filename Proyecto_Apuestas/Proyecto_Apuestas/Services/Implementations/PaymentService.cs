using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using AutoMapper;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly apuestasDbContext _context;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentService(
            apuestasDbContext context,
            IUserService userService,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<PaymentService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _userService = userService;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<PaymentResult> ProcessDepositAsync(DepositViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = _userService.GetCurrentUserId();

                // NOTE: Esto nos valida método de pago usando la entidad PaymentMethod
                var paymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync(pm => pm.PaymentMethodId == model.PaymentMethodId);
                if (paymentMethod == null || paymentMethod.UserId != userId)
                {
                    return new PaymentResult { Success = false, ErrorMessage = "Método de pago inválido" };
                }

                // Valida límites
                var maxDeposit = await GetMaximumDepositAmount(userId);
                if (model.Amount > maxDeposit)
                {
                    return new PaymentResult { Success = false, ErrorMessage = $"El monto máximo de depósito es {maxDeposit:C}" };
                }

                // Crea transacción
                var paymentTransaction = new PaymentTransaction
                {
                    UserId = userId,
                    PaymentMethodId = model.PaymentMethodId,
                    Amount = model.Amount,
                    TransactionType = "DEPOSIT",
                    Status = "PENDING",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.PaymentTransactions.Add(paymentTransaction);
                await _context.SaveChangesAsync();

                // Procesa con pasarela de pago (simulado)
                var paymentGatewayResult = await ProcessWithPaymentGateway(paymentTransaction);

                if (paymentGatewayResult.Success)
                {
                    paymentTransaction.Status = "COMPLETED";

                    // Actualiza balance del usuario
                    await _userService.UpdateUserBalanceAsync(userId, model.Amount, "DEPOSIT");

                    // Notifica al usuario
                    await _notificationService.SendNotificationAsync(userId,
                        $"Depósito exitoso de {model.Amount:C}. Nuevo saldo: {model.CurrentBalance + model.Amount:C}");
                }
                else
                {
                    paymentTransaction.Status = "FAILED";
                    await _context.SaveChangesAsync();
                    await transaction.RollbackAsync();

                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = paymentGatewayResult.ErrorMessage
                    };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = paymentTransaction.TransactionId,
                    TransactionReference = GenerateTransactionReference(paymentTransaction.TransactionId)
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing deposit");
                return new PaymentResult { Success = false, ErrorMessage = "Error al procesar el depósito" };
            }
        }

        public async Task<PaymentResult> ProcessWithdrawalAsync(WithdrawViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = _userService.GetCurrentUserId();

                // Valida saldo
                var user = await _userService.GetCurrentUserAsync();
                if (user == null || user.CreditBalance < model.Amount)
                {
                    return new PaymentResult { Success = false, ErrorMessage = "Saldo insuficiente" };
                }

                // Valida método de pago usando la entidad PaymentMethod
                var paymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync(pm => pm.PaymentMethodId == model.PaymentMethodId);
                if (paymentMethod == null || paymentMethod.UserId != userId)
                {
                    return new PaymentResult { Success = false, ErrorMessage = "Método de pago inválido" };
                }

                // Valida monto mínimo
                var minWithdrawal = await GetMinimumWithdrawalAmount(userId);
                if (model.Amount < minWithdrawal)
                {
                    return new PaymentResult { Success = false, ErrorMessage = $"El monto mínimo de retiro es {minWithdrawal:C}" };
                }

                // Verifica apuestas pendientes
                var hasPendingBets = await _context.Bets
                    .AnyAsync(b => b.Users.Any(u => u.UserId == userId) && b.BetStatus == "P");

                if (hasPendingBets)
                {
                    return new PaymentResult { Success = false, ErrorMessage = "No puede retirar con apuestas pendientes" };
                }

                // Crea transacción
                var paymentTransaction = new PaymentTransaction
                {
                    UserId = userId,
                    PaymentMethodId = model.PaymentMethodId,
                    Amount = model.Amount,
                    TransactionType = "WITHDRAWAL",
                    Status = "PENDING",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.PaymentTransactions.Add(paymentTransaction);
                await _context.SaveChangesAsync();

                // Actualiza balance inmediatamente (se revierte si falla)
                await _userService.UpdateUserBalanceAsync(userId, model.Amount, "WITHDRAWAL");

                // Procesa retiro (puede ser asíncrono en producción)
                var withdrawalResult = await ProcessWithdrawalRequest(paymentTransaction);

                if (withdrawalResult.Success)
                {
                    paymentTransaction.Status = "COMPLETED";

                    await _notificationService.SendNotificationAsync(userId,
                        $"Retiro procesado exitosamente por {model.Amount:C}. Llegará a tu cuenta en 1-3 días hábiles.");
                }
                else
                {
                    paymentTransaction.Status = "FAILED";
                    // Revierte el balance
                    await _userService.UpdateUserBalanceAsync(userId, model.Amount, "DEPOSIT");

                    await _context.SaveChangesAsync();
                    await transaction.RollbackAsync();

                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = withdrawalResult.ErrorMessage
                    };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = paymentTransaction.TransactionId,
                    TransactionReference = GenerateTransactionReference(paymentTransaction.TransactionId)
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing withdrawal");
                return new PaymentResult { Success = false, ErrorMessage = "Error al procesar el retiro" };
            }
        }

        public async Task<bool> AddPaymentMethodAsync(int userId, AddPaymentMethodViewModel model)
        {
            try
            {
                // Valida método de pago
                if (!await ValidatePaymentMethodAsync(model.ProviderName, model.AccountReference))
                {
                    return false;
                }

                // Verifica duplicados
                var exists = await _context.PaymentMethods
                    .AnyAsync(pm => pm.UserId == userId &&
                                   pm.ProviderName == model.ProviderName &&
                                   pm.AccountReference == model.AccountReference);

                if (exists) return false;

                var paymentMethod = new PaymentMethod
                {
                    UserId = userId,
                    ProviderName = model.ProviderName,
                    AccountReference = MaskAccountReference(model.AccountReference, model.ProviderName),
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.PaymentMethods.Add(paymentMethod);
                await _context.SaveChangesAsync();

                if (model.SetAsDefault)
                {
                    await SetDefaultPaymentMethodAsync(userId, paymentMethod.PaymentMethodId);
                }

                await _notificationService.SendNotificationAsync(userId,
                    $"Método de pago {model.ProviderName} agregado exitosamente");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment method");
                return false;
            }
        }

        public async Task<bool> RemovePaymentMethodAsync(int userId, int paymentMethodId)
        {
            try
            {
                var paymentMethod = await _context.PaymentMethods
                    .FirstOrDefaultAsync(pm => pm.PaymentMethodId == paymentMethodId && pm.UserId == userId);

                if (paymentMethod == null) return false;

                // Verifica si tiene transacciones pendientes
                var hasPendingTransactions = await _context.PaymentTransactions
                    .AnyAsync(pt => pt.PaymentMethodId == paymentMethodId && pt.Status == "PENDING");

                if (hasPendingTransactions) return false;

                // Desactiva en lugar de eliminar
                paymentMethod.IsActive = false;
                paymentMethod.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing payment method");
                return false;
            }
        }

        public async Task<bool> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId)
        {
            try
            {
                var paymentMethod = await _context.PaymentMethods
                    .FirstOrDefaultAsync(pm => pm.PaymentMethodId == paymentMethodId &&
                                              pm.UserId == userId &&
                                              pm.IsActive == true);

                if (paymentMethod == null) return false;

                // Esta funcionalidad requeriría agregar un campo IsDefault a PaymentMethod
                // Por ahora, retornamos true
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default payment method");
                return false;
            }
        }

        public async Task<List<PaymentMethodViewModel>> GetUserPaymentMethodsAsync(int userId)
        {
            var methods = await _context.PaymentMethods
                .Where(pm => pm.UserId == userId && pm.IsActive == true)
                .OrderByDescending(pm => pm.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<PaymentMethodViewModel>>(methods);
        }

        public async Task<PaymentMethodViewModel?> GetPaymentMethodAsync(int paymentMethodId)
        {
            var method = await _context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.PaymentMethodId == paymentMethodId);

            return method != null ? _mapper.Map<PaymentMethodViewModel>(method) : null;
        }

        public async Task<TransactionHistoryViewModel> GetTransactionHistoryAsync(int userId, TransactionFilter? filter = null)
        {
            var query = _context.PaymentTransactions
                .Include(pt => pt.PaymentMethod)
                .Where(pt => pt.UserId == userId);

            // Aplica filtros
            if (filter != null)
            {
                if (filter.StartDate.HasValue)
                    query = query.Where(pt => pt.CreatedAt >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    query = query.Where(pt => pt.CreatedAt <= filter.EndDate.Value);

                if (!string.IsNullOrEmpty(filter.TransactionType))
                    query = query.Where(pt => pt.TransactionType == filter.TransactionType);

                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(pt => pt.Status == filter.Status);
            }

            var totalTransactions = await query.CountAsync();
            var pageNumber = filter?.PageNumber ?? 1;
            var pageSize = filter?.PageSize ?? 20;

            var transactions = await query
                .OrderByDescending(pt => pt.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var transactionViewModels = transactions.Select(t => new TransactionViewModel
            {
                TransactionId = t.TransactionId,
                Amount = t.Amount,
                TransactionType = t.TransactionType,
                TransactionTypeDisplay = GetTransactionTypeDisplay(t.TransactionType),
                Status = t.Status,
                StatusDisplay = GetTransactionStatusDisplay(t.Status),
                PaymentMethodName = t.PaymentMethod?.ProviderName ?? "N/A",
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                RelatedBetIds = t.Bets.Select(b => b.BetId).ToList()
            }).ToList();

            // Calcula estadísticas
            var allTransactions = await _context.PaymentTransactions
                .Where(pt => pt.UserId == userId && pt.Status == "COMPLETED")
                .ToListAsync();

            var currentUser = await _userService.GetCurrentUserAsync();

            return new TransactionHistoryViewModel
            {
                Transactions = transactionViewModels,
                TotalDeposits = allTransactions.Where(t => t.TransactionType == "DEPOSIT").Sum(t => t.Amount),
                TotalWithdrawals = allTransactions.Where(t => t.TransactionType == "WITHDRAWAL").Sum(t => t.Amount),
                CurrentBalance = currentUser?.CreditBalance ?? 0,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalTransactions / (double)pageSize),
                PageSize = pageSize
            };
        }

        public async Task<PaymentTransaction?> GetTransactionAsync(int transactionId)
        {
            return await _context.PaymentTransactions
                .Include(pt => pt.PaymentMethod)
                .Include(pt => pt.Bets)
                .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId);
        }

        public async Task<bool> CreateTransactionAsync(PaymentTransactionRequest request)
        {
            try
            {
                var transaction = new PaymentTransaction
                {
                    UserId = request.UserId,
                    PaymentMethodId = request.PaymentMethodId ?? GetSystemPaymentMethodId(),
                    Amount = request.Amount,
                    TransactionType = request.TransactionType,
                    Status = "COMPLETED",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Asocia con apuestas si es necesario
                if (request.RelatedBetId.HasValue)
                {
                    var bet = await _context.Bets.FindAsync(request.RelatedBetId.Value);
                    if (bet != null)
                    {
                        bet.PaymentTransactionId = transaction.TransactionId;
                        await _context.SaveChangesAsync();
                    }
                }
                else if (request.RelatedBetIds != null && request.RelatedBetIds.Any())
                {
                    var bets = await _context.Bets
                        .Where(b => request.RelatedBetIds.Contains(b.BetId))
                        .ToListAsync();

                    foreach (var bet in bets)
                    {
                        bet.PaymentTransactionId = transaction.TransactionId;
                    }
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction");
                return false;
            }
        }

        public async Task<bool> UpdateTransactionStatusAsync(int transactionId, string status)
        {
            try
            {
                var transaction = await _context.PaymentTransactions.FindAsync(transactionId);
                if (transaction == null) return false;

                transaction.Status = status;
                transaction.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction status");
                return false;
            }
        }

        public async Task<Dictionary<string, decimal>> GetPaymentStatisticsAsync(int userId)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.UserId == userId && pt.Status == "COMPLETED")
                .ToListAsync();

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var recentTransactions = transactions.Where(t => t.CreatedAt >= thirtyDaysAgo);

            return new Dictionary<string, decimal>
            {
                ["TotalDeposits"] = transactions.Where(t => t.TransactionType == "DEPOSIT").Sum(t => t.Amount),
                ["TotalWithdrawals"] = transactions.Where(t => t.TransactionType == "WITHDRAWAL").Sum(t => t.Amount),
                ["MonthlyDeposits"] = recentTransactions.Where(t => t.TransactionType == "DEPOSIT").Sum(t => t.Amount),
                ["MonthlyWithdrawals"] = recentTransactions.Where(t => t.TransactionType == "WITHDRAWAL").Sum(t => t.Amount),
                ["AverageDeposit"] = transactions.Where(t => t.TransactionType == "DEPOSIT").DefaultIfEmpty().Average(t => t?.Amount ?? 0),
                ["AverageWithdrawal"] = transactions.Where(t => t.TransactionType == "WITHDRAWAL").DefaultIfEmpty().Average(t => t?.Amount ?? 0),
                ["TotalTransactions"] = transactions.Count
            };
        }

        public async Task<bool> ValidatePaymentMethodAsync(string provider, string accountReference)
        {
            // Implementa validación según el proveedor
            return provider switch
            {
                "VISA" or "MasterCard" => ValidateCreditCard(accountReference),
                "PayPal" or "Skrill" => ValidateEmail(accountReference),
                "Transferencia" => ValidateBankAccount(accountReference),
                _ => false
            };
        }

        public async Task<decimal> GetMinimumWithdrawalAmount(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return 50m;

            // Mínimos por rol
            return user.Role.RoleName switch
            {
                "VIP" => 10m,
                "Premium" => 25m,
                _ => 50m
            };
        }

        public async Task<decimal> GetMaximumDepositAmount(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return 1000m;

            // Máximos por rol
            return user.Role.RoleName switch
            {
                "VIP" => 100000m,
                "Premium" => 50000m,
                _ => 10000m
            };
        }

        // Métodos privados auxiliares
        private async Task<PaymentResult> ProcessWithPaymentGateway(PaymentTransaction transaction)
        {
            // Simula procesamiento con pasarela de pago
            // En producción, aquí se integraría con Stripe, PayPal, etc.
            // NOTE: Agregue Stripe se supone que puede simular el proceso de pago
            await Task.Delay(1000);

            // Simulación: 95% de éxito
            var random = new Random();
            if (random.Next(100) < 95)
            {
                return new PaymentResult { Success = true };
            }

            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "Error en la pasarela de pago. Intente nuevamente."
            };
        }

        private async Task<PaymentResult> ProcessWithdrawalRequest(PaymentTransaction transaction)
        {
            // Simula procesamiento de retiro
            await Task.Delay(1000);
            return new PaymentResult { Success = true };
        }

        private string GenerateTransactionReference(int transactionId)
        {
            return $"TXN{DateTime.Now:yyyyMMdd}{transactionId:D6}";
        }

        private string MaskAccountReference(string reference, string provider)
        {
            return provider switch
            {
                "VISA" or "MasterCard" => MaskCreditCard(reference),
                "PayPal" or "Skrill" => MaskEmail(reference),
                _ => reference.Length > 4 ? $"***{reference.Substring(reference.Length - 4)}" : reference
            };
        }

        private string MaskCreditCard(string cardNumber)
        {
            if (cardNumber.Length < 12) return cardNumber;
            return $"{cardNumber.Substring(0, 4)}****{cardNumber.Substring(cardNumber.Length - 4)}";
        }

        private string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return email;

            var name = parts[0];
            if (name.Length <= 2) return email;

            return $"{name.Substring(0, 2)}***@{parts[1]}";
        }

        private bool ValidateCreditCard(string cardNumber)
        {
            // Implementa algoritmo de Luhn
            if (string.IsNullOrWhiteSpace(cardNumber)) return false;

            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");
            if (!cardNumber.All(char.IsDigit)) return false;
            if (cardNumber.Length < 13 || cardNumber.Length > 19) return false;

            // Algoritmo de Luhn simplificado
            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int n = int.Parse(cardNumber[i].ToString());
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        private bool ValidateEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateBankAccount(string account)
        {
            // Validación básica para cuentas bancarias
            return !string.IsNullOrWhiteSpace(account) &&
                   account.Length >= 10 &&
                   account.All(c => char.IsDigit(c) || c == '-');
        }

        private int GetSystemPaymentMethodId()
        {
            // Método de pago del sistema para transacciones internas
            return 1;
        }

        private string GetTransactionTypeDisplay(string type)
        {
            return type switch
            {
                "DEPOSIT" => "Depósito",
                "WITHDRAWAL" => "Retiro",
                "BET" => "Apuesta",
                "PAYOUT" => "Pago de Ganancia",
                "REFUND" => "Reembolso",
                _ => type
            };
        }

        private string GetTransactionStatusDisplay(string status)
        {
            return status switch
            {
                "PENDING" => "Pendiente",
                "COMPLETED" => "Completada",
                "FAILED" => "Fallida",
                "CANCELLED" => "Cancelada",
                _ => status
            };
        }
    }
}