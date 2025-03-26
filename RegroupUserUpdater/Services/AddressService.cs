using Microsoft.EntityFrameworkCore;
using RegroupUserUpdater.Data;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Services
{
    public class AddressService(ApplicationDbContext context) : IAddressService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<List<Address>> GetAllAddressesAsync()
        {
            return await _context.Addresses.ToListAsync();
        }

        public async Task<Address?> GetAddressByIdAsync(int id)
        {
            return await _context.Addresses.FindAsync(id);
        }

        public async Task<Address?> GetAddressByLegalAddressAsync(string legalAddress)
        {
            return await _context.Addresses
                .FirstOrDefaultAsync(a => a.LegalAddress == legalAddress);
        }

        public async Task<Address?> GetAddressByStreetAddressAsync(string streetAddress)
        {
            return await _context.Addresses
                .FirstOrDefaultAsync(a => a.FullAddress == streetAddress);
        }

        public async Task<Address> AddAddressAsync(Address address)
        {
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task<Address?> UpdateAddressAsync(int id, Address address)
        {
            if (id != address.Id)
            {
                return null;
            }

            var existingAddress = await _context.Addresses.FindAsync(id);
            if (existingAddress == null)
            {
                return null;
            }

            existingAddress.FullAddress = address.FullAddress;
            existingAddress.Lot = address.Lot;
            existingAddress.Block = address.Block;
            existingAddress.Addition = address.Addition;
            existingAddress.LegalAddress = address.LegalAddress;

            _context.Addresses.Update(existingAddress);
            await _context.SaveChangesAsync();

            return existingAddress;
        }

        public async Task<bool> DeleteAddressAsync(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
            {
                return false;
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 