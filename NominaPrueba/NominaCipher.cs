using System.Security.Cryptography;
using System.Text;

namespace NominaPrueba;

public sealed class NominaCipher
{
    private readonly byte[] key;

    public NominaCipher(string? configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey))
            throw new InvalidOperationException("Configura Encryption__Key con la clave del sistema de nomina.");
        // Preserve the client's DES key and IV normalization for existing .enc files.
        var normalized = configuredKey.Length < 8
            ? configuredKey.PadLeft(8, '0') : configuredKey[..8];
        key = Encoding.ASCII.GetBytes(normalized);
        using var des = CreateDes(); // Reject invalid/weak keys at startup.
    }

    private DES CreateDes()
    {
        var des = DES.Create();
        try
        {
            des.Key = key;
            des.IV = key;
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.PKCS7;
            return des;
        }
        catch
        {
            des.Dispose();
            throw new InvalidOperationException("Encryption__Key no es una clave DES valida.");
        }
    }

    public byte[] Encrypt(string content)
    {
        using var des = CreateDes();
        using var encryptor = des.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(content);
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    public string Decrypt(byte[] bytes)
    {
        using var des = CreateDes();
        using var decryptor = des.CreateDecryptor();
        var plaintext = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        using var reader = new StreamReader(new MemoryStream(plaintext));
        return reader.ReadToEnd();
    }
}
