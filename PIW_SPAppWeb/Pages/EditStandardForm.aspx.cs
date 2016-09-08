using System;
using System.Collections.Generic;
using System.Web.Services;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.UserProfiles;
using PIW_SPAppWeb.Helper;
using ListItem = Microsoft.SharePoint.Client.ListItem;

namespace PIW_SPAppWeb.Pages
{
    public partial class EditStandardForm : System.Web.UI.Page
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
        static SharePointHelper helper;
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
                            else//there is list item
                            {
                                PopulateFormStatusAndProperties(clientContext, listItem);//because we need to use FormStatus 
                                if (!helper.CanUserViewForm(clientContext, CurrentUserLogInID,
                                    new[] { Constants.Grp_PIWUsers, Constants.Grp_OSEC, Constants.Grp_SecReview },
                                    FormStatus))
                                {
                                    helper.RedirectToAPage(Request, Response, Constants.Page_AccessDenied);
                                    return;
                                }

                                //populate documents
                                string publicDocumentURLs;
                                string cEiiDocumentUrLs;
                                string privilegedDocumentURLs;
                                helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList,
                                   out publicDocumentURLs, out cEiiDocumentUrLs, out privilegedDocumentURLs);
                                SaveDocumentURLsToPageProperty(publicDocumentURLs, cEiiDocumentUrLs, privilegedDocumentURLs);

                                if (!string.IsNullOrEmpty(publicDocumentURLs))
                                {
                                    PublicDocumentURLsFromViewState = publicDocumentURLs;
                                }

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
                            //check if user can create form - redirect to "Access Denied" if not
                            if (!helper.CanUserViewForm(clientContext, CurrentUserLogInID,
                                    new[] { Constants.Grp_PIWUsers, Constants.Grp_OSEC, Constants.Grp_SecReview }, string.Empty))
                            {
                                helper.RedirectToAPage(Request, Response, Constants.Page_AccessDenied);
                                return;
                            }


                            ListItem newItem = helper.createNewPIWListItem(clientContext, Constants.PIWList_FormType_StandardForm, CurrentUserLogInID);
                            ListItemID = newItem.Id.ToString();

                            //Create subfolder in piwdocuments and mailing list
                            helper.CreatePIWDocumentsSubFolder(clientContext, ListItemID);

