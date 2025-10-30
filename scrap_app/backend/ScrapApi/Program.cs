using System.Linq; // LINQ: Select/Where/OrderBy...
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ScrapApi.Data;
using ScrapApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer") ??
        @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ScrapApiDb;
          Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ScrapApi", Version = "v1" });
});

// CORS cho Flutter (tạm cho phép tất cả trong dev)
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "ScrapApi Swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ScrapApi v1");
    });
}

app.UseCors();

// migrate + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await db.Database.MigrateAsync();
    await SeedData.InitAsync(db);
}

/* ----------- API ----------- */

//
// CUSTOMERS
//
app.MapGet("/api/customers", async (AppDb db) =>
    await db.Customers.AsNoTracking().ToListAsync());

app.MapPost("/api/customers", async (AppDb db, Customer c) =>
{
    db.Customers.Add(c);
    await db.SaveChangesAsync();
    return Results.Created($"/api/customers/{c.Id}", c);
});

// cập nhật vị trí khách (lưu last lat/lng để hỗ trợ điều phối sau này)
app.MapPost("/api/customers/{id:int}/location", async (AppDb db, int id, double lat, double lng) =>
{
    var c = await db.Customers.FindAsync(id);
    if (c is null) return Results.NotFound();
    c.LastLat = lat; c.LastLng = lng;
    await db.SaveChangesAsync();
    return Results.Ok(new { c.Id, c.LastLat, c.LastLng });
});

app.MapGet("/api/customers/{id:int}", async (AppDb db, int id) =>
    await db.Customers.FindAsync(id) is { } c ? Results.Ok(c) : Results.NotFound());

app.MapPut("/api/customers/{id:int}", async (AppDb db, int id, Customer input) =>
{
    var c = await db.Customers.FindAsync(id);
    if (c is null) return Results.NotFound();
    c.FullName = input.FullName;
    c.Phone = input.Phone;
    c.Address = input.Address;
    c.LastLat = input.LastLat;
    c.LastLng = input.LastLng;
    await db.SaveChangesAsync();
    return Results.Ok(c);
});

