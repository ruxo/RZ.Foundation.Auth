using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Uri = TiraxTech.Uri;

namespace RZ.Foundation.Blazor.Auth.Helpers;

public static class OidcUtils
{
    public static Task<OpenIdConnectConfiguration> GetOidcWellKnownConfig(this Uri issuer, CancellationToken cancelToken = default)
        => new ConfigurationManager<OpenIdConnectConfiguration>(issuer.ChangePath(".well-known/openid-configuration").ToString(),
                                                                new OpenIdConnectConfigurationRetriever())
           .GetConfigurationAsync(cancelToken);
}