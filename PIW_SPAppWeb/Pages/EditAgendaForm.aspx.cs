using System;
using System.Collections.Generic;
using System.Linq;
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


        public string PrivilegedDocumentURLsFromViewState
        {
            get
            {
                return ViewState[Constants.PrivilegedDocumentURLsKey] != null ? ViewState[Constants.PrivilegedDocumentURLsKey].ToString() : string.Empty;

            }
            set
            {
                ViewState.Add(Constants.PrivilegedDocumentURLsKey, value);
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

        public string CurrentUserLogInName
        {
            get
            {
                return ViewState[Constants.CurrentLoginNameKey] != null ? ViewState[Constants.CurrentLoginNameKey].ToString() : string.Empty;
            }
            set
            {
                ViewState.Add(Constants.CurrentLoginNameKey, value);
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
                    CurrentUserLogInName = clientContext.Web.CurrentUser.Title;
                }

                lbUploadedDocumentError.Visible = false;

                if (!Page.IsPostBack)
                {
                    if (!string.IsNullOrEmpty(ListItemID))//Edit
                    {
                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
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
                                if (!helper.CanUserViewForm(clientContext, CurrentUserLogInID,
                                    new[] { Constants.Grp_OSEC, Constants.Grp_SecReview }, FormStatus))
                                {
                                    helper.RedirectToAPage(Request, Response, Constants.Page_AccessDenied);
                                    return;
                                }
                                //populate document
                                string publicDocumentURLs;
                                string cEiiDocumentUrLs;
                                string privilegedDocumentURLs;
                                helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList,
                                    out publicDocumentURLs, out cEiiDocumentUrLs, out privilegedDocumentURLs);
                                SaveDocumentURLsToPageProperty(publicDocumentURLs, cEiiDocumentUrLs, privilegedDocumentURLs);
                                helper.PopulateSupplementalMailingListDocumentList(clientContext, ListItemID, rpSupplementalMailingListDocumentList, fieldSetSupplementalMailingList);
                                DisplayListItemInForm(clientContext, listItem);
                                helper.PopulateHistoryList(clientContext, ListItemID, rpHistoryList, Constants.PIWListHistory_FormTypeOption_EditForm);

                                //display form visiblility based on form status
                                ControlsVisiblitilyBasedOnStatus(clientContext, PreviousFormStatus, FormStatus, listItem);

                                //Populate first public document if status is readyforpublishing
                                if ((FormStatus == Constants.PIWList_FormStatus_ReadyForPublishing) ||
                                    (FormStatus == Constants.PIWList_FormStatus_Edited && PreviousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing))
                                {
                                    var PublicDocumentURLs = PublicDocumentURLsFromViewState.Split(new string[] { Constants.DocumentURLsSeparator },
                                        StringSplitOptions.RemoveEmptyEntries);
                                    helper.OpenDocument(Page, PublicDocumentURLs[0]);
                                }
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

                            //check if user is OSEC - redirect to "Access Denied" if not
                            if (!helper.CanUserViewForm(clientContext, CurrentUserLogInID,
                                    new[] { Constants.Grp_OSEC, Constants.Grp_SecReview }, string.Empty))
                            {
                                helper.RedirectToAPage(Request, Response, Constants.Page_AccessDenied);
                                return;
                            }

                            ListItem newItem = helper.createNewPIWListItem(clientContext, Constants.PIWList_FormType_AgendaForm, CurrentUserLogInID);
                            ListItemID = newItem.Id.ToString();

                            //Create subfolder in piwdocuments and mailing list
                            helper.CreatePIWDocumentsSubFolder(clientContext, ListItemID);

                            //Change document and list permission
                            helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, Constants.PIWList_FormStatus_Pending, Constants.PIWList_FormType_AgendaForm);

                            //get current user
                            User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                            clientContext.Load(currentUser);
                            clientContext.ExecuteQuery();

                            //history list
                            if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                            {
                                //Form status must be specified becuae the viewstate hasn't have value
                                helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item created.",
                                    Constants.PIWList_FormStatus_Pending, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                            }
                        }

                        //forward to Edit
                        Response.Redirect(Request.Url + "&ID=" + ListItemID, false);

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, Page.Request.Url.OriginalString);
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

                        //Change document and list permission
                        helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                        //get current user
                        User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                        clientContext.Load(currentUser);
                        clientContext.ExecuteQuery();

                        //Create list history
                        if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                        {
                            string message = "Workflow Item created.";
                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }
                        else
                        {
                            string message = "Workflow Item saved.";
                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }

                        
                        //Refresh or Redirect depends on Previous Status
                        if (PreviousFormStatus.Equals(Constants.PIWList_FormStatus_Pending) ||
                            PreviousFormStatus.Equals(Constants.PIWList_FormStatus_Recalled) ||
                            PreviousFormStatus.Equals(Constants.PIWList_FormStatus_Rejected) ||
                            PreviousFormStatus.Equals(Constants.PIWList_FormStatus_PublishInitiated) ||
                            PreviousFormStatus.Equals(Constants.PIWList_FormStatus_ReOpen))
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
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnSubmitToSecReview_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Submit;
                string currentStatusBeforeWFRun = FormStatus;
                string previousStatusBeforeWFRun = PreviousFormStatus;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    if (ValidFormData(action))
                    {
                        ListItem listItem = null;
                        if (!SaveData(clientContext, action, ref listItem))
                        {
                            return;
                        }

                        //Change document and list permission
                        helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                        //get current user
                        User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                        clientContext.Load(currentUser);
                        clientContext.ExecuteQuery();

                        //email
                        Email emailHelper = new Email();
                        emailHelper.SendEmailForRegularForms(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                            currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);


                        //Create list history
                        if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                        {
                            string message = "Workflow Item created.";
                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }

                        if ((FormStatus == Constants.PIWList_FormStatus_Rejected) || (FormStatus == Constants.PIWList_FormStatus_Recalled))
                        {
                            string message = "Workflow Item resubmitted for Secretary Review.";
                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }
                        else
                        {
                            string message = "Workflow Item submitted for Secretary Review.";
                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }

                        //Redirect
                        helper.RedirectToSourcePage(Page.Request, Page.Response);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Edit;
                string currentStatusBeforeWFRun = FormStatus;
                string previousStatusBeforeWFRun = PreviousFormStatus;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //Change document and list permission
                    helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //email
                    Email emailHelper = new Email();
                    emailHelper.SendEmailForRegularForms(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                        currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                    //Create list history
                    string message = "Workflow Item edited.";
                    helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    //Redirect or Refresh page
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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

                    //Change document and list permission
                    helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //Create list history
                    string message = "Workflow Item accepted.";
                    helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    //Refresh the page
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Reject;
                string currentStatusBeforeWFRun = FormStatus;
                string previousStatusBeforeWFRun = PreviousFormStatus;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //Change document and list permission
                    helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //email
                    Email emailHelper = new Email();
                    emailHelper.SendEmailForRegularForms(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                        currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                    //Create list history
                    string message = "Workflow Item rejected.";
                    helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    //Redirect
                    helper.RedirectToSourcePage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnInitiatePublication_Click(object sender, EventArgs e)
        {
            try
            {
                string currentStatusBeforeWFRun = FormStatus;
                string previousStatusBeforeWFRun = PreviousFormStatus;
                const enumAction action = enumAction.Publish;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //issuance documents
                    Dictionary<string, string> issuanceDocuments = helper.getAllDocumentUrls(rpDocumentList);

                    //supplemental mailing list - only 1 excel document
                    string supplementalMailingListFileName = string.Empty;
                    if (rpSupplementalMailingListDocumentList.Items.Count > 0)
                    {
                        RepeaterItem row = rpSupplementalMailingListDocumentList.Items[0];
                        var downloadedURL = helper.getFileNameFromURL(((HyperLink)row.FindControl("hyperlinkFileURL")).NavigateUrl);
                        supplementalMailingListFileName = downloadedURL.Substring(0, downloadedURL.IndexOf("?web=0"));
                    }

                    //publish
                    EPSPublicationHelper epsHelper = new EPSPublicationHelper();
                    epsHelper.Publish(clientContext, issuanceDocuments, supplementalMailingListFileName, listItem);

                    //Change document and list permission
                    helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //send email
                    Email emailHelper = new Email();
                    emailHelper.SendEmailForRegularForms(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                        currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                    //Create list history
                    string message = "Workflow Item publication to eLibrary Data Entry initiated.";
                    helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    //Redirect
                    helper.RedirectToSourcePage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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

                    //Change document and list permission
                    helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //Create list history
                    string message = "Workflow Item deleted.";
                    helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    //Redirect
                    helper.RedirectToSourcePage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnRecall_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Recall;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    if (ValidFormData(action))
                    {
                        ListItem listItem = null;
                        if (!SaveData(clientContext, action, ref listItem))
                        {
                            return;
                        }


                        //Change document and list permission
                        helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                        //get current user
                        User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                        clientContext.Load(currentUser);
                        clientContext.ExecuteQuery();

                        //Create list history
                        string message = "Workflow Item recalled.";
                        helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                        //Redirect
                        helper.RedirectToSourcePage(Page.Request, Page.Response);


                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnSECReviewTakeOwnership_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.SecReviewTakeOwnerShip;
                string currentStatusBeforeWFRun = FormStatus;
                string previousStatusBeforeWFRun = PreviousFormStatus;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //Change document and list permission
                    helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //email
                    Email emailHelper = new Email();
                    emailHelper.SendEmailForRegularForms(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                        currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                    //Create list history
                    string message = "Secretary Reviewer took ownership of Workflow Item.";
                    helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    //refresh
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnReopen_Click(object sender, EventArgs e)
        {
            try
            {
                string currentStatusBeforeWFRun = FormStatus;
                string previousStatusBeforeWFRun = PreviousFormStatus;
                const enumAction action = enumAction.ReOpen;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //Change document and list permission
                    helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //email
                    Email emailHelper = new Email();
                    emailHelper.SendEmailForRegularForms(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                        currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                    //Create list history
                    string message = "Workflow Item Re-Opened.";
                    helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                        Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    //Refresh
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (fileUpload.HasFiles)
                {
                    using (var clientContext = helper.getElevatedClientContext(Context, Request))
                    {
                        var uploadedFileURL = helper.UploadIssuanceDocument(clientContext, fileUpload, ListItemID, rpDocumentList,
                            lbUploadedDocumentError, lbRequiredUploadedDocumentError, FormStatus,
                            ddlSecurityControl.SelectedValue, Constants.PIWDocuments_DocTypeOption_Issuance, CurrentUserLogInID);
                        if (!string.IsNullOrEmpty(uploadedFileURL)) //only save the document url if the upload is good
                        {
                            string publicDocumentURLs;
                            string cEiiDocumentUrLs;
                            string privilegedDocumentURLs;
                            helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList,
                                out publicDocumentURLs, out cEiiDocumentUrLs, out privilegedDocumentURLs);
                            SaveDocumentURLsToPageProperty(publicDocumentURLs, cEiiDocumentUrLs, privilegedDocumentURLs);
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
                            helper.OpenDocument(Page, uploadedFileURL);
                        }
                    }

                    //reset security level
                    ddlSecurityControl.SelectedIndex = 0;
                }

            }
            catch (Exception ex)
            {
                helper.LogError(Context, Request, ex, ListItemID, string.Empty);
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
                    if (supplementalMailingListFileUpload.PostedFile.ContentLength <= 52428800)
                    {
                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
                            var uploadResult = helper.UploadSupplementalMailingListDocument(clientContext, supplementalMailingListFileUpload, ListItemID, rpSupplementalMailingListDocumentList,
                                lbSupplementalMailingListUploadError, FormStatus, Constants.ddlSecurityControl_Option_Public, Constants.PIWDocuments_DocTypeOption_SupplementalMailingList, CurrentUserLogInID);
                            if (uploadResult) //only save the document url if the upload is good
                            {
                                helper.PopulateSupplementalMailingListDocumentList(clientContext, ListItemID, rpSupplementalMailingListDocumentList, fieldSetSupplementalMailingList);
                            }
                        }
                    }
                    else
                    {
                        lbSupplementalMailingListUploadError.Text = "file size limits to 50 MB";
                        lbSupplementalMailingListUploadError.Visible = true;
                    }
                }

            }
            catch (Exception ex)
            {
                helper.LogError(Context, Request, ex, ListItemID, string.Empty);
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
                            string privilegedDocumentURLs;
                            helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList,
                                out publicDocumentURLs, out cEiiDocumentUrLs, out privilegedDocumentURLs);
                            SaveDocumentURLsToPageProperty(publicDocumentURLs, cEiiDocumentUrLs, privilegedDocumentURLs);

                            //get current user
                            User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                            clientContext.Load(currentUser);
                            clientContext.ExecuteQuery();

                            //history list
                            helper.CreatePIWListHistory(clientContext, ListItemID, string.Format("Document file {0} removed.",
                                removedFileName), FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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

                            //get current user
                            User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                            clientContext.Load(currentUser);
                            clientContext.ExecuteQuery();

                            //history list
                            helper.CreatePIWListHistory(clientContext, ListItemID, string.Format("Supplemental Mailing List file {0} removed.", removedFileName),
                                FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
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
                    helper.GenerateCitation(clientContext, ddDocumentCategory, tbCitationNumber, ddAvailableCitationNumbers, true);

                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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

                                //get current user
                                User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                                clientContext.Load(currentUser);
                                clientContext.ExecuteQuery();

                                //history list
                                helper.CreatePIWListHistory(clientContext, ListItemID, "Citation number assigned: " + tbCitationNumber.Text.Trim(),
                                    FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                                //add citation to first public document
                                //must be the last action because it can throw exceptioniled if the docs is opened in MS-Word
                                //it will not able to finish all actions
                                var documentURLs = PublicDocumentURLsFromViewState.Split(new string[] { Constants.DocumentURLsSeparator },
                                        StringSplitOptions.RemoveEmptyEntries);

                                var documentURL = documentURLs[0];
                                var fileName = helper.getFileNameFromURL(documentURL);
                                helper.AddCitationNumberToDocument(clientContext, tbCitationNumber.Text.Trim(),
                                    ListItemID, fileName);

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
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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

                        //get current user
                        User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                        clientContext.Load(currentUser);
                        clientContext.ExecuteQuery();

                        //history list
                        helper.CreatePIWListHistory(clientContext, ListItemID, "Citation number removed.", FormStatus,
                            Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                        //remove citation number from the documents - must be the last action because it can throw exceptioniled if the docs is opened in MS-Word
                        //it will not able to finish all actions
                        var documentURLs = PublicDocumentURLsFromViewState.Split(new string[] { Constants.DocumentURLsSeparator },
                                StringSplitOptions.RemoveEmptyEntries);
                        var documentURL = documentURLs[0];
                        var fileName = helper.getFileNameFromURL(documentURL);
                        helper.RemoveCitationNumberFromDocument(clientContext, citationNumber, ListItemID, fileName);
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
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void ddAvailableCitationNumbers_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbCitationNumber.Text = ddAvailableCitationNumbers.SelectedValue;
        }

        protected void cbSection206Notice_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    lbNotificationRecipientError.Visible = false;

                    if (cbSection206Notice.Checked)
                    {
                        Group section206Notice = clientContext.Web.SiteGroups.GetByName(Constants.Grp_PIWSection206Notice);
                        clientContext.Load(section206Notice.Users);
                        clientContext.ExecuteQuery();
                        User[] users = section206Notice.Users.ToArray();
                        PeoplePickerHelper.AddPeoplePickerValues(hdnNotificationRecipient, users);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                lbNotificationRecipientError.Visible = true;
                lbNotificationRecipientError.Text = "cannot populate users for Section 206 Notice group";
            }
        }

        protected void cbFederalRegister_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    lbNotificationRecipientError.Visible = false;

                    if (cbFederalRegister.Checked)
                    {
                        Group Grp = clientContext.Web.SiteGroups.GetByName(Constants.Grp_PIWFederalResister);
                        clientContext.Load(Grp.Users);
                        clientContext.ExecuteQuery();
                        User[] users = Grp.Users.ToArray();
                        PeoplePickerHelper.AddPeoplePickerValues(hdnNotificationRecipient, users);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                lbNotificationRecipientError.Visible = true;
                lbNotificationRecipientError.Text = "cannot populate users for Federal Register group";
            }
        }

        protected void cbHearingOrder_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    lbNotificationRecipientError.Visible = false;

                    if (cbHearingOrder.Checked)
                    {
                        Group Grp = clientContext.Web.SiteGroups.GetByName(Constants.Grp_PIWHearingOrder);
                        clientContext.Load(Grp.Users);
                        clientContext.ExecuteQuery();
                        User[] users = Grp.Users.ToArray();
                        PeoplePickerHelper.AddPeoplePickerValues(hdnNotificationRecipient, users);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                lbNotificationRecipientError.Visible = true;
                lbNotificationRecipientError.Text = "cannot populate users for Hearing Order group";
            }
        }

        protected void btnLegalReviewCompleted_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.LegalReviewComplete;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    if (ValidFormData(action))
                    {
                        ListItem listItem = null;
                        if (!SaveData(clientContext, action, ref listItem))
                        {
                            return;
                        }

                        //history list
                        //get current user
                        User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                        clientContext.Load(currentUser);
                        clientContext.ExecuteQuery();

                        //Create list history
                        string message = "Legal Review Completed.";
                        helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                        //Redirect
                        helper.RedirectToSourcePage(Request, Response);

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        #endregion

        #region Save Data
        private bool SaveData(ClientContext clientContext, enumAction action, ref ListItem returnedListItem)
        {
            ListItem listItem = helper.GetPiwListItemById(clientContext, ListItemID, false);

            if (helper.CheckIfListItemChanged(clientContext, listItem, DateTime.Parse(ModifiedDateTime)))
            {
                lbMainMessage.Text = "Form has been modified by other User - Please Refresh by highlighting URL and hitting the ENTER key";
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
            const string errorMessage = "UpdateFormDataToList method: Unknown Status and Previous status combination. Status:{0}, Previous Status: {1}";
            switch (FormStatus)//this is the next status after action is performed
            {
                case Constants.PIWList_FormStatus_Pending:
                    if (FormStatus == PreviousFormStatus)//save action
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Recalled:
                    if (FormStatus == PreviousFormStatus)//save action
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Submitted)
                    {
                        //SaveFormStatusAndRecallRejectComment(clientContext, listItem, action);
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                            action, tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Rejected:
                    if (FormStatus == PreviousFormStatus)//save action
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Edited)//reject from Edited mode
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_SecretaryReview)//reject from sec review
                    {
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                            action, tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_Pending) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Recalled) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_ReOpen) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Rejected))
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_Edited:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_SecretaryReview) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                            action, tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Deleted:
                    //delete item, need to set status and remove citation number if there is assigned one
                    helper.SaveDeleteInfoAndStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                        tbComment.Text.Trim(), CurrentUserLogInName);
                    helper.ReleaseCitationNumberForDeletedListItem(clientContext, ListItemID);
                    break;
                case Constants.PIWList_FormStatus_SecretaryReview:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_Submitted) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Edited))
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:

                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Edited) //save
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_SecretaryReview)
                    {
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                            action, tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Edited)
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                        helper.SavePublishingInfoAndStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                            tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing)
                    {
                        helper.SavePublishingInfoAndStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                            tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_PublishedToeLibrary)
                    {
                        helper.SaveLegalResourcesAndReviewAndComment(clientContext, listItem, DateTime.Now, tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_ReOpen:
                    helper.SaveReOpenInfoAndStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                        tbComment.Text.Trim(), CurrentUserLogInName);
                    break;
                default:
                    throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));

            }
        }

        //private void SaveFormStatusAndRecallRejectComment(ClientContext clientContext, ListItem listItem, enumAction action)
        //{
        //    var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

        //    //recall/reject comment
        //    if ((action == enumAction.Recall) || (action == enumAction.Reject))
        //    {
        //        string comment = tbComment.Text.Trim();
        //        if (!string.IsNullOrEmpty(comment))
        //        {
        //            if (comment.Length <= 255)
        //            {
        //                listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] = comment;
        //            }
        //            else
        //            {
        //                listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] =
        //                    comment.Substring(0, 255);

        //            }

        //        }
        //    }

        //    listItem.Update();
        //    clientContext.ExecuteQuery();
        //}

        //private void ClearSecReviewActionsAndCommentsBeforeReSubmit(ClientContext clientContext, ListItem listItem)
        //{
        //    var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

        //    //listItem[piwListInternalColumnNames[Constants.PIWList_colName_SecReviewAction]] = string.Empty;
        //    //listItem[piwListInternalColumnNames[Constants.PIWList_colName_SecReviewComment]] = string.Empty;

        //    listItem.Update();
        //    clientContext.ExecuteQuery();
        //}

        private void SaveMainPanelAndStatus(ClientContext clientContext, ListItem listItem, enumAction action)
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
            //FieldUserValue[] notificationRecipients = null;
            var notificationRecipients = PopulateNotificationRecipients(clientContext, listItem, piwListInternalColumnNames);


            //Populate current user title
            clientContext.Load(clientContext.Web.CurrentUser, user => user.Title);
            clientContext.ExecuteQuery();

            //Save Data

            //Save IsActive
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_IsActive]] = true;

            //Save Docket Number
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocketNumber]] = helper.RemoveDuplicateDocket(tbDocketNumber.Text.Trim());

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

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]] = ddProgramOfficeWorkflowInitiator.SelectedValue;


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
            if (!string.IsNullOrEmpty(tbComment.Text.Trim()))
            {
                helper.SetCommentHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, tbComment.Text.Trim(), Constants.PIWList_FormType_AgendaForm);
            }

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

            //Privileged Document URLs
            if (!string.IsNullOrEmpty(PrivilegedDocumentURLsFromViewState))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrivilegedDocumentURLs]] = PrivilegedDocumentURLsFromViewState;
            }

            //recall/reject comment
            if ((action == enumAction.Recall) || (action == enumAction.Reject))
            {
                if (!string.IsNullOrEmpty(tbComment.Text.Trim()))
                {
                    listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] = tbComment.Text.Trim();
                }
            }

            //edit form url
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_EditFormURL]] = Request.Url.ToString();

            //execute query
            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private FieldUserValue[] PopulateNotificationRecipients(ClientContext clientContext, ListItem listItem, Dictionary<string, string> piwListInternalColumnNames)
        {
            List<FieldUserValue> notificationRecipients = new List<FieldUserValue>();

            //value specidifed from control
            if (!string.IsNullOrEmpty(hdnNotificationRecipient.Value))
            {
                List<PeoplePickerUser> users = PeoplePickerHelper.GetValuesFromPeoplePicker(hdnNotificationRecipient);

                //notificationRecipients = new FieldUserValue[users.Count];
                for (var i = 0; i < users.Count; i++)
                {
                    var newUser = clientContext.Web.EnsureUser(users[i].Login);
                    //ensure user so usr can be added to site if they are not --> receive email
                    clientContext.Load(newUser);
                    clientContext.ExecuteQuery();
                    notificationRecipients.Add(new FieldUserValue { LookupId = newUser.Id });
                }
            }

            //value populated if checbox "Section 206 Notice" or "Hearing Order" are checked
            //bool Section206NoticePreviouslyChecked = bool.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_Section206Notice]].ToString());
            //bool HearingOrderPreviouslyChecked = bool.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_HearingOrder]].ToString());

            ////populate users if "section 206 notice" is first checked
            //if (cbSection206Notice.Checked)
            //{
            //    if (!Section206NoticePreviouslyChecked)
            //    {
            //        clientContext.Web.SiteGroups.GetByName(Constants.Grp_206)
            //    }
            //}

            ////populate users if "Hearing Order" is first checked
            //if (cbHearingOrder.Checked)
            //{
            //    if (!HearingOrderPreviouslyChecked)
            //    {

            //    }
            //}


            return notificationRecipients.ToArray();
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

            //Check if there is a uploaded Public document
            if (rpDocumentList.Items.Count > 0)//there is uploaded document,
            {
                //check if atleast one public document
                foreach (RepeaterItem i in rpDocumentList.Items)
                {

                    Label lbSecuriLevel = (Label)i.FindControl("lbSecurityLevel");
                    isValid = lbSecuriLevel.Text.Equals(Constants.ddlSecurityControl_Option_Public);

                    if (isValid)//stop checking if found one public document
                    {
                        break;
                    }
                }

                lbRequiredUploadedDocumentError.Visible = !isValid;
            }
            else//no uploaded document
            {
                isValid = false;
                lbRequiredUploadedDocumentError.Visible = true;
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

            //FormType
            if (listItem[internalColumnNames[Constants.PIWList_colName_FormType]] != null)
            {
                FormType = listItem[internalColumnNames[Constants.PIWList_colName_FormType]].ToString();
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
                    lbheaderDocketNumber.Text = tbDocketNumber.Text + " - ";
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
                    var users = helper.getUsersFromField(clientContext, fuv);
                    PeoplePickerHelper.FillPeoplePickerValue(hdnDocumentOwner, users);
                }

                //Notification Recipient
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_NotificationRecipient]] != null)
                {
                    FieldUserValue[] fuv = (FieldUserValue[])listItem[piwListInteralColumnNames[Constants.PIWList_colName_NotificationRecipient]];
                    var users = helper.getUsersFromField(clientContext, fuv);
                    PeoplePickerHelper.FillPeoplePickerValue(hdnNotificationRecipient, users);
                }

                //Due Date
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_DueDate]] != null)
                {
                    tbDueDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_DueDate]].ToString()).ToShortDateString();
                }

                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]] != null)
                {
                    lbCommentValue.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]].ToString();
                }

                //Citation Number
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_CitationNumber]] != null)
                {
                    tbCitationNumber.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_CitationNumber]].ToString();
                }

                //mail room
                hyperlinkPrintReq.NavigateUrl = helper.getEditFormURL(Constants.PIWList_FormType_PrintReqForm, ListItemID, Request, string.Empty);
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]] != null)
                {
                    tbPrintDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]].ToString()).ToShortDateString();
                }

                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] != null)
                {
                    tbMailDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]].ToString()).ToShortDateString();
                }


                //Legal resources and review
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_LegalResourcesAndReviewGroupCompleteDate]] != null)
                {
                    tbLegalResourcesReviewCompletionDateValue.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_LegalResourcesAndReviewGroupCompleteDate]].ToString()).ToShortDateString();
                }

            }
        }

        public void SaveDocumentURLsToPageProperty(string publicDocumentURLs, string cEIIDocumentURLs, string privilegedDocumentURLs)
        {
            if (!string.IsNullOrEmpty(publicDocumentURLs))
            {
                PublicDocumentURLsFromViewState = publicDocumentURLs;
            }

            if (!string.IsNullOrEmpty(cEIIDocumentURLs))
            {
                CEIIDocumentURLsFromViewState = cEIIDocumentURLs;
            }

            if (!string.IsNullOrEmpty(privilegedDocumentURLs))
            {
                PrivilegedDocumentURLsFromViewState = privilegedDocumentURLs;
            }
        }

        public void SetVisiblePropertyInTopButtons()
        {
            btnSave1.Visible = btnSave.Visible;
            btnSubmit1.Visible = btnSubmit.Visible;
            btnSECReviewTakeOwnership1.Visible = btnSECReviewTakeOwnership.Visible;
            btnRecall1.Visible = btnRecall.Visible;
            btnEdit1.Visible = btnEdit.Visible;
            btnAccept1.Visible = btnAccept.Visible;
            btnReject1.Visible = btnReject.Visible;
            btnInitiatePublication1.Visible = btnInitiatePublication.Visible;
            btnDelete1.Visible = btnDelete.Visible;
            btnReopen1.Visible = btnReopen.Visible;
            btnLegalReviewCompleted1.Visible = btnLegalReviewCompleted.Visible;
            btnLegalReviewCompletedWithComment1.Visible = btnLegalReviewCompletedWithComment.Visible;
            btnGenerateMailingList1.Visible = btnGenerateMailingList.Visible;
        }
        #endregion

        #region Visibility
        public void ControlsVisiblitilyBasedOnStatus(ClientContext clientContext, string previousFormStatus, string formStatus, ListItem listItem)
        {
            var piwlistInternalColumnName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            bool isCurrentUserOSEC = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                            new string[] { Constants.Grp_OSEC, Constants.Grp_SecReview });

            bool isCurrentUserSecReviewer = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                            new string[] { Constants.Grp_SecReview });
            bool isCurrentUserLegalResouceTeam = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                            new string[] { Constants.Grp_PIWLegalResourcesReview });


            //number of pritn req copies, used to determine if there is print req submitted
            int numberOfPrintCopies = 0;
            if (listItem[piwlistInternalColumnName[Constants.PIWList_colName_PrintReqNumberofCopies]] != null)
            {
                numberOfPrintCopies = int.Parse(listItem[piwlistInternalColumnName[Constants.PIWList_colName_PrintReqNumberofCopies]].ToString());
            }

            //legal review date
            string legalReviewCompletedDate = listItem[piwlistInternalColumnName[Constants.PIWList_colName_LegalResourcesAndReviewGroupCompleteDate]] != null ?
                listItem[piwlistInternalColumnName[Constants.PIWList_colName_LegalResourcesAndReviewGroupCompleteDate]].ToString() : string.Empty;

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
                    EnableMainPanel(true, true);
                    lbMainMessage.Visible = false;

                    InitiallyEnableCitationNumberControls(clientContext, listItem);
                    //SEC Review section
                    fieldsetSecReview.Visible = false;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = isCurrentUserOSEC;

                    btnSubmit.Visible = btnSave.Visible;

                    btnEdit.Visible = false;

                    btnAccept.Visible = false;

                    btnReject.Visible = false;

                    btnSECReviewTakeOwnership.Visible = false;

                    btnRecall.Visible = false;

                    btnInitiatePublication.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    btnReopen.Visible = false;
                    btnLegalReviewCompleted.Visible = false;
                    btnLegalReviewCompletedWithComment.Visible = false;
                    btnGenerateMailingList.Visible = false;
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    EnableMainPanel(false, false);
                    lbMainMessage.Visible = false;

                    //citation number controls
                    EnableCitationNumberControls(false, false);

                    //SEC Review section
                    fieldsetSecReview.Visible = false;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = false;
                    btnSubmit.Visible = btnSave.Visible;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSECReviewTakeOwnership.Visible = isCurrentUserSecReviewer;
                    btnRecall.Visible = isCurrentUserOSEC;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    btnReopen.Visible = false;
                    btnLegalReviewCompleted.Visible = false;
                    btnLegalReviewCompletedWithComment.Visible = false;
                    btnGenerateMailingList.Visible = false;
                    break;
                case Constants.PIWList_FormStatus_SecretaryReview:
                    //submit section   
                    EnableMainPanel(false, isCurrentUserSecReviewer);
                    lbMainMessage.Visible = false;

                    EnableCitationNumberControls(false, false);
                    //OSEC section
                    fieldsetSecReview.Visible = true;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //Button
                    btnSave.Visible = false;
                    btnSubmit.Visible = btnSave.Visible;
                    btnEdit.Visible = isCurrentUserSecReviewer;
                    btnAccept.Visible = isCurrentUserSecReviewer;
                    btnReject.Visible = isCurrentUserSecReviewer;
                    btnSECReviewTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;
                    btnLegalReviewCompleted.Visible = false;
                    btnLegalReviewCompletedWithComment.Visible = false;
                    btnGenerateMailingList.Visible = false;
                    break;
                case Constants.PIWList_FormStatus_Edited:
                    //submitter
                    lbMainMessage.Visible = false;

                    if (isCurrentUserSecReviewer)
                    {
                        EnableMainPanel(true, true);
                        InitiallyEnableCitationNumberControls(clientContext, listItem);
                    }
                    else if (isCurrentUserOSEC)
                    {
                        EnableMainPanel(false, false);
                        EnableCitationNumberControls(false, false);
                    }

                    //Sec review section
                    if (previousFormStatus.Equals(Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        fieldsetSecReview.Visible = true;
                    }

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //Button
                    btnSave.Visible = isCurrentUserSecReviewer;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;

                    if (previousFormStatus.Equals(Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        btnAccept.Visible = false;
                        btnReject.Visible = false;
                        btnInitiatePublication.Visible = isCurrentUserSecReviewer;
                    }
                    else
                    {
                        btnAccept.Visible = isCurrentUserSecReviewer;
                        btnReject.Visible = isCurrentUserSecReviewer;
                        btnInitiatePublication.Visible = false;
                    }



                    btnSECReviewTakeOwnership.Visible = false;
                    btnRecall.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;
                    btnLegalReviewCompleted.Visible = false;
                    btnLegalReviewCompletedWithComment.Visible = false;
                    btnGenerateMailingList.Visible = false;
                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    //submitter
                    lbMainMessage.Visible = false;


                    EnableMainPanel(false, isCurrentUserSecReviewer);
                    EnableCitationNumberControls(false, false);

                    //Secretary Review
                    fieldsetSecReview.Visible = true;


                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = false;
                    btnSubmit.Visible = btnSave.Visible;
                    btnEdit.Visible = isCurrentUserSecReviewer;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSECReviewTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = isCurrentUserSecReviewer;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;
                    btnLegalReviewCompleted.Visible = false;
                    btnLegalReviewCompletedWithComment.Visible = false;
                    btnGenerateMailingList.Visible = false;
                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    //submitter
                    EnableMainPanel(false, false);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "Publication has been initiated for this issuance.";

                    EnableCitationNumberControls(false, false);

                    //Sec Review
                    fieldsetSecReview.Visible = true;


                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = false;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSECReviewTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                        new string[] { Constants.Grp_PIWAdmin, Constants.Grp_PIWSystemAdmin });
                    btnLegalReviewCompleted.Visible = false;
                    btnLegalReviewCompletedWithComment.Visible = false;
                    btnGenerateMailingList.Visible = false;
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    EnableMainPanel(false, false);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "This issuance is available in eLibrary.";

                    EnableCitationNumberControls(false, false);

                    //Sec Review
                    fieldsetSecReview.Visible = true;


                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = (numberOfPrintCopies > 0);
                    fieldsetLegalResourcesReview.Visible = true;

                    //buttons
                    btnSave.Visible = false;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSECReviewTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = false;
                    btnDelete.Visible = false;
                    btnReopen.Visible = false;
                    btnLegalReviewCompleted.Visible = isCurrentUserLegalResouceTeam && string.IsNullOrEmpty(legalReviewCompletedDate);
                    btnLegalReviewCompletedWithComment.Visible = isCurrentUserLegalResouceTeam && string.IsNullOrEmpty(legalReviewCompletedDate);
                    btnGenerateMailingList.Visible = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                        new string[] { Constants.Grp_PIWAdmin, Constants.Grp_PIWSystemAdmin });
                    break;
                case Constants.PIWList_FormStatus_Deleted:
                    //this status is only viewable by admin

                    EnableMainPanel(false, false);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "This issuance has been deleted.";

                    //citation number controls
                    EnableCitationNumberControls(false, false);

                    //SEC Review section
                    fieldsetSecReview.Visible = true;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = true;
                    fieldsetLegalResourcesReview.Visible = true;

                    //buttons
                    btnSave.Visible = false;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSECReviewTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = false;
                    btnReopen.Visible = false;
                    btnLegalReviewCompleted.Visible = false;
                    btnLegalReviewCompletedWithComment.Visible = false;
                    btnGenerateMailingList.Visible = false;
                    break;

                    break;
                default:
                    throw new Exception("ControlVisibilityBasedOnFormStatus - Unknown Form Status: " + formStatus);

            }

            SetVisiblePropertyInTopButtons();
        }

        protected void btnGenerateMailingList_Click(object sender, EventArgs e)
        {
            using (var clientContext = helper.getElevatedClientContext(Context, Request))
            {
                ListItem listItem = helper.GetPiwListItemById(clientContext, ListItemID, true);
                bool regenerateResult = helper.GenerateAndSubmitPrintReqForm(clientContext, listItem, CurrentUserLogInID,true);
                
                lbMainMessage.Visible = true;
                if (regenerateResult)
                {
                    lbMainMessage.Text = "Print Requisition Form Regenerated";
                }
                else
                {
                    lbMainMessage.Text = "There is no FOLA mailing list or supplemental mailing to generate Print Requisition Form";
                }
                
            }
        }

        private void EnableMainPanel(bool enabled, bool canEditUploadedDocument)
        {
            EnableFileUploadComponent(enabled, canEditUploadedDocument);
            tbDocketNumber.Enabled = enabled;
            cbIsNonDocket.Enabled = enabled;
            tbAlternateIdentifier.Enabled = enabled;
            tbDescription.Enabled = enabled;
            tbInstruction.Enabled = enabled;
            cbFederalRegister.Enabled = enabled;
            cbSection206Notice.Enabled = enabled;
            cbHearingOrder.Enabled = enabled;
            ddDocumentCategory.Enabled = enabled;
            //only osec --> disable from UI
            //ddProgramOfficeWorkflowInitiator.Enabled = enabled;
            //initiator
            inputWorkflowInitiator.Enabled = false;//initiator alsways disabled
            ddProgramOfficeDocumentOwner.Enabled = enabled;
            //document owner
            inputDocumentOwner.Enabled = enabled;
            //notification receiver
            inputNotificationRecipient.Enabled = enabled;
            tbDueDate.Enabled = enabled;
            //tbComment.Enabled = enabled;
        }

        private void EnableFileUploadComponent(bool enabled, bool canEditUploadedDocument)
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

            //disable/enable the Remove and Edit button
            //the link always be enable so user can open document
            foreach (RepeaterItem row in rpDocumentList.Items)
            {
                var btnRemoveDocument = (LinkButton)row.FindControl("btnRemoveDocument");
                if (btnRemoveDocument != null) btnRemoveDocument.Visible = enabled;

                var hplEdit = (HyperLink)row.FindControl("hplEdit");
                if (hplEdit != null) hplEdit.Visible = canEditUploadedDocument;
            }

            foreach (RepeaterItem row in rpSupplementalMailingListDocumentList.Items)
            {
                var btnRemoveDocument = (LinkButton)row.FindControl("btnRemoveDocument");
                if (btnRemoveDocument != null) btnRemoveDocument.Visible = enabled;

                var hplEdit = (HyperLink)row.FindControl("hplEdit");
                if (hplEdit != null) hplEdit.Visible = canEditUploadedDocument;
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

        /// <summary>
        /// if a citation is assigned, generate button should not displayed
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="piwListItem"></param>
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