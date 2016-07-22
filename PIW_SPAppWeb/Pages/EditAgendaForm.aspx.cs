using System;
using System.Collections.Generic;
using System.Web.Services;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = Microsoft.SharePoint.Client.ListItem;

namespace PIW_SPAppWeb.Pages
{
    public partial class EditAgendaForm : System.Web.UI.Page
    {
        #region variable and properties
        //current Form Status must always has value
        public string FormStatus
        {
            get
            {
                return ViewState[Constants.formStatusViewStateKey].ToString();
            }

            set
            {
                ViewState.Add(Constants.formStatusViewStateKey, value);
            }
        }

        //previous form status may not has value at the begining --> nullable
        public string PreviousFormStatus
        {
            get
            {
                return ViewState[Constants.previousFormStatusViewStateKey].ToString();
            }

            set
            {
                ViewState.Add(Constants.previousFormStatusViewStateKey, value);
            }
        }

        public string ModifiedDateTime
        {
            get
            {
                return ViewState[Constants.ModifiedDateTimeKey] != null ? ViewState[Constants.ModifiedDateTimeKey].ToString() : string.Empty;
            }
            set
            {
                ViewState.Add(Constants.ModifiedDateTimeKey, value);
            }
        }

        public string PublicDocumentURLsFromViewState
        {
            get
            {
                return ViewState[Constants.PublicDocumentURLsKey] != null ? ViewState[Constants.PublicDocumentURLsKey].ToString() : string.Empty;

            }
            set
            {
                ViewState.Add(Constants.PublicDocumentURLsKey, value);
            }
        }


        public string CEIIDocumentURLsFromViewState
        {
            get
            {
                return ViewState[Constants.CEIIDocumentURLsKey] != null ? ViewState[Constants.CEIIDocumentURLsKey].ToString() : string.Empty;

            }
            set
            {
                ViewState.Add(Constants.CEIIDocumentURLsKey, value);
            }
        }


        public string PriviledgedDocumentURLsFromViewState
        {
            get
            {
                return ViewState[Constants.PriviledgedDocumentURLsKey] != null ? ViewState[Constants.PriviledgedDocumentURLsKey].ToString() : string.Empty;

            }
            set
            {
                ViewState.Add(Constants.PriviledgedDocumentURLsKey, value);
            }
        }

        public string FormType
        {
            get
            {
                return ViewState[Constants.FormTypeKey] != null ? ViewState[Constants.FormTypeKey].ToString() : string.Empty;
            }
            set
            {
                ViewState.Add(Constants.FormTypeKey, value);
            }
        }

        public string CurrentUserLogInID
        {
            get
            {
                return ViewState[Constants.CurrentLoginIDKey] != null ? ViewState[Constants.CurrentLoginIDKey].ToString() : string.Empty;
            }
            set
            {
                ViewState.Add(Constants.CurrentLoginIDKey, value);
            }
        }

        public string ListItemID
        {
            get
            {
                return ViewState[Constants.ListItemIDKey] != null ? ViewState[Constants.ListItemIDKey].ToString() : string.Empty;
            }
            set
            {
                ViewState.Add(Constants.ListItemIDKey, value);
            }
        }
        

        //fuction
        static SharePointHelper helper = null;
        #endregion

        #region Events
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                helper = new SharePointHelper();
                ListItemID = Page.Request.QueryString["ID"];

                using (var clientContext = helper.getCurrentLoginClientContext(Context, Request))
                {
                    //current login user
                    clientContext.Load(clientContext.Web.CurrentUser);
                    clientContext.ExecuteQuery();
                    CurrentUserLogInID = clientContext.Web.CurrentUser.LoginName;
                }

                lbUploadedDocumentError.Visible = false;

                if (!Page.IsPostBack)
                {
                    if (!string.IsNullOrEmpty(ListItemID))
                    {
                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
                            string publicDocumentURLs;
                            string cEiiDocumentUrLs;
                            string priviledgedDocumentURLs;
                            helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList,
                                out publicDocumentURLs,out cEiiDocumentUrLs,out priviledgedDocumentURLs);
                            SaveDocumentURLsToPageProperty(publicDocumentURLs,cEiiDocumentUrLs,priviledgedDocumentURLs);
                            helper.PopulateSupplementalMailingListDocumentList(clientContext, ListItemID, rpSupplementalMailingListDocumentList, fieldSetSupplementalMailingList);

                            var isCurrentUserAdmin = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                                new[] { Constants.Grp_PIWSystemAdmin });

