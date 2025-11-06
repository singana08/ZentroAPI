using HaluluAPI.DTOs;

namespace HaluluAPI.Services;

public interface IAddressService
{
    Task<(bool Success, string Message, AddressResponseDto? Address)> CreateAddressAsync(CreateAddressDto request);
    Task<(bool Success, string Message, List<AddressResponseDto> Addresses)> GetProfileAddressesAsync(Guid profileId);
    Task<(bool Success, string Message, AddressResponseDto? Address)> GetAddressByIdAsync(Guid id);
    Task<(bool Success, string Message, AddressResponseDto? Address)> UpdateAddressAsync(Guid id, UpdateAddressDto request);
    Task<(bool Success, string Message)> DeleteAddressAsync(Guid id);
}