using System.Security.Claims;
using RZ.Foundation.Blazor.Auth;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Blazor.Auth0.Auth;

public class AfterSignInHandler : IAfterSignInHandler
{
    public async ValueTask<AfterSignInCheck> ProceedAfterSignInFlow(ClaimsPrincipal user) {
        Console.WriteLine($"Check user {user.Identity?.Name} for registration flow.");
        // modify user if wanted
        return new AfterSignInCheck.LoginSuccess(user);
    }

    public async ValueTask<CheckUserResult> VerifyUser(ClaimsPrincipal user) {
        Console.WriteLine($"Verify user expiration of {user.Identity?.Name}.");
        return new CheckUserResult.Passed();
    }
}