                            //if current user is piw admin, load the item even if the isActive is false
                            ListItem listItem = helper.GetPiwListItemById(clientContext, ListItemID, isCurrentUserAdmin);
                            if (listItem == null)
                            {
                                helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_ItemNotFound);
                            }
                            else
                            {
                                PopulateFormStatusAndModifiedDateProperties(clientContext, listItem);
                                DisplayListItemInForm(clientContext, listItem);
                                helper.PopulateHistoryList(clientContext, ListItemID, rpHistoryList, Constants.PIWListHistory_FormTypeOption_EditForm);
                                
                                //display form visiblility based on form status
                                ControlsVisiblitilyBasedOnStatus(clientContext, PreviousFormStatus, FormStatus, listItem);

                                //todo: open documents if status is ready for published
                                ////above method get formStatus from list, store it in viewstate                       
                                //if (FormStatus == enumFormStatus.ReadyForPublishing)
                                //{
                                //    helper.OpenDocument(Page, documentURL);
                                //}
                            }


                        }

                    }
                    else//new form
                    {
                        //if it is new form
                        //Create new PIWListITem
                        //create document libraries
                        //Then redirect to EditForm
                        //By doing it, we can attach multiple document to new piwList item under its folder ID

                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
                            ListItem newItem = helper.createNewPIWListItem(clientContext, Constants.PIWList_FormType_AgendaForm,CurrentUserLogInID);
                            ListItemID = newItem.Id.ToString();

                            //Create subfolder in piwdocuments and mailing list
                            helper.CreatePIWDocumentsSubFolder(clientContext, ListItemID);

                            //history list
                            if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                            {
                                //Form status must be specified becuae the viewstate hasn't have value
                                helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item created",
                                    Constants.PIWList_FormStatus_Pending, Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);
                            }
                        }

                        //forward to Edit
                        Response.Redirect(Request.Url + "&ID=" + ListItemID,false);
                        
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, Page.Request.Url.OriginalString);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Save;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    if (ValidFormData(action))
                    {
                        ListItem listItem = null;
                        if (!SaveData(clientContext, action, ref listItem))
                        {
                            return;
                        }

                        //TODO: Change document and list permission

                        //TODO: send email

                        //Create list history
                        if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item created", FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);
                        }
                        else
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item saved", FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);
                        }

                        //TODO: create list history for Mailing Date and FERC Report Completed.

                        //Refresh or Redirect depends on Previous Status
                        if (PreviousFormStatus.Equals(Constants.PIWList_FormStatus_Pending))
                        {
                            helper.RedirectToSourcePage(Request, Response);
                        }
                        else
                        {
                            helper.RefreshPage(Request, Response);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnSubmitToSecReview_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.SubmitToSecReview;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    if (ValidFormData(action))
                    {
                        ListItem listItem = null;
                        if (!SaveData(clientContext, action, ref listItem))
                        {
                            return;
                        }

                        //TODO: Change document and list permission

                        //TODO: send email

                        //Create list history
                        if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item created", FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);
                        }

                        if ((FormStatus == Constants.PIWList_FormStatus_Rejected) || (FormStatus == Constants.PIWList_FormStatus_Recalled))
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item resubmitted for Secretary Review",
                                FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);
                        }
                        else
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item submitted for Secretary Review",
                                FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);
                        }

                        //Redirect
                        helper.RedirectToSourcePage(Page.Request, Page.Response);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Edit;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }
                    //TODO: Change document and list permission

                    //Create list history
                    helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item edited", FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);

                    //Redirect or Refresh page
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnAccept_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Accept;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //TODO: Change document and list permission

                    //TODO: send email

                    //Create list history
                    helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item accepted", FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);

                    //Refresh the page
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(tbSecReviewComment.Text))
                {
                    lbSecReviewCommentError.Visible = true;
                    lbSecReviewCommentError.Text = "Please provide comment";
                    return;
                }
                else
                {
                    lbSecReviewCommentError.Visible = false;
                    lbSecReviewCommentError.Text = string.Empty;

                    const enumAction action = enumAction.Reject;
                    using (var clientContext = helper.getElevatedClientContext(Context, Request))
                    {
                        ListItem listItem = null;
                        if (!SaveData(clientContext, action, ref listItem))
                        {
                            return;
                        }

                        //TODO: Change document and list permission

                        //TODO: send email

                        //Create list history
                        helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item rejected", FormStatus,
                            Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);

                        //Redirect
                        helper.RedirectToSourcePage(Page.Request, Page.Response);
                    }
                }

                
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnInitiatePublication_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Publish;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //issuance documents
                    Dictionary<string, string> files = new Dictionary<string, string>();
                    foreach (RepeaterItem row in rpDocumentList.Items)
                    {
                        var url = ((HyperLink)row.FindControl("hyperlinkFileURL")).NavigateUrl;
                        var securityLevel = ((Label)row.FindControl("lbSecurityLevel")).Text;
                        if (!files.ContainsKey(url))
                        {
                            files.Add(url, securityLevel);
                        }
                    }

                    //supplemental mailing list - only 1 excel document
                    string supplementalMailingListFileName = string.Empty;
                    if (rpSupplementalMailingListDocumentList.Items.Count > 0)
                    {
                        RepeaterItem row = rpSupplementalMailingListDocumentList.Items[0];
                        supplementalMailingListFileName = ((HyperLink)row.FindControl("hyperlinkFileURL")).Text;
                    }

                    //publish
                    EPSPublicationHelper epsHelper = new EPSPublicationHelper();
                    epsHelper.Publish(clientContext, files, supplementalMailingListFileName, listItem);

                    //TODO: Change document and list permission

                    //TODO: send email

                    //Create list history
                    helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item publication to eLibrary Data Entry initiated",
                        FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);

                    //Refresh
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Delete;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //TODO: Change document and list permission

                    //TODO: send email

                    //Create list history
                    helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item deleted", FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);

                    //Redirect
                    helper.RedirectToSourcePage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnReopen_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.ReOpen;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //TODO: Change document and list permission

                    //TODO: send email

                    //Create list history
                    helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item Re-Opened", FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);

                    //Refresh
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (fileUpload.HasFiles)
                {
                    if (fileUpload.PostedFile.ContentLength < 52428800)
                    {
                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
                            var uploadedFileURL = helper.UploadIssuanceDocument(clientContext, fileUpload, ListItemID, rpDocumentList,
                                lbUploadedDocumentError, lbRequiredUploadedDocumentError, FormStatus,
                                ddlSecurityControl.SelectedValue,Constants.PIWDocuments_DocTypeOption_Issuance,CurrentUserLogInID);
                            if (!string.IsNullOrEmpty(uploadedFileURL)) //only save the document url if the upload is good
                            {
                                string publicDocumentURLs;
                                string cEiiDocumentUrLs;
                                string priviledgedDocumentURLs;
                                helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList, 
                                    out publicDocumentURLs, out cEiiDocumentUrLs, out priviledgedDocumentURLs);
                                SaveDocumentURLsToPageProperty(publicDocumentURLs,cEiiDocumentUrLs,priviledgedDocumentURLs);
                                //Extract docket numner
                                if (rpDocumentList.Items.Count == 1)
                                    //only extract docket number if first document uploaded
                                {
                                    if (!cbIsNonDocket.Checked)
                                    {
                                        tbDocketNumber.Text = helper.ExtractDocket(fileUpload.FileName);
                                    }

                                }

                                //pop-up upload document 
                                helper.OpenDocument(Page,uploadedFileURL);
                            }
                        }
                    }
                    else
                    {
                        lbUploadedDocumentError.Text = "file cannot bigger than 52MB";
                        lbUploadedDocumentError.Visible = true;
                    }
                }

            }
            catch (Exception ex)
            {
                helper.LogError(Context, ex, ListItemID, string.Empty);
                lbUploadedDocumentError.Text = ex.Message;
                lbUploadedDocumentError.Visible = true;
            }
        }

        protected void btnSupplementalMailingListUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (supplementalMailingListFileUpload.HasFiles)
                {
                    if (supplementalMailingListFileUpload.PostedFile.ContentLength < 52428800)
                    {
                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
                            var uploadResult = helper.UploadSupplementalMailingListDocument(clientContext, supplementalMailingListFileUpload, ListItemID, rpSupplementalMailingListDocumentList,
                                lbSupplementalMailingListUploadError, FormStatus, Constants.ddlSecurityControl_Option_Public, Constants.PIWDocuments_DocTypeOption_SupplementalMailingList,CurrentUserLogInID);
                            if (uploadResult) //only save the document url if the upload is good
                            {
                                helper.PopulateSupplementalMailingListDocumentList(clientContext, ListItemID, rpSupplementalMailingListDocumentList, fieldSetSupplementalMailingList);
                            }
                        }
                    }
                    else
                    {
                        lbSupplementalMailingListUploadError.Text = "file cannot bigger than 52MB";
                        lbSupplementalMailingListUploadError.Visible = true;
                    }
                }

            }
            catch (Exception ex)
            {
                helper.LogError(Context, ex, ListItemID, string.Empty);
                lbSupplementalMailingListUploadError.Text = ex.Message;
                lbSupplementalMailingListUploadError.Visible = true;
            }

        }

        protected void rpDocumentList_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            try
            {
                if (e.CommandName == "RemoveDocument")
                {
                    if (!string.IsNullOrEmpty(e.CommandArgument.ToString()))
                    {
                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
                            string removedFileName = helper.RemoveDocument(clientContext, ListItemID, Constants.PIWDocuments_DocumentLibraryName, e.CommandArgument.ToString());
                            string publicDocumentURLs;
                            string cEiiDocumentUrLs;
                            string priviledgedDocumentURLs;
                            helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList,
                                out publicDocumentURLs,out cEiiDocumentUrLs,out priviledgedDocumentURLs );
                            SaveDocumentURLsToPageProperty(publicDocumentURLs,cEiiDocumentUrLs,priviledgedDocumentURLs);

                            //history list
                            helper.CreatePIWListHistory(clientContext, ListItemID, string.Format("Document file {0} removed",
                                removedFileName), FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);
                        }

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void rpSupplementalMailingListDocumentList_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            try
            {
                if (e.CommandName == "RemoveDocument")
                {
                    if (!string.IsNullOrEmpty(e.CommandArgument.ToString()))
                    {
                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
                            string removedFileName = helper.RemoveDocument(clientContext, ListItemID, Constants.PIWDocuments_DocumentLibraryName, e.CommandArgument.ToString());
                            helper.PopulateSupplementalMailingListDocumentList(clientContext, ListItemID, rpSupplementalMailingListDocumentList, fieldSetSupplementalMailingList);

                            //history list
                            helper.CreatePIWListHistory(clientContext, ListItemID, string.Format("Supplemental Mailing List file {0} removed", removedFileName),
                                FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);
                        }

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnGenerateCitationNumber_Click(object sender, EventArgs e)
        {
            try
            {
                lbCitationNumberError.Text = string.Empty;
                lbCitationNumberError.Visible = false;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    helper.GenerateCitation(clientContext, ddDocumentCategory, tbCitationNumber, ddAvailableCitationNumbers,true);

                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnAcceptCitationNumber_Click(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    if (ddDocumentCategory.SelectedIndex > 0)
                    {
                        string errorMessage = string.Empty;
                        int documentCategoryNumber = helper.getDocumentCategoryNumber(ddDocumentCategory.SelectedValue, true);
                        CitationNumber citationNumberHelper = new CitationNumber(documentCategoryNumber, DateTime.Now);

                        if (citationNumberHelper.Save(clientContext, ListItemID, tbCitationNumber.Text.Trim(),
                            ref errorMessage, cbOverrideCitationNumber.Checked))
                        {
                            var listItem = helper.SetCitationNumberFieldInPIWList(clientContext, ListItemID, tbCitationNumber.Text.Trim());

                            try
                            {
                                //need to re-populate the modified date becuase the list item is changed
                                PopulateFormStatusAndModifiedDateProperties(clientContext, listItem);

                                //controls
                                //after accept, citation number cannot be changed
                                EnableCitationNumberControls(false, true);
                                lbCitationNumberError.Text = string.Empty;
                                lbCitationNumberError.Visible = false;

                                //history list
                                helper.CreatePIWListHistory(clientContext, ListItemID, "Citation number assigned: " + tbCitationNumber.Text.Trim(),
                                    FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);

                                //add citation to first public document
                                //must be the last action because it can throw exceptioniled if the docs is opened in MS-Word
                                //it will not able to finish all actions
                                var documentURLs = PublicDocumentURLsFromViewState.Split(new string[] { Constants.DocumentURLsSeparator },
                                        StringSplitOptions.RemoveEmptyEntries);
                                
                                var documentURL = documentURLs[0];
                                var fileName = helper.getFileNameFromURL(documentURL);
                                helper.AddCitationNumberToDocument(clientContext, tbCitationNumber.Text.Trim(),
                                    ListItemID, fileName);

                                //foreach (var documentURL in documentURLs)//add citation to first public document
                                //{
                                //    var fileName = helper.getFileNameFromURL(documentURL);
                                //    helper.AddCitationNumberToDocument(clientContext, tbCitationNumber.Text.Trim(),
                                //        ListItemID, fileName);

                                //}
                            }
                            catch (Exception exc)
                            {
                                lbCitationNumberError.Visible = true;
                                lbCitationNumberError.Text = "Cannot add citation number to the document";
                            }



                        }
                        else//display error message
                        {
                            lbCitationNumberError.Visible = true;
                            lbCitationNumberError.Text = errorMessage;
                        }

                    }

                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void btnRemoveCitationNumber_Click(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    //just delete the citation item - instead of settign the status to deleted
                    var listItem = helper.deleteAssociatedCitationNumberListItem(clientContext, ListItemID);

                    try
                    {
                        var citationNumber = tbCitationNumber.Text.Trim();
                        //need to re-populate the modified date becuase the list item is changed
                        PopulateFormStatusAndModifiedDateProperties(clientContext, listItem);

                        //controls
                        tbCitationNumber.Text = string.Empty;
                        //after remove, citation canbe changed
                        EnableCitationNumberControls(true, false);

                        //history list
                        helper.CreatePIWListHistory(clientContext, ListItemID, "Citation number removed", FormStatus,
                            Constants.PIWListHistory_FormTypeOption_EditForm, CurrentUserLogInID);

                        //remove citation number from the documents - must be the last action because it can throw exceptioniled if the docs is opened in MS-Word
                        //it will not able to finish all actions
                        var documentURLs = PublicDocumentURLsFromViewState.Split(new string[] { Constants.DocumentURLsSeparator },
                                StringSplitOptions.RemoveEmptyEntries);
                        var documentURL = documentURLs[0];
                        var fileName = helper.getFileNameFromURL(documentURL);
                        helper.RemoveCitationNumberFromDocument(clientContext, citationNumber, ListItemID, fileName);
                        //foreach (var documentURL in documentURLs)//add citation to all documents
                        //{
                        //    var fileName = helper.getFileNameFromURL(documentURL);
                        //    helper.RemoveCitationNumberFromDocument(clientContext, citationNumber, ListItemID, fileName);

                        //}
                    }
                    catch (Exception exc)
                    {
                        lbCitationNumberError.Visible = true;
                        lbCitationNumberError.Text = "Cannot remove citation number from the document";
                    }


                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request,Page.Response,"Error.aspx");
            }
        }

        protected void ddAvailableCitationNumbers_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbCitationNumber.Text = ddAvailableCitationNumbers.SelectedValue;
        }
        
        #endregion

        #region Save Data
        private bool SaveData(ClientContext clientContext, enumAction action, ref ListItem returnedListItem)
        {
            ListItem listItem = helper.GetPiwListItemById(clientContext, ListItemID, false);

            if (helper.CheckIfListItemChanged(clientContext, listItem, DateTime.Parse(ModifiedDateTime)))
            {
                lbMainMessage.Text = "The form has been changed, please refresh the page";
                lbMainMessage.Visible = true;
                return false;
            }

            //get next form status
            var currentFormStatus = FormStatus;
            var wf = new AgendaFormWorkflow();
            FormStatus = wf.Execute(PreviousFormStatus, FormStatus, action);
            PreviousFormStatus = currentFormStatus;

            UpdateFormDataToList(clientContext, listItem, action);

            returnedListItem = listItem;
            return true;
        }

        private void UpdateFormDataToList(ClientContext clientContext, ListItem listItem, enumAction action)
        {
            switch (FormStatus)//this is the next status after action is performed
            {
                case Constants.PIWList_FormStatus_Pending:
                    if (FormStatus == PreviousFormStatus)//save action
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                
                case Constants.PIWList_FormStatus_Rejected:
                    if (FormStatus == PreviousFormStatus)//save action
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_SecretaryReview)
                    {
                        SaveSecReviewInfoAndStatus(clientContext, listItem, action);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_SecretaryReview:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_Pending) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Edited) || (PreviousFormStatus == Constants.PIWList_FormStatus_ReOpen))
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Rejected)
                    {
                        //resubmit
                        SaveMainPanelAndStatus(clientContext, listItem);
                        //clear sec review comment and action when a rejected item is re-submit
                        ClearSecReviewActionsAndCommentsBeforeReSubmit(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_Edited:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_SecretaryReview) || 
                        (PreviousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        helper.SaveFormStatus(clientContext, listItem, FormStatus, PreviousFormStatus);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Deleted:
                    //delete item, need to set status and remove citation number if there is assigned one
                    helper.SaveDeleteInfoAndStatus(clientContext, listItem, FormStatus, PreviousFormStatus);
                    helper.ReleaseCitationNumberForDeletedListItem(clientContext, ListItemID);
                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:

                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Edited) //save
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_SecretaryReview)
                    {
                        SaveSecReviewInfoAndStatus(clientContext, listItem, action);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing)
                    {
                        helper.SavePublishingInfoAndStatus(clientContext, listItem, FormStatus, PreviousFormStatus);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_PublishedToeLibrary)
                    {
                        helper.SaveLegalResourcesAndReviewAndStatus(clientContext, listItem, FormStatus, PreviousFormStatus,
                            tbLegalResourcesReviewCompletionDate.Text, tbLegalResourcesReviewNote.Text);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_ReOpen:
                    helper.SaveReOpenInfoAndStatus(clientContext,listItem,FormStatus,PreviousFormStatus);
                    break;
                default:
                    throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));

            }
        }

        private void SaveSecReviewInfoAndStatus(ClientContext clientContext, ListItem listItem, enumAction action)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_SecReviewAction]] = action.ToString();
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] = tbSecReviewComment.Text.Trim();

            if (action == enumAction.Reject)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] = tbSecReviewComment.Text.Trim();
            }
            else//accept
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_SecReviewComment]] = tbSecReviewComment.Text.Trim();
            }

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void ClearSecReviewActionsAndCommentsBeforeReSubmit(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_SecReviewAction]] = string.Empty;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_SecReviewComment]] = string.Empty;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void SaveMainPanelAndStatus(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            //each update has its own Execute query. If we set the field of the list item, then execute the ExecuteQuery to populate data
            //without calling the listitem.update, then the changes is lost 
            //We need to prepare all the necessary data before update all fields without calling any ExecuteQuery in middle of it

            //Populate data

            //Populate document owner
            FieldUserValue[] documentOwners = null;
            if (!string.IsNullOrEmpty(hdnDocumentOwner.Value))
            {
                List<PeoplePickerUser> users = PeoplePickerHelper.GetValuesFromPeoplePicker(hdnDocumentOwner);

                documentOwners = new FieldUserValue[users.Count];
                for (var i = 0; i < users.Count; i++)
                {
                    var newUser = clientContext.Web.EnsureUser(users[i].Login);//ensure user so usr can be added to site if they are not --> receive email
                    clientContext.Load(newUser);
                    clientContext.ExecuteQuery();
                    documentOwners[i] = new FieldUserValue { LookupId = newUser.Id };
                }
            }

            //Populate notification recipient 
            FieldUserValue[] notificationRecipients = null;
            if (!string.IsNullOrEmpty(hdnNotificationRecipient.Value))
            {
                List<PeoplePickerUser> users = PeoplePickerHelper.GetValuesFromPeoplePicker(hdnNotificationRecipient);

                notificationRecipients = new FieldUserValue[users.Count];
                for (var i = 0; i < users.Count; i++)
                {
                    var newUser = clientContext.Web.EnsureUser(users[i].Login);//ensure user so usr can be added to site if they are not --> receive email
                    clientContext.Load(newUser);
                    clientContext.ExecuteQuery();
                    notificationRecipients[i] = new FieldUserValue { LookupId = newUser.Id };
                }
            }

            //Populate current user title
            clientContext.Load(clientContext.Web.CurrentUser, user => user.Title);
            clientContext.ExecuteQuery();

            //Save Data

            //Save IsActive
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_IsActive]] = true;

            //Save Docket Number
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocketNumber]] = tbDocketNumber.Text.Trim();
            
            //Non-Docketed
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_IsNonDocket]] = cbIsNonDocket.Checked;

            //By Pass Docket Validation
            //listItem[internalColumnNames[Constants.PIWList_colName_ByPassDocketValidation]] = cbby.Checked;

            //Description
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_Description]] = tbDescription.Text.Trim();


            //alternate identifier
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_AlternateIdentifier]] = tbAlternateIdentifier.Text.Trim();
            

            //instruction for osec
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_InstructionForOSEC]] = tbInstruction.Text.Trim();
            
            //Federal register
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FederalRegister]] = cbFederalRegister.Checked;

            //section 206 notice
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_Section206Notice]] = cbSection206Notice.Checked;

            //hearing order
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_HearingOrder]] = cbHearingOrder.Checked;

            //document category
            if (ddDocumentCategory.SelectedIndex != 0)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocumentCategory]] = ddDocumentCategory.SelectedValue;
            }
            else
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocumentCategory]] = string.Empty;
            }

            //program office(wokflow initiator)
            if (ddProgramOfficeWorkflowInitiator.SelectedIndex != 0)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]] = ddProgramOfficeWorkflowInitiator.SelectedValue;
            }
            else
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]] = string.Empty;
            }



            //Workflow initiator - set by default to current login value when form is created


            //program office(document owner)
            //program office(wokflow initiator)
            if (ddProgramOfficeDocumentOwner.SelectedIndex != 0)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_ProgramOfficeDocumentOwner]] = ddProgramOfficeDocumentOwner.SelectedValue;
            }
            else
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_ProgramOfficeDocumentOwner]] = string.Empty;
            }

            //document owner
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocumentOwner]] = documentOwners;

            //notification recipient
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_NotificationRecipient]] = notificationRecipients;

            //due date
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_DueDate]] = tbDueDate.Text;

            //comment
            //if (!string.IsNullOrEmpty(tbComment.Text))
            //{
            //    if (listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] == null)
            //    {
            //        listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("{0} ({1}): {2}", clientContext.Web.CurrentUser.Title,
            //            DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text);
            //    }
            //    else
            //    {
            //        //append
            //        listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("{0} ({1}): {2}<br>{3}",
            //            clientContext.Web.CurrentUser.Title, DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text, listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]]);
            //    }

            //}

            if (!string.IsNullOrEmpty(FormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            }

            if (!string.IsNullOrEmpty(PreviousFormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;
            }

            //Public Document URLs
            if (!string.IsNullOrEmpty(PublicDocumentURLsFromViewState))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PublicDocumentURLs]] = PublicDocumentURLsFromViewState;
            }

            //CEII Document URLs
            if (!string.IsNullOrEmpty(CEIIDocumentURLsFromViewState))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_CEIIDocumentURLs]] = CEIIDocumentURLsFromViewState;
            }

            //Priviledged Document URLs
            if (!string.IsNullOrEmpty(PriviledgedDocumentURLsFromViewState))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PriviledgedDocumentURLs]] = PriviledgedDocumentURLsFromViewState;
            }

            //execute query
            listItem.Update();
            clientContext.ExecuteQuery();
        }
        #endregion

        #region Utils
        //This webmethod is called by the csom peoplepicker to retrieve search data
        //In a MVC application you can use a Json Action method
        [WebMethod]
        public static string GetPeoplePickerData()
        {
            //peoplepickerhelper will get the needed values from the querrystring, get data from sharepoint, and return a result in Json format
            return PeoplePickerHelper.GetPeoplePickerSearchData();
        }

        private bool ValidFormData(enumAction action)
        {
            bool isValid = true;

            //Check if there is a uploaded document
            if (rpDocumentList.Items.Count < 1)//validation fails
            {
                isValid = false;
                lbRequiredUploadedDocumentError.Visible = true;
            }
            else
            {
                //check if at least 1 public item is 
                isValid = true;
                lbRequiredUploadedDocumentError.Visible = false;
            }

            //Check docket validation
            string errorMessage = string.Empty;
            helper.CheckDocketNumber(tbDocketNumber.Text.Trim(), ref errorMessage, false, cbDocketValidationByPass.Checked);

            //check error message to see if all dockets are valid
            if (string.IsNullOrEmpty(errorMessage))//dockets are valid
            {
                isValid = isValid & true;
                lbDocketValidationServerSideError.Visible = false;
            }
            else
            {
                isValid = false;
                lbDocketValidationServerSideError.Text = errorMessage;
                lbDocketValidationServerSideError.Visible = true;
                //display ByPass Docket Validation Check
                if (lbDocketValidationServerSideError.Text.Equals(Constants.ATMSRemotingServiceConnectionError))
                {
                    cbDocketValidationByPass.Visible = true;
                }
            }



            return isValid;
        }

        [WebMethod]
        public static string ValidateDocketNumber(string docketNumber, bool isCNF, bool docketValidationByPass)
        {
            //string errorMessage = docketNumber;
            string errorMessage = string.Empty;
            helper.CheckDocketNumber(docketNumber.Trim(), ref errorMessage, isCNF, docketValidationByPass);
            return errorMessage;
        }
        public void PopulateFormStatusAndModifiedDateProperties(ClientContext clientContext, ListItem listItem)
        {
            var internalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            if (listItem[internalColumnNames[Constants.PIWList_colName_FormStatus]] != null)
            {
                FormStatus = listItem[internalColumnNames[Constants.PIWList_colName_FormStatus]].ToString();
            }

            if (listItem[internalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] != null)
            {
                PreviousFormStatus = listItem[internalColumnNames[Constants.PIWList_colName_PreviousFormStatus]].ToString();
            }

            //Modified Date
            if (listItem[internalColumnNames[Constants.PIWList_colName_Modified]] != null)
            {
                ModifiedDateTime = listItem[internalColumnNames[Constants.PIWList_colName_Modified]].ToString();
            }
        }

        private void DisplayListItemInForm(ClientContext clientContext, ListItem listItem)
        {
            if (listItem != null)
            {
                var piwListInteralColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);


                //Main Panel
                //Docket
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]] != null)
                {
                    tbDocketNumber.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]].ToString();
                }

                //Is Non-Docketed
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_IsNonDocket]] != null)
                {
                    cbIsNonDocket.Checked = bool.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_IsNonDocket]].ToString());
                }

                //Alternate Identifier
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_AlternateIdentifier]] != null)
                {
                    tbAlternateIdentifier.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_AlternateIdentifier]].ToString();
                }

                //Description
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_Description]] != null)
                {
                    tbDescription.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_Description]].ToString();
                }

                //Instruction for OSEC
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_InstructionForOSEC]] != null)
                {
                    tbInstruction.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_InstructionForOSEC]].ToString();
                }


                //Federal Register
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_FederalRegister]] != null)
                {
                    cbFederalRegister.Checked = bool.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_FederalRegister]].ToString());
                }

                //Section 206 Notice
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_Section206Notice]] != null)
                {
                    cbSection206Notice.Checked = bool.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_Section206Notice]].ToString());
                }

                //Hearing Order
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_HearingOrder]] != null)
                {
                    cbHearingOrder.Checked = bool.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_HearingOrder]].ToString());
                }

                //Document Category
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocumentCategory]] != null)
                {
                    if (string.IsNullOrEmpty(listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocumentCategory]].ToString()))
                    {
                        ddDocumentCategory.SelectedIndex = 0;
                    }
                    else
                    {
                        ddDocumentCategory.SelectedValue = listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocumentCategory]].ToString();
                    }
                }

                //Program Office (Workflow Initiator)
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null)
                {
                    if (string.IsNullOrEmpty(listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString()))
                    {
                        ddProgramOfficeWorkflowInitiator.SelectedIndex = 0;
                    }
                    else
                    {
                        ddProgramOfficeWorkflowInitiator.SelectedValue = listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString();
                    }
                }



                //Workflow Initiator - one value
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_WorkflowInitiator]] != null)
                {
                    FieldUserValue fuv =
                        (FieldUserValue)
                            listItem[piwListInteralColumnNames[Constants.PIWList_colName_WorkflowInitiator]];
                    User initiator = clientContext.Web.GetUserById(fuv.LookupId);
                    clientContext.Load(initiator);
                    clientContext.ExecuteQuery();
                    PeoplePickerHelper.FillPeoplePickerValue(hdnWorkflowInitiator, initiator);
                }
                else
                {
                    //this is the new form, use current user value to set the people picker
                    //we cannot set the default value, the ensure user fails when get people field from list
                    User initiator = clientContext.Web.CurrentUser;
                    clientContext.Load(initiator);
                    clientContext.ExecuteQuery();
                    PeoplePickerHelper.FillPeoplePickerValue(hdnWorkflowInitiator, initiator);
                }

                //Program Office (Document Owner)
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeDocumentOwner]] != null)
                {
                    if (string.IsNullOrEmpty(listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeDocumentOwner]].ToString()))
                    {
                        ddProgramOfficeDocumentOwner.SelectedIndex = 0;
                    }
                    else
                    {
                        ddProgramOfficeDocumentOwner.SelectedValue = listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeDocumentOwner]].ToString();
                    }
                }


                //Document Owner
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocumentOwner]] != null)
                {
                    FieldUserValue[] fuv = (FieldUserValue[])listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocumentOwner]];
                    User[] users = new User[fuv.Length];
                    for (int i = 0; i < users.Length; i++)
                    {
                        User user = clientContext.Web.GetUserById(fuv[i].LookupId);
                        clientContext.Load(user);
                        clientContext.ExecuteQuery();
                        users[i] = user;

                    }
                    PeoplePickerHelper.FillPeoplePickerValue(hdnDocumentOwner, users);
                }

                //Notification Recipient
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_NotificationRecipient]] != null)
                {
                    FieldUserValue[] fuv = (FieldUserValue[])listItem[piwListInteralColumnNames[Constants.PIWList_colName_NotificationRecipient]];
                    User[] users = new User[fuv.Length];
                    for (int i = 0; i < users.Length; i++)
                    {
                        User user = clientContext.Web.GetUserById(fuv[i].LookupId);
                        clientContext.Load(user);
                        clientContext.ExecuteQuery();
                        users[i] = user;
                    }
                    PeoplePickerHelper.FillPeoplePickerValue(hdnNotificationRecipient, users);
                }

                //Due Date
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_DueDate]] != null)
                {
                    tbDueDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_DueDate]].ToString()).ToShortDateString();
                }

                //Comment
                //if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]] != null)
                //{
                //    lbCommentValue.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]].ToString();
                //}

                //OSEC Reject Comment
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_RecallRejectComment]] != null)
                {
                    lbOSECRejectCommentValue.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_RecallRejectComment]].ToString();
                }
                
                //Sec review 
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_SecReviewAction]] != null)
                {
                    lbSecReviewAction.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_SecReviewAction]].ToString();
                }

                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_SecReviewComment]] != null)
                {
                    tbSecReviewComment.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_SecReviewComment]].ToString();
                }

                //Citation Number
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_CitationNumber]] != null)
                {
                    tbCitationNumber.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_CitationNumber]].ToString();
                }
                
                //Legal resources and review
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_LegalResourcesAndReviewGroupCompleteDate]] != null)
                {
                    tbLegalResourcesReviewCompletionDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_LegalResourcesAndReviewGroupCompleteDate]].ToString()).ToShortDateString();
                }

                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_LegalResourcesAndReviewGroupNote]] != null)
                {
                    tbLegalResourcesReviewNote.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_LegalResourcesAndReviewGroupNote]].ToString();
                }

            }
        }

        public void SaveDocumentURLsToPageProperty(string publicDocumentURLs, string cEIIDocumentURLs, string priviledgedDocumentURLs)
        {
            if (!string.IsNullOrEmpty(publicDocumentURLs))
            {
                PublicDocumentURLsFromViewState = publicDocumentURLs;
            }

            if (!string.IsNullOrEmpty(cEIIDocumentURLs))
            {
                CEIIDocumentURLsFromViewState = cEIIDocumentURLs;
            }

            if (!string.IsNullOrEmpty(priviledgedDocumentURLs))
            {
                PriviledgedDocumentURLsFromViewState = priviledgedDocumentURLs;
            }
        }
        #endregion

        #region Visibility
        public void ControlsVisiblitilyBasedOnStatus(ClientContext clientContext, string previousFormStatus, string formStatus, ListItem listItem)
        {
            var piwlistInternalColumnName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            //SPUser checkoutUser = null;
            var currentUser = clientContext.Web.CurrentUser;
            clientContext.Load(currentUser);

            clientContext.ExecuteQuery();

            switch (formStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Recalled:
                case Constants.PIWList_FormStatus_Rejected:
                case Constants.PIWList_FormStatus_ReOpen:
                    //submit section    
                    EnableMainPanel(true);
                    lbMainMessage.Visible = false;
                    if (formStatus.Equals(Constants.PIWList_FormStatus_Rejected))
                    {
                        fieldsetOSECRejectComment.Visible = true;
                    }
                    else//pending
                    {
                        fieldsetOSECRejectComment.Visible = false;
                    }

                    InitiallyEnableCitationNumberControls(clientContext, listItem);
                    //OSEC section
                    fieldsetSecReview.Visible = false;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                        new[] {Constants.Grp_OSEC, Constants.Grp_SecReview});

                    btnSubmitToSecReview.Visible = btnSave.Visible;

                    btnEdit.Visible = false;

                    btnAccept.Visible = false;

                    btnReject.Visible = false;
                    

                    btnInitiatePublication.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    btnReopen.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_SecretaryReview:
                    //submit section   
                    EnableMainPanel(false);
                    lbMainMessage.Visible = false;
                    fieldsetOSECRejectComment.Visible = false;

                    InitiallyEnableCitationNumberControls(clientContext, listItem);
                    //OSEC section
                    fieldsetSecReview.Visible = true;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //Button
                    btnSave.Visible = false;
                    btnSubmitToSecReview.Visible = btnSave.Visible;
                    btnEdit.Visible = true;
                    btnAccept.Visible = true;
                    btnReject.Visible = true;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_Edited:
                    //submitter
                    EnableMainPanel(true);
                    lbMainMessage.Visible = false;
                    fieldsetOSECRejectComment.Visible = false;
                    InitiallyEnableCitationNumberControls(clientContext, listItem);

                    //Sec review section
                    if (previousFormStatus.Equals(Constants.PIWList_FormStatus_PrePublication) ||
                        previousFormStatus.Equals(Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        fieldsetSecReview.Visible = true;
                        EnableSecReviewControls(false);
                    }

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //Button
                    btnSave.Visible = true;
                    btnSubmitToSecReview.Visible = false;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    //submitter
                    EnableMainPanel(false);
                    lbMainMessage.Visible = false;
                    fieldsetOSECRejectComment.Visible = false;

                    InitiallyEnableCitationNumberControls(clientContext, listItem);
                    //Secretary Review
                    fieldsetSecReview.Visible = true;
                    EnableSecReviewControls(false);

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = false;
                    btnSubmitToSecReview.Visible = btnSave.Visible;
                    btnEdit.Visible = true;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnInitiatePublication.Visible = true;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    //submitter
                    EnableMainPanel(false);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "Publication has been initiated for this issuance.";
                    fieldsetOSECRejectComment.Visible = false;
                    EnableCitationNumberControls(false, false);

                    //Sec Review
                    fieldsetSecReview.Visible = true;
                    EnableSecReviewControls(false);

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = false;
                    btnSubmitToSecReview.Visible = btnSave.Visible;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = true;
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    EnableMainPanel(false);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "This issuance is available in eLibrary.";
                    fieldsetOSECRejectComment.Visible = false;
                    EnableCitationNumberControls(false, false);

                    //Sec Review
                    fieldsetSecReview.Visible = true;
                    EnableSecReviewControls(false);

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = true;
                    fieldsetLegalResourcesReview.Visible = true;

                    //buttons
                    btnSave.Visible = true;
                    btnSubmitToSecReview.Visible = false;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnInitiatePublication.Visible = false;
                    btnDelete.Visible = false;
                    btnReopen.Visible = false;
                    break;
                case Constants.PIWList_FormStatus_Deleted:
                    //this status is only viewable by admin
                    EnableMainPanel(false);

                    //PrePublication
                    fieldsetSecReview.Visible = true;
                    EnableSecReviewControls(false);
                    break;
                default:
                    throw new Exception("Unknown Form Status: " + formStatus);
                    
            }
        }

        private void EnableSecReviewControls(bool enabled)
        {
            tbSecReviewComment.Enabled = enabled;
        }

        private void EnableMainPanel(bool enabled)
        {
            EnableFileUploadComponent(enabled);
            tbDocketNumber.Enabled = enabled;
            cbIsNonDocket.Enabled = enabled;
            tbAlternateIdentifier.Enabled = enabled;
            tbDescription.Enabled = enabled;
            tbInstruction.Enabled = enabled;
            cbFederalRegister.Enabled = enabled;
            cbSection206Notice.Enabled = enabled;
            cbHearingOrder.Enabled = enabled;
            ddDocumentCategory.Enabled = enabled;
            ddProgramOfficeWorkflowInitiator.Enabled = enabled;
            //initiator
            inputWorkflowInitiator.Enabled = false;//initiator alsways disabled

            ddProgramOfficeDocumentOwner.Enabled = enabled;
            //document owner
            inputDocumentOwner.Enabled = enabled;

            //notification receiver
            inputNotificationRecipient.Enabled = enabled;
            
            tbDueDate.Enabled = enabled;
            tbComment.Enabled = enabled;
        }

        private void EnableFileUploadComponent(bool enabled)
        {
            //disable/enable fileupload            
            fieldsetUpload.Visible = enabled;

            //supplemental mailing list only allowd 1 document,
            //need to check first, if the control is invisible, don't change it
            //this method is called to disable controls based on status, the control may be already invisible from the PopulateSupplementalMailingListDocumentList method
            if (fieldSetSupplementalMailingList.Visible)
            {
                fieldSetSupplementalMailingList.Visible = enabled;
            }

            //disable/enable the Remove button
            //the link always be enable so user can open document
            foreach (RepeaterItem row in rpDocumentList.Items)
            {
                var btnRemoveDocument = (LinkButton)row.FindControl("btnRemoveDocument");
                btnRemoveDocument.Enabled = enabled;
            }

            foreach (RepeaterItem row in rpSupplementalMailingListDocumentList.Items)
            {
                var btnRemoveDocument = (LinkButton)row.FindControl("btnRemoveDocument");
                btnRemoveDocument.Enabled = enabled;
            }
        }

        public void EnableCitationNumberControls(bool citationNumberCanBeChanged, bool CitationNumberCanBeRemoved)
        {
            //controls
            tbCitationNumber.Enabled = citationNumberCanBeChanged;
            ddAvailableCitationNumbers.Visible = citationNumberCanBeChanged;
            cbOverrideCitationNumber.Visible = citationNumberCanBeChanged;
            //button
            btnGenerateCitationNumber.Visible = citationNumberCanBeChanged;
            btnAcceptCitationNumber.Visible = citationNumberCanBeChanged;
            btnRemoveCitationNumber.Visible = CitationNumberCanBeRemoved;
        }
        public void InitiallyEnableCitationNumberControls(ClientContext clientContext, ListItem piwListItem)
        {
            var piwListinternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            string citationNumber = string.Empty;
            if (piwListItem[piwListinternalName[Constants.PIWList_colName_CitationNumber]] != null)
            {
                citationNumber = piwListItem[piwListinternalName[Constants.PIWList_colName_CitationNumber]].ToString();
            }

            if (string.IsNullOrEmpty(citationNumber))
            {
                //no citation assigned in piwlist
                //system should allow to generate and assign citation number
                EnableCitationNumberControls(true, false);

            }
            else
            {
                //there is citation number assigned to piw list item
                EnableCitationNumberControls(false, true);

            }
        }
        #endregion
    }
}