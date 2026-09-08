using eSyncMate.Processor.Models;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// A local or UNC folder as an EDI transfer (task 00003).
    ///
    /// BizLink hands eSyncMate its EDI as files in an ordinary Windows folder rather than over
    /// SFTP, so this is the transfer the BizMate EU routes use by default. It deliberately mirrors
    /// <c>FtpConnector</c> and <c>SftpConnector</c>: same <c>Execute</c> shape, same
    /// filename-to-content dictionary back, so a route can switch transport by connector alone.
    ///
    /// Connector fields, following the SFTP connector's meanings:
    ///
    ///   BaseUrl   the folder to read from, and the folder written into when Url is empty
    ///   Url       the folder to write into (acknowledgements and outbound documents)
    ///   Method    optional search pattern, default '*'. FtpConnector already overloads Method as
    ///             a path, so overloading it as a pattern here is in keeping.
    ///   Realm     the ISA sender id the route expects (not used by this class)
    ///
    /// Host, ConsumerKey and ConsumerSecret are unused: a folder has no credentials. For a UNC
    /// share it is the Processor's service account that must have rights, not this connector.
    ///
    /// Two file-drop hazards this class exists to handle, both invisible until they corrupt a
    /// document:
    ///
    ///   Reading a half-written file. BizLink creates the file and then fills it, so a poll that
    ///   lands in between reads a truncated interchange, which parses into a plausible but wrong
    ///   document. Every candidate is opened with FileShare.None first: if the writer still holds
    ///   it, the open throws and the file is simply left for the next pass.
    ///
    ///   Writing a half-written file. The same hazard in reverse, and worse, because BizLink is
    ///   polling our outbound folder. Every write goes to a temporary name in the same folder and
    ///   is then renamed, which is atomic on NTFS, so a reader sees the file only once complete.
    /// </summary>
    public static class FileConnector
    {
        /// <summary>Extensions a writer is still working on. Never read, never counted.</summary>
        private static readonly string[] InProgressExtensions = { ".tmp", ".part", ".filepart", ".writing", ".crdownload" };

        /// <summary>Where processed files go, under the inbound folder.</summary>
        public const string ArchiveFolderName = "archive";

        /// <summary>Where files that failed processing go, under the inbound folder.</summary>
        public const string ErrorFolderName = "error";

        /// <summary>
        /// Download: every readable file in <c>BaseUrl</c>, as filename to content. Upload: writes
        /// <paramref name="fileData"/> into <c>Url</c> (or <c>BaseUrl</c>) and returns it back.
        ///
        /// A file another process is still writing is skipped silently and picked up next pass -
        /// that is the normal case on a busy folder, not a fault.
        /// </summary>
        public static async Task<Dictionary<string, string>> Execute(
            ConnectorDataModel connection, bool download = true, string fileName = "", string fileData = "")
        {
            var l_Result = new Dictionary<string, string>();

            if (connection == null)
            {
                return l_Result;
            }

            if (download)
            {
                string l_Folder = (connection.BaseUrl ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(l_Folder) || !Directory.Exists(l_Folder))
                {
                    throw new DirectoryNotFoundException($"Inbound folder not found: [{l_Folder}]");
                }

                string l_Pattern = string.IsNullOrWhiteSpace(connection.Method) ? "*" : connection.Method.Trim();

                foreach (string l_Path in Directory.EnumerateFiles(l_Folder, l_Pattern, SearchOption.TopDirectoryOnly))
                {
                    string l_Extension = Path.GetExtension(l_Path);

                    if (InProgressExtensions.Any(e => string.Equals(e, l_Extension, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    string? l_Content = await TryReadCompleteFileAsync(l_Path).ConfigureAwait(false);

                    if (l_Content != null)
                    {
                        l_Result[Path.GetFileName(l_Path)] = l_Content;
                    }
                }

                return l_Result;
            }

            string l_Target = (string.IsNullOrWhiteSpace(connection.Url) ? connection.BaseUrl : connection.Url ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(l_Target))
            {
                throw new DirectoryNotFoundException("No outbound folder is configured (Url, or BaseUrl).");
            }

            Directory.CreateDirectory(l_Target);

            // Timestamped like the SFTP connector, so a resend never overwrites its predecessor.
            string l_Name = Path.GetFileNameWithoutExtension(fileName);
            string l_Ext = Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(l_Ext))
            {
                l_Ext = ".edi";
            }

            string l_FinalName = $"{l_Name}-{DateTime.Now:yyyyMMdd-HHmmss}{l_Ext}";
            string l_FinalPath = Path.Combine(l_Target, l_FinalName);

            await WriteAtomicallyAsync(l_FinalPath, fileData).ConfigureAwait(false);

            l_Result[l_FinalName] = fileData;

            return l_Result;
        }

        /// <summary>
        /// Moves a processed file out of the inbound folder, into <c>archive</c> or <c>error</c>
        /// under it, in a dated subfolder.
        ///
        /// Moving rather than deleting: the folder is what an operator looks at when a partner asks
        /// what happened, and W7-08 asks for archiving. The ledger holds the content either way, so
        /// this is for the people, not for recovery.
        ///
        /// If the move cannot be done the file is deleted instead, because leaving it in place
        /// means processing it again on every pass. That is not data loss - the interchange is in
        /// InboundEDI and on the ledger before this is ever called - but it is reported.
        /// </summary>
        public static Task<bool> ArchiveFile(string fileName, ConnectorDataModel connection, bool success)
        {
            try
            {
                string l_Folder = (connection.BaseUrl ?? string.Empty).Trim();
                string l_Source = Path.Combine(l_Folder, fileName);

                if (!File.Exists(l_Source))
                {
                    return Task.FromResult(false);
                }

                string l_Destination = Path.Combine(
                    l_Folder,
                    success ? ArchiveFolderName : ErrorFolderName,
                    DateTime.Now.ToString("yyyy-MM-dd"));

                Directory.CreateDirectory(l_Destination);

                string l_Target = Path.Combine(l_Destination, fileName);

                // A partner that resends the same filename must not overwrite the earlier copy.
                if (File.Exists(l_Target))
                {
                    l_Target = Path.Combine(
                        l_Destination,
                        $"{Path.GetFileNameWithoutExtension(fileName)}-{DateTime.Now:HHmmssfff}{Path.GetExtension(fileName)}");
                }

                File.Move(l_Source, l_Target);

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                try
                {
                    File.Delete(Path.Combine((connection.BaseUrl ?? string.Empty).Trim(), fileName));
                }
                catch (Exception)
                {
                    // Reported by the caller: the file stays and will be seen again next pass.
                }

                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Reads the file only if nothing else holds it open. Returns null when the writer still
        /// has it, which is the signal to leave it for the next pass rather than read a fragment.
        /// </summary>
        private static async Task<string?> TryReadCompleteFileAsync(string path)
        {
            try
            {
                // FileShare.None is the test: it fails while BizLink is still writing.
                using FileStream l_Stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.None, 4096, useAsync: true);

                using StreamReader l_Reader = new StreamReader(l_Stream);

                return await l_Reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (IOException)
            {
                return null;      // still being written, or momentarily locked
            }
            catch (UnauthorizedAccessException)
            {
                return null;      // the service account cannot read it; the route reports the folder
            }
        }

        /// <summary>
        /// Writes through a temporary name in the same folder and renames, so a reader polling the
        /// folder never sees a partial file. Same-folder rename is atomic on NTFS.
        /// </summary>
        private static async Task WriteAtomicallyAsync(string finalPath, string content)
        {
            string l_Temp = finalPath + ".tmp";

            await File.WriteAllTextAsync(l_Temp, content ?? string.Empty).ConfigureAwait(false);

            File.Move(l_Temp, finalPath, overwrite: true);
        }
    }
}
