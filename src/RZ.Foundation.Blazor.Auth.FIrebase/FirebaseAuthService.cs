using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RZ.AspNet.Helper;
using RZ.Foundation.Helpers;
using Uri = TiraxTech.Uri;

namespace RZ.Foundation.Blazor.Auth;

public class FirebaseAuthService
{
    readonly ILogger<FirebaseAuthService> logger;
    readonly Uri issuer;
    readonly SecurityKey[] firebaseKeys;

    static readonly Aes Aes = Encryption.CreateAes(Encryption.RandomAesKey(), Encryption.NonceFromASCII("RZ Auth's nonce ja"));

    public FirebaseAuthService(ILogger<FirebaseAuthService> logger, IConfiguration configuration) {
        this.logger = logger;
        var firebaseConnectionString = configuration.GetConnectionString("Firebase") ?? logger.FailWith<string>("Missing Firebase connection string");
        var kv = KeyValueString.Parse(firebaseConnectionString);

        Config = new FirebaseSdkConfig(kv["apiKey"], kv["authDomain"], kv["projectId"], kv["storageBucket"], kv["messagingSenderId"], kv["appId"], kv["measurementId"]);

        issuer = BaseIssuer.ChangePath(Config.projectId);
        var (error, keys) = Try(issuer, GetFirebaseAuthSecurityKeys);
        if (error is not null)
            logger.FailWith<string>($"Cannot get security keys from Firebase: {error}");

        firebaseKeys = keys.ToArray();
    }

    public FirebaseSdkConfig Config { get; }

    #region Encoding/Decoding

    readonly record struct SerializableClaim(string T, string V, string Y, string O);
    readonly record struct SerializableIdentity(IEnumerable<SerializableClaim> C, string? A);

    public string EncodeSignIn(ClaimsPrincipal user) {
        var serializable = from identity in user.Identities
                           select new SerializableIdentity(from claim in identity.Claims
                                                           select new SerializableClaim(claim.Type, claim.Value, claim.ValueType, claim.OriginalIssuer),
                                                           identity.AuthenticationType);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(serializable));
        var compressed = DeflateCompress(bytes);
        var encrypt = Aes.Encrypt(compressed);
        return Convert.ToBase64String(encrypt);
    }

    public ClaimsPrincipal DecodeSignIn(string encoded) {
        var decoded = Encoding.UTF8.GetString(DeflateDecompress(Aes.Decrypt(Convert.FromBase64String(encoded))));
        var serializable = JsonSerializer.Deserialize<IEnumerable<SerializableIdentity>>(decoded);
        return new ClaimsPrincipal(from identity in serializable
                                   select new ClaimsIdentity(from claim in identity.C
                                                             select new Claim(claim.T, claim.V, claim.Y, claim.O),
                                                             identity.A));
    }

    static byte[] DeflateCompress(byte[] data) {
        using var output = new MemoryStream();
        using var deflate = new DeflateStream(output, CompressionLevel.SmallestSize);
        deflate.Write(data, 0, data.Length);
        deflate.Flush();
        return output.ToArray();
    }

    static byte[] DeflateDecompress(byte[] data) {
        using var input = new MemoryStream(data);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    #endregion

    public async ValueTask LoginSuccess(NavigationManager navManager, FirebaseJsInterop js, ClaimsPrincipal user, string? returnUrl) {
        var encoded = EncodeSignIn(user);
        await js.StoreAfterSignIn(encoded);
        var query = returnUrl is null ? string.Empty : $"?ReturnUrl={HttpUtility.UrlEncode(returnUrl)}";
        navManager.NavigateTo($"/auth/login/success{query}");
    }

    static readonly Uri BaseIssuer = Uri.From("https://securetoken.google.com");

    /// <summary>
    /// Validate token and simply return null if token is invalid. This method doesn't throw.
    /// </summary>
    /// <param name="signInInfo"></param>
    /// <returns></returns>
    public async Task<ClaimsPrincipal?> Validate(SignInInfo signInInfo) {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters {
            ValidIssuer = issuer.ToString(),
            ValidAudience = Config.projectId,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            IssuerSigningKeys = firebaseKeys
        };

        var (error, result) = await Try(tokenHandler.ValidateTokenAsync(signInInfo.AccessToken, validationParameters));
        if (error is not null) {
            logger.LogError(error, "Sign-in token validation failed! [{Token}]", signInInfo.AccessToken);
            return null;
        }

        var fbIdentity = new ClaimsIdentity([new Claim("access_token", signInInfo.AccessToken)], FirebaseAuthentication.Scheme);
        if (signInInfo.RefToken is not null)
            fbIdentity.AddClaim(new Claim("ref_token", signInInfo.RefToken));

        return new ClaimsPrincipal([result.ClaimsIdentity, fbIdentity]);
    }

    static IEnumerable<SecurityKey> GetFirebaseAuthSecurityKeys(Uri issuer) {
        var cm = new ConfigurationManager<OpenIdConnectConfiguration>(issuer.ChangePath(".well-known/openid-configuration").ToString(),
                                                                      new OpenIdConnectConfigurationRetriever());
        var openIdConfig = cm.GetConfigurationAsync().Result;
        return openIdConfig.SigningKeys;
    }
}