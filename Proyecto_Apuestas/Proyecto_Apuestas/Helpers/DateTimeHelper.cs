using System.Globalization;

namespace Proyecto_Apuestas.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo CostaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");

        /// <summary>
        /// Convierte UTC a hora de Costa Rica
        /// </summary>
        public static DateTime ToCostaRicaTime(this DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, CostaRicaTimeZone);
        }

        /// <summary>
        /// Convierte hora local a UTC
        /// </summary>
        public static DateTime ToUtc(this DateTime localDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, CostaRicaTimeZone);
        }

        /// <summary>
        /// Obtiene tiempo relativo amigable (hace 5 minutos, hace 2 horas, etc.)
        /// </summary>
        public static string GetRelativeTime(this DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return "hace un momento";

            if (timeSpan.TotalMinutes < 60)
                return $"hace {(int)timeSpan.TotalMinutes} {((int)timeSpan.TotalMinutes == 1 ? "minuto" : "minutos")}";

            if (timeSpan.TotalHours < 24)
                return $"hace {(int)timeSpan.TotalHours} {((int)timeSpan.TotalHours == 1 ? "hora" : "horas")}";

            if (timeSpan.TotalDays < 7)
                return $"hace {(int)timeSpan.TotalDays} {((int)timeSpan.TotalDays == 1 ? "día" : "días")}";

            if (timeSpan.TotalDays < 30)
                return $"hace {(int)(timeSpan.TotalDays / 7)} {((int)(timeSpan.TotalDays / 7) == 1 ? "semana" : "semanas")}";

            if (timeSpan.TotalDays < 365)
                return $"hace {(int)(timeSpan.TotalDays / 30)} {((int)(timeSpan.TotalDays / 30) == 1 ? "mes" : "meses")}";

            return dateTime.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Formatea fecha para mostrar en UI
        /// </summary>
        public static string ToDisplayFormat(this DateTime dateTime)
        {
            return dateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-CR"));
        }

        /// <summary>
        /// Formatea solo la fecha
        /// </summary>
        public static string ToDateOnlyFormat(this DateTime dateTime)
        {
            return dateTime.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-CR"));
        }

        /// <summary>
        /// Formatea solo la hora
        /// </summary>
        public static string ToTimeOnlyFormat(this DateTime dateTime)
        {
            return dateTime.ToString("HH:mm", CultureInfo.GetCultureInfo("es-CR"));
        }

        /// <summary>
        /// Obtiene el nombre del día en español
        /// </summary>
        public static string GetDayNameInSpanish(this DateTime dateTime)
        {
            var culture = CultureInfo.GetCultureInfo("es-CR");
            return culture.DateTimeFormat.GetDayName(dateTime.DayOfWeek);
        }

        /// <summary>
        /// Obtiene el nombre del mes en español
        /// </summary>
        public static string GetMonthNameInSpanish(this DateTime dateTime)
        {
            var culture = CultureInfo.GetCultureInfo("es-CR");
            return culture.DateTimeFormat.GetMonthName(dateTime.Month);
        }

        /// <summary>
        /// Verifica si es fin de semana
        /// </summary>
        public static bool IsWeekend(this DateTime dateTime)
        {
            return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// Obtiene el inicio de la semana (lunes)
        /// </summary>
        public static DateTime StartOfWeek(this DateTime dateTime)
        {
            int diff = (7 + (dateTime.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dateTime.AddDays(-diff).Date;
        }

        /// <summary>
        /// Obtiene el fin de la semana (domingo)
        /// </summary>
        public static DateTime EndOfWeek(this DateTime dateTime)
        {
            return dateTime.StartOfWeek().AddDays(6);
        }

        /// <summary>
        /// Calcula la edad a partir de una fecha de nacimiento
        /// </summary>
        public static int CalculateAge(this DateOnly birthDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age)) age--;
            return age;
        }

        /// <summary>
        /// Verifica si el evento está en curso
        /// </summary>
        public static bool IsEventLive(DateTime eventDate, int durationHours = 3)
        {
            var now = DateTime.Now;
            return eventDate <= now && eventDate.AddHours(durationHours) >= now;
        }

        /// <summary>
        /// Obtiene tiempo restante para el evento
        /// </summary>
        public static string GetTimeUntilEvent(DateTime eventDate)
        {
            var timeSpan = eventDate - DateTime.Now;

            if (timeSpan.TotalSeconds < 0)
                return "En curso";

            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} días";

            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";

            return $"{timeSpan.Minutes}m";
        }
    }
}