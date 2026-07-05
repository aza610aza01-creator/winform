using System;
using System.Security.Cryptography;

namespace PremiumLivingFurnitureWinForms;

public static class PasswordService
{
    const int SaltSize = 16;
    const int HashSize = 32;
    const int Iterations = 100000;

    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string enteredPassword, string storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword)) return false;
        if (!storedPassword.StartsWith("PBKDF2$", StringComparison.Ordinal))
            return string.Equals(storedPassword, enteredPassword, StringComparison.Ordinal);
        string[] parts = storedPassword.Split('$');
        if (parts.Length != 4) return false;
        if (!int.TryParse(parts[1], out int iterations)) return false;
        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] expectedHash = Convert.FromBase64String(parts[3]);
        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(enteredPassword, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
