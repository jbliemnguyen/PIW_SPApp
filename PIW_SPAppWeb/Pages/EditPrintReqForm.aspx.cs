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
        //variable        
        private string _listItemId;

        //fuction
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

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                _listItemId = Page.Request.QueryString["ID"];
                helper = new SharePointHelper();

                if (!Page.IsPostBack)
                {
                    if (!string.IsNullOrEmpty(_listItemId))
                    {
                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            helper.PopulateIssuanceDocumentList(clientContext, _listItemId, rpDocumentList);
                            PopulateFOLAAndSupplementalMailingListURL(clientContext);

                            var isCurrentUserAdmin = helper.IsCurrentUserMemberOfGroup(clientContext, Constants.Grp_PIWAdmin);

                            //if current user is piw admin, load the item even if the isActive is false
                            ListItem listItem = helper.GetPiwListItemById(clientContext, _listItemId, isCurrentUserAdmin);
                            if (listItem == null)
                            {
                                helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_ItemNotFound);
                            }
                            else
                            {
                                PopulateFormProperties(clientContext, listItem);
                                DisplayListItemInForm(clientContext, listItem);
                                helper.PopulateHistoryList(clientContext, _listItemId, rpHistoryList, Constants.PIWListHistory_FormTypeOption_PrintReq);

                                //display form visiblility based on form status
                                ControlsVisiblitilyBasedOnStatus(FormStatus);

                                //todo: open documents if status is ready for published

                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, Page.Request.Url.OriginalString);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }

        }

        private void DisplayListItemInForm(ClientContext clientContext, ListItem listItem)
        {
            if (listItem != null)
            {
                var piwListInteralColumnNames = helper.getInternalColumnNamesFromCache(clientContext,
                    Constants.PIWListName);

                var formType = listItem[piwListInteralColumnNames[Constants.PIWList_colName_FormType]].ToString();
                //Link to PIW Form
                hplPIWFormLink.NavigateUrl = helper.getEditFormURL(formType, _listItemId, Request, string.Empty);

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

                //Note
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqNotes]] != null)
                {
                    tbNote.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrintReqNotes]].ToString();
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

            if (listItem[internalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleted]] != null)
            {
                PrintJobCompleted = bool.Parse(listItem[internalColumnNames[Constants.PIWList_colName_PrintReqPrintJobCompleted]].ToString());
            }

            if (listItem[internalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleted]] != null)
            {
                MailJobCompleted = bool.Parse(listItem[internalColumnNames[Constants.PIWList_colName_PrintReqMailJobCompleted]].ToString());
            }

        }

        public void PopulateFOLAAndSupplementalMailingListURL(ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();

            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, Constants.PIWDocuments_DocumentLibraryName, _listItemId);
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
        protected void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    ListItem listItem = null;


                    if ((!PrintJobCompleted) && (cbPrintJobCompleted.Checked))
                    {
                        FormStatus = Constants.PrintReq_FormStatus_PrintJobCompleted;
                        helper.CreatePIWListHistory(clientContext, _listItemId, "Print Job marked as Completed on " + tbPrintJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq);
                    }

                    //Create list history
                    if ((!MailJobCompleted) && (cbMailJobCompleted.Checked))
                    {
                        FormStatus = Constants.PrintReq_FormStatus_MailJobCompleted;
                        helper.CreatePIWListHistory(clientContext, _listItemId, "Mail Job marked as Completed on " + tbMailJobCompletedDate.Text, FormStatus,
                            Constants.PIWListHistory_FormTypeOption_PrintReq);
                    }

                    SaveData(clientContext, ref listItem);
                    helper.RefreshPage(Page.Request, Page.Response);

                    //TODO: send email
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        private bool SaveData(ClientContext clientContext, ref ListItem returnedListItem)
        {
            ListItem listItem = helper.GetPiwListItemById(clientContext, _listItemId, false);

            if (helper.CheckIfListItemChanged(clientContext, listItem, DateTime.Parse(ModifiedDateTime)))
            {
                lbMainMessage.Text = "The form has been changed, please refresh the page";
                lbMainMessage.Visible = true;
                return false;
            }

            //update form data to list

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

            //Note
            if (!string.IsNullOrEmpty(tbNote.Text))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqNotes]] = tbNote.Text.Trim();
            }

            //Status
            if (!string.IsNullOrEmpty(FormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqStatus]] = FormStatus;
            }

            //execute query
            listItem.Update();
            clientContext.ExecuteQuery();

            returnedListItem = listItem;
            return true;
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
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    ListItem listItem = null;

                    FormStatus = Constants.PrintReq_FormStatus_PrintReqRejected;


                    SaveData(clientContext, ref listItem);

                    helper.CreatePIWListHistory(clientContext, _listItemId, "Print Job Rejected", FormStatus, Constants.PIWListHistory_FormTypeOption_PrintReq);
                    helper.RefreshPage(Request,Response);
                    
                    //TODO: send email
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        public void ControlsVisiblitilyBasedOnStatus(string Status)
        {
            tbPrintJobCompletedDate.Enabled = cbPrintJobCompleted.Checked;
            tbMailJobCompletedDate.Enabled = cbMailJobCompleted.Checked;

            //view only if status is complete or reject
            if (((cbMailJobCompleted.Checked) && (cbPrintJobCompleted.Checked)) || Status.Equals(Constants.PrintReq_FormStatus_PrintReqRejected))
            {
                btnSave.Visible = false;
                btnReject.Visible = false;
                tbNumberofCopies.Enabled = false;
                tbMailJobCompletedDate.Enabled = false;
                tbPrintJobCompletedDate.Enabled = false;
                tbNumberofPages.Enabled = false;
                cbMailJobCompleted.Enabled = false;
                cbPrintJobCompleted.Enabled = false;
            }
        }
    }
}