using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Interfaces
{
    public interface IAddressService
    {
        Task<List<Address>> GetAllAddressesAsync();
        Task<Address?> GetAddressByIdAsync(int id);
        Task<Address> AddAddressAsync(Address address);
        Task<Address?> UpdateAddressAsync(int id, Address address);
        Task<bool> DeleteAddressAsync(int id);
        Task<Address?> GetAddressByLegalAddressAsync(string fullAddress);
        Task<Address?> GetAddressByStreetAddressAsync(string streetAddress);
    }
} 