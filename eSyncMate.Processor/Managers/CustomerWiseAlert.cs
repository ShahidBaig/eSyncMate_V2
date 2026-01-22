using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using static eSyncMate.DB.Declarations;
using eSyncMate.Processor.Connections;
using System.Data;
using static eSyncMate.Processor.Models.MacysInventoryUploadRequestModel;
using static eSyncMate.Processor.Models.LowesInventoryUploadRequestModel;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Org.BouncyCastle.Asn1.X509;
using System.Net.NetworkInformation;
using DocumentFormat.OpenXml.Wordprocessing;
using static Intercom.Data.AdminConversationMessage;
using SmtpClient = System.Net.Mail.SmtpClient;
using System.Text.RegularExpressions;

namespace eSyncMate.Processor.Managers
{
    public class CustomerWiseAlert
    {
        public static async Task Execute(IConfiguration config, CustomerAlerts customerAlerts)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_data = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            int l_threshold = 2;
            string l_Body = String.Empty;

            EmailSendingData l_EmailSendingData = new EmailSendingData();

            try
            {
                l_EmailSendingData.UseConnection(CommonUtils.ConnectionString);
                DBConnector connection = new DBConnector(CommonUtils.ConnectionString);

                customerAlerts.AlertsConfiguration.Query = customerAlerts.AlertsConfiguration.Query.Replace("@CUSTOMERID@", customerAlerts.CustomerId.ToString());

                customerAlerts.AlertsConfiguration.Query = customerAlerts.AlertsConfiguration.Query.Replace("@EMAILSUBJECT@", SqlString(customerAlerts.EmailSubject));

                customerAlerts.AlertsConfiguration.Query = customerAlerts.AlertsConfiguration.Query.Replace("@EMAILBODY@", SqlString(customerAlerts.EmailBody));


                connection.GetDataSP(customerAlerts.AlertsConfiguration.Query, ref l_data);

                if (l_data.Rows.Count > 0)
                {

                    l_Body = Convert.ToString(l_data.Rows[0]["Message"]);

                    var recipients = (customerAlerts.Emails ?? string.Empty)
                                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(e => e.Trim())
                                    .Where(e => !string.IsNullOrWhiteSpace(e))
                                    .ToList();

                    foreach (var item in recipients)
                    {
                        var result = SendEmailSmtpAsync(Convert.ToString(item), customerAlerts.EmailSubject, l_Body).GetAwaiter().GetResult();

                        if (result.Success == true)
                        {
                            l_EmailSendingData.Type = "Success";
                            l_EmailSendingData.CustomerAlertID = customerAlerts.Id;
                            l_EmailSendingData.Email = item;
                            l_EmailSendingData.Data = Convert.ToString(l_data.Rows[0]["Data"]);
                            l_EmailSendingData.CreatedDate = DateTime.Now;
                            l_EmailSendingData.CreatedBy = 1;

                            l_EmailSendingData.SaveNew();

                        }
                        else
                        {
                            l_EmailSendingData.Type = "Failed";
                            l_EmailSendingData.CustomerAlertID = customerAlerts.Id;
                            l_EmailSendingData.Email = item;
                            l_EmailSendingData.Data = result.Error;
                            l_EmailSendingData.CreatedDate = DateTime.Now;
                            l_EmailSendingData.CreatedBy = 1;

                            l_EmailSendingData.SaveNew();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                //route.SaveLog(LogTypeEnum.Exception, $"Unable to update LowesUpdateInventory for items.", ex.ToString(), userNo);
            }
        }

        private static string SqlString(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "NULL";

            // Escape single quote for SQL string literals
            value = value.Replace("'", "''");

            // Keep line breaks safe (optional)
            value = value.Replace("\r", "").Replace("\n", "\\n");

            // Use N'' for Unicode
            return "N'" + value + "'";
        }

        private static async Task<(bool Success, string MessageId, string Error)> SendEmailSmtpAsync(
      string to,
      string subject,
      string body)
        {
            try
            {
                // ✅ Build email (MimeKit)
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(MailboxAddress.Parse(CommonUtils.FromEmailAccount));
                emailMessage.To.Add(MailboxAddress.Parse(to));
                emailMessage.Subject = subject;

                emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = body ?? ""
                };

                using var smtp = new MailKit.Net.Smtp.SmtpClient();

                smtp.Timeout = 30000;
                smtp.CheckCertificateRevocation = false;

                // Optional: log protocol for debugging (remove in prod)
                // smtp.ProtocolLogger = new ProtocolLogger("smtp-log.txt");

                // ✅ Office 365: 587 = STARTTLS
                var host = CommonUtils.SMTPHost;            // smtp.office365.com
                var port = CommonUtils.SMTPPort;            // 587
                var options = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

                await smtp.ConnectAsync(host, port, options);

                // ✅ sometimes recommended with O365 when using username/password
                smtp.AuthenticationMechanisms.Remove("XOAUTH2");

                await smtp.AuthenticateAsync(CommonUtils.FromEmailAccount, CommonUtils.FromEmailPWD);

                await smtp.SendAsync(emailMessage);
                await smtp.DisconnectAsync(true);

                return (true, emailMessage.MessageId ?? "", "");
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                // ✅ This is your 535 / 5.7.139 case
                return (false, "", "Authentication failed (SMTP AUTH blocked / MFA / Conditional Access / Security Defaults). Details: " + ex.Message);
            }
            catch (SmtpCommandException ex)
            {
                return (false, "", $"SMTP Command Error: {ex.Message} | StatusCode: {ex.StatusCode} ");
            }
            catch (SmtpProtocolException ex)
            {
                return (false, "", $"SMTP Protocol Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, "", ex.ToString());
            }
        }



    }

}

