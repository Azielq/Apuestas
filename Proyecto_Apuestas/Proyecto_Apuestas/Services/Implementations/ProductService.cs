using Proyecto_Apuestas.Models;

namespace Proyecto_Apuestas.Services
{
    public interface IProductService
    {
        List<ChipProduct> GetAvailableProducts();
        ChipProduct? GetById(int id);
    }

    public class ProductService : IProductService
    {
        private static readonly List<ChipProduct> _products = new()
        {
            new ChipProduct { Id = 1, Name = "Paquete Básico", PriceInCents = 500, Chips = 500, Description = "Perfecto para comenzar", IsActive = true, StripePriceId = "prod_SnrZvPZTvOk8Yf"},
            new ChipProduct { Id = 1, Name = "Paquete Medio", PriceInCents = 2000, Chips = 2200, Description = "Perfecto para comenzar", IsActive = true, StripePriceId = "prod_SnrYmVpt7CFhp9"},
            new ChipProduct { Id = 2, Name = "Paquete Pro", PriceInCents = 5000, Chips = 5250, Description = "Ahorro del 10%", IsActive = true, StripePriceId = "prod_SnrdGOwq1jQBiz" },
            new ChipProduct { Id = 3, Name = "Mega Pack", PriceInCents = 100000, Chips = 11000, Description = "Ahorro del 20%", IsActive = true, StripePriceId = "prod_Snri3oa2U1lQ3f" }
        };

        public List<ChipProduct> GetAvailableProducts() =>
            _products.Where(i => i.IsActive).ToList();

        public ChipProduct? GetById(int id) =>
            _products.FirstOrDefault(i => i.Id == id && i.IsActive);
    }
}
