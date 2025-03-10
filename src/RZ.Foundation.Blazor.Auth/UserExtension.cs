using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using JetBrains.Annotations;
using RZ.Foundation.Helpers;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;
using static LanguageExt.Prelude;

namespace RZ.Foundation.Blazor.Auth;

[PublicAPI]
public static class UserExtension
{
    [Pure]
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
        => principal.TryGetEmail() ?? throw new InvalidOperationException($"Email not found in claims of user {principal.GetName()}.");

    [Pure]
    public static string? TryGetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email);

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
    public static string? TryGetPicture(this ClaimsPrincipal principal)
        => principal.FindFirstValue("picture");

    [Pure]
    public static string? TryGetIdentityProviderId(this ClaimsPrincipal principal)
        => principal.Claims.FindValueByPriority(JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);

    [Pure]
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
        => Seq(user.Claims.Where(c => c.Type == RzClaimTypes.Permission)).Any(c => c.Value == permission);

    [Pure]
    public static bool HasPermissions(this ClaimsPrincipal user, params string[] permissions)
        => Seq(user.Claims.Where(c => c.Type == RzClaimTypes.Permission)).Any(p => permissions.Contains(p.Value));
}