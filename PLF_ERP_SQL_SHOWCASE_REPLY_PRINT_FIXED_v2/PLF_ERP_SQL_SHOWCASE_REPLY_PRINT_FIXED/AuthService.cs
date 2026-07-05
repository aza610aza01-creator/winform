using System;
using MySql.Data.MySqlClient;

namespace PremiumLivingFurnitureWinForms;

public static class AuthService
{
    public static string GetRoleForUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return "";
        using var c = Database.GetConnection();
        c.Open();
        using var cmd = new MySqlCommand("SELECT r.RoleName FROM Users u JOIN UserRoles r ON u.RoleId=r.Id WHERE u.Username=@u AND u.Status='Active' LIMIT 1", c);
        cmd.Parameters.AddWithValue("@u", username.Trim());
        return Convert.ToString(cmd.ExecuteScalar()) ?? "";
    }

    public static bool TryLogin(string username, string password, out string role)
    {
        role = "";
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;
        using var c = Database.GetConnection();
        c.Open();
        using var cmd = new MySqlCommand("SELECT u.Password, r.RoleName FROM Users u JOIN UserRoles r ON u.RoleId=r.Id WHERE u.Username=@u AND u.Status='Active' LIMIT 1", c);
        cmd.Parameters.AddWithValue("@u", username.Trim());
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return false;
        string storedPassword = Convert.ToString(reader["Password"]) ?? "";
        string detectedRole = Convert.ToString(reader["RoleName"]) ?? "";
        if (!PasswordService.VerifyPassword(password.Trim(), storedPassword)) return false;
        role = detectedRole;
        return true;
    }

    public static bool Login(string username, string password, string role)
    {
        return TryLogin(username, password, out var detectedRole)
            && string.Equals(detectedRole, role.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool Login(string username, string password)
    {
        return TryLogin(username, password, out _);
    }
}
