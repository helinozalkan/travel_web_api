using api_my_web.Models;
using api_my_web.Data;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class DestinationService
{
    private readonly IDestinationRepository _destinationRepository;
    private readonly IConnectionMultiplexer _redis;

    public DestinationService(IDestinationRepository destinationRepository, IConnectionMultiplexer redis)
    {
        _destinationRepository = destinationRepository;
        _redis = redis;
    }

    public async Task<bool> DestinationExistsAsync(string name)
    {
        return await _destinationRepository.DestinationExistsAsync(name);
    }

    public async Task<DestinationDTO?> GetDestinationByNameAsync(string name, string language)
    {
        var db = _redis.GetDatabase();
        var redisData = await db.StringGetAsync(name.ToLower());

        if (!redisData.IsNullOrEmpty)
        {
            var destination = JsonSerializer.Deserialize<Destination>(redisData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (destination != null)
            {
                return ConvertToDTO(destination, language);
            }
        }

        var destinationFromDb = await _destinationRepository.GetDestinationByNameAsync(name);
        if (destinationFromDb != null)
        {
            var destination = new Destination
            {
                Id = destinationFromDb.Id,
                Name = destinationFromDb.Name,
                DescriptionEnglish = destinationFromDb.DescriptionEnglish,
                AttractionsEnglish = destinationFromDb.AttractionsEnglish,
                LocalDishesEnglish = destinationFromDb.LocalDishesEnglish
            };

            await db.StringSetAsync(name.ToLower(), JsonSerializer.Serialize(destination, new JsonSerializerOptions { WriteIndented = true }));

            return ConvertToDTO(destination, language);
        }

        return null;
    }

    private DestinationDTO ConvertToDTO(Destination destination, string language)
    {
        return new DestinationDTO
        {
            Name = destination.Name,
            DescriptionEnglish = language.ToLower() == "en" ? destination.DescriptionEnglish : "Description not available",
            AttractionsEnglish = language.ToLower() == "en" ? destination.AttractionsEnglish : new List<string>(),
            LocalDishesEnglish = language.ToLower() == "en" ? destination.LocalDishesEnglish : new List<string>()
        };
    }

    public async Task<List<string>?> GetAllCitiesAsync()
    {
        var db = _redis.GetDatabase();
        var redisData = await db.StringGetAsync("cities");

        if (!redisData.IsNullOrEmpty)
        {
            var cities = JsonSerializer.Deserialize<List<string>>(redisData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cities != null)
            {
                return cities;
            }
        }

        var citiesFromDb = await _destinationRepository.GetAllCitiesAsync();
        if (citiesFromDb != null)
        {
            await db.StringSetAsync("cities", JsonSerializer.Serialize(citiesFromDb, new JsonSerializerOptions { WriteIndented = true }));
        }

        return citiesFromDb;
    }

    public async Task AddOrUpdateDestinationAsync(Destination destination)
{
    bool exists = await DestinationExistsAsync(destination.Name);

    if (exists)
    {
        await _destinationRepository.UpdateDestinationAsync(destination);
    }
    else
    {
        await _destinationRepository.AddDestinationAsync(destination);
    }

    var db = _redis.GetDatabase();
    var citiesKey = "cities";
    var cities = await db.StringGetAsync(citiesKey);

    List<string> cityList;
    if (!cities.IsNullOrEmpty)
    {
        cityList = JsonSerializer.Deserialize<List<string>>(cities, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();
    }
    else
    {
        cityList = new List<string>();
    }

    if (!cityList.Contains(destination.Name))
    {
        cityList.Add(destination.Name);
        await db.StringSetAsync(citiesKey, JsonSerializer.Serialize(cityList, new JsonSerializerOptions { WriteIndented = true }));
    }

    await db.StringSetAsync(destination.Name.ToLower(), JsonSerializer.Serialize(destination, new JsonSerializerOptions { WriteIndented = true })); // destination: prefix'i kaldırıldı
}


    public async Task DeleteDestinationAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("City name cannot be null or empty", nameof(name));
        }

        var redisDb = _redis.GetDatabase();
        
        // 1. Veritabanından şehri sil
        await RemoveDestinationFromDbAsync(name);

        // 2. Redis cache'ten şehirle ilgili verileri sil
        await redisDb.KeyDeleteAsync(name.ToLower());

        // 3. Redis cache'teki şehirler listesini güncelle
        await UpdateRedisCacheAsync();

        // 4. Loglama
        Console.WriteLine($"Deleted city '{name}' from database and Redis cache.");
    }

    private async Task RemoveDestinationFromDbAsync(string name)
    {
        var destination = await _destinationRepository.GetDestinationByNameAsync(name);
        if (destination != null)
        {
            await _destinationRepository.RemoveDestinationAsync(destination);
        }
    }

    private async Task UpdateRedisCacheAsync()
    {
        var redisDb = _redis.GetDatabase();
        if (redisDb == null)
        {
            throw new InvalidOperationException("Redis connection has not been initialized.");
        }

        // 1. Güncel şehirler listesini veritabanından al
        var allCities = await _destinationRepository.GetAllCitiesAsync();

        // 2. Şehirler listesini Redis cache'e kaydet
        await redisDb.StringSetAsync("cities", JsonSerializer.Serialize(allCities, new JsonSerializerOptions { WriteIndented = true }));

        // 3. Her şehir için detayları Redis cache'e kaydet
        foreach (var city in allCities)
        {
            var destination = await _destinationRepository.GetDestinationByNameAsync(city);
            if (destination != null)
            {
                await redisDb.StringSetAsync(city.ToLower(), JsonSerializer.Serialize(destination, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }
}
