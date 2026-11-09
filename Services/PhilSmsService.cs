using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PropertyAssessmentApp.Services;

public class PhilSmsResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NormalizedRecipient { get; set; }
}

public interface IPhilSmsService
{
    Task<PhilSmsResult> SendSmsAsync(string recipient, string message, string? senderId = null, CancellationToken cancellationToken = default);
}

public class PhilSmsService : IPhilSmsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PhilSmsService> _logger;

    public PhilSmsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PhilSmsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PhilSmsResult> SendSmsAsync(
        string recipient,
        string message,
        string? senderId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return new PhilSmsResult
            {
                Success = false,
                Message = "Recipient mobile number is required."
            };
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return new PhilSmsResult
            {
                Success = false,
                Message = "SMS message content cannot be empty."
            };
        }

        var normalizedRecipient = NormalizePhilippineMobileNumber(recipient);
        if (string.IsNullOrWhiteSpace(normalizedRecipient))
        {
            return new PhilSmsResult
            {
                Success = false,
                Message = "Invalid mobile number format. Please enter a valid Philippine mobile number (e.g., 09171234567 or +639171234567)."
            };
        }

        var baseUrl = _configuration["PhilSmsSettings:BaseUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://dashboard.philsms.com/api/v3/";
        }

        var apiToken = _configuration["PhilSmsSettings:ApiToken"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return new PhilSmsResult
            {
                Success = false,
                Message = "PhilSMS API Token is not configured."
            };
        }

        var effectiveSenderId = !string.IsNullOrWhiteSpace(senderId)
            ? senderId.Trim()
            : (_configuration["PhilSmsSettings:SenderId"]?.Trim() ?? "PhilSMS");

        var endpoint = $"{baseUrl.TrimEnd('/')}/sms/send";

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                recipient = normalizedRecipient,
                sender_id = effectiveSenderId,
                type = "plain",
                message = message.Trim()
            };

            var jsonBody = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    var status = root.TryGetProperty("status", out var statusProp)
                        ? statusProp.GetString()
                        : null;
                    var apiMsg = root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String
                        ? msgProp.GetString()
                        : null;

                    if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) || response.IsSuccessStatusCode && !string.Equals(status, "error", StringComparison.OrdinalIgnoreCase))
                    {
                        return new PhilSmsResult
                        {
                            Success = true,
                            NormalizedRecipient = normalizedRecipient,
                            Message = !string.IsNullOrWhiteSpace(apiMsg)
                                ? apiMsg
                                : $"SMS sent successfully to {normalizedRecipient}."
                        };
                    }

                    return new PhilSmsResult
                    {
                        Success = false,
                        NormalizedRecipient = normalizedRecipient,
                        Message = !string.IsNullOrWhiteSpace(apiMsg)
                            ? apiMsg
                            : $"PhilSMS returned status '{status ?? response.StatusCode.ToString()}'."
                    };
                }
                catch (JsonException)
                {
                    // Fall through to HTTP status code check
                }
            }

            if (response.IsSuccessStatusCode)
            {
                return new PhilSmsResult
                {
                    Success = true,
                    NormalizedRecipient = normalizedRecipient,
                    Message = $"SMS sent successfully to {normalizedRecipient}."
                };
            }

            return new PhilSmsResult
            {
                Success = false,
                NormalizedRecipient = normalizedRecipient,
                Message = $"PhilSMS request failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase})."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via PhilSMS to {Recipient}", normalizedRecipient);
            return new PhilSmsResult
            {
                Success = false,
                NormalizedRecipient = normalizedRecipient,
                Message = $"Unable to connect to PhilSMS gateway: {ex.Message}"
            };
        }
    }

    public static string? NormalizePhilippineMobileNumber(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return null;

        // Support comma/semicolon/slash separated numbers by taking the first valid mobile number or joining multiple valid numbers
        var parts = rawInput.Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validNumbers = new List<string>();

        foreach (var part in parts)
        {
            var digits = new string(part.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits))
                continue;

            if (digits.StartsWith("09") && digits.Length == 11)
            {
                validNumbers.Add("63" + digits[1..]);
            }
            else if (digits.StartsWith("639") && digits.Length == 12)
            {
                validNumbers.Add(digits);
            }
            else if (digits.StartsWith("9") && digits.Length == 10)
            {
                validNumbers.Add("63" + digits);
            }
            else if (digits.Length >= 10 && digits.Length <= 15)
            {
                validNumbers.Add(digits);
            }
        }

        return validNumbers.Count > 0 ? string.Join(",", validNumbers.Distinct()) : null;
    }
}

