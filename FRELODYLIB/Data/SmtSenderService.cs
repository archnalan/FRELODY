using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Models.SubModels;
using System.Text;

namespace FRELODYAPP.Data
{
    public class SmtpSenderService
    {
        private readonly IEmailService _emailService;
        private IConfiguration _configuration;
        private readonly UserManager<User> _userManager;

        public SmtpSenderService(IEmailService emailService, IConfiguration configuration, UserManager<User> userManager)
        {
            _emailService = emailService;
            _configuration = configuration;
            _userManager = userManager;
        }

        //public IActionResult SubmitForm(string content)
        //{
        //    var allowedDomains = _configuration.GetSection("AllowedOrigins").Get<string[]>();

        //    var requestUrl = HttpContext.Request.HttpContext.Request.Headers.Origin;
        //    if (allowedDomains.Where(x => x.Contains(requestUrl.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).Count() == 0)
        //    {
        //        //not authorized domain
        //        return Unauthorized("Not authorised");
        //    }

        //    //base 64 decode info

        //    var clientJson = Base64Decode(content);
        //    if (string.IsNullOrEmpty(clientJson)) return BadRequest("Invalid input data");


        //    //send email

        //    var emailDto = JsonConvert.DeserializeObject<EmailDto>(clientJson);

        //    var emailSenderCredentials = _configuration.GetSection($"SmtpCredentials:{requestUrl}");
        //    emailDto.emailSenderAccount = emailSenderCredentials?["emailSenderAccount"].ToString();
        //    emailDto.emailSenderSecret = emailSenderCredentials?["emailSenderSecret"].ToString();
        //    emailDto.CompanyName = emailSenderCredentials?["CompanyName"].ToString();
        //    //emailDto.ReplyToEmail = emailSenderCredentials?["ReplyToEmail"].ToString();
        //    emailDto.websiteLink = requestUrl.ToString();
        //    emailDto.ToEmail = emailSenderCredentials?["ToEmail"].ToString();

        //    //this should be updated last 
        //    emailDto.Body = EmailTemplates.EmailTemplates.WebsiteForm(emailDto);

        //    SendMail(emailDto);
        //    return Ok();
        //}

        public void SendMail(EmailDto emailDto)
        {
            try
            {
                emailDto.emailSenderSecret = emailDto.emailSenderSecret ?? "123qwe*";
                emailDto.emailSenderAccount = emailDto.emailSenderAccount ?? "archnalan@gmail.com";
                _emailService.SendEmail(emailDto);
            }
            catch (Exception ex)
            {
                //log error
            }


        }

        public void SendDeveloperNotificationEmail(string message)
        {
            try
            {
                var emailDto = new EmailDto(true);
                emailDto.ToEmail = "archnalan@gmail.com";
                emailDto.Body = message;
                emailDto.Subject = "Error your app. Please check it out";
                _emailService.SendEmail(emailDto);
            }
            catch (Exception ex)
            {
                //log error
            }


        }

        public async Task sendPasswordResetEmail(User appuser, string requestorUri)
        {

            //send confrmation email for the new email
            var code = await _userManager.GeneratePasswordResetTokenAsync(appuser);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));


            //CHANGE THIS TO PRODUCTION LINK
            var callbackUrl = $"{requestorUri ?? "https://app.billtrick.com"}/resetpassword/{code}/{"isLocal"}";

            var emailDto = new EmailDto(true);
            emailDto.Subject = "Reset Your Password";
            emailDto.ToEmail = appuser.Email;
            emailDto.Body = $"You have requested to reset your password with Billtrick POS.  <br/><br/>If you want to rest your password, please click this link.<br/><br/> {callbackUrl}<br/><br/> Thank you for being part of Billtrick POS. ";

            SendMail(emailDto);

        }
        //[HttpGet]
        //public async Task<IActionResult> sendPasswordResetEmail(string email)
        //{
        //    AppUser appuser = await _userManager.FindByEmailAsync(email);
        //    if (appuser == null)
        //    {
        //        return Ok("Email does not exist");
        //     }
        //    //send confrmation email for the new email
        //    var code = await _userManager.GeneratePasswordResetTokenAsync(appuser);
        //    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));


        //    //CHANGE THIS TO PRODUCTION LINK
        //    var callbackUrl = $"{ "https://social.frelody.com"}/resetpassword/{code}";

        //    var emailDto = new EmailDto(true);
        //    emailDto.Subject = "Reset Your Password";
        //    emailDto.ToEmail = appuser.Email;
        //    emailDto.Body = $"You have requested to reset your password with SDA Kampala Central church.  <br/><br/>If you want to rest your password, please click this link.<br/><br/> {callbackUrl}<br/><br/> Thank you for being part of SDA Kampala Central church. ";

        //    SendMail(emailDto);
        //    return Ok("Email confirmation sent");
        //}



        private static string Base64Decode(string base64EncodedData)
        {
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (Exception ex)
            {
                return null;
            }

        }
    }
}
