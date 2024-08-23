using api_my_web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api_my_web.Data
{
    public class DestinationRepository : IDestinationRepository
    {
        private readonly TravelDbContext _context;

        public DestinationRepository(TravelDbContext context)
        {
            _context = context;
        }

        // Şehir adını küçük harfe çevirerek arama yapar ve varsa ilk eşleşeni döndürür
        public async Task<Destination?> GetDestinationByNameAsync(string name)
        {
            return await _context.Destinations
                .Where(d => d.Name.ToLower() == name.ToLower())
                .FirstOrDefaultAsync();
        }

        // Şehirlerin listesini alır, sıralar ve döndürür
        public async Task<List<string>> GetAllCitiesAsync()
        {
            return await _context.Destinations
                .Select(d => d.Name)
                .Distinct()
                .OrderBy(name => name) // Şehir isimlerini sıralar
                .ToListAsync();
        }

        // Yeni bir şehir ekler ve değişiklikleri veritabanına kaydeder
        public async Task AddDestinationAsync(Destination destination)
        {
            _context.Destinations.Add(destination);
            await _context.SaveChangesAsync();
        }

        // Belirtilen şehir adının veritabanında var olup olmadığını kontrol eder
        public async Task<bool> DestinationExistsAsync(string name)
        {
            return await _context.Destinations
                .AnyAsync(d => d.Name.ToLower() == name.ToLower());
        }

        // Verilen isimle eşleşen destinasyonu bulur ve siler
    public async Task RemoveDestinationAsync(Destination destination)
    {
        _context.Destinations.Remove(destination);
        await _context.SaveChangesAsync();
    }

        // Verilen isimle eşleşen destinasyonu bulur ve siler
        public async Task DeleteDestinationAsync(string name)
        {
            var destination = await _context.Destinations
                .Where(d => d.Name.ToLower() == name.ToLower())
                .FirstOrDefaultAsync();

            if (destination != null)
            {
                _context.Destinations.Remove(destination);
                await _context.SaveChangesAsync();
            }
        }

        // Destinasyon verilerini günceller
        public async Task UpdateDestinationAsync(Destination destination)
        {
            var existingDestination = await _context.Destinations
                .Where(d => d.Name.ToLower() == destination.Name.ToLower())
                .FirstOrDefaultAsync();

            if (existingDestination != null)
            {
                // Burada mevcut destinasyonu güncelleriz
                existingDestination.DescriptionEnglish = destination.DescriptionEnglish;
                existingDestination.AttractionsEnglish = destination.AttractionsEnglish;
                existingDestination.LocalDishesEnglish = destination.LocalDishesEnglish;

                await _context.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException("Destination not found");
            }
        }
    }
}
