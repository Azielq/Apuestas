using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Models.Payment;

namespace Proyecto_Apuestas.Services.Implementations
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
            //1 000 Colones
            new ChipProduct { 
                              Id = 1, 
                              Name = "Paquete Básico", 
                              PriceInCents = 1000 * 100, 
                              Chips = 1000, 
                              Description = "Perfecto para comenzar", 
                              IsActive = true, 
                              StripePriceId = "prod_SnrXckySvZEIP4"},

            //5 000 Colones
            new ChipProduct { 
                              Id = 2, 
                              Name = "Paquete Medio", 
                              PriceInCents = 5000 * 100, 
                              Chips = 5000, 
                              Description = "Perfecto para comenzar", 
                              IsActive = true, 
                              StripePriceId = "prod_SnrZvPZTvOk8Yf"},
            
            //10 000 Colones
            new ChipProduct { 
                              Id = 3, 
                              Name = "Paquete Pro", 
                              PriceInCents = 10000 * 100, 
                              Chips = 10000, 
                              Description = "Para gente que se toma mas seriamente la aplicacion", 
                              IsActive = true, 
                              StripePriceId = "prod_Snri3oa2U1lQ3f" },
            
            //50 000 Colones
            new ChipProduct { 
                              Id = 4, 
                              Name = "Mega Pack", 
                              PriceInCents = 50000 * 100, 
                              Chips = 50000, 
                              Description = "Mas dinero para poder hacer mas apuestas", 
                              IsActive = true, 
                              StripePriceId = "prod_SnrZvPZTvOk8Yf" },

            //100 000 Colones
            new ChipProduct {
                              Id = 5,
                              Name = "Max Pack",
                              PriceInCents = 100000 * 100,
                              Chips = 100000,
                              Description = "Mucho mas dinero para poder hacer mas apuestas",
                              IsActive = true,
                              StripePriceId = "prod_SnrYmVpt7CFhp9" }

        };

        public List<ChipProduct> GetAvailableProducts() =>
            _products.Where(i => i.IsActive).ToList();

        public ChipProduct? GetById(int id) =>
            _products.FirstOrDefault(i => i.Id == id && i.IsActive);
    }
}
