using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = Microsoft.SharePoint.Client.ListItem;

namespace PIW_SPAppWeb.Pages
{
    public partial class EditPrintReqForm : System.Web.UI.Page
    {
        #region variables and properties

        
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

        
        static SharePointHelper helper = null;

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

        public bool PrintJobCompleted
        {
            get
            {
                return ViewState[Constants.PrintJobCompletedKey] != null && bool.Parse(ViewState[Constants.PrintJobCompletedKey].ToString());
            }
            set
            {
                ViewState.Add(Constants.PrintJobCompletedKey, value);
            }
        }

        public string PrintJobCompletedDate
        {
            get
            {
                return ViewState[Constants.PrintJobCompletedDateKey] != null ? DateTime.Parse(ViewState[Constants.PrintJobCompletedDateKey].ToString()).ToShortDateString() : string.Empty;
            }
            set
            {
                ViewState.Add(Constants.PrintJobCompletedDateKey, value);
            }
        }

        public bool MailJobCompleted
        {
            get
            {
                return ViewState[Constants.MailJobCompletedKey] != null && bool.Parse(ViewState[Constants.MailJobCompletedKey].ToString());
            }
            set
            {
                ViewState.Add(Constants.MailJobCompletedKey, value);
            }
        }

        public string MailJobCompletedDate
        {
            get
            {
                return ViewState[Constants.MailJobCompletedDateKey] != null ? DateTime.Parse(ViewState[Constants.MailJobCompletedDateKey].ToString()).ToShortDateString():string.Empty;
            }
            set
            {
                ViewState.Add(Constants.MailJobCompletedDateKey, value);
            }
        }

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

        #endregion

        #region Data
        private bool SaveData(ClientContext clientContext, enumAction action,ref ListItem returnedListItem)
        {
            ListItem listItem = helper.GetPiwListItemById(clientContext, ListItemID, false);

            if (helper.CheckIfListItemChanged(clientContext, listItem, DateTime.Parse(ModifiedDateTime)))
            {
                lbMainMessage.Text = "Form has been modified by other User - Please Refresh by highlighting URL and hitting the ENTER key";
                lbMainMessage.Visible = true;
                return false;
            }

            //update form data to list

            //get next form status
            var currentFormStatus = FormStatus;
            var wf = new PrintReqFormWorkflow();
            FormStatus = wf.Execute(FormStatus, action);
            PreviousFormStatus = currentFormStatus;

            UpdateFormDataToList(clientContext, listItem);

            returnedListItem = listItem;
            return true;
        }

        private void UpdateFormDataToList(ClientContext clientContext, ListItem listItem)
        {
            const string errorMessage = "UpdateFormDataToList method: Unknown Status and Previous status combination. Status:{0}, Previous Status: {1}";
            switch (FormStatus) //this is the next status after action is performed
            {
                case Constants.PIWList_FormStatus_Pending:
                    //do nothing - this scenario never happens
                    break;
                case Constants.PIWList_FormStatus_Rejected:
                    if (FormStatus.Equals(PreviousFormStatus))//save action
                    {
                        SaveMainPanelAndStatus(clientContext,listItem);
                    }
                    else if (PreviousFormStatus.Equals(Constants.PIWList_FormStatus_Submitted))//reject
                    {
                        SavePrintReqFormStatusAndComment(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    if (PreviousFormStatus.Equals(Constants.PIWList_FormStatus_Rejected))//submit
                    {
                        SavePrintReqFormStatusAndComment(clientContext,listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PrintReqAccepted:
                case Constants.PIWList_FormStatus_PrintJobCompleted:
                case Constants.PIWList_FormStatus_MailJobCompleted:
                case Constants.PIWList_FormStatus_PrintReqCompleted:
                    SaveMainPanelAndStatus(clientContext, listItem);
                    break;
            }
        }

        public void SavePrintReqFormStatusAndComment(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqPreviousStatus]] = PreviousFormStatus;

            //comment
            if (!string.IsNullOrEmpty(tbComment.Text.Trim()))
            {
                helper.SetCommentHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, tbComment.Text.Trim(), Constants.PIWList_FormType_PrintReqForm);
            }

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SaveMainPanelAndStatus(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            //number of pages
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfPublicPages]] = tbNumberofPages.Text.Trim();

            //number of copies
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]] = tbNumberofCopies.Text.Trim();

