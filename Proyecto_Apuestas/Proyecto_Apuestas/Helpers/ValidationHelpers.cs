using FluentValidation;
using System.Text.RegularExpressions;

namespace Proyecto_Apuestas.Helpers
{
    public static class ValidationHelpers
    {
        /// <summary>
        /// Valida formato de cédula de Costa Rica
        /// </summary>
        public static bool IsValidCostaRicanId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            // Formato: X-XXXX-XXXX
            var pattern = @"^[1-9]-\d{4}-\d{4}$";
            return Regex.IsMatch(id, pattern);
        }

        /// <summary>
        /// Valida número de teléfono de Costa Rica
        /// </summary>
        public static bool IsValidCostaRicanPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Formato: +506 XXXX-XXXX o XXXX-XXXX
            var pattern = @"^(\+506\s?)?\d{4}-?\d{4}$";
            return Regex.IsMatch(phone, pattern);
        }

        /// <summary>
        /// Valida IBAN de Costa Rica
        /// </summary>
        public static bool IsValidCostaRicanIBAN(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban))
                return false;

            // IBAN Costa Rica: CR + 2 dígitos de control + 18 dígitos
            var pattern = @"^CR\d{20}$";
            return Regex.IsMatch(iban.Replace(" ", ""), pattern);
        }

        /// <summary>
        /// Valida que la contraseña sea segura
        /// </summary>
        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            var hasUpperCase = Regex.IsMatch(password, @"[A-Z]");
            var hasLowerCase = Regex.IsMatch(password, @"[a-z]");
            var hasDigit = Regex.IsMatch(password, @"\d");
            var hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]");

            return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }

        /// <summary>
        /// Valida edad mínima
        /// </summary>
        public static bool IsMinimumAge(DateOnly birthDate, int minimumAge = 18)
        {
            var age = birthDate.CalculateAge();
            return age >= minimumAge;
        }

        /// <summary>
        /// Sanitiza entrada de usuario
        /// </summary>
        public static string SanitizeInput(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remover caracteres peligrosos
            input = Regex.Replace(input, @"<script.*?</script>", "", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"<.*?>", "");
            input = input.Trim();

            return input;
        }

        /// <summary>
        /// Valida formato de email
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

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

        /// <summary>
        /// Extensión para FluentValidation - Validación de cédula
        /// </summary>
        public static IRuleBuilderOptions<T, string> MustBeValidCostaRicanId<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Must(IsValidCostaRicanId)
                .WithMessage("El formato de la cédula no es válido");
        }

        /// <summary>
        /// Extensión para FluentValidation - Validación de teléfono
        /// </summary>
        public static IRuleBuilderOptions<T, string> MustBeValidCostaRicanPhone<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Must(IsValidCostaRicanPhone)
                .WithMessage("El formato del teléfono no es válido");
        }

        /// <summary>
        /// Valida monto de apuesta
        /// </summary>
        public static bool IsValidBetAmount(decimal amount, decimal minimum = 100, decimal maximum = 1000000)
        {
            return amount >= minimum && amount <= maximum && amount % 100 == 0;
        }

        /// <summary>
        /// Genera un código de verificación seguro
        /// </summary>
        public static string GenerateVerificationCode(int length = 6)
        {
            const string chars = "0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Valida código de promoción
        /// </summary>
        public static bool IsValidPromoCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            // Formato: XXXXX-XXXXX (alfanumérico)
            var pattern = @"^[A-Z0-9]{5}-[A-Z0-9]{5}$";
            return Regex.IsMatch(code.ToUpper(), pattern);
        }
    }
}