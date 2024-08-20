using api_my_web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api_my_web.Data
{
    public interface IDestinationRepository
    {
        Task<Destination?> GetDestinationByNameAsync(string name);
        Task<List<string>> GetAllCitiesAsync();
        Task AddDestinationAsync(Destination destination);
        Task<bool> DestinationExistsAsync(string name);
        Task DeleteDestinationAsync(string name); // Yeni metod imzası
    }
}
