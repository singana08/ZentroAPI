using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ZentroAPI.Services;

public class AddressService : IAddressService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AddressService> _logger;

    public AddressService(ApplicationDbContext context, ILogger<AddressService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AddressResponseDto? Address)> CreateAddressAsync(CreateAddressDto request)
    {
        try
        {
            // If setting as primary, unset other primary addresses for this profile
            if (request.IsPrimary)
            {
                var existingPrimary = await _context.Addresses
                    .Where(a => a.ProfileId == request.ProfileId && a.IsPrimary)
                    .ToListAsync();
                
                foreach (var addr in existingPrimary)
                {
                    addr.IsPrimary = false;
                }
            }

            var address = new Address
            {
                ProfileId = request.ProfileId,
                Label = request.Label,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                AddressLine = request.AddressLine,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                IsPrimary = request.IsPrimary
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return (true, "Address created successfully", MapToResponseDto(address));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating address");
            return (false, "Failed to create address", null);
        }
    }

    public async Task<(bool Success, string Message, List<AddressResponseDto> Addresses)> GetProfileAddressesAsync(Guid profileId)
    {
        try
        {
            var addresses = await _context.Addresses
                .Where(a => a.ProfileId == profileId)
                .OrderByDescending(a => a.IsPrimary)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            var addressDtos = addresses.Select(MapToResponseDto).ToList();
            return (true, "Addresses retrieved successfully", addressDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting addresses for profile {profileId}");
            return (false, "Failed to retrieve addresses", new List<AddressResponseDto>());
        }
    }

    public async Task<(bool Success, string Message, AddressResponseDto? Address)> GetAddressByIdAsync(Guid id)
    {
        try
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return (false, "Address not found", null);

            return (true, "Address retrieved successfully", MapToResponseDto(address));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting address {id}");
            return (false, "Failed to retrieve address", null);
        }
    }

    public async Task<(bool Success, string Message, AddressResponseDto? Address)> UpdateAddressAsync(Guid id, UpdateAddressDto request)
    {
        try
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return (false, "Address not found", null);

            // If setting as primary, unset other primary addresses for this profile
            if (request.IsPrimary && !address.IsPrimary)
            {
                var existingPrimary = await _context.Addresses
                    .Where(a => a.ProfileId == address.ProfileId && a.IsPrimary && a.Id != id)
                    .ToListAsync();
                
                foreach (var addr in existingPrimary)
                {
                    addr.IsPrimary = false;
                }
            }

            address.Label = request.Label;
            address.Latitude = request.Latitude;
            address.Longitude = request.Longitude;
            address.AddressLine = request.AddressLine;
            address.City = request.City;
            address.State = request.State;
            address.PostalCode = request.PostalCode;
            address.IsPrimary = request.IsPrimary;

            await _context.SaveChangesAsync();
            return (true, "Address updated successfully", MapToResponseDto(address));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating address {id}");
            return (false, "Failed to update address", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteAddressAsync(Guid id)
    {
        try
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return (false, "Address not found");

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
            return (true, "Address deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting address {id}");
            return (false, "Failed to delete address");
        }
    }

    private static AddressResponseDto MapToResponseDto(Address address)
    {
        return new AddressResponseDto
        {
            Id = address.Id,
            ProfileId = address.ProfileId,
            Label = address.Label,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            AddressLine = address.AddressLine,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            IsPrimary = address.IsPrimary,
            CreatedAt = address.CreatedAt
        };
    }
}
