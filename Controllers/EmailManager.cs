using LeafLINQWeb.Models;
using Azure.Communication.Email;
using Azure;

namespace LeafLINQWeb.Controllers;

public class EmailManager
{
    private IConfiguration _config;

    public EmailManager(IConfiguration config)
    {
        _config = config;
    }



    public bool sendEmailUsingAzure(EmailModel emailModel)
    {

        //var connectionString = _config["ConnectionStrings:AzureStorageConnectionString"]; 
        var connectionString = _config["ConnectionStrings:COMMUNICATION_SERVICES_CONNECTION_STRING"];
        EmailClient emailClient = new EmailClient(connectionString);

        try
        {
            var emailSendOperation = emailClient.Send(
                wait: WaitUntil.Completed,
                senderAddress: emailModel.From,

                recipientAddress: emailModel.To[0],
        
                subject: emailModel.Subject,
                htmlContent: emailModel.Body);

            Console.WriteLine($"Email Sent. Status = {emailSendOperation.Value.Status}");

            /// Get the OperationId so that it can be used for tracking the message for troubleshooting
            string operationId = emailSendOperation.Id;
            Console.WriteLine($"Email operation id = {operationId}");
        }
        catch (RequestFailedException ex)
        {
            /// OperationID is contained in the exception message and can be used for troubleshooting purposes
            Console.WriteLine($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
        }

        return true;
    }
        
}

// Old code for sending to gmail the old fashioned way. Doesn't work anymore. 
// Leave for posterity.
//public void sendEmail_V1(UserModel userModel)
//{

//    EmailModel emailModel = GetEmailFields(userModel);

//    MailMessage mail = new MailMessage();

//    mail.To.Add(emailModel.To);
//    mail.From = new MailAddress(emailModel.From);
//    mail.Subject = emailModel.Subject;
//    mail.Body = emailModel.Body;
//    mail.IsBodyHtml = true;

//    SmtpClient smtp = new SmtpClient();

//    smtp.Host = "smtp.gmail.com";
//    smtp.Port = 587;
//    smtp.UseDefaultCredentials = false;

//    // Enter senders User name and password  
//    smtp.Credentials = new System.Net.NetworkCredential("leaflink@gmail.com", "eekegqyqxjkqezju");
//    //smtp.Credentials = new System.Net.NetworkCredential
//    smtp.EnableSsl = true;

//    try
//    {
//        smtp.Send(mail);
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine(ex.ToString());
//    }

//}
// Old code for sending to mailslurp. works but using Azure now. 
// Leave for posterity and/or we might require it at somepoint in the future.
//using mailslurp.Api;
//using mailslurp.Client;
//using mailslurp.Model;
//public bool sendEmailUsingSlurp(EmailModel emailModel)
//{

//    // Get slurp email key from appsettings.json
//    string key = _config.GetSection("emailKey").Value;

//    // configure client
//    var config = new Configuration();

//    // create config with x-api-key
//    config.ApiKey.Add("x-api-key", key);

//    // create new controller instance
//    var inboxControllerApi = new InboxControllerApi(config); //var inbox = inboxControllerApi.CreateInboxWithDefaults();

//    // create virtual inbox for our temp email account to send from
//    var inbox = inboxControllerApi.CreateInboxWithOptions(new CreateInboxDto()
//    {
//        Name = "LeafLinq email host",
//        InboxType = CreateInboxDto.InboxTypeEnum.SMTPINBOX,
//    });

//    try
//    {
//        inboxControllerApi.SendEmail(inbox.Id, new SendEmailOptions()
//        {
//            To = emailModel.To,
//            Subject = emailModel.Subject,
//            Body = emailModel.Body,
//        });

//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine(ex);
//        return false;
//    }

//    // wait for email to arrive if your sending to the inbox (not in our case)
//    // inbox is temporally created above.
//    // Use this to confirm receipt of email to our inbox.
//    // Might be handy for a later project. 
//    //var waitController = new WaitForControllerApi(config);
//    //waitController.WaitForLatestEmail(inboxId: inbox.Id, timeout: 30_000);

//    return true;
//}