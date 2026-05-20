using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IInstitutionFeatureService _institutionFeatureService;

    public NotificationHub(IInstitutionFeatureService institutionFeatureService)
    {
        _institutionFeatureService = institutionFeatureService;
    }

    public override async Task OnConnectedAsync()
    {
        int institutionId = 1;
        var institutionClaim = Context.User?.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
        if (institutionClaim != null)
        {
            institutionId = int.Parse(institutionClaim.Value);
        }

        if (!_institutionFeatureService.IsFeatureEnabled(institutionId, "Communication.EnableSignalR", true))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}
