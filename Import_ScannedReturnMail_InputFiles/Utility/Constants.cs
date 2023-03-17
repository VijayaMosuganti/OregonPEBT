using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Import_ScannedReturnMail_InputFiles.Util
{
    class Constants
    {
        // DB connection details
        public static string DB_CONN_STRING = GetConfigValue("Conn");
        public static int timeOut = Convert.ToInt32(GetConfigValue("timeOut"));

        // Log details.
        public static string LOG_FILE_PATH = GetConfigValue("LogFilePath");
        public static string LOG_FILE_NAME = GetConfigValue("logFileName");
        public static int LOG_LEVEL = 4;
        public static int LOGFILESIZE = Convert.ToInt32(GetConfigValue("LogFileSize"));

        public static int RetryCount = Convert.ToInt32(GetConfigValue("DBConnRetryCount"));
        public static int EXCEPTION_SLEEP = Convert.ToInt32(GetConfigValue("ExceptionSleep"));

        //Location details
        public static string Scanned_SourceFilesFolder = GetConfigValue("Scanned_SourceFilesFolder");
        public static string Scanned_InputFileFolder = GetConfigValue("Scanned_InputFileFolder");
        public static string Scanned_ArchiveFolder = GetConfigValue("Scanned_ArchiveFolder");
        public static string Scanned_ErrorFolder = GetConfigValue("Scanned_ErrorFolder");
        public static string FailedRecords = GetConfigValue("Folder_FailedRecords");

        public static string ScannedReturnMail_Header = GetConfigValue("ScannedReturnMail_Header");
        public static string ScannedReturnMail_FileType = GetConfigValue("ScannedReturnMail_FileType");

        public static string SMTPSERVERNAME = GetConfigValue("SMTPSERVERNAME");
        public static string FromEmail = GetConfigValue("FromEmail");
        public static string FromName = GetConfigValue("FromName");

        public static string HeaderMismatch_ToEmail = GetConfigValue("HeaderMismatch_ToEmail");
        public static string HeaderMismatch_CCEmail = GetConfigValue("HeaderMismatch_CCEmail");
        public static string HeaderMismatch_BCCEmail = GetConfigValue("HeaderMismatch_BCCEmail");
        public static string HeaderMismatch_Subject = GetConfigValue("HeaderMismatch_Subject");
        public static string HeaderMismatch_EmailTemplate = GetConfigValue("HeaderMismatch_EmailTemplate");

        public static string Admin_ToEmail = GetConfigValue("Admin_ToEmail");
        public static string Admin_CCEmail = GetConfigValue("Admin_CCEmail");
        public static string Admin_BCCEmail = GetConfigValue("Admin_BCCEmail");
        public static string Admin_Subject = GetConfigValue("Admin_Subject");
        public static string RuntimeError_FailureTemplate = GetConfigValue("RuntimeError_FailureTemplate");

        public static string RecordsNotMatched_ToEmail = GetConfigValue("RecordsNotMatched_ToEmail");
        public static string RecordsNotMatched_CCEmail = GetConfigValue("RecordsNotMatched_CCEmail");
        public static string RecordsNotMatched_BCCEmail = GetConfigValue("RecordsNotMatched_BCCEmail");
        public static string RecordsNotMatched_Subject = GetConfigValue("RecordsNotMatched_Subject");
        public static string RecordsNotMatched_EmailTemplate = GetConfigValue("RecordsNotMatched_EmailTemplate");


        static string GetConfigValue(string strConfig)
        {
            return ConfigurationManager.AppSettings[strConfig];
        }
    }
}
