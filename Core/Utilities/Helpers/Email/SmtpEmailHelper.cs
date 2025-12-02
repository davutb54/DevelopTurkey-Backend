using System.Net;
using System.Net.Mail;
using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;

namespace Core.Utilities.Helpers.Email;

public class SmtpEmailHelper : IEmailHelper
{
    private readonly IConfiguration _configuration;
    private readonly EmailOptions _emailOptions;

    public SmtpEmailHelper(IConfiguration configuration)
    {
        _configuration = configuration;
        _emailOptions = _configuration.GetSection("EmailOptions").Get<EmailOptions>();
    }

    public IResult Send(string to, string subject, string body)
    {
        try
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            using (var client = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.SmtpPort))
            {
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_emailOptions.SenderEmail, _emailOptions.SenderPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailOptions.SenderEmail, _emailOptions.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true 
                };

                mailMessage.To.Add(to);
                client.Send(mailMessage);
            }
            return new SuccessResult("E-posta başarıyla gönderildi.");
        }
        catch (Exception ex)
        {
            return new ErrorResult("E-posta gönderilemedi: " + ex.Message);
        }
    }
}