            //Print Job Completed
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleted]] = cbPrintJobCompleted.Checked;

            //Print Job Completed Date
            if (!string.IsNullOrEmpty(tbPrintJobCompletedDate.Text))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]] = tbPrintJobCompletedDate.Text.Trim();
            }

            //Mail Job Completed
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleted]] = cbMailJobCompleted.Checked;

            //Mail Job Completed Date
            if (!string.IsNullOrEmpty(tbMailJobCompletedDate.Text))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] = tbMailJobCompletedDate.Text.Trim();
            }

            //Print Req Comment
            if (!string.IsNullOrEmpty(tbComment.Text.Trim()))
            {
                helper.SetCommentHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, tbComment.Text.Trim(), Constants.PIWList_FormType_PrintReqForm);
            }

            //Status
            if (!string.IsNullOrEmpty(FormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqStatus]] = FormStatus;
            }

            //Previous Status
            if (!string.IsNullOrEmpty(PreviousFormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqPreviousStatus]] = PreviousFormStatus;
            }

            //execute query
            listItem.Update();
            clientContext.ExecuteQuery();

            
            
        }

        #endregion

        #region Utils
        private void DisplayListItemInForm(ClientContext clientContext, ListItem listItem)
        {
            if (listItem != null)
            {
                var piwListInteralColumnNames = helper.getInternalColumnNamesFromCache(clientContext,
                    Constants.PIWListName);

                var formType = listItem[piwListInteralColumnNames[Constants.PIWList_colName_FormType]].ToString();
                //Link to PIW Form
                hplPIWFormLink.NavigateUrl = helper.getEditFormURL(formType, ListItemID, Request, string.Empty);

                //Docket
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]] != null)
                {
                    tbDocketNumber.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]].ToString();
                }

                //Date Requested
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqDateRequested]] != null)
                {
                    tbDateRequested.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqDateRequested]].ToString()).ToShortDateString();
                }

                //Date Required
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqDateRequired]] != null)
                {
                    tbDateRequired.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqDateRequired]].ToString()).ToShortDateString();
                }

                //Authorizing Office - map to Program Office Initiator
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null)
                {
                    tbAuthorizingOffice.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString();
                }

                //Number of Pages
                int numberOfPrintPages = 0;
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_NumberOfPublicPages]] != null)
                {
                    numberOfPrintPages = int.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_NumberOfPublicPages]].ToString());
                    tbNumberofPages.Text = numberOfPrintPages.ToString();

                }

                //Number of Copies - this should not be calculated field, user can change --> has it own field
                int numberOfCopies = 0;
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]] != null)
                {
                    numberOfCopies = int.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]].ToString());
                    tbNumberofCopies.Text = numberOfCopies.ToString();
                }

                //Total Print Pages (calculated field)
                tbTotalPrintPages.Text = (numberOfCopies * numberOfPrintPages).ToString();

                //Comment
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqComment]] != null)
                {
                    lbCommentValue.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqComment]].ToString();
                }

                //Print Priority
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintPriority]] != null)
                {
                    tbPriority.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintPriority]].ToString();
                }

                //Print Job Completed
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleted]] != null)
                {
                    cbPrintJobCompleted.Checked = bool.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleted]].ToString());
                }


                //Print Job Completed Date
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]] != null)
                {
                    tbPrintJobCompletedDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]].ToString()).ToShortDateString();
                }

                //Mail Job Completed
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleted]] != null)
                {
                    cbMailJobCompleted.Checked = bool.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleted]].ToString());
                }

                //Mail Job Completed Date
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] != null)
                {
                    tbMailJobCompletedDate.Text = DateTime.Parse(listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]].ToString()).ToShortDateString();
                }

            }
        }

        public void PopulateFormProperties(ClientContext clientContext, ListItem listItem)
        {
            var internalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            //Modified Date
            if (listItem[internalColumnNames[Constants.PIWList_colName_Modified]] != null)
            {
                ModifiedDateTime = listItem[internalColumnNames[Constants.PIWList_colName_Modified]].ToString();
            }

            //Status
            if (listItem[internalColumnNames[Constants.PIWList_colName_PrintReqStatus]] != null)
            {
                FormStatus = listItem[internalColumnNames[Constants.PIWList_colName_PrintReqStatus]].ToString();
            }

            if (listItem[internalColumnNames[Constants.PIWList_colName_PrintReqPreviousStatus]] != null)
            {
                PreviousFormStatus = listItem[internalColumnNames[Constants.PIWList_colName_PrintReqPreviousStatus]].ToString();
            }

            if (listItem[internalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleted]] != null)
            {
                PrintJobCompleted = bool.Parse(listItem[internalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleted]].ToString());
            }

            if (listItem[internalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]] != null)
            {
                PrintJobCompletedDate = DateTime.Parse(listItem[internalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]].ToString()).ToShortDateString();
            }

            if (listItem[internalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleted]] != null)
            {
                MailJobCompleted = bool.Parse(listItem[internalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleted]].ToString());
            }

            if (listItem[internalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] != null)
            {
                MailJobCompletedDate = DateTime.Parse(listItem[internalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]].ToString()).ToShortDateString();
            }

        }

        public void PopulateFOLAAndSupplementalMailingListURL(ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();

            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, Constants.PIWDocuments_DocumentLibraryName, ListItemID);
            var folaMailingList = helper.getDocumentsByDocType(clientContext, uploadSubFolderURL,
                Constants.PIWDocuments_DocTypeOption_FOLAServiceMailingList);
            if (folaMailingList.Count > 0)
            {
                hplFOLAMailingList.NavigateUrl = uploadSubFolderURL + "/" + folaMailingList[0].Name;
            }
            else
            {
                hplFOLAMailingList.Visible = false;
            }


            var supplementalMailingList = helper.getDocumentsByDocType(clientContext, uploadSubFolderURL,
                Constants.PIWDocuments_DocTypeOption_SupplementalMailingList);
            if (supplementalMailingList.Count > 0)
            {
                hplSupplementalMailingList.NavigateUrl = uploadSubFolderURL + "/" + supplementalMailingList[0].Name;
            }
            else
            {
                hplSupplementalMailingList.Visible = false;
            }


        }

        public void ControlsVisiblitilyBasedOnStatus(string Status)
        {
            switch (Status)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Rejected:
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSubmit.Visible = true;

                    btnSave.Visible = true;
                    btnComplete.Visible = false;
                    tbNumberofCopies.Enabled = true;
                    tbNumberofPages.Enabled = true;

                    fieldsetTasks.Visible = false;
                    cbMailJobCompleted.Enabled = false;
                    tbMailJobCompletedDate.Enabled = cbMailJobCompleted.Checked;

                    cbPrintJobCompleted.Enabled = false;
                    tbPrintJobCompletedDate.Enabled = cbPrintJobCompleted.Checked;
                    break;
                case Constants.PIWList_FormStatus_Submitted:

                    btnAccept.Visible = true;
                    btnReject.Visible = true;
                    btnSubmit.Visible = false;
                    btnSave.Visible = false;
                    btnComplete.Visible = false;
                    tbNumberofCopies.Enabled = false;
                    tbNumberofPages.Enabled = false;

                    fieldsetTasks.Visible = false;
                    cbMailJobCompleted.Enabled = false;
                    tbMailJobCompletedDate.Enabled = cbMailJobCompleted.Checked;

                    cbPrintJobCompleted.Enabled = false;
                    tbPrintJobCompletedDate.Enabled = cbPrintJobCompleted.Checked;

                    break;
                case Constants.PIWList_FormStatus_PrintReqAccepted:
                case Constants.PIWList_FormStatus_PrintJobCompleted:
                case Constants.PIWList_FormStatus_MailJobCompleted:

                    //buttons
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSave.Visible = true;
                    btnComplete.Visible = true;
                    btnSubmit.Visible = false;

                    tbNumberofCopies.Enabled = true;
                    tbNumberofPages.Enabled = true;

                    fieldsetTasks.Visible = true;
                    cbMailJobCompleted.Enabled = true;
                    tbMailJobCompletedDate.Enabled = cbMailJobCompleted.Checked;

                    cbPrintJobCompleted.Enabled = true;
                    tbPrintJobCompletedDate.Enabled = cbPrintJobCompleted.Checked;

                    break;
                case Constants.PIWList_FormStatus_PrintReqCompleted:

                    //disable all controls
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSave.Visible = false;
                    btnComplete.Visible = false;
                    btnSubmit.Visible = false;

                    tbNumberofCopies.Enabled = false;
                    tbNumberofPages.Enabled = false;

                    fieldsetTasks.Visible = true;
                    tbMailJobCompletedDate.Enabled = false;
                    tbPrintJobCompletedDate.Enabled = false;

                    cbMailJobCompleted.Enabled = false;
                    cbPrintJobCompleted.Enabled = false;
                    break;
            }

        }
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

                if (!Page.IsPostBack)
                {
                    if (!string.IsNullOrEmpty(ListItemID))
                    {
                        using (var clientContext = helper.getElevatedClientContext(Context, Request))
                        {
                            string publicDocumentURLs;
                            string cEiiDocumentUrLs;
                            string privilegedDocumentURLs;
                            helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList,
                                out publicDocumentURLs, out cEiiDocumentUrLs, out privilegedDocumentURLs);

                            PopulateFOLAAndSupplementalMailingListURL(clientContext);

                            //var isCurrentUserAdmin = helper.IsCurrentUserMemberOfGroup(clientContext, Constants.Grp_PIWAdmin);
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
                                PopulateFormProperties(clientContext, listItem);
                                DisplayListItemInForm(clientContext, listItem);
                                helper.PopulateHistoryList(clientContext, ListItemID, rpHistoryList, Constants.PIWListHistory_FormTypeOption_PrintReq);

                                //display form visiblility based on form status
                                ControlsVisiblitilyBasedOnStatus(FormStatus);



                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, Page.Request.Url.OriginalString);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }

        }

        protected void cbPrintJobCompleted_CheckedChanged(object sender, EventArgs e)
        {
            tbPrintJobCompletedDate.Enabled = cbPrintJobCompleted.Checked;
            if (cbPrintJobCompleted.Checked)
            {
                tbPrintJobCompletedDate.Text = DateTime.Today.ToShortDateString();
            }
            else
            {
                tbPrintJobCompletedDate.Text = string.Empty;
            }
        }

        protected void cbMailJobCompleted_CheckedChanged(object sender, EventArgs e)
        {
            tbMailJobCompletedDate.Enabled = cbMailJobCompleted.Checked;
            if (cbMailJobCompleted.Checked)
            {
                tbMailJobCompletedDate.Text = DateTime.Today.ToShortDateString();
            }
            else
            {
                tbMailJobCompletedDate.Text = string.Empty;
            }
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Reject;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;

                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //todo: send email


                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //history list
                    helper.CreatePIWListHistory(clientContext, ListItemID, "Print Job Rejected",
                        FormStatus, Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                    helper.RefreshPage(Request, Response);

                    //TODO: send email
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
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
                    ListItem listItem = null;

                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //TODO: send email

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //create history list
                    if ((!PrintJobCompleted) && (cbPrintJobCompleted.Checked))
                    {
                        FormStatus = Constants.PIWList_FormStatus_PrintJobCompleted;
                        helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Print Job marked as Completed on " + tbPrintJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                    }
                    else
                    {
                        //if PrintJobCompletedDate change    
                        if (!PrintJobCompletedDate.Equals(tbPrintJobCompletedDate.Text))
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Print Job Completed Date changed to " + tbPrintJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                        }
                    }


                    if ((!MailJobCompleted) && (cbMailJobCompleted.Checked))
                    {
                        FormStatus = Constants.PIWList_FormStatus_MailJobCompleted;
                        helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Mail Job marked as Completed on " + tbMailJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                    }
                    else
                    {
                        //if MailJobCompletedDate change    
                        if (!MailJobCompletedDate.Equals(tbMailJobCompletedDate.Text))
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Mail Job Completed Date changed to " + tbMailJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                        }
                    }

                    helper.RefreshPage(Page.Request, Page.Response);
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
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;

                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //TODO: send email

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //create history list
                    helper.CreatePIWListHistory(clientContext, ListItemID, "Print Requisition Form Submitted",
                        FormStatus, Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);

                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnComplete_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.PrintReqComplete;
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    ListItem listItem = null;

                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //TODO: send email

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //create history list
                    if ((!PrintJobCompleted) && (cbPrintJobCompleted.Checked))
                    {
                        FormStatus = Constants.PIWList_FormStatus_PrintJobCompleted;
                        helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Print Job marked as Completed on " + tbPrintJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                    }
                    else
                    {
                        //if PrintJobCompletedDate change    
                        if (!PrintJobCompletedDate.Equals(tbPrintJobCompletedDate.Text))
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Print Job Completed Date changed to " + tbPrintJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                        }
                    }


                    if ((!MailJobCompleted) && (cbMailJobCompleted.Checked))
                    {
                        FormStatus = Constants.PIWList_FormStatus_MailJobCompleted;
                        helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Mail Job marked as Completed on " + tbMailJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                    }
                    else
                    {
                        //if MailJobCompletedDate change    
                        if (!MailJobCompletedDate.Equals(tbMailJobCompletedDate.Text))
                        {
                            helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Mail Job Completed Date changed to " + tbMailJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                        }
                    }


                    helper.CreatePIWListHistory(clientContext, ListItemID, "Print Requisition Form Completed",
                        FormStatus, Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);

                    //history list for main form
                    var message = string.Format("Print Requisition Form Completed.</br>Print Job Completed Date: {0}</br>Mail Job Completed Date: {1}",
                            tbPrintJobCompletedDate.Text, tbMailJobCompletedDate.Text);
                    helper.CreatePIWListHistory(clientContext, ListItemID, message,
                        FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

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
            const enumAction action = enumAction.Accept;
            using (var clientContext = helper.getElevatedClientContext(Context, Request))
            {
                ListItem listItem = null;

                if (!SaveData(clientContext, action, ref listItem))
                {
                    return;
                }

                //TODO: send email

                //get current user
                User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                clientContext.Load(currentUser);
                clientContext.ExecuteQuery();

                //create history list
                helper.CreatePIWListHistory(clientContext, ListItemID, "Print Requisition Form Accepted",
                    FormStatus, Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);

                helper.RefreshPage(Page.Request, Page.Response);
            }
        }

        #endregion

        
    }
}