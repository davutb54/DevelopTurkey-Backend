using Business.Abstract;
using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Concrete
{
    public class CaptchaManager : ICaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public CaptchaManager(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<IDataResult<bool>> VerifyCaptchaAsync(string captchaToken)
        {
            if (string.IsNullOrWhiteSpace(captchaToken))
            {
                return new ErrorDataResult<bool>(false, "Güvenlik doğrulaması (Captcha) eksik.");
            }

            var secretKey = _configuration["CaptchaOptions:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                // Config'te key yoksa şimdilik dev ortamında true dönebiliriz ya da loglayıp geçebiliriz
                // Ama güvenlik gereği false dönüyoruz
                return new ErrorDataResult<bool>(false, "Captcha ayarları eksik. Lütfen sistem yöneticisiyle iletişime geçin.");
            }

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secretKey),
                new KeyValuePair<string, string>("response", captchaToken)
            });

            // Cloudflare Turnstile API Noktası
            // İleride Google reCAPTCHA v3 istenirse: https://www.google.com/recaptcha/api/siteverify
            var response = await _httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", requestContent);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseContent);
                var success = document.RootElement.GetProperty("success").GetBoolean();

                if (success)
                {
                    return new SuccessDataResult<bool>(true, "Bot doğrulaması başarılı.");
                }
            }

            return new ErrorDataResult<bool>(false, "Bot koruması aşılamadı. Lütfen tekrar deneyin.");
        }
    }
}
