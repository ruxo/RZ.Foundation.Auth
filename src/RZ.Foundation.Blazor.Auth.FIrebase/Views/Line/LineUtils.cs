using System.Text;
using RZ.Foundation.Blazor.Auth.Helpers;

namespace RZ.Foundation.Blazor.Auth.Views.Line;

public static class LineUtils
{
    public static string EncodeState(string clientId, string returnUrl, DateTimeOffset now)
        => Convert.ToBase64String(LibEncryption.Encrypt(Encoding.ASCII.GetBytes($"{clientId}:{now.UtcTicks}:{returnUrl}")));

    public readonly record struct State(string ClientId, string ReturnUrl, DateTimeOffset Timestamp);

    public static State? DecodeState(string state) {
        try{
            var bytes = Convert.FromBase64String(state);
            var str = Encoding.ASCII.GetString(LibEncryption.Decrypt(bytes));
            var parts = str.Split(':');
            if (parts.Length != 3) return null;
            return new State(parts[0], parts[2], new DateTimeOffset(long.Parse(parts[1]), TimeSpan.Zero));
        }
        catch (Exception){
            return null;
        }
    }
}