// Models/Entities.cs
namespace ScrapApi.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Address { get; set; }
        public double? LastLat { get; set; }
        public double? LastLng { get; set; }
        public List<PickupRequest> PickupRequests { get; set; } = new();
    }

    public class CollectorCompany
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ContactPhone { get; set; } = "";
        public string? Address { get; set; }
        public List<Collector> Collectors { get; set; } = new();
    }

    public class Collector
    {
        public int Id { get; set; }

        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";

        public int CompanyId { get; set; }
        public CollectorCompany? Company { get; set; }

        // Navigation đến các pickup mà collector này đã nhận
        public List<PickupRequest> AcceptedRequests { get; set; } = new();

        // ---- vị trí realtime mới thêm ----
        public double? CurrentLat { get; set; }
        public double? CurrentLng { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }

    public class ScrapListing
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public double PricePerKg { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum PickupStatus
    {
        Pending = 0,
        Accepted = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public class PickupRequest
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public string ScrapType { get; set; } = "";
        public double QuantityKg { get; set; }

        public DateTime PickupTime { get; set; }

        public double Lat { get; set; }
        public double Lng { get; set; }

        public string? Note { get; set; }

        public PickupStatus Status { get; set; } = PickupStatus.Pending;

        public int? AcceptedByCollectorId { get; set; }
        public Collector? AcceptedByCollector { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
