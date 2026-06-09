namespace Core.Utilities.Hashing;

public interface IHashidsService
{
    string Encode(int id);
    int? Decode(string publicId);
}
