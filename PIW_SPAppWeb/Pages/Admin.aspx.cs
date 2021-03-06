﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

using System.ServiceModel;
using System.Web;
using System.Web.UI;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FERC.Common;
using FERC.Common.Queues;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using File = Microsoft.SharePoint.Client.File;
using List = Microsoft.SharePoint.Client.List;
using ListItem = System.Web.UI.WebControls.ListItem;

//using FERC.eLibrary.Dvvo.Facade;
//using FERC.eLibrary.Dvvo.Common;



namespace PIW_SPAppWeb
{
    public partial class Admin : System.Web.UI.Page
    {
        //protected void Page_PreInit(object sender, EventArgs e)
        //{
        //    Uri redirectUrl;
        //    switch (SharePointContextProvider.CheckRedirectionStatus(Context, out redirectUrl))
        //    {
        //        case RedirectionStatus.Ok:
        //            return;
        //        case RedirectionStatus.ShouldRedirect:
        //            Response.Redirect(redirectUrl.AbsoluteUri, endResponse: true);
        //            break;
        //        case RedirectionStatus.CanNotRedirect:
        //            Response.Write("An error occurred while processing your request.");
        //            Response.End();
        //            break;
        //    }
        //}

        private SharePointHelper helper = new SharePointHelper();

        protected void Page_Load(object sender, EventArgs e)
        {
            using (var clientContext = SharePointContextProvider.Current.GetSharePointContext(Context).CreateUserClientContextForSPHost())
            {
                try
                {
                    var currentUser = clientContext.Web.CurrentUser;
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();
                    var isAdmin = helper.IsUserMemberOfGroup(clientContext, currentUser.LoginName,
                        new string[] { Constants.Grp_PIWSystemAdmin });
                    if (!isAdmin)
                    {
                        helper.RedirectToAPage(Request, Response, Constants.Page_AccessDenied);
                    }
                }
                catch (Exception exc)
                {
                    if (exc is ServerUnauthorizedAccessException)
                    {
                        helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_AccessDenied);
                    }
                    else
                    {
                        helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
                    }
                }

            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            using (var clientContext = SharePointContextProvider.Current.GetSharePointContext(Context).CreateUserClientContextForSPHost())
            {
                List oList = clientContext.Web.Lists.GetByTitle("Announcements");
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                Microsoft.SharePoint.Client.ListItem oListItem = oList.AddItem(itemCreateInfo);

                oListItem["Title"] = txtTitle.Text;
                oListItem["Body"] = "Hello World!";

                oListItem.Update();

                clientContext.ExecuteQuery();
            }
        }

        protected void btnRegisterRER_Click(object sender, EventArgs e)
        {
            using (var clientContext = SharePointContextProvider.Current.GetSharePointContext(Context).CreateUserClientContextForSPHost())
            {
                var listName = Constants.PIWDocuments_DocumentLibraryName;
                if (String.IsNullOrEmpty(listName)) return;
                List srcList = clientContext.Web.Lists.GetByTitle(listName);
                clientContext.Load(clientContext.Web);
                clientContext.ExecuteQuery();
                // Get the operation context and remote event service URL.
                string remoteUrl;
                if (null != OperationContext.Current)
                {
                    string url = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri;
                    string opContext = url.Substring(0, url.LastIndexOf("/", StringComparison.Ordinal));
                    remoteUrl = String.Format("{0}/{1}", opContext, Constants.PIWDocumentsRERServiceName);
                }
                else if (null != HttpContext.Current)
                {
                    string url = GetSiteRoot();
                    string opContext = url.Substring(0, url.LastIndexOf("/", StringComparison.Ordinal));
                    remoteUrl = String.Format("{0}/Services/{1}", opContext, Constants.PIWDocumentsRERServiceName);
                }
                else
                {
                    return;
                }

                //Register remote event receiver ItemUpdated for the PIW Documents
                if (!IsRemoteEventRegistered(clientContext, EventReceiverType.ItemUpdated,
                            Constants.PIWDocuments_DocumentLibraryName, Constants.LIBEVTRCVR_NAME))
                {
                    srcList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation
                    {
                        EventType = EventReceiverType.ItemUpdated,
                        ReceiverName = Constants.LIBEVTRCVR_NAME,
                        ReceiverUrl = remoteUrl,
                        SequenceNumber = 10
                    });
                    clientContext.ExecuteQuery();
                }

                //register remote event receiver ItemUpdating for the PIW Documents
                if (!IsRemoteEventRegistered(clientContext, EventReceiverType.ItemUpdating,
                            Constants.PIWDocuments_DocumentLibraryName, Constants.LIBEVTRCVR_NAME))
                {
                    srcList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation
                    {
                        EventType = EventReceiverType.ItemUpdating,
                        ReceiverName = Constants.LIBEVTRCVR_NAME,
                        ReceiverUrl = remoteUrl,
                        SequenceNumber = 10
                    });
                    clientContext.ExecuteQuery();
                }

