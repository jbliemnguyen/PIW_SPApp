using System;
using System.Collections.Generic;
using System.Web.Services;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = Microsoft.SharePoint.Client.ListItem;

namespace PIW_SPAppWeb.Pages
{
    public partial class EditDirectPublicationForm : System.Web.UI.Page
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

        public string DocumentURLsFromViewState
        {
            get
            {
                return ViewState[Constants.DocumentURLsKey] != null ? ViewState[Constants.DocumentURLsKey].ToString() : string.Empty;

            }
            set
            {
                ViewState.Add(Constants.DocumentURLsKey, value);
            }
        }

        //variable        
        private string _listItemId;

        //fuction
        static SharePointHelper helper;
        #endregion

        #region Events
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                _listItemId = Page.Request.QueryString["ID"];
                lbUploadedDocumentError.Visible = false;

                helper = new SharePointHelper();


                if (!Page.IsPostBack)
                {
                    if (!string.IsNullOrEmpty(_listItemId))
                    {
                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            DocumentURLsFromViewState = helper.PopulateIssuanceDocumentList(clientContext, _listItemId, rpDocumentList);
                            helper.PopulateSupplementalMailingListDocumentList(clientContext, _listItemId, rpSupplementalMailingListDocumentList, fieldSetSupplementalMailingList);

                            var isCurrentUserAdmin = helper.IsCurrentUserMemberOfGroup(clientContext, Constants.Grp_PIWAdmin);

                            //if current user is piw admin, load the item even if the isActive is false
                            ListItem listItem = helper.GetPiwListItemById(clientContext, _listItemId, isCurrentUserAdmin);
                            if (listItem == null)
                            {
                                helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_ItemNotFound);
                            }
                            else
                            {
                                PopulateFormStatusAndModifiedDateProperties(clientContext, listItem);
                                DisplayListItemInForm(clientContext, listItem);
                                helper.PopulateHistoryList(clientContext, _listItemId, rpHistoryList);
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

                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            ListItem newItem = helper.createNewPIWListItem(clientContext, Constants.PIWList_FormType_DirectPublicationForm);
                            _listItemId = newItem.Id.ToString();

                            //Create subfolder in piwdocuments and mailing list
                            helper.CreatePIWDocumentsSubFolder(clientContext, _listItemId);

                            //history list
                            if (helper.getHistoryListByPIWListID(clientContext, _listItemId).Count == 0)
                            {
                                //Form status must be specified becuae the viewstate hasn't have value
                                helper.CreatePIWListHistory(clientContext, _listItemId, "Workflow Item created", Constants.PIWList_FormStatus_Pending);
                            }
                        }

                        //forward to Edit
                        Response.Redirect(Request.Url + "&ID=" + _listItemId, false);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, Page.Request.Url.OriginalString);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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
                        ListItem listItem = null;
                        if (!SaveData(clientContext, action, ref listItem))
                        {
                            return;
                        }

                        //TODO: Change document and list permission

                        //TODO: send email

                        //Create list history
                        if (helper.getHistoryListByPIWListID(clientContext, _listItemId).Count == 0)
                        {
                            helper.CreatePIWListHistory(clientContext, _listItemId, "Workflow Item created", FormStatus);
                        }
                        else
                        {
                            helper.CreatePIWListHistory(clientContext, _listItemId, "Workflow Item saved", FormStatus);
                        }

                        //TODO: create list history for Mailing Date and FERC Report Completed.

