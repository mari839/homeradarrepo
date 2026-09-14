using HomeRadar.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeRadar.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<City> Cities => Set<City>();
        public DbSet<District> Districts => Set<District>();
        public DbSet<SubDistrict> SubDistricts => Set<SubDistrict>();
        public DbSet<Municipality> Municipalities => Set<Municipality>();
        public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();

        // MyHome
        public DbSet<MyHomeCity> MyHomeCities => Set<MyHomeCity>();
        public DbSet<MyHomeDistrict> MyHomeDistricts => Set<MyHomeDistrict>();
        public DbSet<MyHomeUrban> MyHomeUrbans => Set<MyHomeUrban>();
        public DbSet<MyHomeSavedFilter> MyHomeSavedFilters => Set<MyHomeSavedFilter>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<City>(e =>
            {
                e.HasMany(c => c.Districts)
                    .WithOne(d => d.City)
                    .HasForeignKey(d => d.CityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<District>(e =>
            {
                e.HasMany(d => d.SubDistricts)
                    .WithOne(s => s.District)
                    .HasForeignKey(s => s.DistrictId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SavedFilter>(e =>
            {
                e.HasOne(f => f.User)
                    .WithMany(u => u.SavedFilters)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(f => new { f.IsActive, f.LastCheckedAt });
            });

            // MyHome
            builder.Entity<MyHomeCity>(e =>
            {
                e.HasMany(c => c.Districts).WithOne(d => d.City)
                    .HasForeignKey(d => d.CityId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<MyHomeDistrict>(e =>
            {
                e.HasMany(d => d.Urbans).WithOne(u => u.District)
                    .HasForeignKey(u => u.DistrictId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<MyHomeSavedFilter>(e =>
            {
                e.HasOne(f => f.User).WithMany()
                    .HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(f => new { f.IsActive, f.LastCheckedAt });
            });
        }
    }
}
