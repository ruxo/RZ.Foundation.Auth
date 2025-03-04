using System.Security.Claims;
using LanguageExt;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace RZ.Foundation.Blazor.Auth;

public class RzAuthStateProvider : AuthenticationStateProvider
{
    readonly IAfterSignInHandler afterSignInHandler;

    Task<ClaimsPrincipal> user;

    public RzAuthStateProvider(IHttpContextAccessor httpContextAccessor, IAfterSignInHandler afterSignInHandler) {
        this.afterSignInHandler = afterSignInHandler;
        user = httpContextAccessor.HttpContext?.User is not {} u || !u.IsAuthenticated()
                   ? Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity()))
                   : Task.Run(() => TryCreateUser(u));
    }

    public Task<ClaimsPrincipal> GetUser()
        => GetAuthenticationStateAsync().Map(auth => auth.User);

    public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
        var u = await user;
        var result = await afterSignInHandler.VerifyUser(u);
        return new(result switch {
            CheckUserResult.Passed          => u,
            CheckUserResult.Replace replace => UpdateUser(replace.User),
            CheckUserResult.Expired         => UpdateUser(new(new ClaimsIdentity())),
            _                               => throw new NotSupportedException($"Unknown result: {result}")
        });
    }

    async Task<ClaimsPrincipal> TryCreateUser(ClaimsPrincipal u) {
        var result = await afterSignInHandler.VerifyUser(u);
        return result switch {
            CheckUserResult.Passed          => u,
            CheckUserResult.Replace replace => replace.User,
            CheckUserResult.Expired         => new(new ClaimsIdentity()),
            _                               => throw new NotSupportedException($"Unknown result: {result}")
        };
    }

    ClaimsPrincipal UpdateUser(ClaimsPrincipal u) {
        user = Task.FromResult(u);
        return u;
    }
}