using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace RZ.Foundation.Blazor.Auth.Helpers;

public static class LibEncryption
{
    public static readonly Aes Aes = Encryption.CreateAes(Encryption.RandomAesKey(), Encryption.NonceFromASCII("RZ Auth's nonce ja"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Encrypt(byte[] data)
        => Aes.Encrypt(data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decrypt(byte[] data)
        => Aes.Decrypt(data);
}