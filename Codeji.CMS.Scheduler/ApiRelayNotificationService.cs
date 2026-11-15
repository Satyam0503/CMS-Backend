using System.Net.Http.Json;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;

namespace Codeji.CMS.Scheduler;

/// <summary>SignalR clients are connected to the API, so real-time pushes are relayed through its internal endpoint.</summary>
public sealed class ApiRelayNotificationService(HttpClient http, ILogger<ApiRelayNotificationService> logger) : INotificationService
{
    public async Task SendNotificationToUser(string userId, NotificationViewModel notification)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("internal/notifications", new { userId, notification });
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Notification relay to API returned {Status} for user {UserId}", (int)response.StatusCode, userId);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // The notification is already stored; only the live push is lost, so a down API must not fail the job.
            logger.LogWarning(ex, "Notification relay to API failed for user {UserId}", userId);
        }
    }
}
