namespace Proyecto_Apuestas.Models.API
{
    public class ApiStatusModel
    {
        public bool IsOperational { get; set; }
        public int RequestsUsed { get; set; }
        public int RequestsRemaining { get; set; }
        public DateTime LastUpdated { get; set; }
        public string? ErrorMessage { get; set; }
        public double UsagePercentage => RequestsUsed + RequestsRemaining > 0 
            ? (double)RequestsUsed / (RequestsUsed + RequestsRemaining) * 100 
            : 0;

        public static ApiStatusModel CreateOperational(int used, int remaining)
        {
            return new ApiStatusModel
            {
                IsOperational = true,
                RequestsUsed = used,
                RequestsRemaining = remaining,
                LastUpdated = DateTime.Now
            };
        }

        public static ApiStatusModel CreateError(string errorMessage)
        {
            return new ApiStatusModel
            {
                IsOperational = false,
                RequestsUsed = 0,
                RequestsRemaining = 0,
                LastUpdated = DateTime.Now,
                ErrorMessage = errorMessage
            };
        }
    }
}