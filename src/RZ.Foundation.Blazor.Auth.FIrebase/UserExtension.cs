using System.Security.Claims;
using JetBrains.Annotations;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace RZ.Foundation.Blazor.Auth;

[PublicAPI]
public static class UserExtension
{
    [Pure]
    public static ClaimsIdentity GetFirebaseIdentity(this ClaimsPrincipal principal)
        => principal.Identities.First(i => i.AuthenticationType == FirebaseAuthentication.Scheme);

    [Pure]
    public static ClaimsIdentity? TryGetFirebaseIdentity(this ClaimsPrincipal principal)
        => principal.Identities.FirstOrDefault(i => i.AuthenticationType == FirebaseAuthentication.Scheme);
}