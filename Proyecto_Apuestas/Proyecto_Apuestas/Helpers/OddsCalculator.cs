namespace Proyecto_Apuestas.Helpers
{
    public static class OddsCalculator
    {
        /// <summary>
        /// Calcula la ganancia potencial
        /// </summary>
        public static decimal CalculatePotentialPayout(decimal stake, decimal odds)
        {
            return Math.Round(stake * odds, 2);
        }

        /// <summary>
        /// Calcula la ganancia neta (payout - stake)
        /// </summary>
        public static decimal CalculateNetProfit(decimal stake, decimal odds)
        {
            return Math.Round((stake * odds) - stake, 2);
        }

        /// <summary>
        /// Convierte cuotas decimales a americanas
        /// </summary>
        public static string ToAmericanOdds(this decimal decimalOdds)
        {
            if (decimalOdds >= 2.0m)
            {
                var americanOdds = (decimalOdds - 1) * 100;
                return $"+{Math.Round(americanOdds)}";
            }
            else
            {
                var americanOdds = -100 / (decimalOdds - 1);
                return Math.Round(americanOdds).ToString();
            }
        }

        /// <summary>
        /// Convierte cuotas decimales a fraccionarias
        /// </summary>
        public static string ToFractionalOdds(this decimal decimalOdds)
        {
            var numerator = (int)((decimalOdds - 1) * 100);
            var denominator = 100;

            // Simplificar fracción
            var gcd = GreatestCommonDivisor(numerator, denominator);
            numerator /= gcd;
            denominator /= gcd;

            return $"{numerator}/{denominator}";
        }

        /// <summary>
        /// Calcula la probabilidad implícita
        /// </summary>
        public static decimal CalculateImpliedProbability(decimal odds)
        {
            return Math.Round((1 / odds) * 100, 2);
        }

        /// <summary>
        /// Calcula el margen de la casa
        /// </summary>
        public static decimal CalculateBookmakerMargin(params decimal[] odds)
        {
            var totalProbability = odds.Sum(o => 1 / o);
            return Math.Round((totalProbability - 1) * 100, 2);
        }

        /// <summary>
        /// Calcula cuotas justas removiendo el margen
        /// </summary>
        public static decimal[] CalculateFairOdds(params decimal[] odds)
        {
            var totalProbability = odds.Sum(o => 1 / o);
            return odds.Select(o => Math.Round(o * totalProbability, 2)).ToArray();
        }

        /// <summary>
        /// Calcula el valor esperado de una apuesta
        /// </summary>
        public static decimal CalculateExpectedValue(decimal stake, decimal odds, decimal winProbability)
        {
            var payout = stake * odds;
            var expectedWin = payout * (winProbability / 100);
            var expectedLoss = stake * (1 - (winProbability / 100));
            return Math.Round(expectedWin - expectedLoss, 2);
        }

        /// <summary>
        /// Determina si las cuotas ofrecen valor
        /// </summary>
        public static bool HasValue(decimal odds, decimal estimatedProbability)
        {
            var impliedProbability = CalculateImpliedProbability(odds);
            return estimatedProbability > impliedProbability;
        }

        /// <summary>
        /// Calcula el stake óptimo usando el criterio de Kelly
        /// </summary>
        public static decimal CalculateKellyStake(decimal bankroll, decimal odds, decimal winProbability)
        {
            var decimalProbability = winProbability / 100;
            var loseProbability = 1 - decimalProbability;
            var kellyPercentage = (decimalProbability * (odds - 1) - loseProbability) / (odds - 1);

            if (kellyPercentage <= 0) return 0;

            // Aplicar fracción de Kelly (25%) para ser más conservador
            var conservativeKelly = kellyPercentage * 0.25m;
            return Math.Round(bankroll * conservativeKelly, 2);
        }

        /// <summary>
        /// Formatea las cuotas para mostrar
        /// </summary>
        public static string FormatOdds(this decimal odds)
        {
            return odds.ToString("N2");
        }

        /// <summary>
        /// Obtiene clase CSS según el valor de las cuotas
        /// </summary>
        public static string GetOddsColorClass(this decimal odds)
        {
            if (odds < 1.5m) return "odds-low";
            if (odds < 2.5m) return "odds-medium";
            if (odds < 5.0m) return "odds-high";
            return "odds-very-high";
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
    }
}