using api_my_web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api_my_web.Data
{
    public class DestinationService
    {
        private readonly IDestinationRepository _destinationRepository;

        public DestinationService(IDestinationRepository destinationRepository)
        {
            _destinationRepository = destinationRepository;
        }

        public async Task<Destination?> GetDestinationByNameAsync(string name)
        {
            return await _destinationRepository.GetDestinationByNameAsync(name);
        }

        public async Task<List<string>> GetAllCitiesAsync()
        {
            return await _destinationRepository.GetAllCitiesAsync();
        }

        public async Task AddDestinationAsync(Destination destination)
        {
            await _destinationRepository.AddDestinationAsync(destination);
        }

        public async Task<bool> DestinationExistsAsync(string name)
        {
            return await _destinationRepository.DestinationExistsAsync(name);
        }

        public async Task DeleteDestinationAsync(string name)
        {
            await _destinationRepository.DeleteDestinationAsync(name);
        }
    }
}
