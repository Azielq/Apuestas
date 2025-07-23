using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Proyecto_Apuestas.Helpers
{
    public static class StringHelpers
    {
        /// <summary>
        /// Trunca texto y agrega puntos suspensivos
        /// </summary>
        public static string Truncate(this string value, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(value)) return value;

            return value.Length <= maxLength ?
                value :
                value.Substring(0, maxLength) + suffix;
        }

        /// <summary>
        /// Convierte a slug amigable para URLs
        /// </summary>
        public static string ToSlug(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Convertir a minúsculas
            value = value.ToLowerInvariant();

            // Remover acentos
            value = RemoveAccents(value);

            // Reemplazar caracteres no válidos con guiones
            value = Regex.Replace(value, @"[^a-z0-9\s-]", "");

            // Reemplazar espacios múltiples con un solo espacio
            value = Regex.Replace(value, @"\s+", " ").Trim();

            // Reemplazar espacios con guiones
            value = value.Replace(" ", "-");

            // Remover guiones múltiples
            value = Regex.Replace(value, @"-+", "-");

            return value;
        }

        /// <summary>
        /// Remueve acentos de texto
        /// </summary>
        public static string RemoveAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Capitaliza primera letra de cada palabra
        /// </summary>
        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var cultureInfo = CultureInfo.GetCultureInfo("es-CR");
            return cultureInfo.TextInfo.ToTitleCase(value.ToLower());
        }

        /// <summary>
        /// Genera iniciales de un nombre
        /// </summary>
        public static string GetInitials(this string name, int maxInitials = 2)
        {
            if (string.IsNullOrEmpty(name)) return "";

            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = words.Take(maxInitials)
                .Select(w => w[0].ToString().ToUpper());

            return string.Join("", initials);
        }

        /// <summary>
        /// Formatea nombre completo
        /// </summary>
        public static string FormatFullName(string firstName, string primerApellido, string segundoApellido = null)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(firstName))
                parts.Add(firstName.Trim());

            if (!string.IsNullOrWhiteSpace(primerApellido))
                parts.Add(primerApellido.Trim());

            if (!string.IsNullOrWhiteSpace(segundoApellido))
                parts.Add(segundoApellido.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Extrae números de una cadena
        /// </summary>
        public static string ExtractNumbers(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return Regex.Replace(value, @"[^\d]", "");
        }

        /// <summary>
        /// Convierte texto a formato de búsqueda
        /// </summary>
        public static string ToSearchFormat(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Remover acentos y convertir a minúsculas
            value = RemoveAccents(value.ToLowerInvariant());

            // Remover caracteres especiales
            value = Regex.Replace(value, @"[^a-z0-9\s]", "");

            // Remover espacios extras
            value = Regex.Replace(value, @"\s+", " ").Trim();

            return value;
        }

        /// <summary>
        /// Genera un resumen de texto
        /// </summary>
        public static string GenerateSummary(this string text, int wordCount = 20)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= wordCount)
                return text;

            var summary = string.Join(" ", words.Take(wordCount));
            return $"{summary}...";
        }

        /// <summary>
        /// Valida si es un string numérico
        /// </summary>
        public static bool IsNumeric(this string value)
        {
            return !string.IsNullOrEmpty(value) && value.All(char.IsDigit);
        }

        /// <summary>
        /// Convierte lista a string con formato
        /// </summary>
        public static string ToFormattedList<T>(this IEnumerable<T> items, string separator = ", ", string lastSeparator = " y ")
        {
            var list = items?.ToList();
            if (list == null || !list.Any()) return "";

            if (list.Count == 1) return list[0]?.ToString() ?? "";

            if (list.Count == 2)
                return $"{list[0]}{lastSeparator}{list[1]}";

            var allButLast = string.Join(separator, list.Take(list.Count - 1));
            return $"{allButLast}{lastSeparator}{list.Last()}";
        }
    }
}