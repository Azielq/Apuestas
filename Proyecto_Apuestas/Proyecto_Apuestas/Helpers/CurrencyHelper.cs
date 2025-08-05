using System.Globalization;

namespace Proyecto_Apuestas.Helpers
{
    public static class CurrencyHelper
    {
        private static readonly CultureInfo CostaRicaCulture = CultureInfo.GetCultureInfo("es-CR");

        /// <summary>
        /// Formatea un decimal como moneda de Costa Rica
        /// </summary>
        public static string ToCurrency(this decimal amount)
        {
            return amount.ToString("C", CostaRicaCulture);
        }

        /// <summary>
        /// Formatea un decimal como moneda sin símbolo
        /// </summary>
        public static string ToCurrencyWithoutSymbol(this decimal amount)
        {
            return amount.ToString("N2", CostaRicaCulture);
        }

        /// <summary>
        /// Formatea para mostrar cambio positivo/negativo
        /// </summary>
        public static string ToChangeFormat(this decimal amount)
        {
            var formatted = amount.ToCurrency();
            if (amount > 0)
                return $"+{formatted}";
            return formatted;
        }

        /// <summary>
        /// Convierte de colones a dólares
        /// </summary>
        public static decimal ToUSD(this decimal colonAmount, decimal exchangeRate = 530m)
        {
            return Math.Round(colonAmount / exchangeRate, 2);
        }

        /// <summary>
        /// Convierte de dólares a colones
        /// </summary>
        public static decimal ToColones(this decimal usdAmount, decimal exchangeRate = 530m)
        {
            return Math.Round(usdAmount * exchangeRate, 2);
        }

        /// <summary>
        /// Formatea para mostrar en apuestas (2 decimales)
        /// </summary>
        public static string ToBetFormat(this decimal amount)
        {
            return amount.ToString("N2");
        }

        /// <summary>
        /// Redondea al múltiplo más cercano (útil para apuestas)
        /// </summary>
        public static decimal RoundToNearest(this decimal amount, decimal nearest)
        {
            return Math.Round(amount / nearest) * nearest;
        }

        /// <summary>
        /// Calcula el porcentaje
        /// </summary>
        public static string ToPercentage(this decimal value, int decimals = 2)
        {
            return $"{value.ToString($"N{decimals}")}%";
        }

        /// <summary>
        /// Calcula el porcentaje entre dos valores
        /// </summary>
        public static decimal CalculatePercentage(decimal part, decimal total)
        {
            if (total == 0) return 0;
            return Math.Round((part / total) * 100, 2);
        }

        /// <summary>
        /// Obtiene clase CSS para colorear montos
        /// </summary>
        public static string GetAmountColorClass(this decimal amount)
        {
            if (amount > 0) return "text-success";
            if (amount < 0) return "text-danger";
            return "text-muted";
        }

        /// <summary>
        /// Formatea grandes cantidades (1K, 1M, etc.)
        /// </summary>
        public static string ToShortFormat(this decimal amount)
        {
            if (amount >= 1000000)
                return $"{amount / 1000000:0.#}M";
            if (amount >= 1000)
                return $"{amount / 1000:0.#}K";
            return amount.ToString("N0");
        }
    }
}