﻿using System;
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

        protected void Page_Load(object sender, EventArgs e)
        {
            //enable controls
            tbPrintJobCompletedDate.Enabled = cbPrintJobCompleted.Checked;
            tbMailJobCompletedDate.Enabled = cbMailJobCompleted.Checked;

            try
            {
                _listItemId = Page.Request.QueryString["ID"];
                helper = new SharePointHelper();

                if (!Page.IsPostBack)
                {
                    if (!string.IsNullOrEmpty(_listItemId))
                    {
                        using (var clientContext =(SharePointContextProvider.Current.GetSharePointContext(Context)).CreateUserClientContextForSPHost())
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
                                PopulateModifiedDateProperties(clientContext, listItem);
                                DisplayListItemInForm(clientContext, listItem);
                                //helper.PopulateHistoryList(clientContext, _listItemId, rpHistoryList);

                                //display form visiblility based on form status
                                //ControlsVisiblitilyBasedOnStatus(clientContext, PreviousFormStatus, FormStatus, listItem);

                                //todo: open documents if status is ready for published
                                ////above method get formStatus from list, store it in viewstate                       
                                //if (FormStatus == enumFormStatus.ReadyForPublishing)
                                //{
                                //    helper.OpenDocument(Page, documentURL);
                                //}
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
                    tbDocketNumber.Text =
                        listItem[piwListInteralColumnNames[Constants.PIWList_colName_DocketNumber]].ToString();
                }
            }
        }

        public
            void PopulateModifiedDateProperties(ClientContext clientContext, ListItem listItem)
        {
            var internalColumnNames = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            

            //Modified Date
            if (listItem[internalColumnNames[Constants.PIWList_colName_Modified]] != null)
            {
                ModifiedDateTime = listItem[internalColumnNames[Constants.PIWList_colName_Modified]].ToString();
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
    }
}