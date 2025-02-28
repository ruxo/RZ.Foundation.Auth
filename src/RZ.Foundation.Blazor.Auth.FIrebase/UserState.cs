using System.Security.Claims;

namespace RZ.Foundation.Blazor.Auth;

public interface IAuthUserState
{
    void SetAuthState(ClaimsPrincipal user, string? token);
    void ClearAuthState();
}

public class UserState : IAuthUserState
{
    public ClaimsPrincipal User { get; private set; } = new(new ClaimsIdentity());
    public string? AccessToken { get; private set; }

    void IAuthUserState.SetAuthState(ClaimsPrincipal user, string? token)
    {
        User = user;
        AccessToken = token;
    }

    public void ClearAuthState() {
        User = new(new ClaimsIdentity());
        AccessToken = null;
    }
}