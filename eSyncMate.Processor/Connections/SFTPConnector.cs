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

namespace eSyncMate.Processor.Connections
{
    public class SftpConnector
    {
        public static async Task<Dictionary<string, string>> Execute(ConnectorDataModel connection, bool download = true, string fileName = "", string fileData = "")
        {
            Dictionary<string, string> downloadedFileData = new Dictionary<string, string>();

            try
            {
                //string filePath = @"C:\Users\HP\Downloads\204-EDI-20240603T105655623_204.edi";

                //if (File.Exists(filePath))
                //{
                //    string fileContent = File.ReadAllText(filePath);

                //    downloadedFileData.Add(Path.GetFileName(filePath), fileContent);
                //    return downloadedFileData;
                //}

                using (SftpClient client = new SftpClient(connection.Host, connection.ConsumerKey, connection.ConsumerSecret))
                {
                    client.Connect();

                    string remotePath = connection.BaseUrl.Trim().Replace("\\", "/");

                    // Make sure no trailing slash
                    if (remotePath.EndsWith("/"))
                        remotePath = remotePath.TrimEnd('/');

                    if (!client.Exists(remotePath))
                        throw new DirectoryNotFoundException($"Remote directory not found: {remotePath}");

                    //if (!client.Exists(Path.GetDirectoryName(connection.BaseUrl)))
                    //{
                    //    throw new DirectoryNotFoundException("Remote directory not found");
                    //}

                    if (download)
                    {
                        var files = client.ListDirectory(connection.BaseUrl);

                        foreach (var file in files)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                try
                                {
                                    // Download file content into memory stream asynchronously
                                    client.DownloadFile($"{connection.BaseUrl}/{file.Name}", memoryStream);

                                    // Reset the stream position to the beginning
                                    memoryStream.Position = 0;

                                    // Read the contents of the file from the memory stream
                                    using (var streamReader = new StreamReader(memoryStream))
                                    {
                                        string fileContent = await streamReader.ReadToEndAsync();
                                        downloadedFileData.Add(file.Name, fileContent);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error downloading {file.Name}: {ex.Message}");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Upload the provided file data to the remote directory
                        using (MemoryStream fstream = new MemoryStream(Encoding.UTF8.GetBytes(fileData)))
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(fileName)}-{DateTime.Now.ToString("yyyyMMdd-hhmmss")}.edi";
                            client.UploadFile(fstream, $"{connection.BaseUrl}/{fileName}");
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

        public static async Task<bool> DeleteFileFromSFTP(string fileName, ConnectorDataModel connection)
        {
            try
            {
                // Construct the full path of the file on the SFTP server
                using (SftpClient client = new SftpClient(connection.Host, connection.ConsumerKey, connection.ConsumerSecret))
                {
                    client.Connect();

                    if (client.Exists(Path.GetDirectoryName(connection.BaseUrl)))
                    {
                        client.DeleteFile($"{connection.BaseUrl}/{fileName}");
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
