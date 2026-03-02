namespace Core.Utilities.Context;

public interface IClientContext
{
    string GetIpAddress();
    string GetPort();
    int? GetUserId();
    string GetUserName();
}