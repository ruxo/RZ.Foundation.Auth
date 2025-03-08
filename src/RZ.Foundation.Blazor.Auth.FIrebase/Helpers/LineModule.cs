using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RZ.Foundation.Helpers;

namespace RZ.Foundation.Blazor.Auth.Helpers;

public static class LineModule
{
    public static async Task<Outcome<OpenIdConnectMessage>> GetLineToken(this HttpClient http, TiraxTech.Uri issuer, string code, string redirectUri, string clientId, string clientSecret)
    {
        var oidc = await issuer.GetOidcWellKnownConfig();
        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "authorization_code"
        });

        try{
            using var response = await http.PostAsync(oidc.TokenEndpoint, request);
            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return new OpenIdConnectMessage(json);
            else
                return new ErrorInfo(StandardErrorCodes.HttpError, json, data: new { response.StatusCode });
        }
        catch (Exception e){
            return ErrorFrom.Exception(e);
        }
    }

    public static string CreateFirebaseLineToken(this FirebaseAuthService authService, string idToken) {
        var jwt = new JwtSecurityTokenHandler();
        var token = (JwtSecurityToken)jwt.ReadToken(idToken);
        var claims = token.Claims.ToList();

        using var rsa = RSA.Create();
        rsa.ImportFromPem(authService.ServiceAccount.PrivateKey);

        var signingKey = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var userInfo = from c in claims
                       where c.Type is not "uid"
                                   and not JwtRegisteredClaimNames.Sub
                                   and not JwtRegisteredClaimNames.Iss
                                   and not JwtRegisteredClaimNames.Amr
                                   and not JwtRegisteredClaimNames.Iat
                                   and not JwtRegisteredClaimNames.Exp
                                   and not JwtRegisteredClaimNames.Aud
                       select KeyValuePair.Create(c.Type, c.Value);

        var newJwtDescriptor = new SecurityTokenDescriptor {
            Issuer = authService.ServiceAccount.Email,
            Audience = "https://identitytoolkit.googleapis.com/google.identity.identitytoolkit.v1.IdentityToolkit",
            Claims = new Dictionary<string, object> {
                [JwtRegisteredClaimNames.Sub] = authService.ServiceAccount.Email,
                ["uid"] = claims.FindFirstValue(JwtRegisteredClaimNames.Sub)!,
                [JwtRegisteredClaimNames.Iat] = claims.First(c => c.Type == JwtRegisteredClaimNames.Iat).Value,
                [JwtRegisteredClaimNames.Exp] = claims.First(c => c.Type == JwtRegisteredClaimNames.Exp).Value,
                ["claims"] = userInfo.ToDictionary()
            },
            SigningCredentials = signingKey
        };
        var newJwt = jwt.CreateToken(newJwtDescriptor);
        return jwt.WriteToken(newJwt);
    }
}