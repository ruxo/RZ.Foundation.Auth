using System.Text;
using RZ.Foundation.Blazor.Auth.Helpers;

namespace RZ.Foundation.Blazor.Auth.Views.Line;

public static class LineUtils
{
    public static Outcome<string> EncodeState(string clientId, string returnUrl, DateTimeOffset now) {
        if (Fail(LibEncryption.Encrypt(Encoding.ASCII.GetBytes($"{clientId}:{now.UtcTicks}:{returnUrl}")), out var e, out var encrypted))
            return e.Trace();
        return Convert.ToBase64String(encrypted);
    }

    public readonly record struct State(string ClientId, string ReturnUrl, DateTimeOffset Timestamp);

    public static Outcome<State> DecodeState(string state) {
        if (Fail(TryCatch(() => Convert.FromBase64String(state)), out var e, out var bytes)) return e.Trace("Base64");
        if (Fail(LibEncryption.Decrypt(bytes), out e, out var decrypted)) return e.Trace("Decryption");

        var str = Encoding.ASCII.GetString(decrypted);
        var parts = str.Split(':');
        if (parts.Length != 3) return ErrorInfo._NotFound();
        return new State(parts[0], parts[2], new DateTimeOffset(long.Parse(parts[1]), TimeSpan.Zero));
    }
}