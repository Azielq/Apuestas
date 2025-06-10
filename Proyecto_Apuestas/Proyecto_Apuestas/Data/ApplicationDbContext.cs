using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Models;

namespace Proyecto_Apuestas.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Bet> Bet { get; set; }

        protected ApplicationDbContext()
        {
        }
    }
}
