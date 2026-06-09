using Core.Utilities.Hashing;
using Microsoft.Extensions.Configuration;
using Sqids;

namespace Business.Concrete;

public class HashidsService : IHashidsService
{
    private readonly SqidsEncoder<int> _encoder;

    public HashidsService(IConfiguration configuration)
    {
        var alphabet = configuration["Sqids:Alphabet"]
                       ?? "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var minLength = int.TryParse(configuration["Sqids:MinLength"], out var ml) ? ml : 8;

        _encoder = new SqidsEncoder<int>(new SqidsOptions
        {
            Alphabet  = alphabet,
            MinLength = minLength,
        });
    }

    public string Encode(int id) => _encoder.Encode(id);

    public int? Decode(string publicId)
    {
        try
        {
            var numbers = _encoder.Decode(publicId);
            return numbers.Count == 1 ? numbers[0] : null;
        }
        catch
        {
            return null;
        }
    }
}
