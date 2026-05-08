using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LegalAgreementController : Controller
    {
        private readonly ILegalAgreementService _legalAgreementService;

        public LegalAgreementController(ILegalAgreementService legalAgreementService)
        {
            _legalAgreementService = legalAgreementService;
        }

        /// <summary>
        /// Kayıt ekranında gösterilecek aktif sözleşmeleri döndürür.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("active")]
        public IActionResult GetActiveAgreements()
        {
            var result = _legalAgreementService.GetActiveAgreements();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Giriş yapmış kullanıcının onaylaması gereken major versiyon sözleşmesi var mı?
        /// Frontend, her token yenilemede veya sayfa yüklemede bunu sorgular.
        /// </summary>
        [Authorize]
        [HttpGet("has-pending")]
        public IActionResult HasPending()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = _legalAgreementService.HasPendingMajorAgreement(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Kullanıcı bir sözleşmeyi onaylar. IP adresi KVKK rıza kaydı için saklanır.
        /// </summary>
        [Authorize]
        [HttpPost("accept")]
        public IActionResult Accept([FromQuery] int agreementId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = _legalAgreementService.Accept(userId, agreementId, ip);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