                            //Change document and list permission
                            helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, Constants.PIWList_FormStatus_Pending, Constants.PIWList_FormType_StandardForm);

                            //get current user
                            User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                            clientContext.Load(currentUser);
                            clientContext.ExecuteQuery();

                            //history list
                            if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                            {
                                //Form status must be specified becuae the viewstate hasn't have value
                                helper.CreatePIWListHistory(clientContext, ListItemID, "Workflow Item created.", Constants.PIWList_FormStatus_Pending,
                                    Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
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
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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

                        //TODO: send email

                        //Create list history
                        if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                        {
                            string message = "Workflow Item created.";
                            //if (!string.IsNullOrEmpty(tbComment.Text))
                            //{
                            //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                            //}

                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }
                        else
                        {
                            //create history list for comment - it must be done after commit change to the list, or all pending changes will be clear
                            //because the CreatePIWListHistory call another ExecuteQuery 
                            string message = "Workflow Item saved.";
                            //if (!string.IsNullOrEmpty(tbComment.Text))
                            //{
                            //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                            //}


                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
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
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }
        protected void btnSubmit_Click(object sender, EventArgs e)
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

                        Email emailHelper = new Email();

                        emailHelper.SendEmail(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                            currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                        //Create list history
                        if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                        {
                            string message = "Workflow Item created.";
                            //if (!string.IsNullOrEmpty(tbComment.Text))
                            //{
                            //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                            //}
                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }

                        if ((FormStatus == Constants.PIWList_FormStatus_Rejected) || (FormStatus == Constants.PIWList_FormStatus_Recalled))
                        {
                            string message = "Workflow Item resubmitted.";
                            //if (!string.IsNullOrEmpty(tbComment.Text))
                            //{
                            //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                            //}

                            helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }
                        else
                        {
                            string message = "Workflow Item submitted.";
                            //if (!string.IsNullOrEmpty(tbComment.Text))
                            //{
                            //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                            //}
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

        protected void btnAccept_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Accept;
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
                        string message = "Workflow Item accepted.";

                        //if (!string.IsNullOrEmpty(tbComment.Text))
                        //{
                        //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                        //}
                        helper.CreatePIWListHistory(clientContext, ListItemID, message, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                        //Redirect
                        if (FormStatus.Equals(Constants.PIWList_FormStatus_ReadyForPublishing))
                        {
                            helper.RefreshPage(Request, Response);
                        }
                        else
                        {
                            helper.RedirectToSourcePage(Page.Request, Page.Response);
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

        protected void btnReject_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Reject;
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

                        //send email
                        Email emailHelper = new Email();
                        emailHelper.SendEmail(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                            currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                        //Create list history
                        string message = "Workflow Item rejected.";
                        //if (!string.IsNullOrEmpty(tbComment.Text))
                        //{
                        //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                        //}
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

        protected void btnInitiatePublication_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Publish;
                string currentStatusBeforeWFRun = FormStatus;
                string previousStatusBeforeWFRun = PreviousFormStatus;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //publish
                    //issuance documents
                    Dictionary<string, string> issuanceDocuments = new Dictionary<string, string>();
                    foreach (RepeaterItem row in rpDocumentList.Items)
                    {
                        var url = ((HyperLink)row.FindControl("hplEdit")).NavigateUrl;
                        var securityLevel = ((Label)row.FindControl("lbSecurityLevel")).Text;
                        if (!issuanceDocuments.ContainsKey(url))
                        {
                            issuanceDocuments.Add(url, securityLevel);
                        }
                    }

                    //supplemental mailing list - only 1 excel document
                    string supplementalMailingListFileName = string.Empty;
                    if (rpSupplementalMailingListDocumentList.Items.Count > 0)
                    {
                        RepeaterItem row = rpSupplementalMailingListDocumentList.Items[0];
                        var downloadedURL = helper.getFileNameFromURL(((HyperLink)row.FindControl("hyperlinkFileURL")).NavigateUrl);
                        supplementalMailingListFileName = downloadedURL.Substring(0, downloadedURL.IndexOf("?web=0"));
                    }


                    EPSPublicationHelper epsHelper = new EPSPublicationHelper();
                    epsHelper.Publish(clientContext, issuanceDocuments, supplementalMailingListFileName, listItem);

                    //Change document permission
                    helper.UpdatePermissionBaseOnFormStatus(clientContext, ListItemID, FormStatus, FormType);

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //send email
                    Email emailHelper = new Email();

                    emailHelper.SendEmail(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                        currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                    //create history list
                    string message = "Workflow Item publication to eLibrary Data Entry initiated.";
                    //if (!string.IsNullOrEmpty(tbComment.Text))
                    //{
                    //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                    //}

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

        protected void btnDeleteConfirm_Click(object sender, EventArgs e)
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
                    //if (!string.IsNullOrEmpty(tbComment.Text))
                    //{
                    //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                    //}
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

                        //TODO: send email

                        //Create list history
                        string message = "Workflow Item recalled.";
                        //if (!string.IsNullOrEmpty(tbComment.Text))
                        //{
                        //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                        //}
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

        protected void btnOSECTakeOwnership_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.OSECTakeOwnerShip;
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

                    Email emailHelper = new Email();

                    emailHelper.SendEmail(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                        currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                    //Create list history
                    string message = "OSEC took ownership of Workflow Item.";
                    //if (!string.IsNullOrEmpty(tbComment.Text))
                    //{
                    //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                    //}
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

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //Create list history
                    string message = "Workflow Item edited.";
                    //if (!string.IsNullOrEmpty(tbComment.Text))
                    //{
                    //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                    //}
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

                    Email emailHelper = new Email();

                    emailHelper.SendEmail(clientContext, listItem, action, currentStatusBeforeWFRun, previousStatusBeforeWFRun,
                        currentUser, Request.Url.ToString(), hdnWorkflowInitiator, hdnDocumentOwner, hdnNotificationRecipient, tbComment.Text);

                    //Create list history
                    string message = "Workflow Item Re-Opened.";
                    //if (!string.IsNullOrEmpty(tbComment.Text))
                    //{
                    //    message = message + "</br>Comment: " + tbComment.Text.Trim();
                    //}
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

        protected void btnGenerateMailingList_Click(object sender, EventArgs e)
        {
            using (var clientContext = helper.getElevatedClientContext(Context, Request))
            {
                FOLAMailingList folaMailingList = new FOLAMailingList();
                int numberOfFOLAAddress = folaMailingList.GenerateFOLAMailingExcelFile(clientContext, tbDocketNumber.Text.Trim(), ListItemID);

                //save number of fola mailing list address
                ListItem listItem = helper.GetPiwListItemById(clientContext, ListItemID, false);
                string PrintReqStatus = Constants.PrintReq_FormStatus_PrintReqGenerated;

                bool printReqGenerated = helper.InitiatePrintReqForm(clientContext, listItem, numberOfFOLAAddress, PrintReqStatus);

                //create first history list
                if (printReqGenerated)
                {
                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();
                    //add history list for the main form 
                    helper.CreatePIWListHistory(clientContext, ListItemID, "Print Requisition Generated.",
                            FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    //Add history list for the print req form
                    if (helper.getHistoryListByPIWListID(clientContext, ListItemID, Constants.PIWListHistory_FormTypeOption_PrintReq).Count == 0)
                    {
                        string message = "Print Requisition Generated.";
                        helper.CreatePIWListHistory(clientContext, ListItemID, message,
                            PrintReqStatus, Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                    }

                }

                helper.RefreshPage(Request, Response);
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
                    helper.GenerateCitation(clientContext, ddDocumentCategory, tbCitationNumber,
                        ddAvailableCitationNumbers, false);

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
                        int documentCategoryNumber = helper.getDocumentCategoryNumber(ddDocumentCategory.SelectedValue, false);
                        CitationNumber citationNumberHelper = new CitationNumber(documentCategoryNumber, DateTime.Now);

                        if (citationNumberHelper.Save(clientContext, ListItemID, tbCitationNumber.Text.Trim(),
                            ref errorMessage, cbOverrideCitationNumber.Checked))
                        {
                            var listItem = helper.SetCitationNumberFieldInPIWList(clientContext, ListItemID, tbCitationNumber.Text.Trim());

                            try
                            {
                                //need to re-populate the modified date becuase the list item is changed
                                PopulateFormStatusAndProperties(clientContext, listItem);

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
                                helper.CreatePIWListHistory(clientContext, ListItemID, "Citation number assigned: " + tbCitationNumber.Text.Trim(), FormStatus,
                                    Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                                //add citation number into the documents - must be the last action because it can throw exceptioniled if the docs is opened in MS-Word
                                //it will not able to finish all actions
                                var documentURLs = PublicDocumentURLsFromViewState.Split(new string[] { Constants.DocumentURLsSeparator },
                                        StringSplitOptions.RemoveEmptyEntries);

                                //Add citation number to the first public documents
                                var fileName = helper.getFileNameFromURL(documentURLs[0]);
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
                        PopulateFormStatusAndProperties(clientContext, listItem);

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
                        //remove citation numebr from first public document
                        var fileName = helper.getFileNameFromURL(documentURLs[0]);
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
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void ddAvailableCitationNumbers_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbCitationNumber.Text = ddAvailableCitationNumbers.SelectedValue;
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
                            helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList, out publicDocumentURLs, out cEiiDocumentUrLs, out privilegedDocumentURLs);
                            SaveDocumentURLsToPageProperty(publicDocumentURLs, cEiiDocumentUrLs, privilegedDocumentURLs);

                            //get current user
                            User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                            clientContext.Load(currentUser);
                            clientContext.ExecuteQuery();

                            //history list
                            helper.CreatePIWListHistory(clientContext, ListItemID, string.Format("Document file {0} removed.", removedFileName),
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
                            helper.CreatePIWListHistory(clientContext, ListItemID, string.Format("Supplemental Mailing List file {0} removed.",
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

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (fileUpload.HasFiles)
                {
                    var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

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
            StandardFormWorkflow wf = new StandardFormWorkflow();
            FormStatus = wf.Execute(PreviousFormStatus, FormStatus, action,
                isRequiredOSECVerificationStep(ddDocumentCategory.SelectedValue), ddProgramOfficeWorkflowInitiator.SelectedValue, ddDocumentCategory.SelectedValue);
            PreviousFormStatus = currentFormStatus;

            UpdateFormDataToList(clientContext, listItem, action);

            returnedListItem = listItem;
            return true;
        }

        private void UpdateFormDataToList(ClientContext clientContext, ListItem listItem, enumAction action)
        {
            string errorMessage = "UpdateFormDataToList method: Unknown Status and Previous status combination. Status:{0}, Previous Status: {1}";
            switch (FormStatus)//this is the next status after action is performed
            {
                case Constants.PIWList_FormStatus_Pending:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Pending)
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
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus, action,
                            tbComment.Text.Trim(), CurrentUserLogInName);
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
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_OSECVerification)//reject from osecverification
                    {
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus, action,
                            tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_PrePublication)//reject from prepublication
                    {
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus, action,
                            tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_Pending) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Recalled) ||
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
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_OSECVerification) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_PrePublication) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,action,
                            tbComment.Text.Trim(),CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Deleted:
                    //delete item, need to set status and remove citation number if there is assigned one
                    helper.SaveDeleteInfoAndStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                        tbComment.Text.Trim(),CurrentUserLogInName);
                    helper.ReleaseCitationNumberForDeletedListItem(clientContext, ListItemID);
                    break;
                case Constants.PIWList_FormStatus_OSECVerification:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_Edited) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Pending) ||//this is scenario of OSEC submit Notice, bypass osec take ownership
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Rejected) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Recalled))
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Submitted)
                    {
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus, action,
                            tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_PrePublication:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_Edited) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Pending) ||//this is scenario of OSEC submit Not Notice, bypass osec take ownership
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Rejected) ||//OSEC submits Notices
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Recalled))//OSEC submits Notices
                    {
                        SaveMainPanelAndStatus(clientContext, listItem, action);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_OSECVerification)//come from OSEC Verification
                    {
                        //SaveOsecVerificationInfoAndStatus(clientContext, listItem, action);
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus, action,
                            tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Submitted)//come from Submitted
                    {
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus, action,
                            tbComment.Text.Trim(), CurrentUserLogInName);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_PublishInitiated)//REOPEN- come from Publish Initiated
                    {
                        helper.SaveReOpenInfoAndStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus,
                            tbComment.Text.Trim(),CurrentUserLogInName);
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
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_PrePublication)
                    {
                        //SavePrePublicationInfoAndStatus(clientContext, listItem, action);
                        helper.SaveFormStatusAndComment(clientContext, listItem, FormStatus, PreviousFormStatus, action,
                            tbComment.Text.Trim(), CurrentUserLogInName);
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
                            tbComment.Text.Trim(),CurrentUserLogInName);
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
                        helper.SaveLegalResourcesAndReviewAndStatus(clientContext, listItem, FormStatus, PreviousFormStatus,
                            tbLegalResourcesReviewCompletionDate.Text, tbLegalResourcesReviewNote.Text);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                default:
                    throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));

            }
        }

        //private void SaveReOpenInfoAndStatusAndComment(ClientContext clientContext, ListItem listItem)
        //{
        //    var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

        //    //clear accession number
        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_AccessionNumber]] = string.Empty;

        //    listItem.Update();
        //    clientContext.ExecuteQuery();
        //}

        //private void SavePrePublicationInfoAndStatus(ClientContext clientContext, ListItem listItem, enumAction action)
        //{
        //    var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

        //    //listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrePublicationReviewAction]] = action.ToString();

        //    //if (action == enumAction.Reject)
        //    //{
        //    //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] = tbPrePublicationComment.Text.Trim();
        //    //}
        //    //else
        //    //{
        //    //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrePublicationReviewComment]] = tbPrePublicationComment.Text.Trim();
        //    //}

        //    listItem.Update();
        //    clientContext.ExecuteQuery();
        //}

        //private void SaveOsecVerificationInfoAndStatus(ClientContext clientContext, ListItem listItem, enumAction action)
        //{
        //    var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

        //    //listItem[piwListInternalColumnNames[Constants.PIWList_colName_OSECVerificationAction]] = action.ToString();

        //    //if (action == enumAction.Reject)
        //    //{
        //    //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] = tbOSECVerificationComment.Text.Trim();

        //    //}
        //    //else
        //    //{
        //    //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_OSECVerificationComment]] = tbOSECVerificationComment.Text.Trim();
        //    //}


        //    listItem.Update();
        //    clientContext.ExecuteQuery();
        //}

        //private void SaveFormStatusAndComment(ClientContext clientContext, ListItem listItem, enumAction action)
        //{
        //    var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

        //    string comment = tbComment.Text.Trim();
        //    //recall/reject comment
        //    if ((action == enumAction.Recall) || (action == enumAction.Reject))
        //    {
                
        //        if (!string.IsNullOrEmpty(comment))
        //        {
        //            //recall / reject comment field-single line
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

        //    //comment
        //    helper.SetCommentURLHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, comment);

        //    listItem.Update();
        //    clientContext.ExecuteQuery();
        //}

        //private void ClearOSECActionsAndCommentsBeforeReSubmit(ClientContext clientContext, ListItem listItem)
        //{
        //    var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrePublicationReviewAction]] = string.Empty;
        //    listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrePublicationReviewComment]] = string.Empty;

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

            //Save Data

            //Save IsActive
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_IsActive]] = true;

            //Save Docket Number
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocketNumber]] = helper.RemoveDuplicateDocket(tbDocketNumber.Text.Trim());

            //IsCNF
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_IsCNF]] = cbIsCNF.Checked;

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
            if (!string.IsNullOrEmpty(tbComment.Text.Trim()))
            {
                helper.SetCommentURLHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, tbComment.Text.Trim());

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
            helper.CheckDocketNumber(tbDocketNumber.Text.Trim(), ref errorMessage, cbIsCNF.Checked, cbDocketValidationByPass.Checked);

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

        public void PopulateFormStatusAndProperties(ClientContext clientContext, ListItem listItem)
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

                //Is CNF
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_IsCNF]] != null)
                {
                    cbIsCNF.Checked = bool.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_IsCNF]].ToString());
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
                        (FieldUserValue)listItem[piwListInteralColumnNames[Constants.PIWList_colName_WorkflowInitiator]];
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

                //Comment
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]] != null)
                {
                    lbCommentValue.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]].ToString();
                }


                //Citation Number
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_CitationNumber]] != null)
                {
                    tbCitationNumber.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_CitationNumber]].ToString();
                }

                //Print Requisition
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]] != null)
                {
                    tbPrintDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]].ToString()).ToShortDateString();
                }

                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] != null)
                {
                    tbMailDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]].ToString()).ToShortDateString();
                }

                hyperlinkPrintReq.NavigateUrl = helper.getEditFormURL(Constants.PIWList_FormType_PrintReqForm, ListItemID, Request, string.Empty);

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

        private bool isRequiredOSECVerificationStep(string documentCategory)
        {
            return (documentCategory.Equals(Constants.PIWList_DocCat_Notice) ||
                     documentCategory.Equals(Constants.PIWList_DocCat_NoticeErrata));
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
            btnOSECTakeOwnership1.Visible = btnOSECTakeOwnership.Visible;
            btnRecall1.Visible = btnRecall.Visible;
            btnEdit1.Visible = btnEdit.Visible;
            btnAccept1.Visible = btnAccept.Visible;
            btnReject1.Visible = btnReject.Visible;
            btnInitiatePublication1.Visible = btnInitiatePublication.Visible;
            btnDelete1.Visible = btnDelete.Visible;
            btnReopen1.Visible = btnReopen.Visible;
            btnGenerateMailingList1.Visible = btnGenerateMailingList.Visible;
        }
        #endregion

        #region Visibility
        public void ControlsVisiblitilyBasedOnStatus(ClientContext clientContext, string previousFormStatus, string formStatus, ListItem listItem)
        {
            var piwlistInternalColumnName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            bool isCurrentUserOSEC = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                            new string[] { Constants.Grp_OSEC, Constants.Grp_SecReview });
            bool isCurrentUserLegalResouceTeam = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                            new string[] { Constants.Grp_PIWLegalResourcesReview });

            //document category
            var documentCategory = string.Empty;
            if (listItem[piwlistInternalColumnName[Constants.PIWList_colName_DocumentCategory]] != null)
            {
                documentCategory = listItem[piwlistInternalColumnName[Constants.PIWList_colName_DocumentCategory]].ToString();
            }
            bool isRequireOSECVerification = isRequiredOSECVerificationStep(documentCategory);

            //number of fola mailing list and supp mailing list address
            int numberOfFOLAMailingListAddress = 0;
            int numberOfSuppMailingListAddress = 0;
            if (listItem[piwlistInternalColumnName[Constants.PIWList_colName_NumberOfFOLAMailingListAddress]] != null)
            {
                numberOfFOLAMailingListAddress = int.Parse(listItem[piwlistInternalColumnName[Constants.PIWList_colName_NumberOfFOLAMailingListAddress]].ToString());
            }

            if (listItem[piwlistInternalColumnName[Constants.PIWList_colName_NumberOfSupplementalMailingListAddress]] != null)
            {
                numberOfSuppMailingListAddress = int.Parse(listItem[piwlistInternalColumnName[Constants.PIWList_colName_NumberOfSupplementalMailingListAddress]].ToString());
            }

            switch (formStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Recalled:
                case Constants.PIWList_FormStatus_Rejected:
                    //submit section    
                    EnableMainPanel(true, formStatus, true);
                    lbMainMessage.Visible = false;

                    //OSEC section
                    fieldsetOSECVerification.Visible = false;
                    fieldsetPrePublication.Visible = false;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID, new string[]{Constants.Grp_PIWUsers,
                        Constants.Grp_OSEC,Constants.Grp_SecReview});


                    btnSubmit.Visible = btnSave.Visible;

                    btnEdit.Visible = false;

                    btnAccept.Visible = false;

                    btnReject.Visible = false;

                    btnOSECTakeOwnership.Visible = false;

                    btnRecall.Visible = false;

                    btnInitiatePublication.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    btnReopen.Visible = false;
                    btnGenerateMailingList.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    //submit section   
                    EnableMainPanel(false, formStatus, false);
                    lbMainMessage.Visible = false;

                    //OSEC section
                    fieldsetOSECVerification.Visible = false;
                    fieldsetPrePublication.Visible = false;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //Button
                    btnSave.Visible = false;
                    btnSubmit.Visible = btnSave.Visible;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnOSECTakeOwnership.Visible = isCurrentUserOSEC;
                    btnRecall.Visible = true;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;
                    btnGenerateMailingList.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_Edited:
                    //submitter
                    EnableMainPanel(true, formStatus, isCurrentUserOSEC);
                    lbMainMessage.Visible = false;

                    //fieldsetRecall.Visible = false;

                    //OSEC section
                    if (previousFormStatus.Equals(Constants.PIWList_FormStatus_OSECVerification))
                    {
                        fieldsetOSECVerification.Visible = true;
                    }
                    else if (previousFormStatus.Equals(Constants.PIWList_FormStatus_PrePublication) ||
                        previousFormStatus.Equals(Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        if (isRequireOSECVerification)
                        {
                            fieldsetOSECVerification.Visible = true;

                        }
                        fieldsetPrePublication.Visible = true;
                        EnableCitationNumberControls(false, false);
                    }



                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;


                    //Button
                    btnSave.Visible = isCurrentUserOSEC;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;

                    //if edit is initiated from osecverification and prepub, allow to accept and reject to OSEC group
                    if ((previousFormStatus == Constants.PIWList_FormStatus_OSECVerification) ||
                        (previousFormStatus == Constants.PIWList_FormStatus_PrePublication))
                    {
                        btnAccept.Visible = btnSave.Visible;
                    }
                    else
                    {
                        btnAccept.Visible = false;
                    }

                    btnReject.Visible = btnAccept.Visible;

                    btnOSECTakeOwnership.Visible = false;
                    btnRecall.Visible = false;

                    if (previousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing)
                    {
                        btnInitiatePublication.Visible = btnSave.Visible;
                    }
                    else
                    {
                        btnInitiatePublication.Visible = false;
                    }


                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;
                    btnGenerateMailingList.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_OSECVerification:
                    //submitter
                    EnableMainPanel(false, formStatus, isCurrentUserOSEC);
                    lbMainMessage.Visible = false;

                    //OSEC section
                    //osec verification
                    fieldsetOSECVerification.Visible = true;
                    //tbOSECVerificationComment.Enabled = true;

                    //prepublication
                    fieldsetPrePublication.Visible = false;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //Buttons
                    btnSave.Visible = false;
                    btnSubmit.Visible = btnSave.Visible;
                    btnEdit.Visible = isCurrentUserOSEC;
                    btnAccept.Visible = btnEdit.Visible;
                    btnReject.Visible = btnEdit.Visible;
                    btnOSECTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;
                    btnGenerateMailingList.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_PrePublication:
                    //submitter
                    EnableMainPanel(false, formStatus, isCurrentUserOSEC);
                    lbMainMessage.Visible = false;

                    //OSEC verification
                    if (isRequireOSECVerification)
                    {
                        fieldsetOSECVerification.Visible = true;
                    }

                    //PrePublication
                    fieldsetPrePublication.Visible = true;
                    //EnablePrePublicationControls(true);
                    InitiallyEnableCitationNumberControls(clientContext, listItem);

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //button
                    btnSave.Visible = false;
                    btnSubmit.Visible = btnSave.Visible;

                    if (isCurrentUserOSEC)//OSEC user
                    {
                        btnEdit.Visible = true;
                        //don't need to worry about citation button bc it is handles above in InitiallyEnableCitationNumberControls()
                    }
                    else
                    {
                        btnEdit.Visible = false;
                        //this will prevent user who is not osec generate/remove or accept citation
                        EnableCitationNumberControls(false, false);
                    }

                    btnAccept.Visible = btnEdit.Visible;
                    btnReject.Visible = btnEdit.Visible;

                    btnOSECTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;
                    btnGenerateMailingList.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    //submitter
                    EnableMainPanel(false, formStatus, isCurrentUserOSEC);
                    lbMainMessage.Visible = false;

                    //OSEC section
                    //OSEC verification
                    if (isRequireOSECVerification)
                    {
                        fieldsetOSECVerification.Visible = true;
                    }

                    //PrePublication
                    fieldsetPrePublication.Visible = true;
                    InitiallyEnableCitationNumberControls(clientContext, listItem);

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = false;
                    btnSubmit.Visible = btnSave.Visible;
                    if (isCurrentUserOSEC)//OSEC user
                    {
                        btnEdit.Visible = true;
                        //don't need to worry about citation button bc it is handles above in InitiallyEnableCitationNumberControls()
                    }
                    else
                    {
                        btnEdit.Visible = false;
                        //this will prevent user who is not osec generate/remove or accept citation
                        EnableCitationNumberControls(false, false);
                    }

                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnOSECTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = btnEdit.Visible;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = false;
                    btnGenerateMailingList.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    //submitter
                    EnableMainPanel(false, formStatus, false);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "Publication has been initiated for this issuance.";

                    //OSEC section
                    //OSEC verification
                    if (isRequireOSECVerification)
                    {
                        fieldsetOSECVerification.Visible = true;
                        //tbOSECVerificationComment.Enabled = false;
                    }

                    //PrePublication
                    fieldsetPrePublication.Visible = true;
                    //EnablePrePublicationControls(false);
                    EnableCitationNumberControls(false, false);

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = true;

                    //buttons
                    btnSave.Visible = isCurrentUserLegalResouceTeam;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnOSECTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = false;
                    btnReopen.Visible = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                        new string[] { Constants.Grp_PIWAdmin, Constants.Grp_PIWSystemAdmin });
                    btnGenerateMailingList.Visible = false;
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    EnableMainPanel(false, formStatus, false);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "This issuance is available in eLibrary.";

                    //OSEC section
                    //OSEC verification
                    if (isRequireOSECVerification)
                    {
                        fieldsetOSECVerification.Visible = true;
                        //tbOSECVerificationComment.Enabled = false;
                    }

                    //PrePublication
                    fieldsetPrePublication.Visible = true;
                    //EnablePrePublicationControls(false);
                    EnableCitationNumberControls(false, false);

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = (numberOfFOLAMailingListAddress + numberOfSuppMailingListAddress) > 0;
                    fieldsetLegalResourcesReview.Visible = true;

                    //buttons
                    btnSave.Visible = isCurrentUserLegalResouceTeam; ;
                    btnSubmit.Visible = false;
                    btnEdit.Visible = false;
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnOSECTakeOwnership.Visible = false;
                    btnRecall.Visible = false;
                    btnInitiatePublication.Visible = false;
                    btnDelete.Visible = false;
                    btnReopen.Visible = false;
                    btnGenerateMailingList.Visible = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                        new string[] { Constants.Grp_PIWAdmin, Constants.Grp_PIWSystemAdmin });
                    break;
                case Constants.PIWList_FormStatus_ReOpen:
                    throw new Exception("Not Implemented");
                case Constants.PIWList_FormStatus_Deleted:
                    //this status is only viewable by admin
                    EnableMainPanel(false, formStatus, false);
                    if (isRequireOSECVerification)
                    {
                        fieldsetOSECVerification.Visible = true;
                        //tbOSECVerificationComment.Enabled = false;
                    }

                    //PrePublication
                    fieldsetPrePublication.Visible = true;
                    //EnablePrePublicationControls(false);
                    EnableCitationNumberControls(false, false);

                    break;
                default:
                    throw new Exception("ControlsVisibilityBasedOnStatus method - UnKnown Form Status: " + formStatus);

            }

            //set the top buttons
            SetVisiblePropertyInTopButtons();
        }



        private void EnableMainPanel(bool enabled, string FormStatus, bool canEditUploadedDocument)
        {
            EnableFileUploadComponent(enabled, canEditUploadedDocument);
            tbDocketNumber.Enabled = enabled;
            cbIsCNF.Enabled = enabled;
            cbIsNonDocket.Enabled = enabled;
            tbAlternateIdentifier.Enabled = enabled;
            tbDescription.Enabled = enabled;
            tbInstruction.Enabled = enabled;
            cbFederalRegister.Enabled = enabled;

            //only allow document category to be changed if Status is not Edited --> Pending, recalled or rejected or reOpen
            if (FormStatus.Equals(Constants.PIWList_FormStatus_Edited))
            {
                ddDocumentCategory.Enabled = false;
            }
            else
            {
                ddDocumentCategory.Enabled = enabled;
            }



            ddProgramOfficeWorkflowInitiator.Enabled = enabled;
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

            //disable/enable the Remove button
            //the link always be enable so user can open document
            foreach (RepeaterItem row in rpDocumentList.Items)
            {
                var btnRemoveDocument = (LinkButton)row.FindControl("btnRemoveDocument");
                if (btnRemoveDocument != null) btnRemoveDocument.Visible = enabled;

                var hplEdit = (HyperLink)row.FindControl("hplEdit");
                if (hplEdit != null)
                {
                    hplEdit.Visible = canEditUploadedDocument;
                }
            }

            foreach (RepeaterItem row in rpSupplementalMailingListDocumentList.Items)
            {
                var btnRemoveDocument = (LinkButton)row.FindControl("btnRemoveDocument");
                if (btnRemoveDocument != null)
                {
                    btnRemoveDocument.Visible = enabled;
                }


                var hplEdit = (HyperLink)row.FindControl("hplEdit");
                if (hplEdit != null) hplEdit.Visible = canEditUploadedDocument;
            }
        }


        /// enabled or disable other controls from the textbox enabled property
        /// For example: when textbox is editable,  then the accept button should be clickable to assign the citation number
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