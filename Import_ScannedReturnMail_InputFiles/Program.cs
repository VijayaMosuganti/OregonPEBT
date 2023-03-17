using Import_ScannedReturnMail_InputFiles.Util;
using Microsoft.ApplicationBlocks.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Import_ScannedReturnMail_InputFiles
{
    class Program
    {
        #region "Variables Used                         "

        static LogFile myLogFile = null;
        static string strSource = string.Empty;
        static Mail myEmail;
        static string strFileName = "ScannedReturnMail.txt";
        static string strCurrentFile = string.Empty;
        #endregion

        #region "Main                                   "
        /// <summary>
        /// Entry to the process
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            strSource = "Main()";
            myEmail = new Mail();

            try
            {
                myLogFile = LogFile.GetLogFileInstance();

                //Setting debugging level which is read from the config file.
                myLogFile.DebugMode = Constants.LOG_LEVEL;

                //setting log file name which is read from the config file.
                myLogFile.FileName = Path.Combine(Constants.LOG_FILE_PATH, Constants.LOG_FILE_NAME);
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "****************** Starting of the Process ******************");

                ProcessFiles();
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "****************** End of the Process ******************");
            }
            catch (Exception ex)
            {
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Exception occured: " + ex.Message);
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Sending Failure mail to admin.");
            }
        }
        #endregion

        #region "ProcessFiles                        "
        /// <summary>
        /// This method is used to process each  file
        /// </summary>
        /// <returns></returns>
        static void ProcessFiles()
        {
            bool result = false;
            strSource = "ProcessFiles()";
            myEmail = new Mail();
            string strinputfileIDS = string.Empty;
            try
            {
                if (Directory.GetFiles(Constants.Scanned_SourceFilesFolder, "*.txt").Count() > 0)
                {
                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, Directory.GetFiles(Constants.Scanned_SourceFilesFolder, "PBT_Scanned_Return_*.txt").Count() + " PBT_Scanned_Return_ file(s) found.");

                    foreach (string strFile in Directory.GetFiles(Constants.Scanned_SourceFilesFolder, "PBT_Scanned_Return_*.txt"))
                    {
                        try
                        {
                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Processing the file:" + Path.GetFileName(strFile));
                            if (!Directory.Exists(Constants.Scanned_InputFileFolder))
                            {
                                Directory.CreateDirectory(Constants.Scanned_InputFileFolder);
                            }
                            string ProcessedFilePath = Path.Combine(Constants.Scanned_InputFileFolder, strFileName);
                            File.Copy(strFile, ProcessedFilePath, true);

                            StreamReader streamreader = new StreamReader(ProcessedFilePath);
                            string fileHeader = streamreader.ReadLine();
                            streamreader.Close();
                            if (fileHeader.ToString() == Constants.ScannedReturnMail_Header.ToString())
                            {
                                strCurrentFile = Path.GetFileName(strFile);
                                ReturnData rfile = Do(InsertFile,strCurrentFile);
                                if (rfile != null && rfile.InputFileID > 0)
                                {
                                    strinputfileIDS += rfile.InputFileID + ",";
                                    DataTable dt = GetDataFromFileToDataTable(ProcessedFilePath, Constants.Scanned_InputFileFolder);
                                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully converted into DataTable. ");

                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "No of records in the file are:" + dt.Rows.Count.ToString());
                                        rfile = Do(InsertIDXScannedReturnFile,dt, rfile.InputFileID);
                                        if (rfile != null && rfile.ReturnCode == 0)
                                            result = true;
                                        else
                                            result = false;

                                        if (result)
                                        {
                                            if (!Directory.Exists(Constants.Scanned_ArchiveFolder))
                                            {
                                                Directory.CreateDirectory(Constants.Scanned_ArchiveFolder);
                                            }
                                            //Archiving Success file
                                            File.Copy(strFile, Path.Combine(Constants.Scanned_ArchiveFolder, Path.GetFileName(strFile)), true);
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully moved to Archive Folder.");
                                            File.Delete(strFile);
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successsfully Deleted from Shared Folder.");
                                        }
                                        else
                                        {
                                            if (!Directory.Exists(Constants.Scanned_ErrorFolder))
                                            {
                                                Directory.CreateDirectory(Constants.Scanned_ErrorFolder);
                                            }
                                            File.Copy(strFile, Path.Combine(Constants.Scanned_ErrorFolder, Path.GetFileName(strFile)), true);
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully moved to Error Folder.");
                                            File.Delete(strFile);
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successsfully Deleted from Shared Folder.");
                                            //string scannablefilename = Path.GetFileName(strFile);
                                            //myEmail.SendEmail(Constants.FromEmail, Constants.ToEmail, Constants.CCEmail, scannablefilename, Constants.Scanned_ErrorFolder, "HTML", Constants.FailedSubject.Replace("{filename}", scannablefilename), Constants.FailedTemplate, "");
                                            //myEmail.SendEmail(Constants.FromEmail, Constants.ToEmail, Constants.CCEmail, Path.GetFileName(strFile), Constants.Scanned_ErrorFolder, "HTML", Constants.FailedSubject, Constants.FailedTemplate, "");
                                        }
                                    }
                                }
                                else if (rfile.ReturnCode == 2)//Duplicate file
                                {
                                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Duplicate File Found. Moving file to Archive Folder.");
                                    if (!Directory.Exists(Constants.Scanned_ArchiveFolder))
                                    {
                                        Directory.CreateDirectory(Constants.Scanned_ArchiveFolder);
                                    }
                                    File.Copy(strFile, Path.Combine(Constants.Scanned_ArchiveFolder, Path.GetFileName(strFile)), true);
                                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully moved to Archive Folder.");
                                    File.Delete(strFile);
                                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successsfully Deleted from Shared Folder.");
                                    //string scannablefilename = Path.GetFileName(strFile);
                                }
                            }
                            else
                            {
                                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Header mis match.,moving the file to error folder and sending the email");
                                if (!Directory.Exists(Constants.Scanned_ErrorFolder))
                                {
                                    Directory.CreateDirectory(Constants.Scanned_ErrorFolder);
                                }
                                File.Copy(strFile, Path.Combine(Constants.Scanned_ErrorFolder, Path.GetFileName(strFile)), true);
                                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully moved to Error Folder.");
                                File.Delete(strFile);
                                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully Deleted from Shared Folder.");
                                string scannablefilename = Path.GetFileName(strFile);
                                myEmail.SendEmail(Constants.FromEmail, Constants.HeaderMismatch_ToEmail, Constants.HeaderMismatch_CCEmail, Constants.HeaderMismatch_BCCEmail, scannablefilename, Constants.Scanned_ErrorFolder, "HTML", Constants.HeaderMismatch_Subject.Replace("{filename}", scannablefilename), Constants.HeaderMismatch_EmailTemplate, "");
                                // myEmail.SendEmail(Constants.FromEmail, Constants.ToEmail, Constants.CCEmail, Path.GetFileName(strFile), Constants.Scanned_ErrorFolder, "HTML", Constants.FailedSubject, Constants.FailedTemplate, "");
                            }

                        }
                        catch (Exception iex)
                        {
                            File.Copy(strFile, Path.Combine(Constants.Scanned_ErrorFolder, Path.GetFileName(strFile)), true);
                            File.Delete(strFile);
                            myEmail.SendEmail(Constants.FromEmail, Constants.Admin_ToEmail, Constants.Admin_CCEmail, iex.Message.ToString(), Constants.Admin_BCCEmail, "", "HTML", Constants.Admin_Subject, Constants.RuntimeError_FailureTemplate, "");
                        }
                    }

                    if (!string.IsNullOrEmpty(strinputfileIDS))
                    {
                        myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Generting the file with no matched records");
                        DataTable dtRecordsNotMatched = Do(GetRecordsNotMatched,strinputfileIDS);
                        string strrecordsnotmatched = Path.Combine(Constants.FailedRecords, "Records_Not_Matched_" + DateTime.Now.ToString("MMddyyyy_hhmmss") + ".csv");
                        if (dtRecordsNotMatched.Rows.Count > 0)
                        {
                            GenerateCSV(dtRecordsNotMatched, strrecordsnotmatched);
                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File generated");
                            myEmail.SendEmail(Constants.FromEmail, Constants.RecordsNotMatched_ToEmail, Constants.RecordsNotMatched_CCEmail, Constants.RecordsNotMatched_BCCEmail, Path.GetFileName(strrecordsnotmatched), Constants.FailedRecords, "HTML", Constants.RecordsNotMatched_Subject, Constants.RecordsNotMatched_EmailTemplate, "");
                        }
                        else
                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "No records to generate the file");
                    }
                }
                else
                {
                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "No IDX_Scanned_Return file found to process.");
                }
            }
            catch (Exception ex)
            {
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Exception occured: " + ex.Message);
                myEmail.SendEmail(Constants.FromEmail, Constants.Admin_ToEmail, Constants.Admin_CCEmail, Constants.Admin_BCCEmail, ex.Message.ToString(), "", "HTML", Constants.Admin_Subject, Constants.RuntimeError_FailureTemplate, "");
            }

        }
        #endregion

        #region "GetDataFromFileToDataTable             "
        /// <summary>
        /// This method is used to read data from the file into datatable
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static DataTable GetDataFromFileToDataTable(string filePath, string inputfilefolder)
        {
            DataTable dtTemp = new DataTable();
            strSource = "GetDataFromFileToDataTable()";
            myEmail = new Mail();
            try
            {
                string SchemaFile = Path.Combine(inputfilefolder, "Schema.ini");
                if (File.Exists(SchemaFile))
                    File.Delete(SchemaFile);

                StreamWriter streamwrt = new StreamWriter(SchemaFile);
                streamwrt.WriteLine("[" + strFileName + "]");
                streamwrt.WriteLine("ColNameHeader=TRUE");
                streamwrt.WriteLine("Col1=\"Scanned_Barcode\" Text");
                streamwrt.Close();
                using (OleDbConnection cn = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Extended Properties='text;HDR=Yes;FMT=Delimited(,)';Data Source=" + Path.GetDirectoryName(filePath) + ";"))
                {
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT * FROM " + Path.GetFileName(filePath), cn))
                    {
                        // Open the connection
                        cn.Open();
                        adapter.Fill(dtTemp);
                    }
                }
            }
            catch (Exception ex)
            {
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Error occurd while importing data from file to data table. " + ex.Message);
            }
            return dtTemp;
        }

        #endregion

        #region "InsertFile                             "
        static ReturnData InsertFile(string InputFileName)
        {
            strSource = "InsertFile()";
            ReturnData rData = new ReturnData();
            try
            {
                SqlParameter[] objSqlParam = new SqlParameter[5];
                objSqlParam = SqlHelperParameterCache.GetSpParameterSet(Constants.DB_CONN_STRING, "Usp_PEBT_ProcessFiles_InsertFiledetails");
                objSqlParam[0].Value = InputFileName;
                objSqlParam[1].Value = Constants.ScannedReturnMail_FileType;
                objSqlParam[2].Value = DBNull.Value;
                objSqlParam[3].Value = DBNull.Value;
                objSqlParam[4].Value = DBNull.Value;
                SqlHelper.ExecuteNonQuery(Constants.DB_CONN_STRING, CommandType.StoredProcedure, "Usp_PEBT_ProcessFiles_InsertFiledetails", Constants.timeOut, objSqlParam);

                if (objSqlParam[2].Value != DBNull.Value)
                    rData.ReturnCode = Convert.ToInt32(objSqlParam[2].Value);
                if (objSqlParam[3].Value != DBNull.Value)
                    rData.ReturnMessage = objSqlParam[3].Value.ToString();
                if (objSqlParam[4].Value != DBNull.Value)
                {
                    rData.InputFileID = Convert.ToInt32(objSqlParam[4].Value);
                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File inserted successfully. ");
                }
            }
            catch (Exception ex)
            {
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Exception occured: " + ex.Message);
                throw ex;
            }
            return rData;
        }
        #endregion

        #region "InsertIDXScannedReturnFile                   "
        static ReturnData InsertIDXScannedReturnFile(DataTable dt, int InputFileID)
        {
            strSource = "InsertIDXScannedReturnFile()";
            ReturnData rData = new ReturnData();
            try
            {
                SqlParameter[] objSqlParam = new SqlParameter[5];
                objSqlParam = SqlHelperParameterCache.GetSpParameterSet(Constants.DB_CONN_STRING, "Usp_PEBT_ImportScannedReturnMail");
                objSqlParam[0].TypeName = "ScannedReturnMailDataTableType";
                objSqlParam[0].SqlDbType = SqlDbType.Structured;
                objSqlParam[0].Value = dt;
                objSqlParam[1].Value = InputFileID;
                objSqlParam[2].Value = DBNull.Value;
                objSqlParam[3].Value = DBNull.Value;
                SqlHelper.ExecuteNonQuery(Constants.DB_CONN_STRING, CommandType.StoredProcedure, "Usp_PEBT_ImportScannedReturnMail", Constants.timeOut, objSqlParam);

                if (objSqlParam[2].Value != DBNull.Value)
                    rData.ReturnCode = Convert.ToInt32(objSqlParam[2].Value);
                if (objSqlParam[3].Value != DBNull.Value)
                    rData.ReturnMessage = objSqlParam[3].Value.ToString();
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File data successfully inserted into DataTable MailInputFileData. ");
            }
            catch (Exception ex)
            {
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Exception occured: " + ex.Message);
                throw ex;
            }
            return rData;
        }
        #endregion

        #region "GetRecordsNotMatched                   "
        static DataTable GetRecordsNotMatched(string strfileIDS)
        {
            strSource = "GetRecordsNotMatched()";
            ReturnData rData = new ReturnData();
            try
            {
                SqlParameter[] objSqlParam = new SqlParameter[1];
                objSqlParam = SqlHelperParameterCache.GetSpParameterSet(Constants.DB_CONN_STRING, "Usp_PEBT_ScannedReturn_GetRecordsNotMatched");

                objSqlParam[0].Value = strfileIDS;
                return SqlHelper.ExecuteDataset(Constants.DB_CONN_STRING, CommandType.StoredProcedure, "Usp_PEBT_ScannedReturn_GetRecordsNotMatched", Constants.timeOut, objSqlParam).Tables[0];
            }
            catch (Exception ex)
            {
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Exception occured: " + ex.Message);
                throw ex;
            }
        }
        #endregion

        #region "GenerateCSV                            "
        /// <summary>
        /// For Generating the CSV File with Data
        /// </summary>
        private static bool GenerateCSV(DataTable dt, string _strFilePath)
        {
            bool returnValue = false;
            StringBuilder builder = new StringBuilder();
            string _Delimiter = ",";
            string _TextQualifier = "";
            try
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dc.ColumnName == dt.Columns[dt.Columns.Count - 1].ColumnName)
                        builder.Append(_TextQualifier + dc.ColumnName + _TextQualifier);
                    else
                        builder.Append(_TextQualifier + dc.ColumnName + _TextQualifier + _Delimiter);
                }
                builder.Append("\r\n");
                foreach (DataRow row in dt.Rows)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (dc.ColumnName == dt.Columns[dt.Columns.Count - 1].ColumnName)
                            builder.Append(_TextQualifier + row[dc.ColumnName] + _TextQualifier);
                        else
                            builder.Append(_TextQualifier + row[dc.ColumnName] + _TextQualifier + _Delimiter);
                    }
                    builder.Append("\r\n");
                }

                if (Directory.Exists(_strFilePath))
                    Directory.CreateDirectory(_strFilePath);

                File.WriteAllText(_strFilePath, builder.ToString());
                returnValue = true;
            }
            catch (Exception ex)
            {
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Exception occured: " + ex.Message);
                return false;
            }
            return returnValue;
        }

        #endregion

        public static T Do<T>(Func<string, T> action, string P1)
        {
            double retryInterval = TimeSpan.FromSeconds(double.Parse(Constants.EXCEPTION_SLEEP.ToString())).TotalMilliseconds;
            int maxAttemptCount = Constants.RetryCount;
            var exceptions = new List<Exception>();
            StringBuilder sb = new StringBuilder();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(int.Parse(retryInterval.ToString()));
                    }
                    sb = new StringBuilder();
                    return action(P1);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    sb.Append(string.Concat(ex.Message, "<br/>"));
                }
            }

           // myEmail.SendEmail(Constants.FromEmail, Constants.Admin_ToEmail, Constants.Admin_CCEmail, Constants.Admin_BCCEmail, ex.Message.ToString(), "", "HTML", Constants.Admin_Subject, Constants.RuntimeError_FailureTemplate, "");
            throw new AggregateException("innerexception");
        }
        public static T Do<T>(Func<DataTable, int, T> action, DataTable P1, int P2)
        {
            double retryInterval = TimeSpan.FromSeconds(double.Parse(Constants.EXCEPTION_SLEEP.ToString())).TotalMilliseconds;
            int maxAttemptCount = Constants.RetryCount;
            var exceptions = new List<Exception>();
            StringBuilder sb = new StringBuilder();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(int.Parse(retryInterval.ToString()));
                    }
                    sb = new StringBuilder();
                    return action(P1, P2);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    sb.Append(string.Concat(ex.Message, "<br/>"));
                }
            }


            throw new AggregateException("innerexception");
        }
    }

    public class ReturnData
    {
        public int ReturnCode { get; set; }
        public string ReturnMessage { get; set; }
        public int InputFileID { get; set; }

    }
}
