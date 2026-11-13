using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class PhilSmsResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NormalizedRecipient { get; set; }
    public Guid? LogId { get; set; }
}

public interface IPhilSmsService
{
    Task<PhilSmsResult> SendSmsAsync(
        string recipient,
        string message,
        string? senderId = null,
        Guid? ownerId = null,
        string? recipientName = null,
        string? sentBy = null,
        CancellationToken cancellationToken = default);
}

public class PhilSmsService : IPhilSmsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PhilSmsService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PhilSmsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PhilSmsService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<PhilSmsResult> SendSmsAsync(
        string recipient,
        string message,
        string? senderId = null,
        Guid? ownerId = null,
        string? recipientName = null,
        string? sentBy = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            var failMsg = "Recipient mobile number is required.";
            var logId = await SaveLogAsync(recipient ?? string.Empty, recipientName, ownerId, message ?? string.Empty, senderId, false, failMsg, sentBy);
            return new PhilSmsResult
            {
                Success = false,
                Message = failMsg,
                LogId = logId
            };
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            var failMsg = "SMS message content cannot be empty.";
            var logId = await SaveLogAsync(recipient, recipientName, ownerId, string.Empty, senderId, false, failMsg, sentBy);
            return new PhilSmsResult
            {
                Success = false,
                Message = failMsg,
                LogId = logId
            };
        }

        var normalizedRecipient = NormalizePhilippineMobileNumber(recipient);
        if (string.IsNullOrWhiteSpace(normalizedRecipient))
        {
            var failMsg = "Invalid mobile number format. Please enter a valid Philippine mobile number (e.g., 09171234567 or +639171234567).";
            var logId = await SaveLogAsync(recipient, recipientName, ownerId, message, senderId, false, failMsg, sentBy);
            return new PhilSmsResult
            {
                Success = false,
                Message = failMsg,
                LogId = logId
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
            var failMsg = "PhilSMS API Token is not configured.";
            var logId = await SaveLogAsync(normalizedRecipient, recipientName, ownerId, message, senderId, false, failMsg, sentBy);
            return new PhilSmsResult
            {
                Success = false,
                Message = failMsg,
                NormalizedRecipient = normalizedRecipient,
                LogId = logId
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
                        var successMsg = !string.IsNullOrWhiteSpace(apiMsg) ? apiMsg : $"SMS sent successfully to {normalizedRecipient}.";
                        var logId = await SaveLogAsync(normalizedRecipient, recipientName, ownerId, message, effectiveSenderId, true, successMsg, sentBy);
                        return new PhilSmsResult
                        {
                            Success = true,
                            NormalizedRecipient = normalizedRecipient,
                            Message = successMsg,
                            LogId = logId
                        };
                    }

                    var errorMsg = !string.IsNullOrWhiteSpace(apiMsg)
                        ? apiMsg
                        : $"PhilSMS returned status '{status ?? response.StatusCode.ToString()}'.";
                    var failLogId = await SaveLogAsync(normalizedRecipient, recipientName, ownerId, message, effectiveSenderId, false, errorMsg, sentBy);
                    return new PhilSmsResult
                    {
                        Success = false,
                        NormalizedRecipient = normalizedRecipient,
                        Message = errorMsg,
                        LogId = failLogId
                    };
                }
                catch (JsonException)
                {
                    // Fall through to HTTP status code check
                }
            }

            if (response.IsSuccessStatusCode)
            {
                var successMsg = $"SMS sent successfully to {normalizedRecipient}.";
                var logId = await SaveLogAsync(normalizedRecipient, recipientName, ownerId, message, effectiveSenderId, true, successMsg, sentBy);
                return new PhilSmsResult
                {
                    Success = true,
                    NormalizedRecipient = normalizedRecipient,
                    Message = successMsg,
                    LogId = logId
                };
            }

            var httpErrorMsg = $"PhilSMS request failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
            var httpFailLogId = await SaveLogAsync(normalizedRecipient, recipientName, ownerId, message, effectiveSenderId, false, httpErrorMsg, sentBy);
            return new PhilSmsResult
            {
                Success = false,
                NormalizedRecipient = normalizedRecipient,
                Message = httpErrorMsg,
                LogId = httpFailLogId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via PhilSMS to {Recipient}", normalizedRecipient);
            var exMsg = $"Unable to connect to PhilSMS gateway: {ex.Message}";
            var exLogId = await SaveLogAsync(normalizedRecipient, recipientName, ownerId, message, effectiveSenderId, false, exMsg, sentBy);
            return new PhilSmsResult
            {
                Success = false,
                NormalizedRecipient = normalizedRecipient,
                Message = exMsg,
                LogId = exLogId
            };
        }
    }

    private async Task<Guid?> SaveLogAsync(
        string recipientNumber,
        string? recipientName,
        Guid? ownerId,
        string message,
        string? senderId,
        bool isSuccess,
        string? gatewayResponse,
        string? sentBy)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var log = new SmsNotificationLog
            {
                Id = Guid.NewGuid(),
                RecipientNumber = recipientNumber,
                RecipientName = recipientName,
                OwnerId = ownerId,
                Message = message,
                SenderId = senderId,
                Status = isSuccess ? SmsDeliveryStatus.Success : SmsDeliveryStatus.Failed,
                GatewayResponse = gatewayResponse,
                SentAt = DateTime.Now,
                SentBy = !string.IsNullOrWhiteSpace(sentBy) ? sentBy : "System"
            };
            db.SmsNotificationLogs.Add(log);
            await db.SaveChangesAsync();
            return log.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save SMS log to database for recipient {Recipient}", recipientNumber);
            return null;
        }
    }

    public static string? NormalizePhilippineMobileNumber(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return null;

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
