using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Data;
using System.Data.SqlClient;
using Microsoft.ApplicationBlocks.Data;
using System.Globalization;
using System.Net.Mail;

namespace PEBT.Util
{
	class Mail
	{
		public Mail()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public bool SendEmail(string FromEmail, string ToEmailId, string CcEmailIds, string BccEmailIds, string filename, string location, string BodyFormat, string mailSubject, string EmailTemplatePath, string Attachment)
		{

			string strBody = ""; // Mail body
			bool result = false;
			//string mailSubject = "";

			//Build the Array Object that contains Static values
			string[] lableArr = { "filename", "location" };
			string[] valueArr = new string[2];

			try
			{
				//value array contains the values that corresponding to lableArr() object
				valueArr[0] = filename;
				valueArr[1] = location;
				//read the file format variables by calling the  FunMailBody
				strBody = FunMailBody(lableArr, valueArr, EmailTemplatePath);

				//mailSubject =  System.Configuration.ConfigurationSettings.AppSettings["Subject"].ToString();

				//Performs the Send Mail operation
				result = SendMimeEmail(ToEmailId, CcEmailIds, BccEmailIds, strBody.ToString(), mailSubject);
			}//try
			catch (Exception ex)
			{
				throw ex;
			}

			return result;

		}//SendMail



		#region SendMimeEmail
		public bool SendMimeEmail(string ToEmailId, string CcEmailIds, string BccEmailIds, string htmlMailBody, string Subject)
		{
			try
			{
				if (ToEmailId.Length > 0)
				{
					SmtpClient client = new SmtpClient(Constants.SMTPSERVERNAME);
					MailAddress from = new MailAddress(Constants.FromEmail, Constants.FromName);
					System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
					message.From = from;
					message.To.Add(ToEmailId);
					if (!string.IsNullOrEmpty(CcEmailIds))
					{
						message.CC.Add(CcEmailIds);
					}

					if (!string.IsNullOrEmpty(BccEmailIds))
					{
						message.Bcc.Add(BccEmailIds);
					}

					message.Subject = Subject.Replace("\r\n", "");
					//message.Subject = Subject.TrimEnd("\r\n".ToCharArray());
					message.IsBodyHtml = true;
					message.Body = htmlMailBody;

					client.Send(message);

					return true;

				}//if successful
				else
				{
					return false;
				}//else
			}
			catch (Exception ex)
			{
				//ApplicationLog.WriteToLog(
				return false;
			}
		}
		#endregion

		#region "FunMailBody          "

		/// <summary>
		/// Replaces values in the email template with the original values
		/// </summary>
		/// <param name="lblArr">Attribute name that need to be replaced</param>
		/// <param name="valArr">Value that should be placed</param>
		/// <param name="strFileName">Path and filename of the email template</param>
		/// <returns>The replaced text in the email template</returns>
		static string FunMailBody(string[] lblArr, string[] valArr, string EmailTemplatePath)
		{

			StreamReader objReader; //To read file
			string strLineData = ""; //To hold the message
			string strLine = ""; //To hold a line of message
			string strFileName = "";

			//strFileName = Constants.EMAIL_TEMP_PATH;
			strFileName = EmailTemplatePath;

			try
			{
				//set the file to reader
				objReader = File.OpenText(strFileName);
				//Read the objectReader line by line
				while (objReader != null)
				{
					strLine = objReader.ReadToEnd();
					if (strLine != null)
					{
						if (strLine != "")
						{
							strLineData += "\n";
							strLineData += strLine;
						}
						else
						{
							break;
						}
					}
					else
					{
						break;
					}
				}
				objReader.Close(); //closes the reader 

				//Replace the Value varibles with Value array
				for (int iCnt = 1; iCnt <= valArr.Length; iCnt++)
				{
					strLineData = strLineData.Replace("<V" + Convert.ToString(iCnt) + ">", valArr[iCnt - 1].Trim());
				}
				strLineData = strLineData.Replace("\r\n", "\n");

			}
			catch (Exception ex)
			{
				if (ex != null)
				{
				}
			}

			return strLineData;
		}//FunMailBody

		#endregion
		
	}
}
