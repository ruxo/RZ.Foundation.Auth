using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Web;
using LanguageExt;
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

            issuer = BaseIssuer.ChangePath(Config.projectId).Unwrap();

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

    public static Outcome<string> EncodeSignIn(ClaimsPrincipal user) {
        var serializable = from identity in user.Identities
                           select new SerializableIdentity(from claim in identity.Claims
                                                           select new SerializableClaim(claim.Type, claim.Value, claim.ValueType, claim.OriginalIssuer),
                                                           identity.AuthenticationType);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(serializable));
        var compressed = DeflateCompress(bytes);
        if (Fail(LibEncryption.Encrypt(compressed), out var e, out var encrypt)) return e.Trace();
        return Convert.ToBase64String(encrypt);
    }

    public static Outcome<ClaimsPrincipal> DecodeSignIn(string encoded) {
        if (Fail(LibEncryption.Decrypt(Convert.FromBase64String(encoded)), out var e, out var bytes)
         || Fail(TryCatch(() => Encoding.UTF8.GetString(DeflateDecompress(bytes))), out e, out var decoded)
         || Fail(JsonDeserialize<IEnumerable<SerializableIdentity>>(decoded), out e, out var serializable))
            return e.Trace();

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

    public static async ValueTask<Outcome<Unit>> LoginSuccess(NavigationManager navManager, FirebaseJsInterop js, ClaimsPrincipal user, string? returnUrl) {
        if (Fail(EncodeSignIn(user), out var e, out var encoded)) return e.Trace();

        await js.StoreAfterSignIn(encoded);
        var query = returnUrl is null ? string.Empty : $"?ReturnUrl={HttpUtility.UrlEncode(returnUrl)}";
        navManager.NavigateTo($"/auth/login/success{query}");
        return unit;
    }

    static readonly Uri BaseIssuer = Uri.From("https://securetoken.google.com").Unwrap();

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

        if (Fail(await TryCatch(tokenHandler.ValidateTokenAsync(signInInfo.AccessToken, validationParameters)), out var error, out var result)) {
            logger.LogError("Sign-in token validation failed! [{Token}] {@Error}", signInInfo.AccessToken, error);
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