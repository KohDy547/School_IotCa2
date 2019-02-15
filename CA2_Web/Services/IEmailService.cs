using CA2_Web.Configurations;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace CA2_Assignment.Services
{
    public class EmailService : IEmailSender
    {
        private readonly SendGridConfigurations _SendGridConfigurations;
        public EmailService(IOptions<SendGridConfigurations> SendGridConfigurations)
        {
            _SendGridConfigurations = SendGridConfigurations.Value;
        }

        public Task SendEmailAsync(string inputEmail, string inputSubject, string inputHtmlMessage)
        {
            SendGridClient client = new SendGridClient(_SendGridConfigurations.ClientKey);
            SendGridMessage message = new SendGridMessage()
            {
                From = new EmailAddress(
                    _SendGridConfigurations.ServerEmail,
                    _SendGridConfigurations.ServerEmailName),
                Subject = inputSubject,
                PlainTextContent = inputHtmlMessage,
                HtmlContent = inputHtmlMessage
            };
            message.AddTo(new EmailAddress(inputEmail));
            message.SetClickTracking(false, false);

            return client.SendEmailAsync(message);
        }
    }
}