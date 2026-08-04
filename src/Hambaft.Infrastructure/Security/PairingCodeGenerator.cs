using System.Security.Cryptography;
using Hambaft.Application;

namespace Hambaft.Infrastructure.Security;

public sealed class PairingCodeGenerator : IPairingCodeGenerator
{
    private const string Alphabet="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Iterations=120_000;
    public PairingCodeMaterial Generate()
    {
        var bytes=RandomNumberGenerator.GetBytes(8);
        var raw=string.Create(8,bytes,(span,state)=>{for(var i=0;i<span.Length;i++)span[i]=Alphabet[state[i]%Alphabet.Length];});
        var salt=RandomNumberGenerator.GetBytes(16); var digest=Rfc2898DeriveBytes.Pbkdf2(raw,salt,Iterations,HashAlgorithmName.SHA256,32);
        return new(raw,$"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(digest)}");
    }
    public bool Verify(string rawCode,string persistedHash)
    {
        var parts=persistedHash.Split('$');
        if(parts.Length!=4||parts[0]!="pbkdf2-sha256"||!int.TryParse(parts[1],out var iterations))return false;
        try { var salt=Convert.FromBase64String(parts[2]);var expected=Convert.FromBase64String(parts[3]);var actual=Rfc2898DeriveBytes.Pbkdf2(rawCode,salt,iterations,HashAlgorithmName.SHA256,expected.Length);return CryptographicOperations.FixedTimeEquals(actual,expected); }
        catch(FormatException){return false;}
    }
}