app.MapDelete("/api/customers/{id:int}", async (AppDb db, int id) =>
{
    var c = await db.Customers.FindAsync(id);
    if (c is null) return Results.NotFound();
    db.Customers.Remove(c);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

//
// PICKUPS (yêu cầu thu gom)
//
app.MapGet("/api/pickups", async (AppDb db, int? status, int? collectorId) =>
{
    var q = db.PickupRequests
        .Include(p => p.Customer)
        .Include(p => p.AcceptedByCollector)
        .AsQueryable();

    if (status is not null)
        q = q.Where(p => (int)p.Status == status);
    if (collectorId is not null)
        q = q.Where(p => p.AcceptedByCollectorId == collectorId);

    return await q
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();
});

app.MapPost("/api/pickups", async (AppDb db, PickupRequest req) =>
{
    if (req.QuantityKg <= 0)
        return Results.BadRequest("QuantityKg must be > 0");
    if (string.IsNullOrWhiteSpace(req.ScrapType))
        return Results.BadRequest("ScrapType is required");

    req.Status = PickupStatus.Pending;
    db.PickupRequests.Add(req);
    await db.SaveChangesAsync();

    return Results.Created($"/api/pickups/{req.Id}", req);
});

app.MapPost("/api/pickups/{id:int}/accept", async (AppDb db, int id, int collectorId) =>
{
    var req = await db.PickupRequests.FindAsync(id);
    if (req is null) return Results.NotFound();

    var col = await db.Collectors.FindAsync(collectorId);
    if (col is null) return Results.BadRequest("Collector not found");

    if (req.Status != PickupStatus.Pending)
        return Results.BadRequest("Only pending requests can be accepted");

    req.Status = PickupStatus.Accepted;
    req.AcceptedByCollectorId = collectorId;
    await db.SaveChangesAsync();

    return Results.Ok(req);
});

app.MapPost("/api/pickups/{id:int}/status", async (AppDb db, int id, PickupStatus status) =>
{
    var req = await db.PickupRequests.FindAsync(id);
    if (req is null) return Results.NotFound();

    req.Status = status;
    await db.SaveChangesAsync();

    return Results.Ok(req);
});

//
// COLLECTORS / COMPANIES
// (CRUD Collector/Company nếu bạn có, giả sử đã map chỗ khác;
//  ở đây thêm 2 API mới: cập nhật vị trí collector + lấy việc của collector)
//

// Collector update location (collector app ping mỗi 30-60s)
app.MapPost("/api/collectors/{id:int}/location", async (AppDb db, int id, double lat, double lng) =>
{
    var c = await db.Collectors.FindAsync(id);
    if (c is null) return Results.NotFound();

    c.CurrentLat = lat;
    c.CurrentLng = lng;
    c.LastSeenAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(new { c.Id, c.CurrentLat, c.CurrentLng, c.LastSeenAt });
});

// Pickups của 1 collector (để hiển thị "việc của tôi")
app.MapGet("/api/collectors/{id:int}/pickups", async (AppDb db, int id, int? status) =>
{
    var q = db.PickupRequests
        .Include(p => p.Customer)
        .Where(p => p.AcceptedByCollectorId == id);

    if (status is not null)
        q = q.Where(p => (int)p.Status == status);

    return await q
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();
});

//
// [1.4] Auto-dispatch: gán job Pending cho collector gần nhất
//
app.MapPost("/api/pickups/{id:int}/dispatch-nearest", async (
    AppDb db,
    int id,
    double jobLat,
    double jobLng,
    double? radiusKm,
    int? companyId) =>
{
    var req = await db.PickupRequests.FindAsync(id);
    if (req is null)
        return Results.NotFound("Pickup not found");

    if (req.Status != PickupStatus.Pending)
        return Results.BadRequest("Only Pending can be dispatched");

    // lọc collector theo công ty (nếu có), ngược lại lấy tất cả
    var collectors = companyId is null
        ? await db.Collectors.AsNoTracking().ToListAsync()
        : await db.Collectors
            .Where(c => c.CompanyId == companyId)
            .AsNoTracking()
            .ToListAsync();

    if (collectors.Count == 0)
        return Results.BadRequest("No collectors available");

    // chuẩn bị đọc tọa độ hiện tại của collector
    var currentLatProp = typeof(Collector).GetProperty("CurrentLat");
    var currentLngProp = typeof(Collector).GetProperty("CurrentLng");

    // tính khoảng cách từ job đến từng collector có tọa độ
    var candidates = collectors
        .Select(c =>
        {
            double? la = currentLatProp?.GetValue(c) as double?;
            double? lo = currentLngProp?.GetValue(c) as double?;
            return (Collector: c, Lat: la, Lng: lo);
        })
        .Where(x => x.Lat is not null && x.Lng is not null)
        .Select(x => new
        {
            x.Collector,
            DistKm = GeoUtils.Haversine(jobLat, jobLng, x.Lat!.Value, x.Lng!.Value)
        })
        .OrderBy(x => x.DistKm)
        .ToList();

    var chosen = candidates.FirstOrDefault();

    // không ai có tọa độ -> fallback người đầu
    if (chosen is null)
    {
        var any = collectors.First();
        req.Status = PickupStatus.Accepted;
        req.AcceptedByCollectorId = any.Id;
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            assignedTo = any.Id,
            distanceKm = (double?)null,
            note = "No collector position known; assigned first collector."
        });
    }

    var radius = radiusKm ?? 10.0;
    if (chosen.DistKm > radius)
        return Results.BadRequest(
            $"Nearest collector is {chosen.DistKm:0.00} km away (> {radius:0.##} km)");

    req.Status = PickupStatus.Accepted;
    req.AcceptedByCollectorId = chosen.Collector.Id;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        assignedTo = chosen.Collector.Id,
        distanceKm = chosen.DistKm
    });
});

//
// LISTINGS (nguồn cung phế liệu)
//
app.MapGet("/api/listings", async (AppDb db) =>
    await db.ScrapListings
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync());

app.MapPost("/api/listings", async (AppDb db, ScrapListing s) =>
{
    s.CreatedAt = DateTime.UtcNow;
    db.ScrapListings.Add(s);
    await db.SaveChangesAsync();
    return Results.Created($"/api/listings/{s.Id}", s);
});

// [1.5] Listings search: từ khóa + bán kính
app.MapGet("/api/listings/search", async (
    AppDb db,
    string? q,
    double? lat,
    double? lng,
    double? radiusKm,
    int? top) =>
{
    var query = db.ScrapListings.AsQueryable();

    if (!string.IsNullOrWhiteSpace(q))
        query = query.Where(x =>
            x.Title.Contains(q) || x.Description.Contains(q));

    var max = top is > 0 and <= 200 ? top!.Value : 100;

    // lấy trước 1000 bản ghi mới nhất
    var list = await query
        .OrderByDescending(x => x.CreatedAt)
        .Take(1000)
        .ToListAsync();

    // nếu có tọa độ & bán kính, lọc theo khoảng cách
    if (lat is not null && lng is not null && radiusKm is not null)
    {
        list = list
            .Where(x => x.Lat is not null && x.Lng is not null)
            .Select(x => new
            {
                Item = x,
                DistKm = GeoUtils.Haversine(
                    lat.Value,
                    lng.Value,
                    x.Lat!.Value,
                    x.Lng!.Value)
            })
            .Where(t => t.DistKm <= radiusKm.Value)
            .OrderBy(t => t.DistKm)
            .Take(max)
            .Select(t => t.Item)
            .ToList();
    }
    else
    {
        list = list.Take(max).ToList();
    }

    return Results.Ok(list);
});

app.Run();

//
// Helpers phải đặt SAU top-level statements để tránh CS8803
//
public static class GeoUtils
{
    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // km
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) *
            Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return R * c; // km
    }
}
