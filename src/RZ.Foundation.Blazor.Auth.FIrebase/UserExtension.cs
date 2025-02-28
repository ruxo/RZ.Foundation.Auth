using System.Diagnostics.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace RZ.Foundation.Blazor.Auth;

public static class UserExtension
{
    public static bool IsAuthenticated(this ClaimsPrincipal principal)
        => principal.Identities.Any(i => i.IsAuthenticated);

    [Pure]
    public static DateTimeOffset? GetExpiration(this ClaimsPrincipal jwt)
        => jwt.GetDateTime(JwtRegisteredClaimNames.Exp);

    [Pure]
    public static DateTimeOffset? GetDateTime(this ClaimsPrincipal jwt, string key)
        => jwt.FindFirstValue(key) is {} value? DateTimeOffset.FromUnixTimeSeconds(long.Parse(value)) : null;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsExpired(this ClaimsPrincipal principal, DateTimeOffset now)
        => principal.GetExpiration() <= now;

    [Pure]
    public static string GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email) ??
           throw new InvalidOperationException($"Email not found in claims of user {principal.GetName()}.");

    [Pure]
    public static string GetName(this ClaimsPrincipal principal)
        => principal.Identities.FirstOrDefault(i => i.Name is not null)?.Name ?? principal.FindFirstValue("name") ?? "(Anonymous)";

    [Pure]
    public static string GetShortName(this ClaimsPrincipal principal)
        => new(principal.GetName()
                         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => char.ToUpper(s[0]))
                         .ToArray());

    [Pure]
    public static string? GetPicture(this ClaimsPrincipal principal)
        => principal.FindFirstValue("picture");

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
        => principal.FindFirst(claimType)?.Value;

    [Pure]
    public static ClaimsIdentity GetFirebaseIdentity(this ClaimsPrincipal principal)
        => principal.Identities.First(i => i.AuthenticationType == FirebaseAuthentication.Scheme);

    [Pure]
    public static ClaimsIdentity? TryGetFirebaseIdentity(this ClaimsPrincipal principal)
        => principal.Identities.FirstOrDefault(i => i.AuthenticationType == FirebaseAuthentication.Scheme);
}