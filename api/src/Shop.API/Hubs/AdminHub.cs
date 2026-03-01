using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shop.Domain.Interfaces;

namespace Shop.API.Hubs;

[Authorize]
public class AdminHub : Hub
{
    private readonly ITenantContext _tenantContext;

    public AdminHub(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override async Task OnConnectedAsync()
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"admins-{tenantId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId > 0)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"admins-{tenantId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
