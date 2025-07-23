using Microsoft.Extensions.Options;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            ISendGridClient sendGridClient,
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger)
        {
            _sendGridClient = sendGridClient;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                var toAddress = new EmailAddress(to);
                var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, isHtml ? null : body, isHtml ? body : null);

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation("Email sent successfully to {Email}", to);
                    return true;
                }

                _logger.LogWarning("Failed to send email. Status: {Status}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", to);
                return false;
            }
        }

        public async Task<bool> SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, string> replacements)
        {
            var template = GetEmailTemplate(templateName);
            if (string.IsNullOrEmpty(template)) return false;

            foreach (var replacement in replacements)
            {
                template = template.Replace($"{{{{{replacement.Key}}}}}", replacement.Value);
            }

            return await SendEmailAsync(to, GetTemplateSubject(templateName), template);
        }

        public async Task<bool> SendWelcomeEmailAsync(string to, string userName)
        {
            var replacements = new Dictionary<string, string>
            {
                { "UserName", userName },
                { "Date", DateTime.Now.ToString("dd/MM/yyyy") },
                { "SupportEmail", _emailSettings.SupportEmail }
            };

            return await SendTemplatedEmailAsync(to, "Welcome", replacements);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string to, string resetToken)
        {
            var resetUrl = $"{_emailSettings.BaseUrl}/account/reset-password?token={resetToken}";
            var replacements = new Dictionary<string, string>
            {
                { "ResetUrl", resetUrl },
                { "ExpirationHours", "24" }
            };

            return await SendTemplatedEmailAsync(to, "PasswordReset", replacements);
        }

        public async Task<bool> SendBetConfirmationEmailAsync(string to, int betId, decimal amount)
        {
            var replacements = new Dictionary<string, string>
            {
                { "BetId", betId.ToString() },
                { "Amount", amount.ToString("C") },
                { "Date", DateTime.Now.ToString("dd/MM/yyyy HH:mm") }
            };

            return await SendTemplatedEmailAsync(to, "BetConfirmation", replacements);
        }

        public async Task<bool> SendWithdrawalConfirmationEmailAsync(string to, decimal amount, string reference)
        {
            var replacements = new Dictionary<string, string>
            {
                { "Amount", amount.ToString("C") },
                { "Reference", reference },
                { "ProcessingTime", "1-3 días hábiles" }
            };

            return await SendTemplatedEmailAsync(to, "WithdrawalConfirmation", replacements);
        }

        public async Task<bool> SendAccountLockedEmailAsync(string to, DateTime lockedUntil, string reason)
        {
            var replacements = new Dictionary<string, string>
            {
                { "LockedUntil", lockedUntil.ToString("dd/MM/yyyy HH:mm") },
                { "Reason", reason },
                { "SupportEmail", _emailSettings.SupportEmail }
            };

            return await SendTemplatedEmailAsync(to, "AccountLocked", replacements);
        }

        public async Task<bool> SendPromotionalEmailAsync(List<string> recipients, string subject, string promoContent)
        {
            var tasks = recipients.Select(email => SendEmailAsync(email, subject, promoContent)).ToList();
            var results = await Task.WhenAll(tasks);

            var successCount = results.Count(r => r);
            _logger.LogInformation("Promotional email sent to {Success}/{Total} recipients", successCount, recipients.Count);

            return successCount > 0;
        }

        private string GetEmailTemplate(string templateName)
        {
            // En producción, se cargarían estas plantillas desde archivos o base de datos
            return templateName switch
            {
                "Welcome" => @"
                    <h2>¡Bienvenido a Bet506, {{UserName}}!</h2>
                    <p>Tu cuenta ha sido creada exitosamente el {{Date}}.</p>
                    <p>Ya puedes comenzar a disfrutar de nuestros servicios de apuestas deportivas con las mejores cuotas de Costa Rica.</p>
                    <p><strong>Características de tu cuenta:</strong></p>
                    <ul>
                        <li>Apuestas en vivo</li>
                        <li>Cuotas competitivas</li>
                        <li>Retiros rápidos</li>
                        <li>Soporte 24/7</li>
                    </ul>
                    <p>Recuerda apostar responsablemente.</p>
                    <p>Si tienes alguna pregunta, contáctanos en {{SupportEmail}}</p>
                    <hr>
                    <p><small>Bet506 - La mejor plataforma de apuestas deportivas en Costa Rica</small></p>",

                "PasswordReset" => @"
                    <h2>Restablecimiento de Contraseña - Bet506</h2>
                    <p>Hemos recibido una solicitud para restablecer tu contraseña.</p>
                    <p>Haz clic en el siguiente enlace para crear una nueva contraseña:</p>
                    <p><a href='{{ResetUrl}}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Restablecer Contraseña</a></p>
                    <p>Este enlace expirará en {{ExpirationHours}} horas.</p>
                    <p>Si no solicitaste este cambio, ignora este correo.</p>
                    <hr>
                    <p><small>Bet506 - Seguridad y confianza garantizadas</small></p>",

                "BetConfirmation" => @"
                    <h2>Confirmación de Apuesta - Bet506</h2>
                    <p>Tu apuesta ha sido registrada exitosamente.</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 10px 0;'>
                        <p><strong>ID de Apuesta:</strong> #{{BetId}}</p>
                        <p><strong>Monto:</strong> {{Amount}}</p>
                        <p><strong>Fecha:</strong> {{Date}}</p>
                    </div>
                    <p>Puedes seguir el estado de tu apuesta en tu panel de usuario.</p>
                    <p>¡Buena suerte!</p>
                    <hr>
                    <p><small>Bet506 - Donde los ganadores apuestan</small></p>",

                "WithdrawalConfirmation" => @"
                    <h2>Solicitud de Retiro Procesada - Bet506</h2>
                    <p>Tu solicitud de retiro ha sido procesada exitosamente.</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 10px 0;'>
                        <p><strong>Monto:</strong> {{Amount}}</p>
                        <p><strong>Referencia:</strong> {{Reference}}</p>
                        <p><strong>Tiempo de procesamiento:</strong> {{ProcessingTime}}</p>
                    </div>
                    <p>El dinero será transferido a tu cuenta registrada.</p>
                    <hr>
                    <p><small>Bet506 - Retiros rápidos y seguros</small></p>",

                "AccountLocked" => @"
                    <h2>Cuenta Temporalmente Bloqueada - Bet506</h2>
                    <p>Tu cuenta ha sido temporalmente bloqueada por razones de seguridad.</p>
                    <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 10px 0;'>
                        <p><strong>Bloqueada hasta:</strong> {{LockedUntil}}</p>
                        <p><strong>Motivo:</strong> {{Reason}}</p>
                    </div>
                    <p>Para más información o para apelar esta decisión, contáctanos en {{SupportEmail}}</p>
                    <hr>
                    <p><small>Bet506 - Tu seguridad es nuestra prioridad</small></p>",

                _ => string.Empty
            };
        }

        private string GetTemplateSubject(string templateName)
        {
            return templateName switch
            {
                "Welcome" => "¡Bienvenido a Bet506! Tu cuenta está lista",
                "PasswordReset" => "Bet506 - Restablecer tu contraseña",
                "BetConfirmation" => "Bet506 - Confirmación de apuesta",
                "WithdrawalConfirmation" => "Bet506 - Solicitud de retiro procesada",
                "AccountLocked" => "Bet506 - Cuenta temporalmente bloqueada",
                _ => "Notificación de Bet506"
            };
        }
    }
}

