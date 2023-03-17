using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PEBT.Util
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
        public static string MailInputFile_SourceFilesFolder = GetConfigValue("MailInputFile_SourceFilesFolder");
        public static string MailInputFile_InputFileFolder = GetConfigValue("MailInputFile_InputFileFolder");
        public static string MailInputFile_ArchiveFolder = GetConfigValue("MailInputFile_ArchiveFolder");
        public static string MailInputFile_ErrorFolder = GetConfigValue("MailInputFile_ErrorFolder");


        public static string PrintReady_SourceFilesFolder = GetConfigValue("PrintReady_SourceFilesFolder");
        public static string PrintReady_InputFileFolder = GetConfigValue("PrintReady_InputFileFolder");
        public static string PrintReady_ArchiveFolder = GetConfigValue("PrintReady_ArchiveFolder");
        public static string PrintReady_ErrorFolder = GetConfigValue("PrintReady_ErrorFolder");


        public static string PEBT_Header = GetConfigValue("PEBT_Header");

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

        public static string DuplicatePieceIDS_ToEmail = GetConfigValue("DuplicatePieceIDS_ToEmail");
        public static string DuplicatePieceIDS_CCEmail = GetConfigValue("DuplicatePieceIDS_CCEmail");
        public static string DuplicatePieceIDS_BCCEmail = GetConfigValue("DuplicatePieceIDS_BCCEmail");
        public static string DuplicatePieceIDS_Subject = GetConfigValue("DuplicatePieceIDS_Subject");
        public static string DuplicatePieceIDS_EmailTemplate = GetConfigValue("DuplicatePieceIDS_EmailTemplate");


        static string GetConfigValue(string strConfig)
        {
            return ConfigurationManager.AppSettings[strConfig];
        }
    }
}
