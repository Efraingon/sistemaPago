using System.Security.Cryptography;

namespace Monrys.Api.Services;

public interface IPasswordHasherService
{
    /// <summary>Genera el hash con salt aleatorio. Formato: PBKDF2|iteraciones|saltBase64|hashBase64.</summary>
    string Hashear(string clave);

    /// <summary>Valida una clave contra un hash almacenado (compatible con el seed de 02_seed.sql).</summary>
    bool Verificar(string clave, string hashAlmacenado);
}

/// <summary>PBKDF2-SHA256 con 100.000 iteraciones y salt de 16 bytes.</summary>
public sealed class PasswordHasherService : IPasswordHasherService
{
    public const int Iteraciones = 100_000;
    public const int TamanioSalt = 16;
    public const int TamanioHash = 32;

    public string Hashear(string clave)
    {
        if (string.IsNullOrEmpty(clave))
            throw new ValidacionException("La clave no puede estar vacía.");

        var salt = RandomNumberGenerator.GetBytes(TamanioSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(clave, salt, Iteraciones, HashAlgorithmName.SHA256, TamanioHash);
        return string.Join('|', "PBKDF2", Iteraciones, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verificar(string clave, string hashAlmacenado)
    {
        if (string.IsNullOrWhiteSpace(hashAlmacenado)) return false;

        var partes = hashAlmacenado.Split('|');
        if (partes.Length != 4 || !partes[0].Equals("PBKDF2", StringComparison.OrdinalIgnoreCase)) return false;
        if (!int.TryParse(partes[1], out var iteraciones) || iteraciones <= 0) return false;

        byte[] salt, hashEsperado;
        try
        {
            salt = Convert.FromBase64String(partes[2]);
            hashEsperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || hashEsperado.Length == 0) return false;

        var hashObtenido = Rfc2898DeriveBytes.Pbkdf2(clave, salt, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);
        return CryptographicOperations.FixedTimeEquals(hashObtenido, hashEsperado);
    }
}
