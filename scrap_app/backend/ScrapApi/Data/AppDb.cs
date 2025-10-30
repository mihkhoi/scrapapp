using Microsoft.EntityFrameworkCore;
using ScrapApi.Models;

namespace ScrapApi.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CollectorCompany> CollectorCompanies => Set<CollectorCompany>();
    public DbSet<Collector> Collectors => Set<Collector>();
    public DbSet<PickupRequest> PickupRequests => Set<PickupRequest>();
    public DbSet<ScrapListing> ScrapListings => Set<ScrapListing>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>()
         .HasMany(x => x.PickupRequests)
         .WithOne(x => x.Customer!)
         .HasForeignKey(x => x.CustomerId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CollectorCompany>()
         .HasMany(c => c.Collectors)
         .WithOne(x => x.Company!)
         .HasForeignKey(x => x.CompanyId);

        b.Entity<Collector>()
         .HasMany(x => x.AcceptedRequests)
         .WithOne(r => r.AcceptedByCollector!)
         .HasForeignKey(r => r.AcceptedByCollectorId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}

public static class SeedData
{
    public static async Task InitAsync(AppDb db)
    {
        if (!db.CollectorCompanies.Any())
        {
            var co = new CollectorCompany { Name = "GreenCycle Co.", ContactPhone = "0909-000-111", Address = "HCM" };
            db.CollectorCompanies.Add(co);
            db.Collectors.AddRange(
                new Collector { FullName = "Nguyễn Văn A", Phone = "0901-111-222", Company = co },
                new Collector { FullName = "Trần Thị B", Phone = "0902-333-444", Company = co }
            );
        }
        if (!db.Customers.Any())
        {
            db.Customers.AddRange(
                new Customer { FullName = "Khách 1", Phone = "0987-000-001", Address = "Q1" },
                new Customer { FullName = "Khách 2", Phone = "0987-000-002", Address = "Q3" }
            );
        }
        if (!db.ScrapListings.Any())
            db.ScrapListings.Add(new ScrapListing { Title="Nhựa PET 200kg", Description="Bao sạch", PricePerKg=7000, Lat=10.78, Lng=106.68 });

        await db.SaveChangesAsync();
    }
}
