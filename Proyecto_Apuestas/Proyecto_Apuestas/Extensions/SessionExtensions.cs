using System.Text.Json;

namespace Proyecto_Apuestas.Extensions
{
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonSerializer.Deserialize<T>(value);
        }

        public static void SetInt32(this ISession session, string key, int value)
        {
            session.SetString(key, value.ToString());
        }

        public static int GetInt32(this ISession session, string key, int defaultValue = 0)
        {
            var value = session.GetString(key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        public static void SetDecimal(this ISession session, string key, decimal value)
        {
            session.SetString(key, value.ToString());
        }

        public static decimal GetDecimal(this ISession session, string key, decimal defaultValue = 0)
        {
            var value = session.GetString(key);
            return decimal.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}