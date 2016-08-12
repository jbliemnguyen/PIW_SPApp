using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Security;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.UI.WebControls;
using FERC.Common.Queues;
using Microsoft.SharePoint.Client;
using Org.BouncyCastle.Asn1.X509;
using ListItem = Microsoft.SharePoint.Client.ListItem;

namespace PIW_SPAppWeb.Helper
{
    public class Email
    {
        SharePointHelper helper = new SharePointHelper();

        
        /// <summary>
        /// Call from Scheduler or system service
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="listItem"></param>
        /// <param name="action"></param>
        /// <param name="currentUser"></param>
        public void SendEmail(ClientContext clientContext, ListItem listItem, enumAction action, User currentUser)
        {
            
        }
        
        
        /// <summary>
        /// called from the Edit Form
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="listItem"></param>
        /// <param name="action"></param>
        /// <param name="CurrentFormStatus"></param>
        /// <param name="previousFormStatus"></param>
        /// <param name="currentUser"></param>
        /// <param name="formURL"></param>
        /// <param name="hdnWorkflowInitiator"></param>
        /// <param name="hdnDocumentOwner"></param>
        /// <param name="hdnNotificationRecipient"></param>
        /// <param name="comment"></param>
        public void SendEmail(ClientContext clientContext, ListItem listItem, enumAction action, string CurrentFormStatus, string previousFormStatus,
            User currentUser, string formURL, HiddenField hdnWorkflowInitiator, 
            HiddenField hdnDocumentOwner, HiddenField hdnNotificationRecipient,string comment)
        {
            var piwListInteralColumnNames = helper.getInternalColumnNamesFromCache(clientContext,Constants.PIWListName);
            

            var initiatorEmails = string.IsNullOrEmpty(hdnWorkflowInitiator.Value) ? null : 
                PeoplePickerHelper.GetValuesFromPeoplePicker(hdnWorkflowInitiator).Select(t => t.Email);

            var documentOwnerEmails = string.IsNullOrEmpty(hdnDocumentOwner.Value) ? null :
                PeoplePickerHelper.GetValuesFromPeoplePicker(hdnDocumentOwner).Select(t => t.Email);

            var notificationRecipientEmails = string.IsNullOrEmpty(hdnNotificationRecipient.Value) ? null :
                PeoplePickerHelper.GetValuesFromPeoplePicker(hdnNotificationRecipient).Select(t => t.Email);
            
            var docket = listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]].ToString()
                : string.Empty;