                        //Refresh
                        helper.RefreshPage(Page.Request, Page.Response);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }


        }

        protected void btnInitiatePublication_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Publish;
                //using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                using (var clientContext = new ClientContext(Request.QueryString["SPHostUrl"]))
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //publish
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
                    EPSPublicationHelper epsHelper = new EPSPublicationHelper();
                    epsHelper.Publish(clientContext, files, listItem);


                    //TODO: Change document and list permission

                    //TODO: send email

                    //Create list history
                    helper.CreatePIWListHistory(clientContext, _listItemId, "Workflow Item publication to eLibrary Data Entry initiated", FormStatus);

                    //Refresh
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        protected void btnDeleteConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.Delete;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }

                    //TODO: Change document and list permission

                    //TODO: send email

                    //Create list history
                    helper.CreatePIWListHistory(clientContext, _listItemId, "Workflow Item deleted", FormStatus);

                    //Redirect
                    helper.RedirectToSourcePage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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

                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            string removedFileName = helper.RemoveDocument(clientContext, _listItemId, Constants.PIWDocuments_DocumentLibraryName, e.CommandArgument.ToString());
                            DocumentURLsFromViewState = helper.PopulateIssuanceDocumentList(clientContext, _listItemId, rpDocumentList);
                            //history list
                            helper.CreatePIWListHistory(clientContext, _listItemId, string.Format("Document file {0} removed", removedFileName), FormStatus);
                        }

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
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

                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            string removedFileName = helper.RemoveDocument(clientContext, _listItemId, Constants.PIWDocuments_DocumentLibraryName, e.CommandArgument.ToString());
                            helper.PopulateSupplementalMailingListDocumentList(clientContext, _listItemId, rpSupplementalMailingListDocumentList, fieldSetSupplementalMailingList);

                            //history list
                            helper.CreatePIWListHistory(clientContext, _listItemId, string.Format("Supplemental Mailing List file {0} removed", removedFileName), FormStatus);
                        }

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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
                        var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

                        using (var clientContext = spContext.CreateUserClientContextForSPHost())
                        {
                            var uploadedFileURL = helper.UploadIssuanceDocument(clientContext, fileUpload, _listItemId, rpDocumentList,
                                lbUploadedDocumentError, lbRequiredUploadedDocumentError, FormStatus,
                                ddlSecurityControl.SelectedValue, Constants.PIWDocuments_DocTypeOption_Issuance);
                            if (!string.IsNullOrEmpty(uploadedFileURL)) //only save the document url if the upload is good
                            {
                                DocumentURLsFromViewState = helper.PopulateIssuanceDocumentList(clientContext, _listItemId,
                                    rpDocumentList);
                                //Extract docket numner
                                if (rpDocumentList.Items.Count == 1)
                                //only extract docket number if first document uploaded
                                {
                                    if (!cbIsNonDocket.Checked)
                                    {
                                        tbDocketNumber.Text = helper.ExtractDocket(fileUpload.FileName);
                                    }

                                }
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
                helper.LogError(Context, ex, _listItemId, string.Empty);
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
                        using (var clientContext = SharePointContextProvider.Current.GetSharePointContext(Context).CreateUserClientContextForSPHost())
                        {
                            var uploadResult = helper.UploadSupplementalMailingListDocument(clientContext, supplementalMailingListFileUpload, _listItemId, rpSupplementalMailingListDocumentList,
                                lbSupplementalMailingListUploadError, FormStatus, Constants.PIWDocuments_EPSSecurityLevel_Option_Public, Constants.PIWDocuments_DocTypeOption_SupplementalMailingList);
                            if (uploadResult) //only save the document url if the upload is good
                            {
                                helper.PopulateSupplementalMailingListDocumentList(clientContext, _listItemId, rpSupplementalMailingListDocumentList, fieldSetSupplementalMailingList);
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
                helper.LogError(Context, ex, _listItemId, string.Empty);
                lbSupplementalMailingListUploadError.Text = ex.Message;
                lbSupplementalMailingListUploadError.Visible = true;
            }

        }

        protected void btnReopen_Click(object sender, EventArgs e)
        {
            try
            {
                const enumAction action = enumAction.ReOpen;
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    ListItem listItem = null;
                    if (!SaveData(clientContext, action, ref listItem))
                    {
                        return;
                    }
                    //TODO: Change document and list permission

                    //Create list history
                    helper.CreatePIWListHistory(clientContext, _listItemId, "Workflow Item Re-Opened", FormStatus);

                    //Redirect or Refresh page
                    helper.RefreshPage(Page.Request, Page.Response);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, _listItemId, string.Empty);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }
        }

        #endregion

        #region Save Data
        private bool SaveData(ClientContext clientContext, enumAction action, ref ListItem returnedListItem)
        {
            ListItem listItem = helper.GetPiwListItemById(clientContext, _listItemId, false);

            if (helper.CheckIfListItemChanged(clientContext, listItem, DateTime.Parse(ModifiedDateTime)))
            {
                lbMainMessage.Text = "The form has been changed, please refresh the page";
                lbMainMessage.Visible = true;
                return false;
            }

            //get next form status
            var currentFormStatus = FormStatus;
            DirectPublicationFormWorkflow wf = new DirectPublicationFormWorkflow();
            FormStatus = wf.Execute(FormStatus, action);
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
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_Pending)
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_ReOpen:
                    if (PreviousFormStatus == Constants.PIWList_FormStatus_PublishInitiated)
                    {
                        SaveReOpenInfoAndStatus(clientContext, listItem);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Deleted:
                    //delete item, need to set status 
                    helper.SaveDeleteInfoAndStatus(clientContext, listItem, FormStatus, PreviousFormStatus);
                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    if ((PreviousFormStatus == Constants.PIWList_FormStatus_Pending) || (PreviousFormStatus == Constants.PIWList_FormStatus_ReOpen))
                    {
                        SaveMainPanelAndStatus(clientContext, listItem);
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

                default:
                    throw new Exception(string.Format("Unknown Status:{0}, Previous Status: {1}", FormStatus, PreviousFormStatus));

            }
        }

        private void SaveReOpenInfoAndStatus(ClientContext clientContext, ListItem listItem)
        {
            var piwListInternalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            //clear accession number
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_AccessionNumber]] = string.Empty;

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
                        clientContext.Web.CurrentUser.Title, DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text, listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]]);
                }

            }

            //FOLA Service Required
            if (ddFolaServiceRequired.SelectedIndex != 0)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_FOLAServiceRequired]] = ddFolaServiceRequired.SelectedValue;
            }
            else
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_FOLAServiceRequired]] = string.Empty;
            }


            if (!string.IsNullOrEmpty(FormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            }

            if (!string.IsNullOrEmpty(PreviousFormStatus))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;
            }

            //Document URLs
            if (!string.IsNullOrEmpty(DocumentURLsFromViewState))
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocumentURLs]] = DocumentURLsFromViewState;
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

                //Comment
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]] != null)
                {
                    lbCommentValue.Text = listItem[piwListInteralColumnNames[Constants.PIWList_colName_Comment]].ToString();
                }


                //FOLA Service Required
                if (listItem[piwListInteralColumnNames[Constants.PIWList_colName_FOLAServiceRequired]] != null)
                {
                    if (string.IsNullOrEmpty(listItem[piwListInteralColumnNames[Constants.PIWList_colName_FOLAServiceRequired]].ToString()))
                    {
                        ddFolaServiceRequired.SelectedIndex = 0;
                    }
                    else
                    {
                        ddFolaServiceRequired.SelectedValue = listItem[piwListInteralColumnNames[Constants.PIWList_colName_FOLAServiceRequired]].ToString();
                    }
                }


                //todo: Mail Room - Print

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


        #endregion

        #region Visibility
        public void ControlsVisiblitilyBasedOnStatus(ClientContext clientContext, string previousFormStatus, string formStatus, ListItem listItem)
        {
            var piwlistInternalColumnName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            var documentCategory = string.Empty;
            if (listItem[piwlistInternalColumnName[Constants.PIWList_colName_DocumentCategory]] != null)
            {
                documentCategory = listItem[piwlistInternalColumnName[Constants.PIWList_colName_DocumentCategory]].ToString();
            }

            //SPUser checkoutUser = null;
            var currentUser = clientContext.Web.CurrentUser;
            clientContext.Load(currentUser);

            clientContext.ExecuteQuery();

            switch (formStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_ReOpen:
                    //submit section    
                    EnableMainPanel(true, formStatus);
                    lbMainMessage.Visible = false;

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = helper.IsUserMemberOfGroup(clientContext, currentUser, Constants.Grp_PIWUsers) || helper.IsUserMemberOfGroup(clientContext, currentUser, Constants.Grp_OSECGroupName) || helper.IsUserMemberOfGroup(clientContext, currentUser, Constants.Grp_SecretaryReviewGroupName);

                    btnInitiatePublication.Visible = true;

                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;

                    btnReopen.Visible = false;

                    break;

                case Constants.PIWList_FormStatus_PublishInitiated:
                    //submitter
                    EnableMainPanel(false, formStatus);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "Publication has been initiated for this issuance.";

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = false;
                    fieldsetLegalResourcesReview.Visible = false;

                    //buttons
                    btnSave.Visible = false;
                    btnInitiatePublication.Visible = false;
                    //delete button has the same visibility as Save button
                    btnDelete.Visible = btnSave.Visible;
                    btnReopen.Visible = true;
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    EnableMainPanel(false, formStatus);
                    lbMainMessage.Visible = true;
                    lbMainMessage.Text = "This issuance is available in eLibrary.";

                    //Mailed Room and Legal Resources and Review
                    fieldsetMailedRoom.Visible = true;
                    fieldsetLegalResourcesReview.Visible = true;

                    //buttons
                    btnSave.Visible = true;
                    btnInitiatePublication.Visible = false;
                    btnDelete.Visible = false;
                    btnReopen.Visible = false;
                    break;

                case Constants.PIWList_FormStatus_Deleted:
                    //this status is only viewable by admin
                    EnableMainPanel(false, formStatus);
                    break;
                default:
                    throw new Exception("UnRecognized Form Status: " + formStatus);
                    break;
            }
        }


        private void EnableMainPanel(bool enabled, string FormStatus)
        {
            EnableFileUploadComponent(enabled);
            tbDocketNumber.Enabled = enabled;
            cbIsNonDocket.Enabled = enabled;
            tbAlternateIdentifier.Enabled = enabled;
            tbDescription.Enabled = enabled;

            //only allow document category to be changed if Status is not Edited
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
            tbComment.Enabled = enabled;
            ddFolaServiceRequired.Enabled = enabled;
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

        #endregion



    }

}