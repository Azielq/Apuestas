using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;

namespace Proyecto_Apuestas.Helpers
{
    public static class SecurityHelper
    {
        /// <summary>
        /// Genera un hash seguro para contraseñas
        /// </summary>
        public static string HashPassword(string password, byte[] salt = null)
        {
            if (salt == null)
            {
                salt = new byte[128 / 8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        /// <summary>
        /// Verifica una contraseña contra su hash
        /// </summary>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var parts = hashedPassword.Split('.');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = parts[1];

            var newHash = HashPassword(password, salt).Split('.')[1];
            return hash == newHash;
        }

        /// <summary>
        /// Genera un token seguro
        /// </summary>
        public static string GenerateSecureToken(int length = 32)
        {
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        /// <summary>
        /// Encripta texto usando AES
        /// </summary>
        public static string Encrypt(string plainText, string key)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        /// <summary>
        /// Desencripta texto usando AES
        /// </summary>
        public static string Decrypt(string cipherText, string key)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Genera un código QR para autenticación de dos factores
        /// </summary>
        public static string GenerateTwoFactorCode(string userEmail, string appName = "Proyecto Apuestas")
        {
            var secret = GenerateSecureToken(20);
            var uri = $"otpauth://totp/{appName}:{userEmail}?secret={secret}&issuer={appName}";
            return uri;
        }

        /// <summary>
        /// Valida intentos de fuerza bruta
        /// </summary>
        public static bool IsRateLimited(string identifier, int maxAttempts, TimeSpan window, IDictionary<string, List<DateTime>> attemptStore)
        {
            if (!attemptStore.ContainsKey(identifier))
            {
                attemptStore[identifier] = new List<DateTime>();
            }

            var attempts = attemptStore[identifier];
            var cutoff = DateTime.Now.Subtract(window);

            // Limpiar intentos antiguos
            attempts.RemoveAll(a => a < cutoff);

            if (attempts.Count >= maxAttempts)
            {
                return true;
            }

            attempts.Add(DateTime.Now);
            return false;
        }

        /// <summary>
        /// Genera un hash para URLs seguras
        /// </summary>
        public static string GenerateUrlSafeHash(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "")
                    .Substring(0, 22);
            }
        }

        /// <summary>
        /// Valida token CSRF
        /// </summary>
        public static bool ValidateCsrfToken(string token, string sessionToken)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(sessionToken))
                return false;

            return string.Equals(token, sessionToken, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ofusca información sensible
        /// </summary>
        public static string MaskSensitiveData(string data, int visibleStart = 3, int visibleEnd = 3)
        {
            if (string.IsNullOrEmpty(data) || data.Length <= visibleStart + visibleEnd)
                return data;

            var start = data.Substring(0, visibleStart);
            var end = data.Substring(data.Length - visibleEnd);
            var maskLength = data.Length - visibleStart - visibleEnd;

            return $"{start}{new string('*', maskLength)}{end}";
        }
    }
}