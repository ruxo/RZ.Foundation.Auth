using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RZ.Foundation.Blazor.Auth.Helpers;
using RZ.Foundation.Helpers;
using Uri = TiraxTech.Uri;

namespace RZ.Foundation.Blazor.Auth;

public class FirebaseAuthService
{
    readonly ILogger<FirebaseAuthService> logger;
    readonly Uri issuer;
    readonly SecurityKey[] firebaseKeys;

    public FirebaseAuthService(ILogger<FirebaseAuthService> logger, IConfiguration configuration) {
        this.logger = logger;

        try{
            var firebaseConnectionString = configuration.GetConnectionString("Firebase")!;
            var kv = KeyValueString.Parse(firebaseConnectionString);

            Config = new FirebaseSdkConfig(kv["apiKey"], kv["authDomain"], kv["projectId"], kv["storageBucket"], kv["messagingSenderId"], kv["appId"], kv["measurementId"]);

            var fbAdmin = configuration.GetConnectionString("FirebaseAdmin")!;
            kv = KeyValueString.Parse(fbAdmin);
            ServiceAccount = new(kv["serviceAccount"], kv["privateKey"]);

            issuer = BaseIssuer.ChangePath(Config.projectId);

            firebaseKeys = GetFirebaseAuthSecurityKeys(issuer).ToArray();
        }
        catch (Exception e){
            logger.LogError(e, "Failed to initialize Firebase authentication service");
            throw;
        }
    }

    public FirebaseSdkConfig Config { get; }
    public ServiceAccountInfo ServiceAccount { get; }

    public readonly record struct ServiceAccountInfo(string Email, string PrivateKey);

    #region Encoding/Decoding

    readonly record struct SerializableClaim(string T, string V, string Y, string O);
    readonly record struct SerializableIdentity(IEnumerable<SerializableClaim> C, string? A);

    public static string EncodeSignIn(ClaimsPrincipal user) {
        var serializable = from identity in user.Identities
                           select new SerializableIdentity(from claim in identity.Claims
                                                           select new SerializableClaim(claim.Type, claim.Value, claim.ValueType, claim.OriginalIssuer),
                                                           identity.AuthenticationType);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(serializable));
        var compressed = DeflateCompress(bytes);
        var encrypt = LibEncryption.Encrypt(compressed);
        return Convert.ToBase64String(encrypt);
    }

    public static ClaimsPrincipal DecodeSignIn(string encoded) {
        var decoded = Encoding.UTF8.GetString(DeflateDecompress(LibEncryption.Decrypt(Convert.FromBase64String(encoded))));
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

    public static async ValueTask LoginSuccess(NavigationManager navManager, FirebaseJsInterop js, ClaimsPrincipal user, string? returnUrl) {
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

        var fbIdentity = new ClaimsIdentity([
            new("access_token", signInInfo.AccessToken),
            new("sign_in_provider", signInInfo.Type)
        ], FirebaseAuthentication.Scheme);
        if (signInInfo.RefToken is not null)
            fbIdentity.AddClaim(new Claim("ref_token", signInInfo.RefToken));

        return new ClaimsPrincipal([result.ClaimsIdentity, fbIdentity]);
    }

    static IEnumerable<SecurityKey> GetFirebaseAuthSecurityKeys(Uri issuer)
        => issuer.GetOidcWellKnownConfig().Result.SigningKeys;
}