using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HaluluAPI.Utilities;

/// <summary>
/// Validation helper utilities
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validate email format
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate phone number format
    /// </summary>
    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Basic phone number validation - allows digits, +, -, (, ), and spaces
        var pattern = @"^\+?[0-9\s\-\(\)]{7,}$";
        return Regex.IsMatch(phoneNumber, pattern);
    }

    /// <summary>
    /// Validate OTP format
    /// </summary>
    public static bool IsValidOtp(string otp, int expectedLength = 6)
    {
        if (string.IsNullOrWhiteSpace(otp))
            return false;

        // OTP should contain only digits
        if (!Regex.IsMatch(otp, "^[0-9]+$"))
            return false;

        return otp.Length >= expectedLength - 1 && otp.Length <= expectedLength + 1;
    }

    /// <summary>
    /// Validate name format
    /// </summary>
    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name.Length < 2 || name.Length > 100)
            return false;

        // Allow letters, spaces, hyphens, and apostrophes
        return Regex.IsMatch(name, @"^[a-zA-Z\s\-']{2,100}$");
    }

    /// <summary>
    /// Get validation errors for an object
    /// </summary>
    public static Dictionary<string, string[]> GetValidationErrors(object obj)
    {
        var context = new ValidationContext(obj);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(obj, context, results, true);

        return results
            .GroupBy(r => r.MemberNames.FirstOrDefault() ?? "General")
            .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Validation error").ToArray());
    }
}