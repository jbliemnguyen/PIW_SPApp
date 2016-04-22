using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
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
        
        //variable        
        private string _listItemId;
        private bool _isEditForm;
        private bool isMail;

        //fuction
        static SharePointHelper helper = null;
        #endregion

        #region Events
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                _listItemId = this.Page.Request.QueryString["ID"];

                //Set CitationError to invisible
                //Validation errors may be visible from previous step, need to turn off
                //lbCitationError.Visible = false;

                //lbOSECVerificationError.Visible = false;
                lbUploadedDocumentError.Visible = false;

                _listItemId = this.Page.Request.QueryString["ID"];

                helper = new SharePointHelper();
                _isEditForm = (!string.IsNullOrEmpty(_listItemId));

                if (!Page.IsPostBack)
                {
                    if (_isEditForm)
                    {
                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            //TODO: recome comment when working with edit form

                            PopulateDocumentList(clientContext);
                            //PopulateHistoryList();
                            ListItem listItem = helper.GetPiwListItemById(clientContext, _listItemId, false);
                            PopulateFormStatusAndModifiedDate(clientContext, listItem);
                            DisplayListItemInForm(clientContext, listItem);
                            ////display form visiblility based on form status
                            ControlsVisiblitilyBasedOnStatus(clientContext, PreviousFormStatus, FormStatus, listItem);
                            ////above method get formStatus from list, store it in viewstate                       
                            //if (FormStatus == enumFormStatus.ReadyForPublishing)
                            //{
                            //    helper.OpenDocument(Page, documentURL);
                            //}
                        }

                    }
                    else//new form
                    {
                        //if it is new form
                        //Create new PIWListITem
                        //create document libraries
                        //Then redirect to EditForm
                        //By doing it, we can attach multiple document to new piwList item under its folder ID

                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            ListItem newItem = helper.createNewPIWListItem(clientContext, Constants.PIWList_FormType_StandardForm);
                            _listItemId = newItem.Id.ToString();

                            //Create subfolder in piwdocuments and mailing list
                            helper.CreatePIWDocumentsSubFolder(clientContext, _listItemId);
                        }

                        //forward to Edit
                        Response.Redirect(Request.Url + "&ID=" + _listItemId);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, Page.Request.Url.OriginalString);
                throw exc;
            }
        }
        protected void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Save;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    if (ValidFormData(action))
                    {
                        var listItem = SaveData(clientContext, action);

                        //TODO: Change document and list permission

                        //TODO: send email

                        //TODO: Create list history

                        //Refresh
                        helper.RefreshPage(Page.Request, Page.Response);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }


        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Submit;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    if (ValidFormData(action))
                    {
                        var listItem = SaveData(clientContext, action);

                        //TODO: Change document and list permission

                        //TODO: send email

                        //TODO: Create list history
                        //helper.CreatePIWListHistory(clientContext, _listItemId, "Submit", "Submited");

                        //Redirect
                        helper.RedirectToPreviousPage(Page.Request, Page.Response);


                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnAccept_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Accept;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                        var listItem = SaveData(clientContext, action);

                        //TODO: Change document and list permission

                        //TODO: send email

                        //TODO: Create list history
                        //helper.CreatePIWListHistory(clientContext, _listItemId, "Submit", "Submited");

                        //Redirect
                        helper.RedirectToPreviousPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Reject;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    var listItem = SaveData(clientContext, action);

                    //TODO: Change document and list permission

                    //TODO: send email

                    //TODO: Create list history
                    //helper.CreatePIWListHistory(clientContext, _listItemId, "Submit", "Submited");

                    //Redirect
                    helper.RedirectToPreviousPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnInitiatePublication_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Publish;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    var listItem = SaveData(clientContext, action);

                    //TODO: Change document and list permission

                    //TODO: send email

                    //TODO: Create list history
                    //helper.CreatePIWListHistory(clientContext, _listItemId, "Submit", "Submited");

                    //Refresh
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnDeleteConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Delete;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    var listItem = SaveData(clientContext, action);

                    //TODO: Change document and list permission

                    //TODO: send email

                    //TODO: Create list history
                    //helper.CreatePIWListHistory(clientContext, _listItemId, "Submit", "Submited");

                    //Redirect
                    helper.RedirectToPreviousPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }
        
        protected void btnRecall_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Recall;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    if (ValidFormData(action))
                    {
                        var listItem = SaveData(clientContext, action);


                        //TODO: Change document and list permission

                        //TODO: send email

                        //TODO: Create list history
                        //helper.CreatePIWListHistory(clientContext, _listItemId, "Submit", "Submited");

                        //Redirect
                        helper.RedirectToPreviousPage(Page.Request, Page.Response);


                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnOSECTakeOwnership_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.OSECTakeOwnerShip;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    var listItem = SaveData(clientContext, action);

                    //TODO: Change document and list permission

                    //TODO: send email

                    //TODO: Create list history
                    //helper.CreatePIWListHistory(clientContext, _listItemId, "Submit", "Submited");

                    //refresh
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Edit;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    var listItem = SaveData(clientContext, action);
                    //TODO: Change document and list permission

                    //TODO: Create list history
                    //helper.CreatePIWListHistory(clientContext, _listItemId, "Submit", "Submited");

                    //Redirect or Refresh page
                    helper.RefreshPage(Page.Request,Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnGenerateCitationNumber_Click(object sender, EventArgs e)
        {
            try
            {
                lbCitationNumberError.Text = string.Empty;
                lbCitationNumberError.Visible = false;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    if (ddDocumentCategory.SelectedIndex > 0)
                    {
                        int documentCategoryNumber = helper.getDocumentCategoryNumber(ddDocumentCategory.SelectedValue);

                        CitationNumber citationNumberHelper = new CitationNumber(documentCategoryNumber, DateTime.Now);

                        tbCitationNumber.Text = citationNumberHelper.GetNextCitationNumber(clientContext);

                        var availableCitationNumbers = citationNumberHelper.getAllAvailableCitationNumber(clientContext);
                        if (availableCitationNumbers.Count > 1) //more than 1, 1 is already displayed in textbox
                        {
                            ddAvailableCitationNumbers.Visible = true;
                            ddAvailableCitationNumbers.Items.Clear();
                            ddAvailableCitationNumbers.Items.Add("-- Available Citation # --");

                            foreach (string s in availableCitationNumbers)
                            {
                                ddAvailableCitationNumbers.Items.Add(s);
                            }
                        }
                        else
                        {
                            ddAvailableCitationNumbers.Visible = false;
                        }

                    }

                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnAcceptCitationNumber_Click(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    if (ddDocumentCategory.SelectedIndex > 0)
                    {
                        string errorMessage = string.Empty;
                        int documentCategoryNumber = helper.getDocumentCategoryNumber(ddDocumentCategory.SelectedValue);

                        CitationNumber citationNumberHelper = new CitationNumber(documentCategoryNumber, DateTime.Now);

                        if (citationNumberHelper.Save(clientContext, _listItemId, tbCitationNumber.Text.Trim(),
                            ref errorMessage, cbOverrideCitationNumber.Checked))
                        {
                            lbCitationNumberError.Visible = false;
                            lbCitationNumberError.Text = string.Empty;
                            helper.SetCitationNumberFieldInPIWList(clientContext, _listItemId, tbCitationNumber.Text.Trim());

                            //controls
                            //after accept, citation number cannot be changed
                            EnableCitationNumberControls(false,true);
                            lbCitationNumberError.Text = string.Empty;
                            lbCitationNumberError.Visible = false;
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
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }
        
        protected void btnRemoveCitationNumber_Click(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    //just delete the citation item - instead of settign the status to deleted
                    helper.deleteAssociatedCitationNumberListItem(clientContext, _listItemId);

                    //controls
                    tbCitationNumber.Text = string.Empty;
                    //after remove, citation canbe changed
                    EnableCitationNumberControls(true,false);

                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
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

                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            helper.RemoveDocument(clientContext, _listItemId, Constants.PIWDocuments_DocumentLibraryName, e.CommandArgument.ToString());
                            PopulateDocumentList(clientContext);
                        }

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                throw exc;
            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if ((fileUpload.HasFiles) && (fileUpload.PostedFile.ContentLength > 0) &&
                    (fileUpload.PostedFile.ContentLength < 52428800))
                {
                    var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

                    using (var clientContext = spContext.CreateUserClientContextForSPHost())
                    {
                        using (var fileStream = fileUpload.PostedFile.InputStream)
                        {
                            helper.UploadDocumentContentStream(clientContext, fileStream,
                                Constants.PIWDocuments_DocumentLibraryName, _listItemId, fileUpload.FileName,
                                ddlSecurityControl.SelectedValue);
                            PopulateDocumentList(clientContext);
                            //clear validation error
                            lbRequiredUploadedDocumentError.Visible = false;
                        }
                    }

                    if (rpDocumentList.Items.Count == 1)//only extract docket number if first document uploaded
                    {
                        tbDocketNumber.Text = helper.ExtractDocket(fileUpload.FileName);
                    }


                }

            }
            catch (Exception ex)
            {
                helper.LogError(Context, ex, _listItemId, string.Empty);
                lbUploadedDocumentError.Text = ex.Message.ToString();
                lbUploadedDocumentError.Visible = true;
            }

        }

        #endregion

        #region Save Data
        private ListItem SaveData(ClientContext clientContext, enumAction action)
        {
            ListItem listItem = helper.GetPiwListItemById(clientContext, _listItemId, false);

            if (CheckIfListItemChanged(clientContext, listItem))
            {
                return listItem;
            }

            //get next form status
            var currentFormStatus = FormStatus;
            StandardFormWorkflow wf = new StandardFormWorkflow();
            FormStatus = wf.Execute(PreviousFormStatus, FormStatus, action,
                isRequiredOSECVerificationStep(ddDocumentCategory.SelectedValue), true);
            PreviousFormStatus = currentFormStatus;

            UpdateFormDataToList(clientContext, listItem, action);
            return listItem;
        }
        private void UpdateFormDataToList(ClientContext clientContext, ListItem listItem, enumAction action)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);
            switch (FormStatus)//this is the next status after action is performed
            {
                case Constants.PIWList_FormStatus_Pending:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Pending)
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Recalled:
                    if (FormStatus == PreviousFormStatus)//save action
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Submitted)
                    {
                        SaveRecallInfoAndStatus(clientContext, listItem);
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
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_OSECVerification)
                    {
                        SaveOSECVerificationInfoAndStatus(clientContext, listItem, action);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_PrePublication)
                    {
                        SavePrePublicationInfoAndStatus(clientContext, listItem, action);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_Pending) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_Recalled))
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Rejected)
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                        //clear OSECVerification and PrePublication action when a rejected item is re-submit
                        ClearOSECActionsAndCommentsBeforeReSubmit(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_Edited:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_OSECVerification) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_PrePublication) ||
                        (PreviousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        SaveFormStatus(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Deleted:
                    //delete item, need to set status and remove citation number if there is assigned one
                    SaveDeleteInfoAndStatus(clientContext,listItem);
                    helper.ReleaseCitationNumberForDeletedListItem(clientContext,_listItemId);
                    break;
                case Constants.PIWList_FormStatus_OSECVerification:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Edited)
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Submitted)
                    {
                        SaveFormStatus(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_PrePublication:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Edited)//save
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_OSECVerification)//come from OSEC Verification
                    {
                        SaveOSECVerificationInfoAndStatus(clientContext, listItem, action);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_Submitted)//come from Submitted
                    {
                        SaveFormStatus(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:

                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Edited) //save
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else if (PreviousFormStatus == Constants.PIWList_FormStatus_PrePublication)
                    {
                        SavePrePublicationInfoAndStatus(clientContext, listItem, action);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }

                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_ReadyForPublishing)
                    {
                        SavePublishingInfoAndStatus(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                default:
                    throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));

            }
        }

        private void SavePublishingInfoAndStatus(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

            clientContext.Load(clientContext.Web.CurrentUser, user => user.Id);
            clientContext.ExecuteQuery();

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            //publisher
            FieldUserValue publisher = new FieldUserValue { LookupId = clientContext.Web.CurrentUser.Id };
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PublishedBy]] = publisher;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void SaveDeleteInfoAndStatus(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

            clientContext.Load(clientContext.Web.CurrentUser, user => user.Id);
            clientContext.ExecuteQuery();

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;
            
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_IsActive]] = false;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_CitationNumber]] = string.Empty;


            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void SaveFormStatus(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void SavePrePublicationInfoAndStatus(ClientContext clientContext, ListItem listItem, enumAction action)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrePublicationReviewAction]] = action.ToString();

            if (action == enumAction.Reject)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_OSECRejectedComment]] = tbPrePublicationComment.Text.Trim();
            }
            else
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrePublicationReviewComment]] = tbPrePublicationComment.Text.Trim();
            }



            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void SaveOSECVerificationInfoAndStatus(ClientContext clientContext, ListItem listItem, enumAction action)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_OSECVerificationAction]] = action.ToString();

            if (action == enumAction.Reject)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_OSECRejectedComment]] = tbOSECVerificationComment.Text.Trim();
                
            }
            else
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_OSECVerificationComment]] = tbOSECVerificationComment.Text.Trim();
            }


            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void SaveRecallInfoAndStatus(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallComment]] = tbRecallComment.Text.Trim();

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void ClearOSECActionsAndCommentsBeforeReSubmit(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_OSECVerificationAction]] = string.Empty;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_OSECVerificationComment]] = string.Empty;

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrePublicationReviewAction]] = string.Empty;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrePublicationReviewComment]] = string.Empty;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private void SaveMainPanelAndStatus(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

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
            if (!string.IsNullOrEmpty(tbComment.Text))
            {
                if (listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] == null)
                {
                    listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("{0} ({1}): {2}", clientContext.Web.CurrentUser.Title,
                        DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text);
                }
                else
                {
                    //append
                    listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("{0} ({1}): {2}<br>{3}",
                        clientContext.Web.CurrentUser.Title, DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text, listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]].ToString());
                }

            }

            if (!string.IsNullOrEmpty(FormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            }

            if (!string.IsNullOrEmpty(PreviousFormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;
            }

            //execute query
            listItem.Update();
            clientContext.ExecuteQuery();
        }
        #endregion

        #region Utilities

        /// <summary>
        /// Populate document list and return URL of the document
        /// </summary>
        /// <returns></returns>
        private void PopulateDocumentList(ClientContext clientContext)
        {
            string returnedURL = string.Empty;
            List<string> result = new List<string>();
            System.Data.DataTable table = helper.getAllDocumentsTable(clientContext, _listItemId, Constants.PIWDocuments_DocumentLibraryName);

            rpDocumentList.DataSource = table;
            rpDocumentList.DataBind();
        }

        protected void Timer1_Tick(object sender, EventArgs e)
        {
            //TODO: Only refresh in some certain status. Change the time span to 30 seconds 
            using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
            {
                PopulateDocumentList(clientContext);
            }
        }
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
                isValid = isValid & true;
                lbRequiredUploadedDocumentError.Visible = false;
            }

            if (action == enumAction.Recall)//if recall, recall comment is required
            {
                if (string.IsNullOrEmpty(tbRecallComment.Text))
                {
                    isValid = false;
                    lbRecallCommentValidation.Visible = true;
                }
                else
                {
                    isValid = isValid & true;
                    lbRecallCommentValidation.Visible = false;
                }
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
                isValid = isValid & false;
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
        private void PopulateFormStatusAndModifiedDate(ClientContext clientContext, ListItem listItem)
        {
            var internalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);
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

            if (listItem[internalColumnNames[Constants.PIWList_colName_Modified]] != null)
            {
                ModifiedDateTime = listItem[internalColumnNames[Constants.PIWList_colName_Modified]].ToString();
            }
        }

        private void DisplayListItemInForm(ClientContext clientContext, ListItem listItem)
        {
            if (listItem != null)
            {
                var piwListInteralColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);


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
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]] != null)
                {
                    lbCommentValue.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]].ToString();
                }

                //OSEC Reject Comment
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_OSECRejectedComment]] != null)
                {
                    lbOSECRejectCommentValue.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_OSECRejectedComment]].ToString();
                }

                //Recall Comment
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_RecallComment]] != null)
                {
                    tbRecallComment.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_RecallComment]].ToString();
                }

                //OSEC Verification
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_OSECVerificationAction]] != null)
                {
                    lbOSECVerificationAction.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_OSECVerificationAction]].ToString();
                }

                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_OSECVerificationComment]] != null)
                {
                    tbOSECVerificationComment.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_OSECVerificationComment]].ToString();
                }

                //Pre-Publication Review
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrePublicationReviewAction]] != null)
                {
                    lbPrePublicationReviewAction.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrePublicationReviewAction]].ToString();
                }

                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrePublicationReviewComment]] != null)
                {
                    tbPrePublicationComment.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_PrePublicationReviewComment]].ToString();
                }

                //Citation Number
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_CitationNumber]] != null)
                {
                    tbCitationNumber.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_CitationNumber]].ToString();
                }

            }
        }

        private void PopulateHistoryList()
        {
            throw new NotImplementedException();
        }

        private bool isRequiredOSECVerificationStep(string documentCategory)
        {
            return (documentCategory.Equals(Constants.PIWList_DocCat_Notice) ||
                     documentCategory.Equals(Constants.PIWList_DocCat_NoticeErrata));
        }

        private bool CheckIfListItemChanged(ClientContext clientContext, ListItem listItem)
        {
            if (helper.CheckIfListItemChanged(clientContext, listItem, DateTime.Parse(ModifiedDateTime)))
            {
                lbMainMessage.Text = "The form has been changed, please refresh the page";
                lbMainMessage.Visible = true;

                return true;
            }
            return false;
        }
        #endregion

        #region Visibility
        public void ControlsVisiblitilyBasedOnStatus(ClientContext clientContext, string previousFormStatus, string formStatus, ListItem listItem)
        {
            var piwlistInternalColumnName = helper.getInternalColumnNames(clientContext, Constants.PIWListName);
            var documentCategory = string.Empty;
            if (listItem[piwlistInternalColumnName[Constants.PIWList_colName_DocumentCategory]] != null)
            {
                documentCategory = listItem[piwlistInternalColumnName[Constants.PIWList_colName_DocumentCategory]].ToString();
            }
            bool isRequireOSECVerification = isRequiredOSECVerificationStep(documentCategory);
            //SPUser checkoutUser = null;
            var currentUser = clientContext.Web.CurrentUser;
            clientContext.Load(currentUser);

            clientContext.ExecuteQuery();

            switch (formStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Recalled:
                case Constants.PIWList_FormStatus_Rejected:
                    //submit section    
                    EnableMainPanel(true);
                    lbMainMessage.Visible = false;
                    if (formStatus.Equals(Constants.PIWList_FormStatus_Recalled))
                    {
                        fieldsetOSECRejectComment.Visible = false;
                        fieldsetRecall.Visible = true;
                        btnRecall.Visible = false;
                        tbRecallComment.Enabled = false;//prevent user from update the recall comment
                    }
                    else if (formStatus.Equals(Constants.PIWList_FormStatus_Rejected))
                    {
                        fieldsetOSECRejectComment.Visible = true;
                        fieldsetRecall.Visible = false;
                    }
                    else//pending
                    {
                        fieldsetOSECRejectComment.Visible = false;
                        fieldsetRecall.Visible = false;
                    }


                    //OSEC section
                    fieldsetOSECVerification.Visible = false;
                    fieldsetPrePublication.Visible = false;

                    //buttons
                    btnSave.Visible = helper.IsUserMemberOfGroup(clientContext, currentUser, Constants.Grp_PIWUsers) || helper.IsUserMemberOfGroup(clientContext, currentUser, Constants.Grp_OSECGroupName) || helper.IsUserMemberOfGroup(clientContext, currentUser, Constants.Grp_SecretaryReviewGroupName);

                    btnSubmit.Visible = btnSave.Visible;

                    btnEdit.Visible = false;

                    btnAccept.Visible = false;

                    btnReject.Visible = false;

                    btnOSECTakeOwnership.Visible = false;

                    btnPublish.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    //submit section   
                    EnableMainPanel(false);
                    lbMainMessage.Visible = false;
                    fieldsetOSECRejectComment.Visible = false;
                    fieldsetRecall.Visible = true;

                    //OSEC section
                    fieldsetOSECVerification.Visible = false;
                    fieldsetPrePublication.Visible = false;

                    //Button
                    btnSave.Visible = false;

                    btnSubmit.Visible = btnSave.Visible;

                    btnEdit.Visible = false;

                    btnAccept.Visible = false;

                    btnReject.Visible = false;

                    btnOSECTakeOwnership.Visible = true;

                    btnPublish.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    break;
                case Constants.PIWList_FormStatus_Edited:
                    //submitter
                    EnableMainPanel(true);
                    lbMainMessage.Visible = false;
                    fieldsetOSECRejectComment.Visible = false;
                    fieldsetRecall.Visible = false;

                    //OSEC section
                    if (previousFormStatus.Equals(Constants.PIWList_FormStatus_OSECVerification))
                    {
                        fieldsetOSECVerification.Visible = true;
                        tbOSECVerificationComment.Enabled = false;
                    }
                    else if (previousFormStatus.Equals(Constants.PIWList_FormStatus_PrePublication) ||
                        previousFormStatus.Equals(Constants.PIWList_FormStatus_ReadyForPublishing))
                    {
                        fieldsetPrePublication.Visible = true;
                        EnablePrePublicationControls(false);
                        EnableCitationNumberControls(false,false);
                    }

                    //Button
                    btnSave.Visible = true;

                    btnSubmit.Visible = false;

                    btnEdit.Visible = false;

                    btnAccept.Visible = false;

                    btnReject.Visible = false;

                    btnOSECTakeOwnership.Visible = false;

                    btnPublish.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    break;
                case Constants.PIWList_FormStatus_OSECVerification:
                    //submitter
                    EnableMainPanel(false);
                    lbMainMessage.Visible = false;
                    fieldsetOSECRejectComment.Visible = false;
                    fieldsetRecall.Visible = false;

                    //OSEC section
                    //osec verification
                    fieldsetOSECVerification.Visible = true;
                    tbOSECVerificationComment.Enabled = true;

                    //prepublication
                    fieldsetPrePublication.Visible = false;

                    //Button
                    btnSave.Visible = false;

                    btnSubmit.Visible = btnSave.Visible;

                    btnEdit.Visible = true;

                    btnAccept.Visible = true;

                    btnReject.Visible = true;

                    btnOSECTakeOwnership.Visible = false;

                    btnPublish.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    break;
                case Constants.PIWList_FormStatus_PrePublication:
                    //submitter
                    EnableMainPanel(false);
                    lbMainMessage.Visible = false;
                    fieldsetOSECRejectComment.Visible = false;
                    fieldsetRecall.Visible = false;

                    //OSEC section
                    //OSEC verification
                    if (isRequireOSECVerification)
                    {
                        fieldsetOSECVerification.Visible = true;
                        tbOSECVerificationComment.Enabled = false;
                    }

                    //PrePublication
                    fieldsetPrePublication.Visible = true;
                    EnablePrePublicationControls(true);
                    InitiallyEnableCitationNumberControls(clientContext, listItem);

                    //button
                    btnSave.Visible = false;

                    btnSubmit.Visible = btnSave.Visible;

                    btnEdit.Visible = true;

                    btnAccept.Visible = true;

                    btnReject.Visible = true;

                    btnOSECTakeOwnership.Visible = false;

                    btnPublish.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    //submitter
                    EnableMainPanel(false);
                    lbMainMessage.Visible = false;
                    fieldsetOSECRejectComment.Visible = false;
                    fieldsetRecall.Visible = false;

                    //OSEC section
                    //OSEC verification
                    if (isRequireOSECVerification)
                    {
                        fieldsetOSECVerification.Visible = true;
                        tbOSECVerificationComment.Enabled = false;
                    }

                    //PrePublication
                    fieldsetPrePublication.Visible = true;
                    EnablePrePublicationControls(false);
                    InitiallyEnableCitationNumberControls(clientContext,listItem);

                    //button
                    btnSave.Visible = false;

                    btnSubmit.Visible = btnSave.Visible;

                    btnEdit.Visible = true;

                    btnAccept.Visible = false;

                    btnReject.Visible = false;

                    btnOSECTakeOwnership.Visible = false;

                    btnPublish.Visible = true;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    //submitter
                    EnableMainPanel(false);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "Publication has been initiated for this issuance.";
                    fieldsetOSECRejectComment.Visible = false;
                    fieldsetRecall.Visible = false;

                    //OSEC section
                    //OSEC verification
                    if (isRequireOSECVerification)
                    {
                        fieldsetOSECVerification.Visible = true;
                        tbOSECVerificationComment.Enabled = false;
                    }

                    //PrePublication
                    fieldsetPrePublication.Visible = true;
                    EnablePrePublicationControls(false);
                    EnableCitationNumberControls(false,false);

                    //button
                    btnSave.Visible = false;

                    btnSubmit.Visible = btnSave.Visible;

                    btnEdit.Visible = false;

                    btnAccept.Visible = false;

                    btnReject.Visible = false;

                    btnOSECTakeOwnership.Visible = false;

                    btnPublish.Visible = false;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    throw new Exception("Not Implemented");
                    break;
                case Constants.PIWList_FormStatus_ReOpen:
                    throw new Exception("Not Implemented");
                    break;
                default:
                    throw new Exception("UnRecognized Form Status: " + formStatus);
                    break;
            }
        }

        private void EnablePrePublicationControls(bool enabled)
        {
            //btnGenerateCitationNumber.Enabled = enabled;
            //tbCitationNumber.Enabled = enabled;
            //ddAvailableCitationNumbers.Enabled = enabled;
            //cbOverrideCitationNumber.Enabled = enabled;
            //btnAcceptCitationNumber.Enabled = enabled;
            //btnRemoveCitationNumber.Enabled = enabled;
            tbPrePublicationComment.Enabled = enabled;
        }

        private void EnableMainPanel(bool enabled)
        {
            EnableFileUploadComponent(enabled);
            tbDocketNumber.Enabled = enabled;
            cbIsCNF.Enabled = enabled;
            cbIsNonDocket.Enabled = enabled;
            tbAlternateIdentifier.Enabled = enabled;
            tbDescription.Enabled = enabled;
            tbInstruction.Enabled = enabled;
            cbFederalRegister.Enabled = enabled;
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
            //disable/enable the Remove button
            //the link always be enable so user can open document
            foreach (RepeaterItem row in rpDocumentList.Items)
            {
                var btnRemoveDocument = (LinkButton)row.FindControl("btnRemoveDocument");
                btnRemoveDocument.Enabled = enabled;
            }
        }

        /// <summary>
        /// enabled or disable other controls from the textbox enabled property
        /// For example: when textbox is editable,  then the accept button should be clickable to assign the citation number
        /// </summary>
        /// <param name="citationTextBoxEnabled"></param>
        public void EnableCitationNumberControls(bool citationNumberCanBeChanged, bool CitationNumberCanBeRemoved)
        {
            //controls
            tbCitationNumber.Enabled = citationNumberCanBeChanged;
            btnGenerateCitationNumber.Enabled = citationNumberCanBeChanged;
            ddAvailableCitationNumbers.Enabled = citationNumberCanBeChanged;
            cbOverrideCitationNumber.Enabled = citationNumberCanBeChanged;
            tbCitationNumber.Enabled = citationNumberCanBeChanged;
            btnAcceptCitationNumber.Enabled = citationNumberCanBeChanged;

            
            btnRemoveCitationNumber.Enabled = CitationNumberCanBeRemoved;
        }

        public void InitiallyEnableCitationNumberControls(ClientContext clientContext, ListItem piwListItem)
        {
            var piwListinternalName = helper.getInternalColumnNames(clientContext, Constants.PIWListName);
            string citationNumber = string.Empty;
            if (piwListItem[piwListinternalName[Constants.PIWList_colName_CitationNumber]] != null)
            {
                citationNumber = piwListItem[piwListinternalName[Constants.PIWList_colName_CitationNumber]].ToString();
            }

            if (string.IsNullOrEmpty(citationNumber))
            {
                //no citation assigned in piwlist
                //system should allow to generate and assign citation number
                EnableCitationNumberControls(true,false);

            }
            else
            {
                //there is citation number assigned to piw list item
                EnableCitationNumberControls(false,true);

            }
        }
        #endregion

    }
}