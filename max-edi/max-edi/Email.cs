using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Configuration;

namespace max_edi
{
    public class Email
    {
        public static int CountFilesPO { get; set; }
        public static int CountFilesEDI { get; set; }

        public static void Send(ExcelGenerator generator)
        {
            DateTime now = DateTime.Now;
            string now_str = now.ToString("yyyy-MM-dd");
            string emailFrom = ConfigurationManager.AppSettings["EmailFrom"];
            string emailsTo = ConfigurationManager.AppSettings["EmailsTo"];
            string emailHost = ConfigurationManager.AppSettings["EmailHost"];
            string emailSubject = ConfigurationManager.AppSettings["EmailSubject"];
            string emailBodyFile = ConfigurationManager.AppSettings["EmailBodyFile"];
            string emailBodyEmpty = ConfigurationManager.AppSettings["EmailBodyEmpty"];
            string EmailBodyNoFilePO = ConfigurationManager.AppSettings["EmailBodyNoFilePO"];
            string EmailBodyNoFileEDI = ConfigurationManager.AppSettings["EmailBodyNoFileEDI"];
            string EmailBodyNoFilePOEDI = ConfigurationManager.AppSettings["EmailBodyNoFilePOEDI"];
            string EmailBodyNoFilePOEDIWithData = ConfigurationManager.AppSettings["EmailBodyNoFilePOEDIWithData"];

            string credentialUser = ConfigurationManager.AppSettings["EmailCredentialUser"];
            string credentialPassword = ConfigurationManager.AppSettings["EmailCredentialPassword"];

            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(emailFrom);
                foreach(string address in emailsTo.Split(","))
                {
                    message.To.Add(new MailAddress(address));
                }
                
                message.Subject = emailSubject + " " + now_str;
                message.IsBodyHtml = true; //to make message body as html 
                emailBodyEmpty = emailBodyEmpty.Replace("@", "<br />");
                message.Body = emailBodyEmpty;

                Attachment attachment;
                if (File.Exists(generator.ExcelFindingsFilePath))
                {
                    // Valida que haya procesado registros nuevos 
                    // ------------------------------------------
                    if (CountFilesPO == 0 || CountFilesEDI == 0)
                    {
                        EmailBodyNoFilePOEDIWithData = EmailBodyNoFilePOEDIWithData.Replace("@", "<br />");
                        message.Body = EmailBodyNoFilePOEDIWithData;
                        attachment = new Attachment(generator.ExcelFindingsFilePath);
                        message.Attachments.Add(attachment);
                    }
                    else
                    {
                        emailBodyFile = emailBodyFile.Replace("@", "<br />");
                        message.Body = emailBodyFile;
                        attachment = new Attachment(generator.ExcelFindingsFilePath);
                        message.Attachments.Add(attachment);
                    }
                }
                else
                {
                    // si no encuentra archivos recived
                    // --------------------------------
                    if(CountFilesPO == 0)
                    {
                        EmailBodyNoFilePO = EmailBodyNoFilePO.Replace("@", "<br />");
                        message.Body = EmailBodyNoFilePO;
                    }

                    // si no encuentra archivos EDI
                    // ----------------------------
                    if (CountFilesEDI == 0)
                    {
                        EmailBodyNoFileEDI = EmailBodyNoFileEDI.Replace("@", "<br />");
                        message.Body = EmailBodyNoFileEDI;
                    }

                    // si no encuentra archivos EDI y PO
                    // ---------------------------------
                    if (CountFilesPO == 0 && CountFilesEDI == 0)
                    {
                        EmailBodyNoFilePOEDI = EmailBodyNoFilePOEDI.Replace("@", "<br />");
                        message.Body = EmailBodyNoFilePOEDI;
                    }
                }

                smtp.Port = 587;
                smtp.Host = emailHost; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(credentialUser, credentialPassword);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch (Exception exception) {
                Logguer.Log("Email: " + exception.ToString());
            }
        }
    }
}
