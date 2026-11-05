using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Helpers;

public class JwtPayload
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("sub")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("roleId")]
    public int? RoleId { get; set; }

    [JsonPropertyName("divisionId")]
    public int? DivisionId { get; set; }

    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("iss")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    [JsonPropertyName("exp")]
    public long ExpiresAt { get; set; }
}

public static class JwtHelper
{
    private const string DefaultSecretKey = "FAIRTRACK_ASSESSOR_JWT_SECURE_KEY_2026_KORONADAL_CITY_SECRET_KEY_9988";
    private const string DefaultIssuer = "FairTrack_CityAssessor_Server";

    public static string GenerateToken(UserAccount user, string? secretKey = null, int expiryDays = 7)
    {
        ArgumentNullException.ThrowIfNull(user);

        var key = string.IsNullOrWhiteSpace(secretKey) ? DefaultSecretKey : secretKey;
        var now = DateTimeOffset.UtcNow;
        var exp = now.AddDays(expiryDays);

        bool isAdmin = user.Role == UserRole.SystemAdmin || user.Role == UserRole.CityAssessor;

        var header = new
        {
            alg = "HS256",
            typ = "JWT"
        };

        var payload = new JwtPayload
        {
            Uid = user.Id.ToString(),
            Username = user.Username,
            Name = user.FullName,
            RoleId = (int)user.Role,
            DivisionId = user.DivisionId ?? 1,
            IsAdmin = isAdmin,
            Issuer = DefaultIssuer,
            IssuedAt = now.ToUnixTimeSeconds(),
            ExpiresAt = exp.ToUnixTimeSeconds()
        };

        string headerJson = JsonSerializer.Serialize(header);
        string payloadJson = JsonSerializer.Serialize(payload);

        string encodedHeader = Base64UrlEncode(headerJson);
        string encodedPayload = Base64UrlEncode(payloadJson);
        string rawSignature = $"{encodedHeader}.{encodedPayload}";

        string signature = ComputeHmacSha256(rawSignature, key);
        return $"{rawSignature}.{signature}";
    }

    public static bool ValidateToken(string? token, out JwtPayload? payload, out string errorMessage, string? secretKey = null)
    {
        payload = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            errorMessage = "Token is missing or empty.";
            return false;
        }

        string[] parts = token.Trim().Split('.');
        if (parts.Length != 3)
        {
            errorMessage = "Invalid JWT structure. Expected 3 segments.";
            return false;
        }

        string encodedHeader = parts[0];
        string encodedPayload = parts[1];
        string receivedSignature = parts[2];
        string rawData = $"{encodedHeader}.{encodedPayload}";

        var key = string.IsNullOrWhiteSpace(secretKey) ? DefaultSecretKey : secretKey;
        string expectedSignature = ComputeHmacSha256(rawData, key);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(receivedSignature),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            errorMessage = "Signature validation failed. Unauthorized or tampered token.";
            return false;
        }

        try
        {
            string payloadJson = Base64UrlDecode(encodedPayload);
            payload = JsonSerializer.Deserialize<JwtPayload>(payloadJson);
            if (payload == null)
            {
                errorMessage = "Malformed payload content.";
                return false;
            }

            long currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentEpoch > payload.ExpiresAt)
            {
                errorMessage = $"Token has expired. Expiration: {payload.ExpiresAt}, Current: {currentEpoch}.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error parsing token payload: {ex.Message}";
            return false;
        }
    }

    private static string Base64UrlEncode(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string Base64UrlDecode(string base64Url)
    {
        string base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        byte[] bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string ComputeHmacSha256(string data, string secret)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        byte[] messageBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
