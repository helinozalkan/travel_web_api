using api_my_web.Models;
using api_my_web.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

// Top-level statements (üst düzey ifadeler)
var builder = WebApplication.CreateBuilder(args);

// Configure DbContext
builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseSqlServer("Server=localhost,1433;Database=TravelDb;User Id=SA;Password=Password1;TrustServerCertificate=True;"));

// Repository ve Service katmanlarını bağımlılık olarak ekleme
builder.Services.AddScoped<IDestinationRepository, DestinationRepository>();
builder.Services.AddScoped<DestinationService>();

// FluentValidation'ı konfigüre etme
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<DestinationDTOValidator>();

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
    var destination = await destinationService.GetDestinationByNameAsync(name);

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

app.MapGet("/cities", async (DestinationService destinationService) =>
{
    var cities = await destinationService.GetAllCitiesAsync();

    return Results.Ok(cities);
})
.WithName("GetCities")
.WithOpenApi();

app.MapPost("/destinations", async ([FromBody] DestinationDTO newDestinationDto, DestinationService destinationService, IValidator<DestinationDTO> validator) =>
{
    var validationResult = await validator.ValidateAsync(newDestinationDto);

    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new { Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)) });
    }

    var newDestination = new Destination
    {
        Name = newDestinationDto.Name,
        DescriptionEnglish = newDestinationDto.DescriptionEnglish,
        AttractionsEnglish = newDestinationDto.AttractionsEnglish,
        LocalDishesEnglish = newDestinationDto.LocalDishesEnglish
    };

    if (await destinationService.DestinationExistsAsync(newDestination.Name))
    {
        return Results.BadRequest(new { Message = "The city you are trying to add already exists." });
    }

    await destinationService.AddDestinationAsync(newDestination);

    var cities = await destinationService.GetAllCitiesAsync();

    return Results.Ok(new { Message = "City added successfully.", Cities = cities });
})
.WithName("AddDestination")
.WithOpenApi();

app.MapDelete("/destinations/{name}", async (string name, DestinationService destinationService) =>
{
    if (!await destinationService.DestinationExistsAsync(name))
    {
        return Results.NotFound(new { Message = "Destination not found." });
    }

    await destinationService.DeleteDestinationAsync(name);

    return Results.Ok(new { Message = "Destination deleted successfully." });
})
.WithName("DeleteDestination")
.WithOpenApi();

app.Run();
