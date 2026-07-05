using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace PremiumLivingFurnitureWinForms;

public static class EmailService
{
    const string ConfigFileName = "smtp_config.json";

    public static void SendTemporaryPassword(string toEmail, string username, string temporaryPassword)
    {
        var config = LoadConfig();
        if (string.IsNullOrWhiteSpace(config.Host)) throw new InvalidOperationException("SMTP Host is missing in smtp_config.json.");
        if (string.IsNullOrWhiteSpace(config.From)) throw new InvalidOperationException("SMTP From is missing in smtp_config.json.");
        if (config.Port <= 0) throw new InvalidOperationException("SMTP Port is invalid in smtp_config.json.");

        using var message = new MailMessage();
        message.From = new MailAddress(config.From, "Premium Living Furniture ERP");
        message.To.Add(new MailAddress(toEmail));
        message.Subject = "Your Premium Living Furniture ERP temporary password";
        message.Body = $@"Hello,

A Premium Living Furniture ERP employee account has been created for you.

Username: {username}
Temporary password: {temporaryPassword}

Please sign in with the temporary password and change it from Update Profile.

Premium Living Furniture ERP";

        using var client = new SmtpClient(config.Host, config.Port) { EnableSsl = config.EnableSsl, DeliveryMethod = SmtpDeliveryMethod.Network };
        if (!string.IsNullOrWhiteSpace(config.Username)) client.Credentials = new NetworkCredential(config.Username, config.Password ?? "");
        client.Send(message);
    }

    static SmtpConfig LoadConfig()
    {
        string baseDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        string currentDirPath = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
        string path = File.Exists(baseDirPath) ? baseDirPath : currentDirPath;
        if (!File.Exists(path)) throw new FileNotFoundException("SMTP configuration file not found. Please create smtp_config.json next to the application .exe or set the file to Copy if newer in Visual Studio.", ConfigFileName);
        try
        {
            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<SmtpConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return config ?? throw new InvalidOperationException("smtp_config.json is empty or invalid.");
        }
        catch (JsonException ex) { throw new InvalidOperationException("smtp_config.json contains invalid JSON. Please check commas, quotes and brackets.", ex); }
    }

    class SmtpConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string From { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
    }
}
