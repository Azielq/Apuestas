using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace Proyecto_Apuestas.Helpers
{
    public static class HtmlHelpers
    {
        /// <summary>
        /// Genera clases CSS para estado activo en menú
        /// </summary>
        public static string IsActive(this IHtmlHelper html, string controller, string action = null)
        {
            var routeData = html.ViewContext.RouteData;
            var currentController = routeData.Values["controller"]?.ToString();
            var currentAction = routeData.Values["action"]?.ToString();

            if (string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase))
            {
                if (action == null || string.Equals(currentAction, action, StringComparison.OrdinalIgnoreCase))
                {
                    return "active";
                }
            }

            return "";
        }

        /// <summary>
        /// Genera badge HTML para estados
        /// </summary>
        public static IHtmlContent StatusBadge(this IHtmlHelper html, string status)
        {
            var (cssClass, text) = status.ToUpper() switch
            {
                "P" or "PENDING" => ("badge-warning", "Pendiente"),
                "W" or "WON" => ("badge-success", "Ganada"),
                "L" or "LOST" => ("badge-danger", "Perdida"),
                "C" or "CANCELLED" => ("badge-secondary", "Cancelada"),
                "COMPLETED" => ("badge-success", "Completada"),
                "FAILED" => ("badge-danger", "Fallida"),
                _ => ("badge-secondary", status)
            };

            return new HtmlString($"<span class='badge {cssClass}'>{text}</span>");
        }

        /// <summary>
        /// Genera estrellas de rating
        /// </summary>
        public static IHtmlContent RatingStars(this IHtmlHelper html, decimal rating, int maxStars = 5)
        {
            var fullStars = (int)Math.Floor(rating);
            var hasHalfStar = rating - fullStars >= 0.5m;
            var emptyStars = maxStars - fullStars - (hasHalfStar ? 1 : 0);

            var sb = new StringBuilder("<div class='rating-stars'>");

            for (int i = 0; i < emptyStars; i++)
                sb.Append("<i class='far fa-star text-warning'></i>");

            sb.Append("</div>");

            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Genera indicador de cambio de cuotas
        /// </summary>
        public static IHtmlContent OddsChangeIndicator(this IHtmlHelper html, decimal currentOdds, decimal? previousOdds)
        {
            if (!previousOdds.HasValue || currentOdds == previousOdds.Value)
                return new HtmlString($"<span class='odds-value'>{currentOdds:N2}</span>");

            var change = currentOdds - previousOdds.Value;
            var changeClass = change > 0 ? "odds-up" : "odds-down";
            var arrow = change > 0 ? "↑" : "↓";

            return new HtmlString($@"
                <span class='odds-value {changeClass}'>
                    {currentOdds:N2} 
                    <small>{arrow} {Math.Abs(change):N2}</small>
                </span>");
        }

        /// <summary>
        /// Genera breadcrumb dinámico
        /// </summary>
        public static IHtmlContent Breadcrumb(this IHtmlHelper html, params (string Text, string Action, string Controller)[] items)
        {
            var sb = new StringBuilder("<nav aria-label='breadcrumb'><ol class='breadcrumb'>");

            sb.Append("<li class='breadcrumb-item'><a href='/'>Inicio</a></li>");

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var isLast = i == items.Length - 1;

                if (isLast)
                {
                    sb.Append($"<li class='breadcrumb-item active' aria-current='page'>{item.Text}</li>");
                }
                else
                {
                    var url = html.ViewContext.HttpContext.Request.PathBase +
                             $"/{item.Controller}/{item.Action}";
                    sb.Append($"<li class='breadcrumb-item'><a href='{url}'>{item.Text}</a></li>");
                }
            }

            sb.Append("</ol></nav>");

            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Genera tarjeta de equipo
        /// </summary>
        public static IHtmlContent TeamCard(this IHtmlHelper html, string teamName, string logoUrl, decimal odds)
        {
            var defaultLogo = "/images/default-team-logo.png";
            var actualLogo = string.IsNullOrEmpty(logoUrl) ? defaultLogo : logoUrl;

            return new HtmlString($@"
                <div class='team-card'>
                    <img src='{actualLogo}' alt='{teamName}' class='team-logo'>
                    <h5 class='team-name'>{teamName}</h5>
                    <div class='team-odds'>{odds:N2}</div>
                </div>");
        }

        /// <summary>
        /// Genera alerta de mensaje
        /// </summary>
        public static IHtmlContent Alert(this IHtmlHelper html, string message, string type = "info", bool dismissible = true)
        {
            var dismissButton = dismissible ?
                @"<button type='button' class='close' data-dismiss='alert' aria-label='Close'>
                    <span aria-hidden='true'>&times;</span>
                  </button>" : "";

            return new HtmlString($@"
                <div class='alert alert-{type} {(dismissible ? "alert-dismissible fade show" : "")}' role='alert'>
                    {message}
                    {dismissButton}
                </div>");
        }

        /// <summary>
        /// Genera indicador de evento en vivo
        /// </summary>
        public static IHtmlContent LiveIndicator(this IHtmlHelper html, bool isLive)
        {
            if (!isLive) return HtmlString.Empty;

            return new HtmlString(@"
                <span class='live-indicator'>
                    <span class='live-dot'></span>
                    EN VIVO
                </span>");
        }

        /// <summary>
        /// Genera paginación Bootstrap
        /// </summary>
        public static IHtmlContent Pagination(this IHtmlHelper html, int currentPage, int totalPages,
            string action, string controller, object routeValues = null)
        {
            if (totalPages <= 1) return HtmlString.Empty;

            var sb = new StringBuilder("<nav><ul class='pagination'>");

            // Botón anterior
            var prevDisabled = currentPage <= 1 ? "disabled" : "";
            sb.Append($@"
                <li class='page-item {prevDisabled}'>
                    <a class='page-link' href='/{controller}/{action}?page={currentPage - 1}' tabindex='-1'>
                        Anterior
                    </a>
                </li>");

            // Páginas
            var startPage = Math.Max(1, currentPage - 2);
            var endPage = Math.Min(totalPages, currentPage + 2);

            if (startPage > 1)
            {
                sb.Append("<li class='page-item'><a class='page-link' href='/{controller}/{action}?page=1'>1</a></li>");
                if (startPage > 2)
                    sb.Append("<li class='page-item disabled'><span class='page-link'>...</span></li>");
            }

            for (int i = startPage; i <= endPage; i++)
            {
                var active = i == currentPage ? "active" : "";
                sb.Append($@"
                    <li class='page-item {active}'>
                        <a class='page-link' href='/{controller}/{action}?page={i}'>{i}</a>
                    </li>");
            }

            if (endPage < totalPages)
            {
                if (endPage < totalPages - 1)
                    sb.Append("<li class='page-item disabled'><span class='page-link'>...</span></li>");
                sb.Append($"<li class='page-item'><a class='page-link' href='/{controller}/{action}?page={totalPages}'>{totalPages}</a></li>");
            }

            // Botón siguiente
            var nextDisabled = currentPage >= totalPages ? "disabled" : "";
            sb.Append($@"
                <li class='page-item {nextDisabled}'>
                    <a class='page-link' href='/{controller}/{action}?page={currentPage + 1}'>
                        Siguiente
                    </a>
                </li>");

            sb.Append("</ul></nav>");

            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Formatea tiempo restante
        /// </summary>
        public static IHtmlContent TimeRemaining(this IHtmlHelper html, DateTime eventDate)
        {
            var timeRemaining = DateTimeHelper.GetTimeUntilEvent(eventDate);
            var cssClass = eventDate <= DateTime.Now ? "text-danger" : "text-info";

            return new HtmlString($"<span class='{cssClass}'>{timeRemaining}</span>");
        }

        /// <summary>
        /// Genera gráfico de barras simple
        /// </summary>
        public static IHtmlContent SimpleBarChart(this IHtmlHelper html, Dictionary<string, decimal> data)
        {
            var maxValue = data.Values.Max();
            var sb = new StringBuilder("<div class='simple-bar-chart'>");

            foreach (var item in data)
            {
                var percentage = maxValue > 0 ? (item.Value / maxValue) * 100 : 0;
                sb.Append($@"
                    <div class='chart-row'>
                        <span class='chart-label'>{item.Key}</span>
                        <div class='chart-bar'>
                            <div class='chart-fill' style='width: {percentage}%'></div>
                            <span class='chart-value'>{item.Value:N0}</span>
                        </div>
                    </div>");
            }

            sb.Append("</div>");
            return new HtmlString(sb.ToString());
        }
    }
}