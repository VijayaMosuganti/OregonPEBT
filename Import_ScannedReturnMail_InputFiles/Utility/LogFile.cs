using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;


namespace Import_ScannedReturnMail_InputFiles.Util
{
    class LogFile
    {
        #region "enums                    "
        // Member Vars.
        public enum LogLevel
        {
            ERROR = 1,
            DEBUG = 2,
            LOW = 3,
            LOWEST = 4
        }

        private enum Mode
        {
            NONE = 0,
            LOW = 1,
            MEDIUM = 2,
            HIGH = 3,
            ALL = 4
        }
        #endregion

        #region "Class Variables          "

        private int m_maxFileSize; //Value to denote the maximum size of log file.
        private int m_debugMode; // Value to denote the Mode.
        private string m_fileName; //Value to denote the name of the log file.
        private static object m_syncRoot = new object(); //static synchronization root object, for locking
        private static LogFile _logFileInstance; //Static variable that represents the LogFile class.

        #endregion

        #region "Properties               "
        public string FileName
        {
            set
            {
                m_fileName = value;
            }
        }// FileName property.

        public int DebugMode
        {
            set
            {
                m_debugMode = value;
            }// get
        }// DebugMode property.

        #endregion

        #region "Constructor              "
        /// <summary>
        /// Constructor.
        /// </summary>
        private LogFile()
        {
            // Set the default log file maximum size to 5Mb.
            m_maxFileSize = Convert.ToInt32(Constants.LOGFILESIZE);

            // Set the default debug mode to False. i.e. Do not log the debug messages.
            m_debugMode = (int)Mode.NONE;
        }// Constructor.
        #endregion

        #region "GetLogFileInstance       "
        /// <summary>
        /// This method returns the Logfile if its instance already exists, else creates a new object and returns it.
        /// </summary>
        /// <returns>LogFile, instance of the logfile class.</returns>
        public static LogFile GetLogFileInstance()
        {
            // Using 'Lazy initialization' 
            if (_logFileInstance == null)
            {
                //locking the object.
                lock (m_syncRoot)
                {
                    _logFileInstance = new LogFile();
                }
            }
            return _logFileInstance;
        }
        #endregion

        #region "LogMessage               "

        /// <summary>
        /// Method to write a message to the log file. 
        /// </summary>
        /// <param name="level">Input log level for the message</param>
        /// <param name="strInputMsg">Input string message</param>
        /// <param name="strSource">Source of the message</param>
        public void LogMessage(int level, string strSource, string strInputMsg)
        {
            TextWriter twLogFile;

            try
            {
                // Do not log unless m_debugMode is something greater than the current level.
                if (m_debugMode < level)
                {
                    return;
                }

                // To make it thread safe -- that is only one thread can get into this block of code.
                lock (this)
                {
                    // Check to see if current log file size exceeds the maxSize.
                    if (m_maxFileSize > 0)
                    {
                        CheckLogSize();
                    }// To see if we want to check log size and create another log file.

                    try
                    {
                        // Open the logfile for writing - create it if it does not exist.
                        if (!File.Exists(m_fileName))
                        {
                            FileInfo fInfo = new FileInfo(m_fileName);
                            fInfo.Directory.Create();
                        }
                        twLogFile = File.AppendText(m_fileName);

                        // Write out the datetime and message.
                        twLogFile.WriteLine(DateTime.Now.ToString() + "\t" + strSource + ": " + strInputMsg); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").

                        // Close the writer and underlying file.
                        twLogFile.Close();
                    }// Try.
                    catch (Exception e)
                    {
                        try
                        {
                            // Also write to another file, the error message.
                            twLogFile = File.AppendText(m_fileName + ".ERROR");

                            // Write out the datetime and message.
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tToString: " + e.ToString()); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tMessage: " + e.Message); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tSource: " + e.Source); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tTargetSite: " + e.TargetSite); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tStackTrace: " + e.StackTrace); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").

                            // Close the writer and underlying file.
                            twLogFile.Close();
                        }
                        catch (Exception)
                        {
                            // Ignore the second exception, if any.
                        }// Catch.
                    }// catch.

                    Monitor.PulseAll(this);
                }// Lock.
            }// Try.
            catch (Exception e)
            {
                try
                {
                    // Also write to another file, the error message.
                    twLogFile = File.AppendText(m_fileName + ".ERROR");

                    // Write out the datetime and message.
                    twLogFile.WriteLine(DateTime.Now.ToString() + "\tToString: " + e.ToString());    // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                    twLogFile.WriteLine(DateTime.Now.ToString() + "\tMessage: " + e.Message);        // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                    twLogFile.WriteLine(DateTime.Now.ToString() + "\tSource: " + e.Source);          // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                    twLogFile.WriteLine(DateTime.Now.ToString() + "\tTargetSite: " + e.TargetSite); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                    twLogFile.WriteLine(DateTime.Now.ToString() + "\tStackTrace: " + e.StackTrace); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").

                    // Close the writer and underlying file.
                    twLogFile.Close();
                }
                catch (Exception)
                {
                    // Ignore the second exception, if any.
                }// Catch.
            }// Catch.
        }// LogMessage.

        #endregion

        #region "CheckLogSize             "

        /// <summary>
        /// Method to check the log file size and archive it, if it exceeds the set limit.
        /// </summary>
        protected void CheckLogSize()
        {
            FileInfo checkFile = new FileInfo(m_fileName);

            // Check size of log. If greater than m_maxFileSize archive and start a new one.
            // Multiply m_maxFileSize by 1048576 (= 1Mb) to get the number of bytes as length is in bytes.
            if (checkFile.Exists)
            {
                if (checkFile.Length >= (m_maxFileSize * 1048576))
                {
                    // Get the unique temp names for this run.
                    DateTimeFormatInfo dfi = new DateTimeFormatInfo();
                    dfi.ShortDatePattern = "MM-dd-yyyy";
                    dfi.ShortTimePattern = "HH-mm-ss";
                    string strTime = DateTime.Now.ToString("d", dfi); //"MM-dd-yyyy";
                    strTime += "_" + DateTime.Now.ToString("t", dfi); //"HH-mm-ss";
                    string strNewFileName = m_fileName + "." + strTime;
                    try
                    {
                        File.Move(m_fileName, strNewFileName);
                    }
                    catch (Exception e)
                    {
                        TextWriter twLogFile;
                        try
                        {
                            // Also write to another file, the error message.
                            twLogFile = File.AppendText(m_fileName + ".ERROR");

                            // Write out the datetime and message.
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tToString: " + e.ToString()); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tMessage: " + e.Message); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tSource: " + e.Source); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tTargetSite: " + e.TargetSite); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").
                            twLogFile.WriteLine(DateTime.Now.ToString() + "\tStackTrace: " + e.StackTrace); // Format(DateTime.Now, "dd-MMM-yyyy hh:mm:ss").

                            // Close the writer and underlying file.
                            twLogFile.Close();
                        }
                        catch (Exception)
                        {
                            // Ignore the second exception, if any.
                        }// Catch.
                    }// Catch.
                }// If size has exceeded.
            }// If file exists.
        }// CheckLogSize.
        #endregion
    }
}
