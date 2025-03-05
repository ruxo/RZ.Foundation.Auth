using System.Security.Claims;
using RZ.Foundation.Blazor.Auth;
using TiraxTech;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Blazor.Auth0.Auth;

public class AfterSignInHandler : IAfterSignInHandler
{
    // Just for example, in real world, you should use a database or other persistent storage
    static readonly HashSet<string> RegisteredUser = [];

    public async ValueTask<AfterSignInCheck> ProceedAfterSignInFlow(ClaimsPrincipal user) {
        Console.WriteLine($"Check user {user.Identity?.Name} for registration flow.");

        var userId = user.GetUserId() ?? throw new InvalidOperationException("User ID not found.");
        if (!RegisteredUser.Add(userId))
            return new AfterSignInCheck.LoginSuccess(user);

        return new AfterSignInCheck.CustomLoginFlow(RelativeUri.From("/registration"));
    }

    public async ValueTask<CheckUserResult> VerifyUser(ClaimsPrincipal user) {
        Console.WriteLine($"Verify user expiration of {user.Identity?.Name}.");
        return new CheckUserResult.Passed();
    }
}