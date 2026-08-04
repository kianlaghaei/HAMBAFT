using Hambaft.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Hambaft.Api.Realtime;

public interface ISessionHubClient { Task StateChanged(StateChangedNotification notification); }

[Authorize]
public sealed class SessionHub : Hub<ISessionHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var role=Context.User?.FindFirst("client_role")?.Value;
        var sessionId=Claim("session_id");
        await Groups.AddToGroupAsync(Context.ConnectionId,HubGroups.Session(sessionId));
        if(role=="Team")await Groups.AddToGroupAsync(Context.ConnectionId,HubGroups.Team(Claim("team_id")));
        else if(role=="Admin")await Groups.AddToGroupAsync(Context.ConnectionId,HubGroups.Admins(sessionId));
        else if(role=="PublicDisplay")await Groups.AddToGroupAsync(Context.ConnectionId,HubGroups.Display(sessionId));
        else throw new HubException("A recognized client_role claim is required.");
        await base.OnConnectedAsync();
    }
    private Guid Claim(string name)=>Guid.TryParse(Context.User?.FindFirst(name)?.Value,out var id)?id:throw new HubException($"Required {name} claim is missing.");
}

public static class HubGroups
{
    public static string Session(Guid id)=>$"session:{id:D}";
    public static string Team(Guid id)=>$"team:{id:D}";
    public static string Admins(Guid id)=>$"admins:{id:D}";
    public static string Display(Guid id)=>$"display:{id:D}";
}
