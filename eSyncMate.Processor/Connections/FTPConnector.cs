using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic; // Include the appropriate namespace for dictionaries and lists
using eSyncMate.Processor.Models;
using Renci.SshNet;
using Renci.SshNet.Async; // Ensure asynchronous methods are included
using System.Text;
using static Hangfire.Storage.JobStorageFeatures;
using Intercom.Core;
using Hangfire.States;
using FluentFTP.Exceptions;
using FluentFTP;
using Microsoft.Extensions.Hosting;

namespace eSyncMate.Processor.Connections
{
    public class FtpConnector
    {
        public static async Task<Dictionary<string, string>> Execute(string Host,string ConsumerKey,string ConsumerSecret,string path, bool download = true, string fileName = "", string fileData = "")
        {
            Dictionary<string, string> downloadedFileData = new Dictionary<string, string>();

            try
            {
                //string filePath = @"C:\Users\HP\OneDrive\Desktop\Hassan\856-Multi-lines.edi";

                //if (File.Exists(filePath))
                //{
                //    string fileContent = File.ReadAllText(filePath);

                //    downloadedFileData.Add(Path.GetFileName(filePath), fileContent);
                //    return downloadedFileData;
                //}
                using (FtpClient client = new FtpClient(Host, ConsumerKey, ConsumerSecret))
                {
                    client.Connect();

                    if (!client.DirectoryExists(Path.GetDirectoryName(path)))
                    {
                        throw new DirectoryNotFoundException("Remote directory not found");
                    }

                    if (download)
                    {
                        var files = client.GetListing(path);

                        foreach (var file in files)
                        {
                            try
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    // Download file content into memory stream
                                    client.DownloadStream(memoryStream, $"{path}/{file.Name}");

                                    // Reset the stream position to the beginning
                                    memoryStream.Position = 0;

                                    // Read the contents of the file from the memory stream asynchronously
                                    using (var streamReader = new StreamReader(memoryStream))
                                    {
                                        string fileContent = await streamReader.ReadToEndAsync();
                                        downloadedFileData.Add(file.Name, fileContent);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error downloading {file.Name}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            // Prepare the file data for upload
                            using (MemoryStream fstream = new MemoryStream(Encoding.UTF8.GetBytes(fileData)))
                            {
                                fileName = $"{Path.GetFileNameWithoutExtension(fileName)}-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.edi";
                                client.UploadStream(fstream, $"{path}/{fileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error uploading file: {ex.Message}");
                        }
                    }

                    client.Disconnect();
                }


                // Return the dictionary of downloaded file contents
                return downloadedFileData;
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Error transferring file: {ex.Message}");
                return downloadedFileData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error transferring file: {ex.Message}");
                return downloadedFileData;
            }
        }

        public static async Task<bool> DeleteFileFromFTP(string Host, string ConsumerKey, string ConsumerSecret, string path, string fileName)
        {
            try
            {
                // Construct the full path of the file on the SFTP server
                using (FtpClient client = new FtpClient(Host, ConsumerKey, ConsumerSecret))
                {
                    client.Connect();

                    if (client.DirectoryExists(Path.GetDirectoryName(path)))
                    {
                        client.DeleteFile($"{path}/{fileName}");
                        return true;
                    }
                    else
                        return false;

                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file '{fileName}': {ex.Message}");
                return false;
            }
        }
    }
}
