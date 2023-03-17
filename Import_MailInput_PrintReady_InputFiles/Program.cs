using Microsoft.ApplicationBlocks.Data;
using PEBT.Util;
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

namespace Import_MailInput_PrintReady_InputFiles
{
    class Program
    {
        #region "Variables Used                         "

        static LogFile myLogFile = null;
        static string strSource = string.Empty;
        static Mail myEmail;
        static string strPEBTFileName = "PEBT.txt";
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

        #region "ProcesstxtFiles                        "
        /// <summary>
        /// This method is used to process each  file
        /// </summary>
        /// <returns></returns>
        static void ProcessFiles()
        {
            bool result = false;
            strSource = "ProcessFiles()";
            myEmail = new Mail();
            try
            {
                string mailInputfilename = string.Empty;
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Checking whether there are any PEBT(MailInput) files in the input folder");
                if (Directory.GetFiles(Constants.MailInputFile_SourceFilesFolder, "*.csv").Count() > 0)
                {
                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, Directory.GetFiles(Constants.MailInputFile_SourceFilesFolder, "*.csv").Count() + " PEBT(MailInput) file(s) were found.");

                    foreach (string strFile in Directory.GetFiles(Constants.MailInputFile_SourceFilesFolder, "*.csv"))
                    {
                        try
                        {
                            mailInputfilename = string.Empty;
                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Processing the file:" + Path.GetFileName(strFile));
                            if (!Directory.Exists(Constants.MailInputFile_InputFileFolder))
                            {
                                Directory.CreateDirectory(Constants.MailInputFile_InputFileFolder);
                            }
                            string ProcessedFilePath = Path.Combine(Constants.MailInputFile_InputFileFolder, strPEBTFileName);
                            File.Copy(strFile, ProcessedFilePath, true);

                            StreamReader streamreader = new StreamReader(ProcessedFilePath);
                            string fileHeader = streamreader.ReadLine();
                            streamreader.Close();
                            if (fileHeader.ToString() == Constants.PEBT_Header.ToString())
                            {
                                strCurrentFile = Path.GetFileName(strFile);
                                ReturnData rfile = Do(InsertFile,strCurrentFile);
                                if (rfile != null && rfile.InputFileID > 0)
                                {
                                    DataTable dt = GetDataFromFileToDataTable(ProcessedFilePath, strPEBTFileName, Constants.MailInputFile_InputFileFolder);
                                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully converted into DataTable. ");

                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Inserting the data to MailInputFileData table");
                                        rfile = Do(InsertPEBTManiestFile, dt, rfile.InputFileID);

                                        if (rfile != null && rfile.ReturnCode == 0)
                                            result = true;
                                        //else if (rfile != null && rfile.ReturnCode == 2)
                                        //{
                                        //    result = false;
                                        //    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File Contains Duplicate Piece ID's.");
                                        //    DataSet ds = rfile.DuplicatePieceIDList;
                                        //    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                        //    {
                                        //        DataTable DuplicatePieceIDList = ds.Tables[0];
                                        //        mailInputfilename = Path.GetFileName(strFile);
                                        //        string strBodyData = string.Empty;
                                        //        strBodyData = BuildHTML(DuplicatePieceIDList);
                                        //        myEmail.SendEmail(Constants.FromEmail, Constants.DuplicatePieceIDS_ToEmail, Constants.DuplicatePieceIDS_CCEmail, Constants.DuplicatePieceIDS_BCCEmail, mailInputfilename, strBodyData, "HTML", Constants.DuplicatePieceIDS_Subject, Constants.DuplicatePieceIDS_EmailTemplate, "");
                                        //    }
                                        //}
                                        else
                                            result = false;

                                        if (result)
                                        {
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Data inserted to MailInputFileData table");
                                            if (!Directory.Exists(Constants.MailInputFile_ArchiveFolder))
                                            {
                                                Directory.CreateDirectory(Constants.MailInputFile_ArchiveFolder);
                                            }
                                            //Archiving Success file
                                            File.Copy(strFile, Path.Combine(Constants.MailInputFile_ArchiveFolder, Path.GetFileName(strFile)), true);
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully moved to Archive Folder.");
                                            File.Delete(strFile);
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Input file successfully Deleted from Shared Folder.");
                                        }
                                        else
                                        {
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Data insertion failed, moving the file to error folder");
                                            if (!Directory.Exists(Constants.MailInputFile_ErrorFolder))
                                            {
                                                Directory.CreateDirectory(Constants.MailInputFile_ErrorFolder);
                                            }
                                            File.Copy(strFile, Path.Combine(Constants.MailInputFile_ErrorFolder, Path.GetFileName(strFile)), true);
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully moved to Error Folder.");
                                            File.Delete(strFile);
                                            myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Input file successfully Deleted from Shared Folder.");
                                            // mailInputfilename = Path.GetFileName(strFile);
                                            //myEmail.SendEmail(Constants.FromEmail, Constants.ToEmail, Constants.CCEmail, mailInputfilename, Constants.MailInputFile_ErrorFolder, "HTML", Constants.FailedSubject.Replace("{filename}", mailInputfilename), Constants.FailedTemplate, "");
                                        }
                                    }
                                }
                                else if (rfile.ReturnCode == 2)//Duplicate file
                                {
                                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Duplicate File Found. Moving file to Archive Folder.");
                                    if (!Directory.Exists(Constants.MailInputFile_ArchiveFolder))
                                    {
                                        Directory.CreateDirectory(Constants.MailInputFile_ArchiveFolder);
                                    }
                                    File.Copy(strFile, Path.Combine(Constants.MailInputFile_ArchiveFolder, Path.GetFileName(strFile)), true);
                                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully moved to Archive Folder.");
                                    File.Delete(strFile);
                                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Input file successfully Deleted from Shared Folder.");
                                }
                            }
                            else
                            {
                                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Header mis match.,moving the file to error folder and sending the email");
                                if (!Directory.Exists(Constants.MailInputFile_ErrorFolder))
                                {
                                    Directory.CreateDirectory(Constants.MailInputFile_ErrorFolder);
                                }
                                File.Copy(strFile, Path.Combine(Constants.MailInputFile_ErrorFolder, Path.GetFileName(strFile)), true);
                                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File successfully moved to Error Folder.");
                                File.Delete(strFile);
                                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Input file successfully Deleted from Shared Folder.");
                                mailInputfilename = Path.GetFileName(strFile);
                                myEmail.SendEmail(Constants.FromEmail, Constants.HeaderMismatch_ToEmail, Constants.HeaderMismatch_CCEmail, Constants.HeaderMismatch_BCCEmail, mailInputfilename, Constants.MailInputFile_ErrorFolder, "HTML", Constants.HeaderMismatch_Subject.Replace("{filename}", mailInputfilename), Constants.HeaderMismatch_EmailTemplate, "");
                            }

                        }
                        catch (Exception iex)
                        {
                            File.Copy(strFile, Path.Combine(Constants.MailInputFile_ErrorFolder, Path.GetFileName(strFile)), true);
                            File.Delete(strFile);
                            myEmail.SendEmail(Constants.FromEmail, Constants.Admin_ToEmail, Constants.Admin_CCEmail, Constants.Admin_BCCEmail, iex.Message.ToString(), "", "HTML", Constants.Admin_Subject.Replace("{filename}", mailInputfilename), Constants.RuntimeError_FailureTemplate, "");
                        }
                    }
                }
                else
                {
                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "No PEBT files were found to process.");
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
        private static DataTable GetDataFromFileToDataTable(string filePath, string strPEBTFileName, string inputfilefolder)
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
                streamwrt.WriteLine("[" + strPEBTFileName + "]");
                streamwrt.WriteLine("Format=Delimited(,)");
                streamwrt.WriteLine("ColNameHeader=TRUE");
                streamwrt.WriteLine("Col1=\"DateProcessedInspire\" Text");
                streamwrt.WriteLine("Col2=\"BatchID\" Text");
                streamwrt.WriteLine("Col3=\"SequenceNumber\" Text");
                streamwrt.WriteLine("Col4=\"PBT_FileName\" Text");
                streamwrt.WriteLine("Col5=\"PBT_FileCreationDate\" Text");
                streamwrt.WriteLine("Col6=\"PBT_FileFrequency\" Text");
                streamwrt.WriteLine("Col7=\"PBT_Environment\" Text");
                streamwrt.WriteLine("Col8=\"FileControlNum\" Text");
                streamwrt.WriteLine("Col9=\"NoticeID\" Text");
                streamwrt.WriteLine("Col10=\"CaseNumber\" Text");
                streamwrt.WriteLine("Col11=\"FirstName\" Text");
                streamwrt.WriteLine("Col12=\"MiddleInitital\" Text");
                streamwrt.WriteLine("Col13=\"LastName\" Text");
                streamwrt.WriteLine("Col14=\"Suffix\" Text");
                streamwrt.WriteLine("Col15=\"AddressLine1\" Text");
                streamwrt.WriteLine("Col16=\"AddressLine2\" Text");
                streamwrt.WriteLine("Col17=\"City\" Text");
                streamwrt.WriteLine("Col18=\"State\" Text");
                streamwrt.WriteLine("Col19=\"Zip+4\" Text");
                streamwrt.WriteLine("Col20=\"Lang\" Text");
                streamwrt.WriteLine("Col21=\"ReturnCode\" Text");
                streamwrt.WriteLine("Col22=\"IMB_String\" Text");
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
                objSqlParam[1].Value = DBNull.Value;
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
                    myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "File inserted successfly. ");
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

