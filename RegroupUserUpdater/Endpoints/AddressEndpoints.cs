using Microsoft.AspNetCore.Mvc;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Endpoints
{
    public static class AddressEndpoints
    {
        public static void MapAddressEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/addresses").WithTags("Addresses");

            group.MapGet("/", async (IAddressService addressService) =>
            {
                var addresses = await addressService.GetAllAddressesAsync();
                return Results.Ok(addresses);
            })
            .WithName("GetAllAddresses")
            .WithOpenApi();

            group.MapGet("/{id}", async (int id, IAddressService addressService) =>
            {
                var address = await addressService.GetAddressByIdAsync(id);
                if (address == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(address);
            })
            .WithName("GetAddressById")
            .WithOpenApi();

            group.MapPost("/", async (Address address, IAddressService addressService) =>
            {
                var newAddress = await addressService.AddAddressAsync(address);
                return Results.Created($"/api/addresses/{newAddress.Id}", newAddress);
            })
            .WithName("CreateAddress")
            .WithOpenApi();

            group.MapPut("/{id}", async (int id, Address address, IAddressService addressService) =>
            {
                var updatedAddress = await addressService.UpdateAddressAsync(id, address);
                if (updatedAddress == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(updatedAddress);
            })
            .WithName("UpdateAddress")
            .WithOpenApi();

            group.MapDelete("/{id}", async (int id, IAddressService addressService) =>
            {
                var result = await addressService.DeleteAddressAsync(id);
                if (!result)
                {
                    return Results.NotFound();
                }
                return Results.NoContent();
            })
            .WithName("DeleteAddress")
            .WithOpenApi();
        }
    }
} 