using api_my_web.Models;
using api_my_web.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure DbContext
builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseSqlServer("Server=localhost,1433;Database=TravelDb;User Id=SA;Password=Password1;TrustServerCertificate=True;"));

// Register the DestinationService
builder.Services.AddScoped<DestinationService>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// API endpoints
app.MapGet("/travel/{name}/{language}", async (string name, string language, DestinationService destinationService) =>
{
    var destination = destinationService.GetDestinationByName(name);

    if (destination == null)
    {
        return Results.NotFound(new { Message = "Destination not found." });
    }

    Random random = new Random();
    int minCost = random.Next(500, 5000);

    var response = new
    {
        Name = destination.Name,
        Description = language.ToLower() == "en" ? destination.DescriptionEnglish : destination.DescriptionEnglish,
        Attractions = language.ToLower() == "en" ? destination.AttractionsEnglish : destination.AttractionsEnglish,
        LocalDishes = language.ToLower() == "en" ? destination.LocalDishesEnglish : destination.LocalDishesEnglish,
        MinimumCost = $"${minCost}"
    };

    return Results.Ok(response);
})
.WithName("GetDestination")
.WithOpenApi();

app.MapGet("/cities", async (TravelDbContext db) =>
{
    var cities = await db.Destinations
        .Select(d => d.Name)
        .Distinct()
        .OrderBy(name => name)
        .ToListAsync();

    return Results.Ok(cities);
})
.WithName("GetCities")
.WithOpenApi();

app.MapPost("/destinations", async ([FromBody] Destination newDestination, TravelDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(newDestination.Name))
    {
        return Results.BadRequest(new { Message = "Name is required." });
    }
    if (newDestination.Name.Length > 20)
    {
        return Results.BadRequest(new { Message = "Name cannot be more than 20 characters." });
    }
    if (string.IsNullOrWhiteSpace(newDestination.DescriptionEnglish))
    {
        return Results.BadRequest(new { Message = "Description is required." });
    }
    if (newDestination.AttractionsEnglish == null || !newDestination.AttractionsEnglish.Any())
    {
        return Results.BadRequest(new { Message = "Attractions are required." });
    }
    if (newDestination.LocalDishesEnglish == null || !newDestination.LocalDishesEnglish.Any())
    {
        return Results.BadRequest(new { Message = "Local dishes are required." });
    }

    if (await db.Destinations.AnyAsync(d => d.Name.ToLower() == newDestination.Name.ToLower()))
    {
        return Results.BadRequest(new { Message = "Eklemeye çalıştığınız şehir zaten listede mevcut." });
    }

    db.Destinations.Add(newDestination);
    await db.SaveChangesAsync();

    var cities = await db.Destinations
        .Select(d => d.Name)
        .Distinct()
        .OrderBy(name => name)
        .ToListAsync();

    return Results.Ok(new { Message = "City added successfully.", Cities = cities });
})
.WithName("AddDestination")
.WithOpenApi();

app.Run();
