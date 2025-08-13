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
            new ChipProduct { Id = 1, Name = "Paquete Básico", PriceInCents = 1000, Chips = 100, Description = "Perfecto para comenzar" },
            new ChipProduct { Id = 2, Name = "Paquete Pro", PriceInCents = 4500, Chips = 500, Description = "Ahorro del 10%" },
            new ChipProduct { Id = 3, Name = "Mega Pack", PriceInCents = 8000, Chips = 1000, Description = "Ahorro del 20%" }
        };

        public List<ChipProduct> GetAvailableProducts() =>
            _products.Where(i => i.IsActive).ToList();

        public ChipProduct? GetById(int id) =>
            _products.FirstOrDefault(i => i.Id == id && i.IsActive);
    }
}
