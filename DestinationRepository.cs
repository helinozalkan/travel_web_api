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

        public async Task<Destination> GetDestinationByNameAsync(string name)
        {
            return await _context.Destinations
                .Where(d => d.Name.ToLower() == name.ToLower())
                .FirstOrDefaultAsync();
        }

        public async Task<List<string>> GetAllCitiesAsync()
        {
            return await _context.Destinations
                .Select(d => d.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();
        }

        public async Task AddDestinationAsync(Destination destination)
        {
            _context.Destinations.Add(destination);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DestinationExistsAsync(string name)
        {
            return await _context.Destinations
                .AnyAsync(d => d.Name.ToLower() == name.ToLower());
        }
    }
}
