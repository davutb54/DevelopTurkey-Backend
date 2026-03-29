using Core.Utilities.Results;
using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface ICaptchaService
    {
        Task<IDataResult<bool>> VerifyCaptchaAsync(string captchaToken);
    }
}
