using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using static eSyncMate.DB.Declarations;
using eSyncMate.Processor.Connections;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace eSyncMate.Processor.Managers
{
    public class CustomerWiseAlert
    {
        public static async Task Execute(IConfiguration config, CustomerAlerts customerAlerts)
        {
            DataTable l_data = new();
            EmailSendingData l_EmailSendingData = new();
            string l_Body = string.Empty;

            try
            {
                l_EmailSendingData.UseConnection(CommonUtils.ConnectionString);
                
                DBConnector connection = new(CommonUtils.ConnectionString);

                string l_CustomerId = customerAlerts.CustomerId.ToString();
                DataTable l_CustData = new();
                
                connection.GetData($"SELECT ERPCustomerID FROM Customers WITH (NOLOCK) WHERE Id = {customerAlerts.CustomerId}", ref l_CustData);
                
                if (l_CustData.Rows.Count > 0 && !l_CustData.Rows[0].IsNull("ERPCustomerID"))
                {
                    string erpId = Convert.ToString(l_CustData.Rows[0]["ERPCustomerID"]);
                    if (!string.IsNullOrWhiteSpace(erpId))
                    {
                        l_CustomerId = erpId;
                    }
                }
                else
                {
                    return;
                }

                string replacementValue = $"'{l_CustomerId}'";
                customerAlerts.AlertsConfiguration.Query = Regex.Replace(
                    customerAlerts.AlertsConfiguration.Query,
                    @"'?@CustomerI[dD]@?'?",
                    replacementValue,
                    RegexOptions.IgnoreCase
                );

                string l_Query = customerAlerts.AlertsConfiguration.Query;
                connection.GetDataSP(l_Query, ref l_data);

                if (l_data.Rows.Count > 0)
                {
                    string alertStatus = Convert.ToString(l_data.Rows[0]["AlertStatus"]);
                    if (alertStatus != null && !alertStatus.Equals("ALERT", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return;
                    }

                    string l_Subject = BindPlaceholders(customerAlerts.EmailSubject, l_data);

                    // Priority 1: SP returns body content in AlertReason/BodyData/Data columns
                    l_Body = ResolveEmailBodyContent(l_data);

                    // Replace all SP column placeholders in the resolved body
                    if (!string.IsNullOrWhiteSpace(l_Body))
                    {
                        l_Body = BindPlaceholders(l_Body, l_data);
                    }

                    // Priority 2: Use CustomerAlerts.EmailBody template with SP column placeholders
                    if (string.IsNullOrWhiteSpace(l_Body) && !string.IsNullOrWhiteSpace(customerAlerts.EmailBody))
                    {
                        l_Body = BindPlaceholders(customerAlerts.EmailBody, l_data);
                    }

                    // Priority 3: Auto-generate HTML table from SP result
                    if (string.IsNullOrWhiteSpace(l_Body))
                    {
                        l_Body = GenerateHtmlTableFromDataTable(l_data);
                    }

                    if (string.IsNullOrWhiteSpace(l_Body))
                    {
                        //Console.WriteLine($"[CustomerWiseAlert] No content for alert {customerAlerts.Id}. Skipping email.");
                        return;
                    }

                    var recipients = GetRecipientEmails(customerAlerts.Emails);

                    if (recipients.Count == 0) return;
                    string beautifiedBody = GetBeautifiedHtmlBody(l_Subject, l_Body, customerAlerts.AlertsConfiguration.AlertName.ToString());
                    var graphSettings = GetGraphSettings(config);
                    var result = await SendEmailGraphAsync(graphSettings.tenantId, graphSettings.clientId, graphSettings.clientSecret, graphSettings.senderEmail, recipients, l_Subject, beautifiedBody);
                    foreach (var item in recipients)
                    {
                        l_EmailSendingData.Type = result.Success ? "Success" : "Failed";
                        l_EmailSendingData.CustomerAlertID = customerAlerts.Id;
                        l_EmailSendingData.Email = item;
                        l_EmailSendingData.Data = result.Success ? (l_data.Columns.Contains("Data") ? Convert.ToString(l_data.Rows[0]["Data"]) : l_Body) : result.Error;
                        l_EmailSendingData.CreatedDate = DateTime.Now;
                        l_EmailSendingData.CreatedBy = 1;
                        l_EmailSendingData.SaveNew();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CustomerWiseAlert] Error: {ex.Message}");
            }
        }

        private static async Task ResolveCustomerPlaceholders(DBConnector connection, CustomerAlerts customerAlerts)
        {
            try
            {
                string l_CustomerId = customerAlerts.CustomerId.ToString();
                DataTable l_CustData = new();
                connection.GetData($"SELECT ERPCustomerID FROM Customers WITH (NOLOCK) WHERE Id = {customerAlerts.CustomerId}", ref l_CustData);
                if (l_CustData.Rows.Count > 0 && !l_CustData.Rows[0].IsNull("ERPCustomerID"))
                {
                    string erpId = Convert.ToString(l_CustData.Rows[0]["ERPCustomerID"]);
                    if (!string.IsNullOrWhiteSpace(erpId))
                    {
                        l_CustomerId = erpId;
                    }
                }

                string replacementValue = $"'{l_CustomerId}'";
                customerAlerts.AlertsConfiguration.Query = Regex.Replace(
                    customerAlerts.AlertsConfiguration.Query,
                    @"'?@CustomerI[dD]@?'?",
                    replacementValue,
                    RegexOptions.IgnoreCase
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResolveCustomerPlaceholders] Error: {ex.Message}");
            }
        }

        private static string ResolveEmailBodyContent(DataTable dt)
        {
            try
            {
                string[] bodyColumns = { "AlertReason", "BodyData", "Data" };
                foreach (var col in bodyColumns)
                {
                    if (dt.Columns.Contains(col) && !dt.Rows[0].IsNull(col))
                    {
                        string val = Convert.ToString(dt.Rows[0][col]);
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            return (col == "BodyData" || val.Contains('\t') || val.Contains('|'))
                                     ? ProcessTabularData(val)
                                     : $"<p>{val.Replace("\n", "<br/>")}</p>";
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResolveEmailBodyContent] Error: {ex.Message}");
            }
            return string.Empty;
        }

        private static (string tenantId, string clientId, string clientSecret, string senderEmail) GetGraphSettings(IConfiguration config)
        {
            var graphConfig = config.GetSection("MicrosoftGraph");
            return (
                tenantId: graphConfig["TenantId"] ?? string.Empty,
                clientId: graphConfig["ClientId"] ?? string.Empty,
                clientSecret: graphConfig["ClientSecret"] ?? string.Empty,
                senderEmail: graphConfig["SenderEmail"] ?? CommonUtils.FromEmailAccount
            );
        }

        private static List<string> GetRecipientEmails(string? emails)
        {
            return (emails ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();
        }
        private static string GenerateHtmlTableFromDataTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return string.Empty;

            const string tableStyle = "style=\"width:100%;border-collapse:collapse;margin:20px 0;font-size:14px;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"";
            const string thStyle = "bgcolor='#f1f5f9' style=\"background-color:#f1f5f9;color:#1e293b;font-weight:600;text-align:left;padding:12px;border:1px solid #e2e8f0;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"";
            const string tdStyle = "style=\"padding:12px;border:1px solid #e2e8f0;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"";
            const string trEvenStyle = "bgcolor='#f8fafc' style=\"background-color:#f8fafc;\"";

            StringBuilder sb = new StringBuilder();
            sb.Append($"<table {tableStyle}><thead><tr>");

            string[] excludedColumns = { "AlertStatus" };

            List<string> visibleColumns = new List<string>();
            foreach (DataColumn column in dt.Columns)
            {
                if (!excludedColumns.Contains(column.ColumnName, StringComparer.OrdinalIgnoreCase))
                {
                    visibleColumns.Add(column.ColumnName);
                    sb.Append($"<th {thStyle}>{column.ColumnName}</th>");
                }
            }
            sb.Append("</tr></thead><tbody>");

            int rowIndex = 0;
            foreach (DataRow row in dt.Rows)
            {
                string rowStyle = (rowIndex % 2 == 1) ? $" {trEvenStyle}" : "";
                sb.Append($"<tr{rowStyle}>");
                foreach (string colName in visibleColumns)
                {
                    sb.Append($"<td {tdStyle}>{Convert.ToString(row[colName])}</td>");
                }
                sb.Append("</tr>");
                rowIndex++;
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }
        private static string SqlString(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "NULL";

            value = value.Replace("'", "''");
            value = value.Replace("\r", "").Replace("\n", "\\n");
            return "N'" + value + "'";
        }

        private static async Task<(bool Success, string MessageId, string Error)> SendEmailGraphAsync(string tenantId,string clientId,string clientSecret,string senderEmail,List<string> toRecipients,string subject,string htmlBody)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    return (false, "", "MicrosoftGraph credentials not configured. Check TenantId, ClientId, and ClientSecret in appsettings.json");
                }

                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    return (false, "", "SenderEmail is not configured in MicrosoftGraph settings");
                }

                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

                var message = new Microsoft.Graph.Models.Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = Microsoft.Graph.Models.BodyType.Html,
                        Content = htmlBody
                    },
                    ToRecipients = toRecipients.Select(email => new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = email }
                    }).ToList()
                };

                await graphClient.Users[senderEmail].SendMail.PostAsync(new SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                });

                return (true, Guid.NewGuid().ToString(), "");
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError odataEx)
            {
                var errorCode = odataEx.Error?.Code ?? "Unknown";
                var errorMessage = odataEx.Error?.Message ?? odataEx.Message;
                var innerError = odataEx.Error?.InnerError?.AdditionalData != null
                    ? string.Join(", ", odataEx.Error.InnerError.AdditionalData.Select(kv => $"{kv.Key}={kv.Value}"))
                    : "";

                var fullError = $"Graph API Error [{errorCode}]: {errorMessage}";
                if (!string.IsNullOrEmpty(innerError))
                    fullError += $" | InnerError: {innerError}";

                Console.WriteLine($"[SendEmailGraphAsync] {fullError}");
                Console.WriteLine($"[SendEmailGraphAsync] TenantId: {tenantId}, ClientId: {clientId}, SenderEmail: {senderEmail}, Recipients: {string.Join(", ", toRecipients)}");

                return (false, "", fullError);
            }
            catch (Azure.Identity.AuthenticationFailedException authEx)
            {
                var fullError = $"Authentication Failed: {authEx.Message}";
                Console.WriteLine($"[SendEmailGraphAsync] {fullError}");
                return (false, "", fullError);
            }
            catch (Exception ex)
            {
                var fullError = $"{ex.GetType().Name}: {ex.Message}";
                if (ex.InnerException != null)
                    fullError += $" | Inner: {ex.InnerException.Message}";

                Console.WriteLine($"[SendEmailGraphAsync] {fullError}");
                return (false, "", fullError);
            }
        }

        private static string GetBeautifiedHtmlBody(string subject, string content, string alertName)
        {
            // NOTE: Microsoft 365 / Outlook does NOT support:
            //   - linear-gradient (use background-color only)
            //   - border-radius (ignored)
            //   - max-width (use width attribute on table)
            //   - <style> blocks (must be inline)

            string processedContent = content;
            if (!content.Contains("<table") && !content.Contains("<div") && !content.Contains("<p"))
            {
                processedContent = ProcessTabularData(content);
            }

            processedContent = ApplyInlineTableStyles(processedContent);

            StringBuilder sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'></head>");
            sb.Append("<body style=\"font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;line-height:1.6;color:#333333;margin:0;padding:0;background-color:#f4f7f6;\">");

            // Outer wrapper table
            sb.Append("<table width='100%' cellpadding='0' cellspacing='0' border='0' style=\"background-color:#f4f7f6;\">");
            sb.Append("<tr><td align='center' valign='top' style=\"padding:20px 10px;\">");

            // Container table
            sb.Append("<table width='800' cellpadding='0' cellspacing='0' border='0' style=\"width:800px;background-color:#ffffff;border:1px solid #e0e0e0;\">");

            // Header row — solid color (no gradient)
            sb.Append("<tr><td bgcolor='#1e3a8a' style=\"background-color:#1e3a8a;color:#ffffff;padding:30px 20px;text-align:center;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\">");
            sb.Append($"<h1 style=\"margin:0;font-size:24px;font-weight:600;letter-spacing:0.5px;color:#ffffff;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\">{alertName}</h1>");
            sb.Append("</td></tr>");

            // Content row
            sb.Append("<tr><td style=\"padding:30px 40px;font-size:14px;color:#333333;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\">");
            sb.Append(processedContent);
            sb.Append("</td></tr>");

            // Footer row
            sb.Append("<tr><td bgcolor='#f8fafc' style=\"background-color:#f8fafc;color:#64748b;padding:20px;text-align:center;font-size:12px;border-top:1px solid #e2e8f0;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\">");
            sb.Append("&copy; " + DateTime.Now.Year + " eSyncMate. All rights reserved.<br/>This is an automated notification, please do not reply.");
            sb.Append("</td></tr>");

            // Close container
            sb.Append("</table>");
            sb.Append("</td></tr></table>");

            sb.Append("</body></html>");

            return sb.ToString();
        }

        private static string ApplyInlineTableStyles(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return html;

            html = Regex.Replace(html, @"<table(?![^>]*style)",
                "<table style=\"width:100%;border-collapse:collapse;margin:20px 0;font-size:14px;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"",
                RegexOptions.IgnoreCase);

            html = Regex.Replace(html, @"<th(?![^>]*style)",
                "<th bgcolor='#f1f5f9' style=\"background-color:#f1f5f9;color:#1e293b;font-weight:600;text-align:left;padding:12px;border:1px solid #e2e8f0;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"",
                RegexOptions.IgnoreCase);

            html = Regex.Replace(html, @"<td(?![^>]*style)",
                "<td style=\"padding:12px;border:1px solid #e2e8f0;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"",
                RegexOptions.IgnoreCase);

            return html;
        }

        private static string ProcessTabularData(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length <= 1) return $"<p style=\"margin:0;padding:4px 0;\">{content.Replace("\n", "<br/>")}</p>";

            char delimiter = '\0';
            if (lines[0].Contains('\t')) delimiter = '\t';
            else if (lines[0].Contains('|')) delimiter = '|';
            else if (lines[0].Contains(',') && lines[0].Split(',').Length > 2) delimiter = ',';

            if (delimiter == '\0')
                return $"<p style=\"margin:0;padding:4px 0;\">{content.Replace("\n", "<br/>")}</p>";

            const string tableStyle = "style=\"width:100%;border-collapse:collapse;margin:20px 0;font-size:14px;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"";
            const string thStyle = "bgcolor='#f1f5f9' style=\"background-color:#f1f5f9;color:#1e293b;font-weight:600;text-align:left;padding:12px;border:1px solid #e2e8f0;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"";
            const string tdStyle = "style=\"padding:12px;border:1px solid #e2e8f0;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;\"";

            StringBuilder tableSb = new();
            tableSb.Append($"<table {tableStyle}><thead><tr>");

            string[] headers = lines[0].Split(delimiter);
            foreach (var header in headers)
            {
                tableSb.Append($"<th {thStyle}>{header.Trim().Trim('\"')}</th>");
            }
            tableSb.Append("</tr></thead><tbody>");

            for (int i = 1; i < lines.Length; i++)
            {
                string[] cells = lines[i].Split(delimiter);
                string rowStyle = (i % 2 == 0) ? " style=\"background-color:#f8fafc;\"" : "";
                tableSb.Append($"<tr{rowStyle}>");
                foreach (var cell in cells)
                {
                    tableSb.Append($"<td {tdStyle}>{cell.Trim().Trim('\"')}</td>");
                }
                tableSb.Append("</tr>");
            }

            tableSb.Append("</tbody></table>");
            return tableSb.ToString();
        }

        private static string BindPlaceholders(string template, DataTable data)
        {
            if (string.IsNullOrWhiteSpace(template) || data == null || data.Rows.Count == 0) return template;
            string result = template;
            DataRow row = data.Rows[0];

            foreach (DataColumn column in data.Columns)
            {
                string colName = column.ColumnName;
                string colValue = Convert.ToString(row[colName]) ?? string.Empty;

                if (colName.Equals("BodyData", StringComparison.OrdinalIgnoreCase))
                {
                    colValue = ProcessTabularData(colValue);
                }

                // Support both [ColumnName] and @ColumnName formats
                result = Regex.Replace(result, Regex.Escape("[" + colName + "]"), colValue, RegexOptions.IgnoreCase);
                result = Regex.Replace(result, @"@" + colName + @"(?![a-zA-Z0-9_])", colValue, RegexOptions.IgnoreCase);
            }

            return result;
        }
    }
}

