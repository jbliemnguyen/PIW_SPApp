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
        private string listItemID;
        private bool isEditForm;
        private enumAction action;
        private bool isMail;

        //fuction
        SharePointHelper helper = null;
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                listItemID = this.Page.Request.QueryString["ID"];

                //Set CitationError to invisible
                //Validation errors may be visible from previous step, need to turn off
                //lbCitationError.Visible = false;

                //lbOSECVerificationError.Visible = false;
                lbUploadedDocumentError.Visible = false;

                listItemID = this.Page.Request.QueryString["ID"];

                if (string.IsNullOrEmpty(listItemID))
                {
                    helper = new SharePointHelper();
                }
                else
                {
                    helper = new SharePointHelper(listItemID);
                    //if there is ID value in URL --> Edit Form
                    isEditForm = true;
                }


                if (!Page.IsPostBack)
                {
                    if (isEditForm)
                    {
                        using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
                        {
                            //TODO: recome comment when working with edit form
                            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

                            //no need - removed
                            //using (var clientContext = spContext.CreateUserClientContextForSPHost())
                            //{
                            //    //Fill initiator people picker field
                            //    clientContext.Load(clientContext.Web, web => web.Title, user => user.CurrentUser);
                            //    clientContext.ExecuteQuery();
                            //    PeoplePickerHelper.FillPeoplePickerValue(hdnWorkflowInitiator, clientContext.Web.CurrentUser);

                            //}

                            PopulateDocumentList(clientContext);
                            //PopulateHistoryList();
                            ListItem listItem = helper.GetPiwListItemById(clientContext, listItemID, false);
                            PopulateFormStatus(clientContext, listItem);
                            //displayListItem(listItem);
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
                        //assign formStatus and previous form status to Pending
                        //Then redirect to EditForm
                        //By doing it, we can attach multiple document to new piwList item under its folder ID

                        var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);
                        using (var clientContext = spContext.CreateUserClientContextForSPHost())
                        {
                            ListItem newItem = helper.createNewPIWListItem(clientContext, Constants.PIWList_FormType_StandardForm);
                            listItemID = newItem.Id.ToString();

                            //Create subfolder in piwdocuments
                            helper.CreatePIWDocumentsSubFolder(clientContext, listItemID);
                        }

                        //forward to Edit
                        Response.Redirect(Request.Url.ToString() + "&ID=" + listItemID);
                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, listItemID, Page.Request.Url.OriginalString);
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
                                Constants.PIWDocuments_DocumentLibraryName, listItemID, fileUpload.FileName,
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
                helper.LogError(Context, ex, listItemID, string.Empty);
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
            System.Data.DataTable table = helper.getAllDocumentsTable(clientContext, listItemID, Constants.PIWDocuments_DocumentLibraryName);

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
                            helper.RemoveDocument(clientContext, listItemID, Constants.PIWDocuments_DocumentLibraryName, e.CommandArgument.ToString());
                            PopulateDocumentList(clientContext);
                        }

                    }
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, exc, listItemID, string.Empty);
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
            using (var clientContext = (SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
            {
                if (ValidFormData())
                {
                    bool isNewlyGeneratedCitationNumber = false;
                    ListItem listItem = helper.GetPiwListItemById(clientContext, listItemID, false);

                    //TODO: check if anyone change the form
                    if (!UpdateFormDataToList(clientContext, listItem, ref isNewlyGeneratedCitationNumber))
                    {
                        return;
                    }
                }
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
            //string errorMessage = string.Empty;
            //helper.CheckDocketNumber(tbDocket.Text.Trim(), ref errorMessage, cbIsCNF.Checked, cbDocketValidationByPass.Checked);

            ////check error message to see if all dockets are valid
            //if (string.IsNullOrEmpty(errorMessage))//dockets are valid
            //{
            //    isValid = isValid & true;
            //    lbDocketValidationError.Visible = false;
            //}
            //else
            //{
            //    isValid = isValid & false;
            //    lbDocketValidationError.Text = errorMessage;
            //    lbDocketValidationError.Visible = true;
            //    //display ByPass Docket Validation Check
            //    if (lbDocketValidationError.Text.Equals(SPListSetting.ATMSRemotingServiceConnectionError))
            //    {
            //        cbDocketValidationByPass.Visible = true;
            //    }
            //}

            return isValid;
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



            //Workflow initiator -  - No need to save becuase this is field is not editable - it is first set when form is created
            //User user = clientContext.Web.EnsureUser();
            //clientContext.Load((user));
            //clientContext.ExecuteQuery();
            //listItem[internalColumnNames[Constants.PIWList_colName_WorkflowInitiator]] = user;



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
            if (!string.IsNullOrEmpty(hdnDocumentOwner.Value))
            {
                List<PeoplePickerUser> users = PeoplePickerHelper.GetValuesFromPeoplePicker(hdnDocumentOwner);

                var userArr = new FieldUserValue[users.Count];
                for (var i = 0; i < users.Count; i++)
                {
                    var newUser = clientContext.Web.EnsureUser(users[i].Login);//ensure user so usr can be added to site if they are not --> receive email
                    clientContext.Load(newUser);
                    clientContext.ExecuteQuery();
                    userArr[i] = new FieldUserValue { LookupId = newUser.Id };
                }
                listItem[internalColumnNames[Constants.PIWList_colName_DocumentOwner]] = userArr;
            }
            else
            {
                listItem[internalColumnNames[Constants.PIWList_colName_DocumentOwner]] = null;
            }

            //notification recipient
            if (!string.IsNullOrEmpty(hdnNotificationRecipient.Value))
            {
                List<PeoplePickerUser> users = PeoplePickerHelper.GetValuesFromPeoplePicker(hdnNotificationRecipient);

                var userArr = new FieldUserValue[users.Count];
                for (var i = 0; i < users.Count; i++)
                {
                    var newUser = clientContext.Web.EnsureUser(users[i].Login);//ensure user so usr can be added to site if they are not --> receive email
                    clientContext.Load(newUser);
                    clientContext.ExecuteQuery();
                    userArr[i] = new FieldUserValue { LookupId = newUser.Id };
                }
                listItem[internalColumnNames[Constants.PIWList_colName_NotificationRecipient]] = userArr;
            }
            else
            {
                listItem[internalColumnNames[Constants.PIWList_colName_NotificationRecipient]] = null;
            }

            //due date
            listItem[internalColumnNames[Constants.PIWList_colName_DueDate]] = tbDueDate.Text;

            //comment
            if (!string.IsNullOrEmpty(tbComment.Text))
            {
                clientContext.Load(clientContext.Web.CurrentUser, user => user.Title);
                clientContext.ExecuteQuery();
                if (listItem[internalColumnNames[Constants.PIWList_colName_Comment]] == null)
                {
                    listItem[internalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("{0} ({1}): {2}", clientContext.Web.CurrentUser.Title,
                        DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text);
                }
                else
                {
                    //append
                    listItem[internalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("{0}<br>{1} ({2}): {3}", listItem[internalColumnNames[Constants.PIWList_colName_Comment]].ToString()
                        , clientContext.Web.CurrentUser.Title,DateTime.Now.ToString("MM/dd/yy H:mm:ss"), tbComment.Text);
                }
                
            }

            //execute query
            listItem.Update();
            clientContext.ExecuteQuery();
            return true;
        }
    }
}