        #region "InsertPEBTManiestFile                   "
        static ReturnData InsertPEBTManiestFile(DataTable dt, int InputFileID)
        {
            strSource = "InsertPEBTManiestFile()";
            ReturnData rData = new ReturnData();
            try
            {
                SqlParameter[] objSqlParam = new SqlParameter[5];
                objSqlParam = SqlHelperParameterCache.GetSpParameterSet(Constants.DB_CONN_STRING, "Usp_PEBT_Import_MailInputAndPrintReadyFilesData");
                objSqlParam[0].TypeName = "MailInputFileDataTableType";
                objSqlParam[0].SqlDbType = SqlDbType.Structured;
                objSqlParam[0].Value = dt;
                objSqlParam[1].Value = InputFileID;
                objSqlParam[2].Value = DBNull.Value;
                objSqlParam[3].Value = DBNull.Value;
                objSqlParam[4].Value = DBNull.Value;
                 SqlHelper.ExecuteNonQuery(Constants.DB_CONN_STRING, CommandType.StoredProcedure, "Usp_PEBT_Import_MailInputAndPrintReadyFilesData", Constants.timeOut, objSqlParam);
               // DataSet ds = SqlHelper.ExecuteDataset(Constants.DB_CONN_STRING, CommandType.StoredProcedure, "Usp_PEBT_Import_MailInputAndPrintReadyFilesData", Constants.timeOut, objSqlParam);
               // rData.DuplicatePieceIDList = ds;
                if (objSqlParam[3].Value != DBNull.Value)
                    rData.ReturnCode = Convert.ToInt32(objSqlParam[3].Value);
                if (objSqlParam[4].Value != DBNull.Value)
                    rData.ReturnMessage = objSqlParam[4].Value.ToString();
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

        #region "BuildHTML                                              "

        public static string BuildHTML(DataTable dtDetails)
        {
            string strSource = "BuildHTML";
            string myHtml = "";
            try
            {

                if (dtDetails != null && dtDetails.Rows.Count > 0)
                {
                    StringBuilder myBuilder = new StringBuilder();

                    myBuilder.Append("<table border='1' cellpadding='0' cellspacing='0' style='border-color:gray;'>");

                    myBuilder.Append("<tr align='left' valign='top'>");
                    foreach (DataColumn dc in dtDetails.Columns)
                    {
                        myBuilder.Append("<td align='center' valign='top' style='padding:5px; border-left:none;border-right:none;border-top:none;border-bottom:none;'><b>" + dc.ColumnName.ToString() + "</b></td>");
                        // myBuilder.Append("<td align='center' valign='top' style='padding-left:5px; padding-right:5px;border-left:none; border-top:none; border-bottom:solid 0.5 px gray;border-right:none;'><b>" + dc.ColumnName.ToString() + "</b></td>");
                    }
                    myBuilder.Append("</tr>");


                    for (int i = 0; i < dtDetails.Rows.Count - 1; i++)
                    {
                        myBuilder.Append("<tr align='left' valign='top'>");

                        for (int j = 0; j < dtDetails.Columns.Count; j++)
                        {
                            myBuilder.Append("<td align='left' valign='top' style='padding:5px; border-left:none;border-right:none;border-bottom:none;'>");
                            //myBuilder.Append("<td align='left' valign='top' style='padding-left:5px; padding-right:5px; border-left:none; border-top:none; border-bottom:solid 0.5 px gray; border-right:none;'>");
                            myBuilder.Append(dtDetails.Rows[i][j].ToString());

                            myBuilder.Append("</td>");
                        }
                        myBuilder.Append("</tr>");
                    }

                    myBuilder.Append("<tr align='left' valign='top'>");
                    int LastRowNo = dtDetails.Rows.Count - 1;
                    for (int j = 0; j < dtDetails.Columns.Count; j++)
                    {
                        myBuilder.Append("<td align='left' valign='top' style='padding:5px; border-left:none; border-right:none;border-bottom:none;'>");
                        //myBuilder.Append("<td align='left' valign='top' style='padding-left:5px; padding-right:5px; border-left:none; border-top:none;border-bottom:solid 0px Gray; border-right:none;'>");
                        myBuilder.Append(dtDetails.Rows[LastRowNo][j].ToString());

                        myBuilder.Append("</td>");
                    }
                    myBuilder.Append("</tr>");

                    myBuilder.Append("</table>");
                    //myBuilder.Append("</body>");

                    myHtml = myBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                myLogFile.LogMessage((int)LogFile.LogLevel.DEBUG, strSource, "Exception occured: " + ex.Message.ToString());

            }
            return myHtml;
        }
        #endregion

        public static T Do<T>(Func<string,T> action, string P1)
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

           
            throw new AggregateException("innerexception");
        }
        public static T Do<T>(Func<DataTable,int, T> action, DataTable P1,int P2)
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
                    return action(P1,P2);
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

        public DataSet DuplicatePieceIDList { get; set; }
    }
}
