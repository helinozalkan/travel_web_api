using Microsoft.EntityFrameworkCore;
using api_my_web.Models;

namespace api_my_web.Data
{
    public class DestinationService
    {
        private readonly TravelDbContext _context;

        public DestinationService(TravelDbContext context)
        {
            _context = context;
        }

        public Destination GetDestinationByName(string name)
        {
            var destination = _context.Destinations
                .Where(d => d.Name.ToLower() == name.ToLower())
                .FirstOrDefault();

            return destination;
        }
    }
}
