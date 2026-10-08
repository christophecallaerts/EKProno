using System.Buffers.Text;
using System.Security.Cryptography;

namespace EKProno.Services;

public interface IJoinTokenGenerator
{
    string Generate();
}

/// <summary>
/// Produces URL-safe tokens of 160 random bits, comfortably over the 128 bits spec 001
/// §5.3 asks for, from a cryptographically secure source (NFR-004).
/// </summary>
public sealed class JoinTokenGenerator : IJoinTokenGenerator
{
    private const int TokenBytes = 20;

    public string Generate() =>
        Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
}
