using api_my_web.Models;
using api_my_web.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// SQL Server için DbContext yapılandırması
builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseSqlServer("Server=localhost,1433;Database=TravelDb;User Id=SA;Password=Password1;TrustServerCertificate=True;"));

// Redis Bağlantısı Yapılandırması
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

// Repository ve Servis Katmanı Bağımlılıkları
builder.Services.AddScoped<IDestinationRepository, DestinationRepository>();
builder.Services.AddScoped<DestinationService>();

// FluentValidation yapılandırması
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<DestinationDTOValidator>();

// CORS yapılandırması: Tüm kökenlere, yöntemlere ve başlıklara izin ver
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

// Geliştirme ortamında Swagger UI'yi etkinleştir
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Cache veya veritabanından veri alma yardımcı metodu
async Task<T?> GetFromCacheOrDb<T>(string cacheKey, Func<Task<T?>> dbQuery, IConnectionMultiplexer redis) where T : class
{
    var redisDb = redis.GetDatabase();
    var cachedData = await redisDb.StringGetAsync(cacheKey.ToLower()); // destination: prefix'i kaldırıldı

    if (!cachedData.IsNullOrEmpty)
    {
        return JsonSerializer.Deserialize<T>(cachedData);
    }

    var dbData = await dbQuery();
    if (dbData != null)
    {
        await redisDb.StringSetAsync(cacheKey.ToLower(), JsonSerializer.Serialize(dbData)); // destination: prefix'i kaldırıldı
    }
    return dbData;
}


// API Endpoint'i: İsim ve dil ile seyahat detaylarını getir
app.MapGet("/travel/{name}/{language}", async (string name, string language, DestinationService destinationService, IConnectionMultiplexer redis) =>
{
    var redisDb = redis.GetDatabase();
    var cacheKey = name.ToLower();
    var cachedData = await redisDb.StringGetAsync(cacheKey);

    DestinationDTO? destinationDto = null;
    bool fromCache = false;

    if (!cachedData.IsNullOrEmpty)
    {
        destinationDto = JsonSerializer.Deserialize<DestinationDTO>(cachedData);
        fromCache = true;
    }
    else
    {
        destinationDto = await destinationService.GetDestinationByNameAsync(name, language);
        if (destinationDto != null)
        {
            await redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(destinationDto));
        }
    }

    if (destinationDto == null)
    {
        return Results.NotFound(new { Message = "Destination not found." });
    }

    Random random = new Random();
    int minCost = random.Next(500, 5000);

    var response = new
    {
        Name = destinationDto.Name,
        Description = destinationDto.DescriptionEnglish,
        Attractions = destinationDto.AttractionsEnglish,
        LocalDishes = destinationDto.LocalDishesEnglish,
        MinimumCost = $"${minCost}",
        DataSource = fromCache ? "Redis" : "Database"
    };

    return Results.Ok(response);
})
.WithName("GetDestination")
.WithOpenApi();



// API Endpoint'i: Şehirlerin listesini getir
app.MapGet("/cities", async (DestinationService destinationService, IConnectionMultiplexer redis) =>
{
    var cacheKey = "cities";
    var cities = await GetFromCacheOrDb<List<string>>(cacheKey, () => destinationService.GetAllCitiesAsync(), redis);

    if (cities == null)
    {
        return Results.NotFound(new { Message = "Cities not found." });
    }

    return Results.Ok(cities);
})
.WithName("GetCities")
.WithOpenApi();

// API Endpoint'i: Yeni bir destinasyon ekle veya güncelle
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

    await destinationService.AddOrUpdateDestinationAsync(newDestination);

    return Results.Ok(new { Message = "City added/updated successfully." });
})
.WithName("AddOrUpdateDestination")
.WithOpenApi();

// API Endpoint'i: Bir destinasyonu sil
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
