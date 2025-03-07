using System.Security.Claims;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;

namespace RZ.Foundation.Blazor.Auth;

public class AuthCircuitWatcher(IHttpContextAccessor httpAccessor, UserState userState) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next) {
        var user = httpAccessor.HttpContext?.User;
        var token = user?.FindFirstValue("access_token");
        if (user is not null){
            IAuthUserState authState = userState;
            authState.SetAuthState(user, token);
        }
        return base.CreateInboundActivityHandler(next);
    }
}