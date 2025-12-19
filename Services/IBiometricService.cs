namespace ZentroAPI.Services;

public interface IBiometricService
{
    Task<string> GenerateBiometricPinAsync(Guid userId);
    Task<bool> ValidateBiometricPinAsync(string pin);
    Task<bool> DisableBiometricAsync(Guid userId);
}