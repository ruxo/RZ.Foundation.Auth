using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using JetBrains.Annotations;
using RZ.Foundation.Blazor.Auth;
using RZ.Foundation.Helpers;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace RZ.Foundation.Auth;

[PublicAPI]
public static class UserExtension
{
    extension(ClaimsPrincipal principal)
    {
        [Pure, PublicAPI]
        public bool IsAuthenticated()
            => principal.Identities.Any(i => i.IsAuthenticated);

        [Pure, PublicAPI]
        public DateTimeOffset? GetExpiration()
            => principal.GetDateTime(JwtRegisteredClaimNames.Exp);

        [Pure, PublicAPI]
        public string? Get(string claimType)
            => principal.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

        [Pure, PublicAPI]
        public DateTimeOffset? GetDateTime(string key)
            => principal.Get(key) is { } value ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(value)) : null;

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExpired(DateTimeOffset now)
            => principal.GetExpiration() <= now;

        [Pure, PublicAPI]
        public string? GetEmail()
            => principal.Get(ClaimTypes.Email);

        [Pure, PublicAPI]
        public string? GetName()
            => principal.Identities.FirstOrDefault(i => i.Name is not null)?.Name ?? principal.Get("name");

        [Pure, PublicAPI]
        public string? GetShortName()
            => principal.GetName() is { } fullName
                   ? new string(fullName
                               .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => char.ToUpper(s[0]))
                               .ToArray())
                   : null;

        [Pure, PublicAPI]
        public string? GetIdentityProviderId()
            => principal.Claims.FindValueByPriority(JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);

        [Pure, PublicAPI]
        public bool HasPermission(string permission)
            => principal.Claims.Where(c => c.Type == RzClaimTypes.Permission).Any(c => c.Value == permission);

        [Pure, PublicAPI]
        public bool HasPermissions(params string[] permissions)
            => principal.Claims.Where(c => c.Type == RzClaimTypes.Permission).Any(p => permissions.Contains(p.Value));
    }
}