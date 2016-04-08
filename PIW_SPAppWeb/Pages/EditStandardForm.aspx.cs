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
using File = Microsoft.SharePoint.Client.File;
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
        #endregion
        //variable        
        private string _listItemId;
        private bool _isEditForm;
        private enumAction action;
        private bool isMail;

        //fuction
        static SharePointHelper helper = null;
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
                            PopulateFormStatus(clientContext, listItem);
                            DisplayListItemInForm(clientContext, listItem);
                            ////display form visiblility based on form status
                            //FormControlsVisiblitilyBasedOnState(PreviousFormStatus, FormStatus, listItem);
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

        private void PopulateFormStatus(ClientContext clientContext, ListItem listItem)
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

            }
        }

        private void PopulateHistoryList()
        {
            throw new NotImplementedException();
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

                }

            }
            catch (Exception ex)
            {
                helper.LogError(Context, ex, _listItemId, string.Empty);
                lbUploadedDocumentError.Text = ex.Message.ToString();
                lbUploadedDocumentError.Visible = true;
            }

        }

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

        protected void Timer1_Tick(object sender, EventArgs e)
        {
            ///TODO: Only refresh in some certain status. Change the time span to 30 seconds 
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

        protected void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                {
                    if (ValidFormData())
                    {
                        bool isNewlyGeneratedCitationNumber = false;
                        ListItem listItem = helper.GetPiwListItemById(clientContext, _listItemId, false);

                        //TODO: check if anyone change the form
                        if (!UpdateFormDataToList(clientContext, listItem, ref isNewlyGeneratedCitationNumber))
                        {
                            return;
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

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            //Exception exc = new Exception("Test exception");
            //helper.LogError(Context, exc, listItemID, "test.aspx");
        }

        private bool ValidFormData()
        {
            bool isValid = true;

            //Check if there is a uploaded document
            if (rpDocumentList.Items.Count < 1)//validation fails
            {
                isValid = isValid & false;
                lbRequiredUploadedDocumentError.Visible = true;
            }
            else
            {
                //check if at least 1 public item is 
                isValid = isValid & true;
                lbRequiredUploadedDocumentError.Visible = false;
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
        //public static string ValidateDocketNumber(string docketNumber)
        {
            //string errorMessage = docketNumber;
            string errorMessage = string.Empty;
            helper.CheckDocketNumber(docketNumber.Trim(), ref errorMessage, isCNF, docketValidationByPass);
            return errorMessage;
        }

        private bool UpdateFormDataToList(ClientContext clientContext, ListItem listItem, ref bool isNewlyGeneratedCitationNumber)
        {
            bool isSuccessSave = true;

            switch (FormStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                    //Save Main panel data
                    isSuccessSave = SaveMainPanelData(clientContext, listItem);
                    break;

            }

            return isSuccessSave;
        }

        private bool SaveMainPanelData(ClientContext clientContext, ListItem listItem)
        {
            var internalColumnNames = helper.getInternalColumnNames(clientContext, Constants.PIWListName);

            //each update has its own Execute query. If we set the field of the list item, then execute the ExecuteQuery to populate data
            //without calling the listitem.update, then the changes is lost 
            //We need to prrepare all the necessary data before update all fields without calling any ExecuteQuery in middle of it

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
            listItem[internalColumnNames[Constants.PIWList_colName_IsActive]] = true;

            //Save Docket Number
            listItem[internalColumnNames[Constants.PIWList_colName_DocketNumber]] = tbDocketNumber.Text.Trim();

            //IsCNF
            listItem[internalColumnNames[Constants.PIWList_colName_IsCNF]] = cbIsCNF.Checked;

            //Non-Docketed
            listItem[internalColumnNames[Constants.PIWList_colName_IsNonDocket]] = cbIsNonDocket.Checked;

            //By Pass Docket Validation
            //listItem[internalColumnNames[Constants.PIWList_colName_ByPassDocketValidation]] = cbby.Checked;

            //Description
            listItem[internalColumnNames[Constants.PIWList_colName_Description]] = tbDescription.Text.Trim();


            //alternate identifier
            listItem[internalColumnNames[Constants.PIWList_colName_AlternateIdentifier]] = tbAlternateIdentifier.Text.Trim();

            //instruction for osec
            listItem[internalColumnNames[Constants.PIWList_colName_InstructionForOSEC]] = tbInstruction.Text.Trim();

            //Federal register
            listItem[internalColumnNames[Constants.PIWList_colName_FederalRegister]] = cbFederalRegister.Checked;

            //document category
            if (ddDocumentCategory.SelectedIndex != 0)
            {
                listItem[internalColumnNames[Constants.PIWList_colName_DocumentCategory]] = ddDocumentCategory.SelectedValue;
            }
            else
            {
                listItem[internalColumnNames[Constants.PIWList_colName_DocumentCategory]] = string.Empty;
            }

            //program office(wokflow initiator)
            if (ddProgramOfficeWorkflowInitiator.SelectedIndex != 0)
            {
                listItem[internalColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]] = ddProgramOfficeWorkflowInitiator.SelectedValue;
            }
            else
            {
                listItem[internalColumnNames[Constants.PIWList_colName_ProgramOfficeWFInitator]] = string.Empty;
            }



            //Workflow initiator - set by default to current login value when form is created


            //program office(document owner)
            //program office(wokflow initiator)
            if (ddProgramOfficeDocumentOwner.SelectedIndex != 0)
            {
                listItem[internalColumnNames[Constants.PIWList_colName_ProgramOfficeDocumentOwner]] = ddProgramOfficeDocumentOwner.SelectedValue;
            }
            else
            {
                listItem[internalColumnNames[Constants.PIWList_colName_ProgramOfficeDocumentOwner]] = string.Empty;
            }

            //document owner
            listItem[internalColumnNames[Constants.PIWList_colName_DocumentOwner]] = documentOwners;

            //notification recipient
            listItem[internalColumnNames[Constants.PIWList_colName_NotificationRecipient]] = notificationRecipients;

            //due date
            listItem[internalColumnNames[Constants.PIWList_colName_DueDate]] = tbDueDate.Text;

            //comment
            if (!string.IsNullOrEmpty(tbComment.Text))
            {
                if (listItem[internalColumnNames[Constants.PIWList_colName_Comment]] == null)
                {
                    listItem[internalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("{0} ({1}): {2}", clientContext.Web.CurrentUser.Title,
                        DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text);
                }
                else
                {
                    //append
                    listItem[internalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("{0} ({1}): {2}<br>{3}",
                        clientContext.Web.CurrentUser.Title, DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text, listItem[internalColumnNames[Constants.PIWList_colName_Comment]].ToString());
                }

            }

            //execute query
            listItem.Update();
            clientContext.ExecuteQuery();
            return true;
        }
    }
}