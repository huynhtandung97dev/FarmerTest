using FarmerTest.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmerTest.Models
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Farmer> Farmers => Set<Farmer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Farmer>(e =>
            {
                e.ToTable("Farmer");
                e.HasKey(x => x.FarmerID);

                e.HasIndex(x => x.FarmerCode).IsUnique();

                e.Property(x => x.FarmerCode).HasMaxLength(20).IsRequired();
                e.Property(x => x.FarmerName).HasMaxLength(100).IsRequired();
                e.Property(x => x.FarmerNameEN).HasMaxLength(100);
                e.Property(x => x.Address).HasMaxLength(200);
                e.Property(x => x.Phone1).HasMaxLength(15);
                e.Property(x => x.Phone2).HasMaxLength(15);

                e.Property(x => x.CreatedAt)
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                e.Property(x => x.UpdatedAt)
                    .HasColumnType("datetime2")
                    .IsRequired(false);
            });
        }
    }
}
