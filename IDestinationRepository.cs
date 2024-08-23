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
    Task UpdateDestinationAsync(Destination destination);
    Task<bool> DestinationExistsAsync(string name); // Bu metodu ekleyin
    Task RemoveDestinationAsync(Destination destination); // Yeni metod

    Task DeleteDestinationAsync(string name);
}

}
