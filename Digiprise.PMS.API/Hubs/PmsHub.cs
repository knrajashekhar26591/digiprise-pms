using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Digiprise.PMS.API.Hubs;

/// <summary>
/// PmsHub — tenant-scoped real-time hub for board updates, notifications, and live collaboration.
/// Groups: board:{boardId}, project:{projectId}, user:{userId}
/// </summary>
[Authorize]
public class PmsHub : Hub
{
    private readonly ILogger<PmsHub> _logger;

    public PmsHub(ILogger<PmsHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.UserIdentifier;
        var tenantId = Context.User?.FindFirst("tenantId")?.Value ?? "0";

        _logger.LogInformation("SignalR connected: user={UserId} tenant={TenantId} conn={ConnId}",
            userId, tenantId, Context.ConnectionId);

        // Auto-join user-specific group
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR disconnected: conn={ConnId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Join a board room to receive live card updates</summary>
    public async Task JoinBoard(int boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"board:{boardId}");
        _logger.LogDebug("Joined board group {BoardId}", boardId);
    }

    /// <summary>Leave a board room</summary>
    public async Task LeaveBoard(int boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"board:{boardId}");
    }

    /// <summary>Join a project room</summary>
    public async Task JoinProject(int projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }

    /// <summary>Broadcast issue status change (called server-side via IHubContext)</summary>
    public static class Events
    {
        public const string IssueStatusChanged = "IssueStatusChanged";
        public const string IssueAssigned = "IssueAssigned";
        public const string CommentAdded = "CommentAdded";
        public const string SprintStarted = "SprintStarted";
        public const string SprintClosed = "SprintClosed";
        public const string NotificationReceived = "NotificationReceived";
    }
}
