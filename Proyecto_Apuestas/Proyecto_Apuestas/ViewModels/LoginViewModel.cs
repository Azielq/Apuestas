namespace Proyecto_Apuestas.ViewModels
{
    public class LoginViewModel
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        
        // Propiedad calculada para mantener compatibilidad
        public string Email => EmailOrUsername;
        
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
        public DateOnly? BirthDate { get; set; }
        public bool AcceptTerms { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
