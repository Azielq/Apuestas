namespace Proyecto_Apuestas.Helpers
{
    public static class EmailTemplateHelper
    {
        /// <summary>
        /// Genera el layout base para emails
        /// </summary>
        public static string GetBaseTemplate(string content, string title = "Proyecto Apuestas")
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; }}
        .footer {{ background-color: #ecf0f1; padding: 20px; text-align: center; font-size: 12px; }}
        .button {{ background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; 
                   border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .alert {{ padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .alert-warning {{ background-color: #fcf8e3; border: 1px solid #faebcc; color: #8a6d3b; }}
        .alert-info {{ background-color: #d9edf7; border: 1px solid #bce8f1; color: #31708f; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{title}</h1>
        </div>
        <div class='content'>
            {content}
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} Proyecto Apuestas. Todos los derechos reservados.</p>
            <p>Este es un correo automático, por favor no responder.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Genera template para confirmación de apuesta
        /// </summary>
        public static string GetBetConfirmationTemplate(string userName, int betId, decimal amount,
            string eventName, decimal odds)
        {
            var content = $@"
                <h2>¡Hola {userName}!</h2>
                <p>Tu apuesta ha sido registrada exitosamente.</p>
                
                <div class='alert alert-info'>
                    <h3>Detalles de la apuesta:</h3>
                    <ul>
                        <li><strong>ID de Apuesta:</strong> #{betId}</li>
                        <li><strong>Evento:</strong> {eventName}</li>
                        <li><strong>Monto:</strong> {amount:C}</li>
                        <li><strong>Cuotas:</strong> {odds:N2}</li>
                        <li><strong>Ganancia potencial:</strong> {(amount * odds):C}</li>
                    </ul>
                </div>
                
                <p>Puedes seguir el estado de tu apuesta en tu cuenta.</p>
                <center>
                    <a href='https://proyectoapuestas.com/betting/details/{betId}' class='button'>
                        Ver mi apuesta
                    </a>
                </center>
                
                <p><small>Recuerda apostar responsablemente.</small></p>";

            return GetBaseTemplate(content, "Confirmación de Apuesta");
        }

        /// <summary>
        /// Genera template para notificación de ganancia
        /// </summary>
        public static string GetWinningNotificationTemplate(string userName, decimal amount, string eventName)
        {
            var content = $@"
                <h2>🎉 ¡Felicidades {userName}!</h2>
                <p>¡Has ganado tu apuesta!</p>
                
                <div style='background-color: #d4edda; border: 1px solid #c3e6cb; color: #155724; 
                           padding: 20px; border-radius: 5px; margin: 20px 0; text-align: center;'>
                    <h1 style='margin: 0; color: #155724;'>{amount:C}</h1>
                    <p style='margin: 10px 0 0 0;'>Ganancia en: {eventName}</p>
                </div>
                
                <p>El monto ya ha sido acreditado a tu cuenta y está disponible para retirar o 
                   usar en nuevas apuestas.</p>
                
                <center>
                    <a href='https://proyectoapuestas.com/payment/withdraw' class='button' 
                       style='background-color: #28a745;'>
                        Retirar ganancias
                    </a>
                </center>";

            return GetBaseTemplate(content, "¡Ganaste tu apuesta!");
        }

        /// <summary>
        /// Genera template para recuperación de contraseña
        /// </summary>
        public static string GetPasswordResetTemplate(string resetUrl, int expirationHours = 24)
        {
            var content = $@"
                <h2>Restablecimiento de Contraseña</h2>
                <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.</p>
                
                <p>Si realizaste esta solicitud, haz clic en el siguiente botón:</p>
                
                <center>
                    <a href='{resetUrl}' class='button'>
                        Restablecer Contraseña
                    </a>
                </center>
                
                <div class='alert alert-warning'>
                    <p><strong>Importante:</strong></p>
                    <ul>
                        <li>Este enlace expirará en {expirationHours} horas</li>
                        <li>Si no solicitaste este cambio, ignora este correo</li>
                        <li>Tu contraseña actual seguirá funcionando hasta que la cambies</li>
                    </ul>
                </div>
                
                <p><small>Si el botón no funciona, copia y pega este enlace en tu navegador:<br>
                {resetUrl}</small></p>";

            return GetBaseTemplate(content, "Restablecer Contraseña");
        }
    }
}
