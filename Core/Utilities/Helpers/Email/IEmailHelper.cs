using Core.Utilities.Results;

namespace Core.Utilities.Helpers.Email;

public interface IEmailHelper
{
    IResult Send(string to, string subject, string body);
}