                //register remote event receiver ItemAdded
                if (!IsRemoteEventRegistered(clientContext, EventReceiverType.ItemAdded,
                            Constants.PIWDocuments_DocumentLibraryName, Constants.LIBEVTRCVR_NAME))
                {
                    srcList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation
                    {
                        EventType = EventReceiverType.ItemAdded,
                        ReceiverName = Constants.LIBEVTRCVR_NAME,
                        ReceiverUrl = remoteUrl,
                        SequenceNumber = 10
                    });
                    clientContext.ExecuteQuery();
                }

            }
        }

        private static string GetSiteRoot()
        {
            if (HttpContext.Current == null) return null;
            HttpRequest request = HttpContext.Current.Request;
            string siteRoot = request.Url.AbsoluteUri
                .Replace(request.Url.AbsolutePath, String.Empty) // trim the current page off
                .Replace(request.Url.Query, string.Empty); // trim the query string off
            if (request.Url.Segments.Length == 4)
                // If hosted in a virtual directory, restore that segment
                siteRoot += "/" + request.Url.Segments[1];
            if (!siteRoot.EndsWith("/"))
                siteRoot += "/";
            return siteRoot;
        }

        public static bool IsRemoteEventRegistered(ClientContext clientContext, EventReceiverType type,
            string listName, string ListEvtRcvr)
        {
            bool result = false;
            if (null == clientContext) throw new ArgumentNullException("clientContext");
            try
            {
                // Get the list
                //Logger.Logger.LogInfo("Checking if remote events registered", () =>
                //var listName = Constants.PIWDocumentListName;

                List srcList = clientContext.Web.Lists.GetByTitle(listName);
                clientContext.Load(clientContext.Web);
                clientContext.ExecuteQuery();
                // Iterate all event receivers.
                clientContext.Load(srcList.EventReceivers);
                clientContext.ExecuteQuery();
                foreach (EventReceiverDefinition er in srcList.EventReceivers)
                    if (er.ReceiverName == ListEvtRcvr && er.EventType == type)
                    {
                        result = true;
                        break;
                    }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //Logger.Logger.LogError(ex.ToString());
            }
            return false;
        }

        protected void btnRemoveRER_Click(object sender, EventArgs e)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                UnregisterRemoteEvents(clientContext);
            }
        }

        public static void UnregisterRemoteEvents(ClientContext clientContext)
        {
            if (null == clientContext) throw new ArgumentNullException("clientContext");
            try
            {

                // Get the list
                var listName = Constants.PIWDocuments_DocumentLibraryName;

                List srcList = clientContext.Web.Lists.GetByTitle(listName);
                clientContext.Load(clientContext.Web);
                clientContext.ExecuteQuery();
                // Remove all event receivers.
                clientContext.Load(srcList.EventReceivers);
                clientContext.ExecuteQuery();
                var toDelete = new List<EventReceiverDefinition>();

                foreach (EventReceiverDefinition er in srcList.EventReceivers)
                    if (er.ReceiverName == Constants.LIBEVTRCVR_NAME)
                        toDelete.Add(er);
                foreach (EventReceiverDefinition er in toDelete)
                {
                    er.DeleteObject();
                    clientContext.ExecuteQuery();
                }

            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
                //Logger.Logger.LogError(ex.ToString());
            }
        }

        protected void EPSValidation_Click(object sender, EventArgs e)
        {
            //EPSPublicationHelper pubHelper = new EPSPublicationHelper();
            //pubHelper.ValidateDocument();
        }

        protected void btnTestCitationAppended_Click(object sender, EventArgs e)
        {
            //this call must use this clientContext. Create clientContext as usual CreateUserClientContextForSPHost not works
            using (var clientContext = new ClientContext(Request.QueryString["SPHostUrl"]))
            {
                var listName = Constants.PIWDocuments_DocumentLibraryName;

                var citationNumber = "Citation Number " + DateTime.Now.ToLongTimeString();
                string listItemID = "36";
                var fileName = "ER02-2001-000.docx";
                var documentServerRelativeURL = getDocumentServerRelativeURL(clientContext, listItemID, fileName);

                //var newclientContext = new ClientContext(Request.QueryString["SPHostUrl"]);
                FileInformation fileInformation = File.OpenBinaryDirect(clientContext, documentServerRelativeURL);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    fileInformation.Stream.CopyTo(memoryStream);
                    using (WordprocessingDocument doc = WordprocessingDocument.Open(memoryStream, true))
                    {
                        // Insert a new paragraph at the beginning of the document.
                        var paragraph = GenerateCitParagraph(citationNumber);
                        doc.MainDocumentPart.Document.Body.InsertAt(paragraph, 0);
                    }
                    // Seek to beginning before writing to the SharePoint server.
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    File.SaveBinaryDirect(clientContext, documentServerRelativeURL, memoryStream, true);
                }



            }


        }

        public string getDocumentServerRelativeURL(ClientContext clientContext, string listItemID, string fileName)
        {
            clientContext.Load(clientContext.Web);
            clientContext.ExecuteQuery();

            return string.Format("{0}/{1}/{2}/{3}", clientContext.Web.ServerRelativeUrl,
                    Constants.PIWDocuments_DocumentLibraryName, listItemID, fileName);

        }

        public Paragraph GenerateCitParagraph(string text)
        {
            //citation paragraph will be bold, centered and size 13, font size by default will be Times New Romain
            Paragraph paragraph1 = new Paragraph() { };

            ParagraphProperties paragraphProperties1 = new ParagraphProperties();

            Justification justification1 = new Justification()
            {
                Val = JustificationValues.Center,

            };

            paragraphProperties1.Append(justification1);

            Run run1 = new Run();

            RunProperties runProperties1 = new RunProperties();

            //RunFonts runFonts1 = new RunFonts() { Ascii = "Times New Roman"};
            Bold bold1 = new Bold();
            FontSize fontSize1 = new FontSize() { Val = "26" };//font size 13 - half size paramater

            runProperties1.Append(bold1);
            runProperties1.Append(fontSize1);

            Text text1 = new Text();
            text1.Text = text;

            run1.Append(runProperties1);
            run1.Append(text1);

            paragraph1.Append(paragraphProperties1);
            paragraph1.Append(run1);
            return paragraph1;
        }

        protected void btnTestExcelGeneration_Click(object sender, EventArgs e)
        {


            //System.IO.File.WriteAllBytes(@"E:\PIWDocuments\TestMailingList.xlsx",file);
        }

        protected void btnTestGetNumberOfPages_Click(object sender, EventArgs e)
        {
            //using (var clientContext =SharePointContextProvider.Current.GetSharePointContext(Context).CreateUserClientContextForSPHost())
            using (var clientContext = new ClientContext(Request.QueryString["SPHostUrl"]))
            {
                EPSPublicationHelper helper = new EPSPublicationHelper();
                //https://fdc1s-sp23wfed2.ferc.gov/piw/PIW%20Documents/101/P-14769-21rows.xlsx
                //string fileURN = @"E:\PIWDocuments\P-10855-259.pdf";
                string fileURN = @"E:\PIWDocuments\SharePoint2016Features.pdf";
                int numberOfPages = helper.getNumberOfRowsFromSupplementalMailingListExcelFile(clientContext, "101", "P-14769-21rows.xlsx");

                lbNumberOfPages.Text = "Number of Pages:" + numberOfPages;
            }

        }

        protected void btnTestPermissionSetting_Click(object sender, EventArgs e)
        {
            //elevated 
            using (var clientContext = new ClientContext(Request.QueryString["SPHostUrl"]))
            {
                //helper.AssignUniqueRoles(clientContext, "118", "Read", "Contribute", "Read", "Read", "Read");
            }
        }

        protected void btnEmail_Click(object sender, EventArgs e)
        {
            using (var clientContext = helper.getElevatedClientContext(Context, Request))
            {
                Email emailHelper = new Email();

                emailHelper.SendEmail(clientContext, "liem.nguyen@ferc.gov", "PIW Test Email", "PIW Test");
            }

        }

        protected void btnTestDvvo_Click(object sender, EventArgs e)
        {
            try
            {
                //string filePath = @"\\fdc1s-sp23wfed2\PIWDocuments\SharePoint2016Features.pdf";
                string filePath = @"\\fdc1s-sp23wfed2\PIWDocuments\SharePoint2016Features.abcz";
                //FERC.Common.Result result = helper.DvvoProxy.ValidateFile(filePath);
                FERC.Common.Result result = helper.getDVVORemoteObject().ValidateFile(filePath);

                if (result.ErrorList != null)
                {
                    foreach (var error in result.ErrorList)
                    {
                        lbDVVO.Text = error.Description + " -- ";
                    }

                }
                else
                {
                    lbDVVO.Text = "File good";
                }

            }
            catch (Exception exc)
            {
                lbDVVO.Text = exc.Message;

            }

        }

        //protected void Button2_Click(object sender, EventArgs e)
        //{
        //    FOLAMailingList fola = new FOLAMailingList();
        //    var folaMailingList = fola.GetFOLAMailingList("p-14780");
        //}

    }
}