            var formType = listItem[piwListInteralColumnNames[Constants.PIWList_colName_FormType]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_FormType]].ToString() : string.Empty;

            SendEmail(clientContext, listItem, action, CurrentFormStatus, previousFormStatus, currentUser, formURL, comment, formType, docket, initiatorEmails, documentOwnerEmails, notificationRecipientEmails);

        }

        private void SendEmail(ClientContext clientContext, ListItem listItem, enumAction action, string CurrentFormStatus,
            string previousFormStatus, User currentUser, string formURL, string comment, string formType, string docket,
            IEnumerable<string> initiatorEmails, IEnumerable<string> documentOwnerEmails, IEnumerable<string> notificationRecipientEmails)
        {
            switch (CurrentFormStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Recalled:
                case Constants.PIWList_FormStatus_Rejected:
                    if (action.Equals(enumAction.Submit))
                    {
                        if (formType.Equals(Constants.PIWList_FormType_StandardForm))
                        {
                            string subject = "PIW - Workflow Item Submitted for Processing";
                            string message = String.Format(@"Workflow Item <a href='{0}'>{1}</a> 
                                            has been submitted for processing in Publish Issuance Workflow by {2}.",
                                formURL, docket, currentUser.Title);
                            string htmlContent = getHTMLFullMessageContent(clientContext, listItem, message);
                            String To = string.Empty;

                            //email to initiator, document owner and notification recipient
                            To = AddEmailAddress(To, initiatorEmails);
                            To = AddEmailAddress(To, documentOwnerEmails);
                            To = AddEmailAddress(To, notificationRecipientEmails);


                            SendEmail(clientContext,To, subject, htmlContent);
                        }
                        else if (formType.Equals(Constants.PIWList_FormType_AgendaForm))
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else if (action.Equals(enumAction.Publish))
                    {
                        if (formType.Equals(Constants.PIWList_FormType_DirectPublicationForm))
                        {
                            throw new NotImplementedException();
                        }
                    }

                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    if (action.Equals(enumAction.OSECTakeOwnerShip))
                    {
                        if (formType.Equals(Constants.PIWList_FormType_StandardForm))
                        {
                            //PIW - OSEC Took Ownership of Workflow Item
                            string subject = "PIW - OSEC Took Ownership of Workflow Item";
                            string message = String.Format
                                (@"OSEC has taken ownership of the following Workflow Item in Publish Issuance Workflow: 
                                            <a href='{0}'>{1}</a>.", formURL, docket);


                            string htmlContent = getHTMLFullMessageContent(clientContext, listItem, message);
                            String To = string.Empty;

                            //email to initiator
                            To = AddEmailAddress(To, initiatorEmails);
                            SendEmail(clientContext, To, subject, htmlContent);
                        }
                        else if (formType.Equals(Constants.PIWList_FormType_AgendaForm))
                        {
                            throw new NotImplementedException();
                        }
                    }
                    break;
                case Constants.PIWList_FormStatus_OSECVerification:
                    if (action.Equals(enumAction.Reject))
                    {
                        if (formType.Equals(Constants.PIWList_FormType_StandardForm))
                        {
                            string subject = "PIW – Workflow Item  Rejected by OSEC Verifier";
                            string message = String.Format(@"Workflow Item <a href='{0}'>{1}</a> 
                                    submitted through Publish Issuance Workflow has been rejected by OSEC Verifier {2}.",
                                formURL, docket, currentUser.Title);
                            string htmlContent = getRejectHTMLFullMessageContent(message, comment);
                            String To = string.Empty;

                            //email to initiator, document owner and notification recipient
                            To = AddEmailAddress(To, initiatorEmails);
                            To = AddEmailAddress(To, documentOwnerEmails);
                            To = AddEmailAddress(To, notificationRecipientEmails);

                            SendEmail(clientContext, To, subject, htmlContent);
                        }
                    }
                    break;
                case Constants.PIWList_FormStatus_Edited:
                    if (previousFormStatus.Equals(Constants.PIWList_FormStatus_OSECVerification))
                    {
                        goto case Constants.PIWList_FormStatus_OSECVerification;
                    }
                    else if (previousFormStatus.Equals(Constants.PIWList_FormStatus_PrePublication))
                    {
                        goto case Constants.PIWList_FormStatus_PrePublication;
                    }
                    else if (previousFormStatus.Equals(Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        goto case Constants.PIWList_FormStatus_ReadyForPublishing;
                    }
                    break;
                case Constants.PIWList_FormStatus_PrePublication:
                    if (action.Equals(enumAction.Reject))
                    {
                        if (formType.Equals(Constants.PIWList_FormType_StandardForm))
                        {
                            string subject = "PIW – Workflow Item  Rejected by Pre-Publication Reviewer";
                            string message = String.Format(@"Workflow Item <a href='{0}'>{1}</a> 
                                    submitted through Publish Issuance Workflow has been rejected by Pre-Publication Reviewer {2}.",
                                formURL, docket, currentUser.Title);
                            string htmlContent = getRejectHTMLFullMessageContent(message, comment);
                            String To = string.Empty;

                            //email to initiator, document owner and notification recipient
                            To = AddEmailAddress(To, initiatorEmails);
                            To = AddEmailAddress(To, documentOwnerEmails);
                            To = AddEmailAddress(To, notificationRecipientEmails);

                            SendEmail(clientContext, To, subject, htmlContent);
                        }
                    }
                    break;
                case Constants.PIWList_FormStatus_SecretaryReview:
                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    if (action.Equals(enumAction.Publish))
                    {
                        string subject = "PIW – Publication of Workflow Item was Initiated";
                        string message = String.Format(@"Publication of Workflow Item <a href='{0}'>{1}</a> 
                                    has been initiated (i.e. routed to the eLibrary Data Entry group).",
                            formURL, docket);
                        string htmlContent = getHTMLFullMessageContent(clientContext, listItem, message);
                        String To = string.Empty;

                        if (formType.Equals(Constants.PIWList_FormType_StandardForm))
                        {
                            //email to initiator, document owner and notification recipient
                            To = AddEmailAddress(To, initiatorEmails);
                            To = AddEmailAddress(To, documentOwnerEmails);
                            To = AddEmailAddress(To, notificationRecipientEmails);
                        }
                        else if (formType.Equals(Constants.PIWList_FormType_AgendaForm))
                        {
                        }
                        else if (formType.Equals(Constants.PIWList_FormType_DirectPublicationForm))
                        {
                        }

                        SendEmail(clientContext, To, subject, htmlContent);
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    if (action.Equals(enumAction.PrintJobComplete))
                    {
                        string subject = "PIW - Issuance Document Mailed";
                        string message = String.Format(@"The issuance associated with Workflow Item <a href='{0}'>{1}</a> 
                                has been mailed via the USPS.", formURL, docket);
                        string htmlContent = getHTMLFullMessageContent(clientContext, listItem, message);
                        String To = string.Empty;

                        //email to initiator
                        To = AddEmailAddress(To, initiatorEmails);
                        SendEmail(clientContext, To, subject, htmlContent);
                    }
                    else if (action.Equals(enumAction.ReOpen))
                    {
                        string subject = "PIW Item has been reopened";
                        string message = String.Format(@"Publication of Workflow Item <a href='{0}'>{1}</a> 
                                        has been reopened.", formURL, docket);
                        string htmlContent = getHTMLFullMessageContent(clientContext, listItem, message);
                        String To = string.Empty;

                        //email to initiator, document owner and notification recipient
                        To = AddEmailAddress(To, initiatorEmails);
                        To = AddEmailAddress(To, documentOwnerEmails);
                        To = AddEmailAddress(To, notificationRecipientEmails);

                        SendEmail(clientContext, To, subject, htmlContent);
                    }
                    else if (action.Equals(enumAction.PublishedToElibrary))
                    {
                        string subject = "PIW Item has been published to eLibrary";
                        string message = String.Format(@"Publication of Workflow Item <a href='{0}'>{1}</a> 
                                        has been published to eLibrary.", formURL, docket);
                        string htmlContent = getHTMLFullMessageContent(clientContext, listItem, message);
                        String To = string.Empty;

                        //email to initiator, document owner and notification recipient
                        To = AddEmailAddress(To, initiatorEmails);
                        To = AddEmailAddress(To, documentOwnerEmails);
                        To = AddEmailAddress(To, notificationRecipientEmails);

                        SendEmail(clientContext, To, subject, htmlContent);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Add Email address into comma seperated string
        /// </summary>
        /// <param name="ToAddress"></param>
        /// <param name="emailList"></param>
        /// <returns></returns>
        private string AddEmailAddress(string ToAddress,IEnumerable<string> emailList)
        {
            if (emailList != null)
            {
                foreach (var email in emailList)
                {
                    if (string.IsNullOrEmpty(ToAddress))
                    {
                        ToAddress = email;
                    }
                    else
                    {
                        if (ToAddress.IndexOf(email) < 0)//avoid duplicate email
                        {
                            ToAddress = ToAddress + "," + email;
                        }
                    }
                }
            }

            return ToAddress;
        }

        public void SendEmail(ClientContext clientContext,string ToAddress, string subject, string htmlContent)
        {
            string mailrelay = ConfigurationManager.AppSettings["mailrelay"];
            string env = ConfigurationManager.AppSettings["Env"];

            if (!env.ToLower().Equals("prod"))//if not prod, concat the Env before the subject
            {
                subject = string.Format("!!! {0} !!! {1}", env, subject);
            }

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("piw@ferc.gov");
            
            mailMessage.To.Add(ToAddress);

            mailMessage.Subject = subject;

            mailMessage.Body = htmlContent;
            mailMessage.ReplyToList.Add("sharepointteam@ferc.gov");
            mailMessage.IsBodyHtml = true;

            try
            {
                SmtpClient smtpClient = new SmtpClient(mailrelay, 25);
                smtpClient.Send(mailMessage);    
            }
            catch (Exception exc)
            {
                //TODO: Suppress exception for now
                helper.CreateLog(clientContext,"Cannot send email",exc.InnerException.Message);
            }
            

            //insert email into email Log list // it can be resent by designer wf in case the mail relay fails to send 
            helper.CreateEmailLog(clientContext,ToAddress,subject,htmlContent);

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
                ? System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem["Created"].ToString())).ToString() : string.Empty;


            var PublicDocsURL = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PublicDocumentURLs]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_PublicDocumentURLs]].ToString() : string.Empty;

            var CEIIDocsURL = listItem[piwListInteralColumnNames[Constants.PIWList_colName_CEIIDocumentURLs]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_CEIIDocumentURLs]].ToString() : string.Empty;

            var PrivilegedDocsURL = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrivilegedDocumentURLs]] != null
                ? listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrivilegedDocumentURLs]].ToString() : string.Empty;

            
            string fileNameListHTML = helper.getDocumentURLsHTML(PublicDocsURL, CEIIDocsURL, PrivilegedDocsURL, true);

            string[] args = new string[] { message, fileNameListHTML, description, initiatorOffice, documentCategory, createdDate };
            string htmlContent = string.Format(@" 
                                                            {0}<br/><br/>
                                                            - File Name: {1}<br/>                                                            
                                                            - Description: {2}<br/>
                                                            - Initiator Office: {3}<br/>
                                                            - Document Category: {4}<br/> 
                                                            - Created Date: {5}                                                       
                                                 ", args);
            return htmlContent;
        }

        public string getRejectHTMLFullMessageContent(string message,string comment)
        {
            string[] args = new string[] { message, comment };
            string htmlContent = string.Format(@"{0}<br/><br/>
                                                            - Comment: {1}<br/><br/>
                                                            Please review, make changes, and resubmit.", args);
            return htmlContent;
        }
    }
}