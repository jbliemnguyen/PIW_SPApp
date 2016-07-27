using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security;
using System.ServiceModel.Channels;
using System.Web;
using Email_Service.Entity;
using FERC.Common.Queues;
using Microsoft.SharePoint.Client;
using Org.BouncyCastle.Asn1.X509;

namespace PIW_SPAppWeb.Helper
{
    public class Email
    {
        SharePointHelper helper = new SharePointHelper();
        public void SendEmail(ClientContext clientContext, ListItem listItem, enumAction action, string Status, string nextStatus,
            string currentUserName, string formURL)
        {
            var piwListInteralColumnNames = helper.getInternalColumnNamesFromCache(clientContext,Constants.PIWListName);

            var docket = listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]].ToString()
                : string.Empty;

            var formType = listItem[piwListInteralColumnNames[Constants.PIWList_colName_FormType]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_FormType]].ToString() : string.Empty;

            switch (Status)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Recalled:
                case Constants.PIWList_FormStatus_Rejected:
                    if (action.Equals(enumAction.Submit))
                    {
                        if (formType.Equals(Constants.PIWList_FormType_StandardForm))
                        {
                            string subject = "PIW - Workflow Item Submitted for Processing";
                            string message = String.Format(@"Workflow Item <a href='{0}'>{1}</a> has been submitted for processing in Publish Issuance Workflow by {2}",
                                formURL, docket, currentUserName);
                            string htmlContent = getHTMLFullMessageContent(clientContext, listItem, message);
                            List<String> To = new List<string>();
                            To.Add("liem.nguyen@ferc.gov");
                            SendEmail(To,subject,htmlContent);
                        }
                        else if (formType.Equals(Constants.PIWList_FormType_AgendaForm))
                        {

                        }
                    }
                    else if (action.Equals(enumAction.Publish))
                    {
                        if (formType.Equals(Constants.PIWList_FormType_DirectPublicationForm))
                        {

                        }
                    }

                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    break;
                default:
                    break;
            }

        }

        private void getSubjectAndContent(ClientContext clientContext, string action, string nextStatus, ref string subject,
            ref string content)
        {

        }

        private void SendEmail(List<string> ToAddress, string subject, string htmlContent)
        {
            Email_Job msg = new Email_Job();
            msg.From = "piw@ferc.gov";
            msg.To = ToAddress;
            msg.Subject = subject;
            msg.Body = htmlContent;

            string jobqueue = ConfigurationManager.AppSettings["eMailqueue"].ToString();
            QueueSender<QueueMessage<Email_Job>> qs = new QueueSender<QueueMessage<Email_Job>>(jobqueue);
            qs.Send(new QueueMessage<Email_Job>(string.Empty, 1, msg));
        }

        public string getHTMLFullMessageContent(ClientContext clientContext, ListItem listItem, string message)
        {
            var piwListInteralColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            var description = listItem[piwListInteralColumnNames[Constants.PIWList_colName_Description]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_Description]].ToString() : string.Empty;

            var initiatorOffice = listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString() : string.Empty;

            var documentCategory = listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocumentCategory]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocumentCategory]].ToString() : string.Empty;

            var createdDate = listItem[piwListInteralColumnNames["Created"]] != null
                ? listItem[piwListInteralColumnNames["Created"]].ToString() : string.Empty;

            var PublicDocsURL = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PublicDocumentURLs]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_PublicDocumentURLs]].ToString() : string.Empty;

            var CEIIDocsURL = listItem[piwListInteralColumnNames[Constants.PIWList_colName_CEIIDocumentURLs]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_CEIIDocumentURLs]].ToString() : string.Empty;

            var PriviledgedDocsURL = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PriviledgedDocumentURLs]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_PriviledgedDocumentURLs]].ToString() : string.Empty;

            
            string fileNameListHTML = helper.getDocumentURLsHTML(PublicDocsURL, CEIIDocsURL, PriviledgedDocsURL, true);

            string[] args = new string[] { message, fileNameListHTML, description, initiatorOffice, documentCategory, createdDate };
            string htmlContent = string.Format(@"<html>
                                                    <body> 
                                                            {0}<br/><br/>
                                                            - File Name: {1}<br/>                                                            
                                                            - Description: {2}<br/>
                                                            - Initiator Office: {3}<br/>
                                                            - Document Category: {4}<br/> 
                                                            - Created Date: {5}                                                       
                                                    </body>
                                                 </html>", args);
            return htmlContent;
        }
    }
}