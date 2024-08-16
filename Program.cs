using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// CORS'yi yapılandır
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

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS'i uygulama boru hattına ekleyin
app.UseCors("AllowAll");

// API endpoints

// 1. Travel endpoint
app.MapGet("/travel/{name}/{language}", (string name, string language) =>
{
    var destination = DataStore.Destinations
        .FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    if (destination == null)
    {
        return Results.NotFound(new { Message = "Destination not found." });
    }

    // Rastgele bir ücret oluşturma
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

// 2. Cities endpoint
app.MapGet("/cities", () =>
{
    var cities = DataStore.Destinations
        .Select(d => d.Name)
        .Distinct()
        .OrderBy(name => name) // Şehirleri alfabetik sıraya göre sırala
        .ToList();

    return Results.Ok(cities);
})
.WithName("GetCities")
.WithOpenApi();

// 3. Add Destination endpoint
app.MapPost("/destinations", ([FromBody] Destination newDestination) =>
{
    // Validation
    // Gerekli doğrulamaları yapın
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

        // Şehir zaten mevcut mu kontrolü
    if (DataStore.Destinations.Any(d => d.Name.ToLower() == newDestination.Name.ToLower()))
    {
        return Results.BadRequest(new { Message = "Eklemeye çalıştığınız şehir zaten listede mevcut." });
    }

    // Yeni şehri listeye ekle
    DataStore.Destinations.Add(newDestination);

    // Güncellenmiş şehir listesini döndür
    var cities = DataStore.Destinations
        .Select(d => d.Name)
        .Distinct()
        .OrderBy(name => name) // Şehirleri alfabetik sıraya göre sırala
        .ToList();

    return Results.Ok(new { Message = "City added successfully.", Cities = cities });
})
.WithName("AddDestination")
.WithOpenApi();

app.Run();
