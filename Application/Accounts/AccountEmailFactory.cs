using System.Text.RegularExpressions;
using static Snipster.Helpers.GeneralHelpers;

namespace Snipster.Application.Accounts;

public static class AccountEmailFactory
{
    public static EmailSendingClass CreateRegistrationEmail(string email, string name, string token)
    {
        var url = BuildUrl("validate-registration", token);
        var htmlContent = Regex.Replace(RegistrationEmailTemplate, "<url>", url);
        htmlContent = Regex.Replace(htmlContent, "<Name>", name);

        return new EmailSendingClass
        {
            htmlContent = htmlContent,
            PlainTextContent = $"Dear {name}, confirm your registration on Snipster.com by opening this link: {url}. If you did not request this, please ignore this email.",
            To = email,
            Subject = "Confirm your registration on Snipster.com"
        };
    }

    public static EmailSendingClass CreatePasswordResetEmail(string email, string name, string token)
    {
        var resetUrl = BuildUrl("pw-reset", token);
        var htmlContent = Regex.Replace(ResetEmailTemplate, "<resetUrl>", resetUrl);
        htmlContent = Regex.Replace(htmlContent, "<Name>", name);

        return new EmailSendingClass
        {
            htmlContent = htmlContent,
            PlainTextContent = $"Dear {name}, reset your Snipster.com password by opening this link: {resetUrl}. If you did not request this, please ignore this email. This link expires in 1 hour.",
            To = email,
            Subject = "Reset Your Password in Snipster.com"
        };
    }

    private static string BuildUrl(string path, string token)
    {
        var baseUrl = Environment.GetEnvironmentVariable("Environment") == "Production"
            ? "https://snipster.co"
            : "https://localhost:7225";

        var encodedToken = Uri.EscapeDataString(token);
        return $"{baseUrl}/{path}?token={encodedToken}";
    }

    private const string RegistrationEmailTemplate = @"
                <!DOCTYPE html> <html> <head> <style> p { margin: 0;} OL { list-style-type: decimal; } OL OL  {list-style-type: upper-roman;} UL  {list-style-type: disc;} UL UL  {list-style-type: square;} .cal {font: 15px Calibri;} </style> </head><body>
                <body>
                <div><p>Dear <Name>, </p> <p> <o:p>&nbsp;</o:p></p>
                <p>To confirm your registration on Snipster.com, please click on this <a href='<url>'>link</a> </p> <p><o:p>&nbsp;</o:p></p>

                <p>If you cannot find this email later, please check your spam or junk folder.</p> <p><o:p>&nbsp;</o:p></p>

                <p>If you didn’t request this, please ignore this email.</p> <p><o:p>&nbsp;</o:p></p>

                <p>Best regards,</p> 
                <p>Snipster Team</p><p><o:p>&nbsp;</o:p></p>
                </body>
                ";

    private const string ResetEmailTemplate = @"
                <!DOCTYPE html> <html> <head> <style> p { margin: 0;} OL { list-style-type: decimal; } OL OL  {list-style-type: upper-roman;} UL  {list-style-type: disc;} UL UL  {list-style-type: square;} .cal {font: 15px Calibri;} </style> </head><body>
                <body>
                <div><p>Dear <Name>, </p> <p> <o:p>&nbsp;</o:p></p>
                <p>You received this email because you requested a password reset.</p> <p><o:p>&nbsp;</o:p></p>
                <p>Click <a href='<resetUrl>'>here</a> to reset your password.</p> <p><o:p>&nbsp;</o:p></p>
                <p>If you cannot find this email later, please check your spam or junk folder.</p> <p><o:p>&nbsp;</o:p></p>
                <p>If you didn’t request this, please ignore this email. This link will expire in 1 hour.</p> <p><o:p>&nbsp;</o:p></p>

                <p>Best regards,</p> 
                <p>Snipster Team</p><p><o:p>&nbsp;</o:p></p>
                </body>
                ";
}
