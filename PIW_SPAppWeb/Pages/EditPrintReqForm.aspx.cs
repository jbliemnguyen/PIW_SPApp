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
        private bool SaveData(ClientContext clientContext, enumAction action, ref ListItem returnedListItem)
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

            SaveMainPanelAndStatus(clientContext, listItem, action);

            returnedListItem = listItem;
            return true;
        }

        public void SaveMainPanelAndStatus(ClientContext clientContext, ListItem listItem, enumAction action)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);


            //number of pages
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfPublicPages]] = tbNumberofPages.Text.Trim();

            //number of copies
            if (action == enumAction.Delete)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]] = 0;
                    //this is the reset
            }
            else
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]] = tbNumberofCopies.Text.Trim();    
            }
            

            //Print Job Completed Date
            if (action == enumAction.PrintJobComplete)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]] = DateTime.Now.ToShortDateString();
            }

            //Mail Job Completed Date
            if (action == enumAction.MailJobComplete)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] = DateTime.Now.ToShortDateString();
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

            //print req form url
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqFormURL]] = Request.Url.ToString();




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

        public void ControlsVisiblitilyBasedOnStatus(ClientContext clientContext, string Status)
        {
            bool isCurrentUserCopyCenterMember = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                            new string[] { Constants.Grp_CopyCenter });

            bool isCurrentUserpiwAdmin = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                            new string[] { Constants.Grp_PIWAdmin, Constants.Grp_PIWSystemAdmin });
            switch (Status)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Rejected:
                    tbNumberofCopies.Enabled = true;
                    tbNumberofPages.Enabled = true;

                    //buttons
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSubmit.Visible = isCurrentUserpiwAdmin;

                    btnSave.Visible = isCurrentUserpiwAdmin;
                    btnPrintJobComplete.Visible = false;
                    btnMailJobComplete.Visible = false;
                    btnDelete.Visible = isCurrentUserpiwAdmin;

                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    tbNumberofCopies.Enabled = false;
                    tbNumberofPages.Enabled = false;

                    //buttons
                    btnAccept.Visible = isCurrentUserCopyCenterMember;
                    btnReject.Visible = isCurrentUserCopyCenterMember;
                    btnSubmit.Visible = false;
                    btnSave.Visible = false;
                    btnPrintJobComplete.Visible = false;
                    btnMailJobComplete.Visible = false;
                    btnDelete.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_PrintReqAccepted:
                    tbNumberofCopies.Enabled = true;
                    tbNumberofPages.Enabled = true;

                    //buttons
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSave.Visible = isCurrentUserCopyCenterMember;
                    btnPrintJobComplete.Visible = isCurrentUserCopyCenterMember;
                    btnMailJobComplete.Visible = false;
                    btnSubmit.Visible = false;
                    btnDelete.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_PrintJobCompleted:
                    tbNumberofCopies.Enabled = true;
                    tbNumberofPages.Enabled = true;

                    //buttons
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSave.Visible = isCurrentUserCopyCenterMember;
                    btnPrintJobComplete.Visible = false;
                    btnMailJobComplete.Visible = isCurrentUserCopyCenterMember;
                    btnSubmit.Visible = false;
                    btnDelete.Visible = false;

                    break;
                case Constants.PIWList_FormStatus_MailJobCompleted:
                    tbNumberofCopies.Enabled = true;
                    tbNumberofPages.Enabled = true;

                    //buttons
                    btnAccept.Visible = false;
                    btnReject.Visible = false;
                    btnSave.Visible = false;
                    btnPrintJobComplete.Visible = false;
                    btnMailJobComplete.Visible = false;
                    btnSubmit.Visible = false;
                    btnDelete.Visible = false;
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
                            var isCurrentUserAdmin = helper.IsUserMemberOfGroup(clientContext, CurrentUserLogInID,
                                new[] { Constants.Grp_PIWSystemAdmin });

                            //if current user is piw admin, load the item even if the isActive is false
                            ListItem listItem = helper.GetPiwListItemById(clientContext, ListItemID, isCurrentUserAdmin);
                            if (listItem == null)
                            {
                                helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_ItemNotFound);
                                return;
                            }
                            else
                            {

                                var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
                                int numberofCopies = 0;
                                if (listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]] != null)
                                {
                                    numberofCopies = int.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]].ToString());
                                }

                                if (numberofCopies < 1)
                                {
                                    helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_ItemNotFound);
                                    return;
                                }



                                string publicDocumentURLs;
                                string cEiiDocumentUrLs;
                                string privilegedDocumentURLs;
                                helper.PopulateIssuanceDocumentList(clientContext, ListItemID, rpDocumentList,
                                out publicDocumentURLs, out cEiiDocumentUrLs, out privilegedDocumentURLs);

                                PopulateFOLAAndSupplementalMailingListURL(clientContext);
                                PopulateFormProperties(clientContext, listItem);
                                DisplayListItemInForm(clientContext, listItem);
                                helper.PopulateHistoryList(clientContext, ListItemID, rpHistoryList, Constants.PIWListHistory_FormTypeOption_PrintReq);

                                //display form visiblility based on form status
                                ControlsVisiblitilyBasedOnStatus(clientContext, FormStatus);



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

        //protected void btnComplete_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        const enumAction action = enumAction.PrintReqComplete;
        //        using (var clientContext = helper.getElevatedClientContext(Context, Request))
        //        {
        //            ListItem listItem = null;

        //            if (!SaveData(clientContext, action, ref listItem))
        //            {
        //                return;
        //            }

        //            //TODO: send email

        //            //get current user
        //            User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
        //            clientContext.Load(currentUser);
        //            clientContext.ExecuteQuery();

        //            //create history list
        //            if ((!PrintJobCompleted) && (cbPrintJobCompleted.Checked))
        //            {
        //                FormStatus = Constants.PIWList_FormStatus_PrintJobCompleted;
        //                helper.CreatePIWListHistory(clientContext, ListItemID,
        //                    "Print Job marked as Completed on " + tbPrintJobCompletedDate.Text, FormStatus,
        //                    Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
        //            }
        //            else
        //            {
        //                //if PrintJobCompletedDate change    
        //                if (!PrintJobCompletedDate.Equals(tbPrintJobCompletedDate.Text))
        //                {
        //                    helper.CreatePIWListHistory(clientContext, ListItemID,
        //                    "Print Job Completed Date changed to " + tbPrintJobCompletedDate.Text, FormStatus,
        //                    Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
        //                }
        //            }


        //            if ((!MailJobCompleted) && (cbMailJobCompleted.Checked))
        //            {
        //                FormStatus = Constants.PIWList_FormStatus_MailJobCompleted;
        //                helper.CreatePIWListHistory(clientContext, ListItemID,
        //                    "Mail Job marked as Completed on " + tbMailJobCompletedDate.Text, FormStatus,
        //                    Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
        //            }
        //            else
        //            {
        //                //if MailJobCompletedDate change    
        //                if (!MailJobCompletedDate.Equals(tbMailJobCompletedDate.Text))
        //                {
        //                    helper.CreatePIWListHistory(clientContext, ListItemID,
        //                    "Mail Job Completed Date changed to " + tbMailJobCompletedDate.Text, FormStatus,
        //                    Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
        //                }
        //            }


        //            helper.CreatePIWListHistory(clientContext, ListItemID, "Print Requisition Form Completed",
        //                FormStatus, Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);

        //            //history list for main form
        //            var message = string.Format("Print Requisition Form Completed.</br>Print Job Completed Date: {0}</br>Mail Job Completed Date: {1}",
        //                    tbPrintJobCompletedDate.Text, tbMailJobCompletedDate.Text);
        //            helper.CreatePIWListHistory(clientContext, ListItemID, message,
        //                FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

        //            helper.RefreshPage(Page.Request, Page.Response);
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        helper.LogError(Context, Request, exc, ListItemID, string.Empty);
        //        helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
        //    }
        //}

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

        protected void btnPrintJobComplete_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.PrintJobComplete;
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
                    helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Print Job marked as Completed on " + DateTime.Now.ToShortDateString(), FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);

                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnMailJobComplete_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.MailJobComplete;
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
                    helper.CreatePIWListHistory(clientContext, ListItemID,
                            "Mail Job marked as Completed on " + DateTime.Now.ToShortDateString(), FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);

                    //set the history list for main form
                    var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

                    var printJobCompletedDate = listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]] != null ?
                        DateTime.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleteDate]].ToString()).ToShortDateString()
                        : string.Empty;
                    var mailJobCompletedDate = listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] != null ?
                        DateTime.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleteDate]].ToString()).ToShortDateString()
                        : string.Empty;
                    var piwFormStatus = listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] != null ?
                        listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]].ToString() : string.Empty;

                    var message = string.Format("Print Requisition Form Completed.</br>Print Job Completed Date: {0}</br>Mail Job Completed Date: {1}",
                            printJobCompletedDate, mailJobCompletedDate);
                    helper.CreatePIWListHistory(clientContext, ListItemID, message,
                        piwFormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, ListItemID, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        #endregion

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

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

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




    }
}