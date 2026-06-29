using System.Runtime.CompilerServices;
using RZ.Foundation.Helpers;

namespace RZ.Foundation.Blazor.Auth.Helpers;

public static class LibEncryption
{
    static readonly byte[] PassKey = Encryption.RandomAesKey();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Outcome<byte[]> Encrypt(byte[] data)
        => Encryption.Encrypt(PassKey, data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Outcome<byte[]> Decrypt(byte[] data)
        => Encryption.Decrypt(PassKey, data);
}