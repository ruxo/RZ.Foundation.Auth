using System.Security.Claims;
using JetBrains.Annotations;
using TiraxTech;

namespace RZ.Foundation.Blazor.Auth;

public abstract record AfterSignInCheck
{
    public sealed record LoginSuccess(ClaimsPrincipal User) : AfterSignInCheck;
    public sealed record CustomLoginFlow(RelativeUri CustomFlowPath) : AfterSignInCheck;
    public sealed record Failed(string Message) : AfterSignInCheck;

    AfterSignInCheck(){}
}

public abstract record CheckUserResult
{
    public sealed record Passed : CheckUserResult
    {
        [UsedImplicitly]
        public static readonly CheckUserResult Instance = new Passed();
    }
    public sealed record Replace(ClaimsPrincipal User) : CheckUserResult;

    public sealed record Expired : CheckUserResult
    {
        [UsedImplicitly]
        public static readonly CheckUserResult Instance = new Expired();
    }
}

public interface IAfterSignInHandler
{
    /// <summary>
    /// Check if a custom flow is required after sign in, otherwise the user will be successfully logged in.
    /// </summary>
    /// <returns></returns>
    ValueTask<AfterSignInCheck> ProceedAfterSignInFlow(ClaimsPrincipal user);

    /// <summary>
    /// Decorate user state with additional claims.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    ValueTask<CheckUserResult> VerifyUser(ClaimsPrincipal user);
}