using System.Security.Cryptography;

namespace ZentroAPI.Utilities;

/// <summary>
/// Utility class to generate unique 8-digit alphanumeric codes
/// </summary>
public static class UniqueIdGenerator
{
    private const string AlphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Generates a random 8-character alphanumeric code
    /// </summary>
    /// <returns>8-character alphanumeric string</returns>
    public static string Generate()
    {
        var result = new char[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] uintBuffer = new byte[sizeof(uint)];
            for (int i = 0; i < 8; i++)
            {
                rng.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                result[i] = AlphanumericChars[(int)(num % (uint)AlphanumericChars.Length)];
            }
        }
        return new string(result);
    }
}
