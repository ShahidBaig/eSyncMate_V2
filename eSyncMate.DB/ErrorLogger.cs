using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic;

namespace eSyncMate.DB
{
    public class ErrorLogger
    {
        private static readonly object _LockObj = new object();

        public static void WriteToErrorLog(string msg, string stkTrace)
        {
            try
            {
                var l_ServerDate = DateAndTime.Now;
                string l_ProcessID = Process.GetCurrentProcess().Id.ToString();
                string baseDir = Directory.GetCurrentDirectory();
                string l_FileName = l_ServerDate.Month + "-" + l_ServerDate.Day + "-" + l_ServerDate.Year + "-" + l_ServerDate.Hour + "-" + l_ProcessID;
                string l_Path = Path.Combine(baseDir, "Errors");
                string l_FilesContainer = Path.Combine(l_Path, l_ServerDate.Month + "-" + l_ServerDate.Day + "-" + l_ServerDate.Year);

                lock (_LockObj)
                {
                    if (!Directory.Exists(l_Path))
                    {
                        Directory.CreateDirectory(l_Path);
                    }

                    if (!Directory.Exists(l_FilesContainer))
                    {
                        Directory.CreateDirectory(l_FilesContainer);
                    }

                    using (var s1 = new StreamWriter(new FileStream(Path.Combine(l_FilesContainer, l_FileName + "_Errors.txt"), FileMode.Append, FileAccess.Write)))
                    {
                        s1.Write("Date/Time: " + DateTime.Now.ToString() + ControlChars.CrLf);
                        s1.Write("Message: " + msg + ControlChars.CrLf);
                        s1.Write("StackTrace: " + stkTrace + ControlChars.CrLf);
                        s1.Write("===========================================================================================" + ControlChars.CrLf);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }

    public class QueryLogger
    {
        private static readonly object _LockObj = new object();

        public static void WriteToQueryLog(string l_Query)
        {
            var l_ServerDate = DateAndTime.Now;
            string l_ProcessID = Process.GetCurrentProcess().Id.ToString();
            string baseDir = Directory.GetCurrentDirectory();
            string l_FileName = l_ServerDate.Month + "-" + l_ServerDate.Day + "-" + l_ServerDate.Year + "-" + l_ServerDate.Hour + "-" + l_ProcessID;
            string l_Path = Path.Combine(baseDir, "Queries");
            string l_FilesContainer = Path.Combine(l_Path, l_ServerDate.Month + "-" + l_ServerDate.Day + "-" + l_ServerDate.Year);

            lock (_LockObj)
            {
                if (!Directory.Exists(l_Path))
                {
                    Directory.CreateDirectory(l_Path);
                }

                if (!Directory.Exists(l_FilesContainer))
                {
                    Directory.CreateDirectory(l_FilesContainer);
                }

                using (var s1 = new StreamWriter(new FileStream(Path.Combine(l_FilesContainer, l_FileName + "_Queries.txt"), FileMode.Append, FileAccess.Write)))
                {
                    s1.Write("/*" + DateTime.Now.ToString() + ": */ " + l_Query + ControlChars.CrLf);
                }
            }
        }
    }
}