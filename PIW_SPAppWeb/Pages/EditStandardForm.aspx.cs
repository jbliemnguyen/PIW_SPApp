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
                        //TODO: recome comment when working with edit form
                        //string documentURL = PopulateDocumentList();
                        var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);
                        using (var clientContext = spContext.CreateUserClientContextForSPHost())
                        {
                            PopulateDocumentList(clientContext);

                            //Fill initiator people picker field
                            clientContext.Load(clientContext.Web, web => web.Title, user => user.CurrentUser);
                            clientContext.ExecuteQuery();
                            PeoplePickerHelper.FillPeoplePickerValue(hdnWorkflowInitiator, clientContext.Web.CurrentUser);

                        }

                        
                        //PopulateHistoryList();
                        //SPListItem listItem = helper.getPIWListItemByID(listItemID);
                        //PopulateFormStatusAndViewModifiedDate(listItem);
                        //displayListItem(listItem);
                        ////display form visiblility based on form status
                        //FormControlsVisiblitilyBasedOnState(PreviousFormStatus, FormStatus, listItem);
                        ////above method get formStatus from list, store it in viewstate                       
                        //if (FormStatus == enumFormStatus.ReadyForPublishing)
                        //{
                        //    helper.OpenDocument(Page, documentURL);
                        //}
                    }
                    else//new form
                    {
                        //if it is new form
                        //Create new PIWListITem
                        //assign formStatus and previous form status to Pending
                        //Then redirect to EditForm
                        //By doing it, we can attach multiple document to new piwList item under its folder ID
                        FormStatus = Constants.PIWList_FormStatus_Pending;
                        PreviousFormStatus = FormStatus;

                        var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);
                        using (var clientContext = spContext.CreateUserClientContextForSPHost())
                        {
                            ListItem newItem = helper.createNewPIWListItem(clientContext, FormStatus, PreviousFormStatus, Constants.PIWList_FormType_StandardForm);
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
                //helper.LogError(exc, listItemID, Page.Request.Url.OriginalString);
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
                                Constants.PIWDocuments_DocumentLibraryName, listItemID, fileUpload.FileName,
                                ddlSecurityControl.SelectedValue);
                            PopulateDocumentList(clientContext);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
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
            System.Data.DataTable table = helper.getAllDocumentsTable(clientContext,listItemID,Constants.PIWDocuments_DocumentLibraryName);

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
                        var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

                        using (var clientContext = spContext.CreateUserClientContextForSPHost())
                        {
                            helper.RemoveDocument(clientContext,listItemID,Constants.PIWDocuments_DocumentLibraryName,e.CommandArgument.ToString());
                            PopulateDocumentList(clientContext);
                        }
                        
                    }
                }
            }
            catch (Exception exc)
            {
                throw exc;
            }}

        protected void Timer1_Tick(object sender, EventArgs e)
        {
            ///TODO: Only refresh in some certain status. Change the time span to 30 seconds 
            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
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

        }
        }
}