using Microsoft.EntityFrameworkCore;
using api_my_web.Models;

namespace api_my_web.Data
{
    public class TravelDbContext : DbContext
    {
        public TravelDbContext(DbContextOptions<TravelDbContext> options)
            : base(options)
        {
        }

        public DbSet<Destination> Destinations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Destination>()
                .HasKey(d => d.Id); // Birincil anahtar tanımlaması
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost,1433;Database=TravelDb;User Id=SA;Password=Password1;TrustServerCertificate=True;");
            }
        }
    }
}
