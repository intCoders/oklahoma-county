using Microsoft.EntityFrameworkCore;
using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Address> Addresses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the Address entity
            modelBuilder.Entity<Address>(entity =>
            {
                entity.ToTable("Addresses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullAddress).IsRequired();
                entity.Property(e => e.Lot).IsRequired(false);
                entity.Property(e => e.Block).IsRequired(false);
                entity.Property(e => e.Addition).IsRequired(false);
                entity.Property(e => e.LegalAddress).IsRequired(false);
            });
        }
    }
} 