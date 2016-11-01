using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = System.Web.UI.WebControls.ListItem;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;

namespace PIW_SPAppWeb.Pages
{
    public partial class ItemsByPublishedDate : System.Web.UI.Page
    {
        private SharePointHelper helper = new SharePointHelper();

        public string SelectedDocumentCategory
        {
            get
            {
                return ViewState[Constants.SelectedDocumentCategory].ToString();
            }

            set
            {
                ViewState.Add(Constants.SelectedDocumentCategory, value);
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    tbToDate.Text = DateTime.Now.ToShortDateString();
                    tbFromDate.Text = tbToDate.Text;
                    if (Page.Request.QueryString["Office"] != null)
                    {
                        ddProgramOfficeWorkflowInitiator.SelectedValue = Page.Request.QueryString["Office"].ToString();
                    }

                    if (Page.Request.QueryString["FormType"] != null)
                    {
                        formTypeRadioButtonList.SelectedValue = Page.Request.QueryString["FormType"].ToString();
                        formTypeRadioButtonList_SelectedIndexChanged(null, null);
                    }

                    //Run the report in the first time
                    btnRun_OnClick(null, null);

                }
                //displayData();
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, string.Empty, Page.Request.Url.OriginalString);
                if (exc is ServerUnauthorizedAccessException)
                {
                    helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_AccessDenied);
                }
                else
                {
                    helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
                }
            }
        }

        protected void tmrRefresh_Tick(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    displayData(clientContext);
                }
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, string.Empty, Page.Request.Url.OriginalString);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }

        }

        private void displayData(ClientContext clientContext)
        {
            RenderGridView(clientContext);
            lbLastUpdated.Text = "Last Updated: " + DateTime.Now.ToString("g");
        }

        private void RenderGridView(ClientContext clientContext)
        {
            DataTable dataTable = new DataTable();
            DataRow dataRow;
            var piwListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            gridView.Columns.Clear();

            var listItemCollection = getPIWListItem(clientContext);

            if (listItemCollection.Count > 0)
            {
                //create dictionary of selected document category
                Dictionary<string, int> dicDocumentCategory = new Dictionary<string, int>();
                foreach (ListItem item in cblDocumentCategory.Items)
                {
                    if (item.Selected)
                    {
                        // If the item is selected, add the value to the dictionary
                        if (!dicDocumentCategory.ContainsKey(item.Value))
                        {
                            dicDocumentCategory.Add(item.Value, 1);
                        }
                    }
                }



                dataTable.Columns.Add("Docket", typeof(string));
                dataTable.Columns.Add("URL", typeof(string));
                dataTable.Columns.Add("DocumentURL", typeof(string));
                dataTable.Columns.Add("Initiator", typeof(string));
                dataTable.Columns.Add("InitiatorOffice", typeof(string));
                dataTable.Columns.Add("DocumentOwner", typeof(string));
                dataTable.Columns.Add("OwnerOffice", typeof(string));
                dataTable.Columns.Add("FormType", typeof(string));
                dataTable.Columns.Add("CreatedDate", typeof(string));
                dataTable.Columns.Add("PublishedDate", typeof(string));
                dataTable.Columns.Add("MailedDate", typeof(string));
                dataTable.Columns.Add("AccessionNumber", typeof(string));
                dataTable.Columns.Add("PublishedError", typeof(string));



                foreach (var listItem in listItemCollection)
                {

                    if (isCount(listItem, piwListInternalName, dicDocumentCategory))
                    {
                        dataRow = dataTable.Rows.Add();

                        dataRow["Docket"] = listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]] !=
                                            null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]].ToString()
                            : string.Empty;

                        dataRow["URL"] = listItem[piwListInternalName[Constants.PIWList_colName_EditFormURL]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_EditFormURL]].ToString()
                            : string.Empty;

                        var publicDocsURL =
                            listItem[piwListInternalName[Constants.PIWList_colName_PublicDocumentURLs]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_PublicDocumentURLs]].ToString()
                                : string.Empty;
                        var CEIIDocsURL = listItem[piwListInternalName[Constants.PIWList_colName_CEIIDocumentURLs]] !=
                                          null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_CEIIDocumentURLs]].ToString()
                            : string.Empty;
                        var privilegedDocsURL =
                            listItem[piwListInternalName[Constants.PIWList_colName_PrivilegedDocumentURLs]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_PrivilegedDocumentURLs]]
                                    .ToString()
                                : string.Empty;

                        dataRow["DocumentURL"] = helper.getDocumentURLsHTML(publicDocsURL, CEIIDocsURL,
                            privilegedDocsURL, false);


                        dataRow["Initiator"] =
                            listItem[piwListInternalName[Constants.PIWList_colName_WorkflowInitiator]] != null
                                ? ((FieldUserValue)
                                    listItem[piwListInternalName[Constants.PIWList_colName_WorkflowInitiator]])
                                    .LookupValue
                                : string.Empty;

                        dataRow["InitiatorOffice"] =
                            listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]]
                                    .ToString()
                                : string.Empty;

                        dataRow["DocumentOwner"] =
                            listItem[piwListInternalName[Constants.PIWList_colName_DocumentOwner]] != null
                                ? ((FieldUserValue)
                                    listItem[piwListInternalName[Constants.PIWList_colName_DocumentOwner]]).LookupValue
                                : string.Empty;

                        dataRow["OwnerOffice"] =
                            listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeDocumentOwner]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeDocumentOwner]]
                                    .ToString()
                                : string.Empty;


                        //Form Type
                        dataRow["FormType"] = listItem[piwListInternalName[Constants.PIWList_colName_FormType]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_FormType]].ToString()
                            : string.Empty;


                        dataRow["CreatedDate"] =
                            System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem["Created"].ToString()))
                                .ToString();

                        dataRow["PublishedDate"] =
                            listItem[piwListInternalName[Constants.PIWList_colName_PublishedDate]] != null
                                ? System.TimeZone.CurrentTimeZone.ToLocalTime(
                                    DateTime.Parse(
                                        listItem[piwListInternalName[Constants.PIWList_colName_PublishedDate]].ToString()))
                                    .ToString()
                                : string.Empty;

                        dataRow["MailedDate"] =
                            listItem[piwListInternalName[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] != null
                                ? System.TimeZone.CurrentTimeZone.ToLocalTime(
                                    DateTime.Parse(
                                        listItem[
                                            piwListInternalName[Constants.PIWList_colName_PrintReqMailJobCompleteDate]]
                                            .ToString())).ToShortDateString()
                                : string.Empty;

                        dataRow["AccessionNumber"] = getAccessionNumberHtml(listItem, piwListInternalName);

                        dataRow["PublishedError"] =
                            listItem[piwListInternalName[Constants.PIWList_colName_PublishedError]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_PublishedError]].ToString()
                                : string.Empty;

                    }
                }
            }



            //Bound to gridview
            HyperLinkField hyperlinkField;


            string[] urls = new string[1] { "URL" };
            hyperlinkField = new HyperLinkField { HeaderText = "Docket Number", DataTextField = "Docket", };
            hyperlinkField.HeaderStyle.CssClass = "col-xs-2";
            hyperlinkField.ItemStyle.CssClass = "col-xs-2";
            hyperlinkField.DataNavigateUrlFields = urls;
            hyperlinkField.Target = "_blank";
            gridView.Columns.Add(hyperlinkField);


            BoundField boundField = new BoundField
            {
                HeaderText = "Document",
                DataField = "DocumentURL",
                HtmlEncode = false,

            };
            boundField.HeaderStyle.CssClass = "col-xs-2";
            boundField.ItemStyle.CssClass = "col-xs-2";

            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Initiator", DataField = "Initiator" };
            gridView.Columns.Add(boundField);



            boundField = new BoundField { HeaderText = "Initiator Office", DataField = "InitiatorOffice" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Document Owner", DataField = "DocumentOwner" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Owner Office", DataField = "OwnerOffice" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Form Type", DataField = "FormType" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Created Date", DataField = "CreatedDate" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Published Date", DataField = "PublishedDate" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Mailed Date", DataField = "MailedDate" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField
            {
                HeaderText = "Accession Number",
                DataField = "AccessionNumber",
                HtmlEncode = false,
            };
            gridView.Columns.Add(boundField);


            boundField = new BoundField { HeaderText = "PublishedError", DataField = "PublishedError" };
            gridView.Columns.Add(boundField);


            gridView.AutoGenerateColumns = false;
            DataView view = dataTable.DefaultView;

            if (tbToDate.Text.Equals(tbFromDate.Text))
            {
                gridView.AllowPaging = false;
            }
            else
            {
                gridView.AllowPaging = true;
                gridView.PageSize = 25;
            }

            gridView.DataSource = view;
            gridView.DataBind();
        }

        private string getAccessionNumberHtml(Microsoft.SharePoint.Client.ListItem listItem, Dictionary<string, string> piwListInternalName)
        {
            string html = string.Empty;
            string accessionNumber = listItem[piwListInternalName[Constants.PIWList_colName_AccessionNumber]] != null
                ? listItem[piwListInternalName[Constants.PIWList_colName_AccessionNumber]].ToString()
                : string.Empty;

            string previousAccessionNumber = listItem[piwListInternalName[Constants.PIWList_colName_PreviousAccessionNumber]] != null
                ? listItem[piwListInternalName[Constants.PIWList_colName_PreviousAccessionNumber]].ToString()
                : string.Empty;

            string formStatus = listItem[piwListInternalName[Constants.PIWList_colName_FormStatus]] != null
                ? listItem[piwListInternalName[Constants.PIWList_colName_FormStatus]].ToString()
                : string.Empty;

            if (!string.IsNullOrEmpty(accessionNumber))
            {
                if (formStatus.Equals(Constants.PIWList_FormStatus_PublishedToeLibrary))
                {
                    html = string.Format("<span style='color:green;font-weight: bold;' data-toggle='tooltip' title='Available in eLibrary'>{0}</span>", accessionNumber);
                }
                else
                {
                    html = string.Format("<span>{0}</span>", accessionNumber);
                }

            }

            if (!string.IsNullOrEmpty(previousAccessionNumber))
            {
                if (string.IsNullOrEmpty(html))
                {
                    html = "<del>" + previousAccessionNumber + "</del>";
                }
                else
                {
                    html = html + "<br><del>" + previousAccessionNumber + "</del>";
                }
            }

            return html;
        }

        private ListItemCollection getPIWListItem(ClientContext clientContext)
        {
            List piwList = clientContext.Web.Lists.GetByTitle(Constants.PIWListName);
            var piwListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            string office = ddProgramOfficeWorkflowInitiator.SelectedIndex > 0 ? ddProgramOfficeWorkflowInitiator.SelectedValue : string.Empty;
            string fromPublishedDate = DateTime.Parse(tbFromDate.Text).ToString("yyyy-MM-ddTHH:mm:ssZ");
            string toPublishedDate = DateTime.Parse(tbToDate.Text).ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (string.IsNullOrEmpty(office)) //All Office
            {
                CamlQuery query = new CamlQuery();
                var args = new string[]
            {
                piwListInternalName[Constants.PIWList_colName_IsActive],
                piwListInternalName[Constants.PIWList_colName_PublishedDate],
                fromPublishedDate,
                toPublishedDate,

                
            };

                query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>			                                            
				                                        <And>
					                                        <Eq>
						                                        <FieldRef Name='{0}'/>
						                                        <Value Type='Bool'>True</Value>
					                                        </Eq>
					                                        <And>
						                                        <Geq>
                                                                    <FieldRef Name='{1}'/>
                                                                    <Value Type='DateTime' IncludeTimeValue='False'>{2}</Value>
                                                                </Geq>
                                                                <Leq>
                                                                    <FieldRef Name='{1}'/>
                                                                    <Value Type='DateTime' IncludeTimeValue='False'>{3}</Value>
                                                                </Leq>
					                                        </And>
				                                        </And>
		                                            </Where>
		                                            <OrderBy>
			                                            <FieldRef Name='{1}' Ascending='False'/>
		                                            </OrderBy>
	                                            </Query>
                                            </View>", args);

                var piwListItems = piwList.GetItems(query);
                clientContext.Load(piwListItems);
                clientContext.ExecuteQuery();
                return piwListItems;
            }
            else
            {
                CamlQuery query = new CamlQuery();
                var args = new string[]
            {
                piwListInternalName[Constants.PIWList_colName_IsActive],
                piwListInternalName[Constants.PIWList_colName_PublishedDate],
                fromPublishedDate,
                toPublishedDate,
                piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator],
                office
            };

                query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>			                                            
				                                        <And>
                                                            <And>
					                                            <Eq>
						                                            <FieldRef Name='{0}'/>
						                                            <Value Type='Bool'>True</Value>
					                                            </Eq>
					                                            <And>
						                                            <Geq>
                                                                        <FieldRef Name='{1}'/>
                                                                        <Value Type='DateTime' IncludeTimeValue='False'>{2}</Value>
                                                                    </Geq>
                                                                    <Leq>
                                                                        <FieldRef Name='{1}'/>
                                                                        <Value Type='DateTime' IncludeTimeValue='False'>{3}</Value>
                                                                    </Leq>
					                                            </And>
				                                            </And>
                                                            <Eq>
					                                            <FieldRef Name='{4}'/>
					                                            <Value Type='Text'>{5}</Value>
				                                            </Eq>
                                                        </And>                                                        
		                                            </Where>
		                                            <OrderBy>
			                                            <FieldRef Name='{1}' Ascending='False'/>
		                                            </OrderBy>
	                                            </Query>
                                            </View>", args);

                var piwListItems = piwList.GetItems(query);
                clientContext.Load(piwListItems);
                clientContext.ExecuteQuery();
                return piwListItems;
            }
        }

        protected void formTypeRadioButtonList_SelectedIndexChanged(object sender, EventArgs e)
        {

            PopulateDocumentCategory();
        }

        private void PopulateDocumentCategory()
        {
            cblDocumentCategory.Items.Clear();
            ListItem allCheckBox;
            if (string.IsNullOrEmpty(SelectedDocumentCategory))//no saved value for document category --> select All by default
            {
                allCheckBox = new ListItem() {Selected = true, Text = "All", Value = "All"};
            }
            else//value will be populate later
            {
                allCheckBox = new ListItem() {Text = "All", Value = "All" };
            }


            allCheckBox.Attributes.Add("class", "jqueryselector_CategoryAllCheckBox");

            if (formTypeRadioButtonList.SelectedValue.Equals(Constants.PIWList_FormType_StandardForm))
            {

                cblDocumentCategory.Items.Add(allCheckBox);

                //Delegated Letter                
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_DelegatedLetter,
                    "jqueryselector_CategoryCheckBox"));

                //Delegated Notice
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_DelegatedNotice,
                    "jqueryselector_CategoryCheckBox"));

                //Delegated Order
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_DelegatedOrder,
                    "jqueryselector_CategoryCheckBox"));

                //Delegated Errata
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_DelegatedErrata,
                    "jqueryselector_CategoryCheckBox"));

                //OALJ
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_OALJ, "jqueryselector_CategoryCheckBox"));

                //OALJ Errata
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_OALJErrata,
                    "jqueryselector_CategoryCheckBox"));

                //Notice
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_Notice,
                    "jqueryselector_CategoryCheckBox"));

                //Notice Errata
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_NoticeErrata,
                    "jqueryselector_CategoryCheckBox"));
            }
            else if (formTypeRadioButtonList.SelectedValue.Equals(Constants.PIWList_FormType_AgendaForm))
            {

                cblDocumentCategory.Items.Add(allCheckBox);

                //Notational Order
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_NotationalOrder,
                    "jqueryselector_CategoryCheckBox"));

                //Notational Notice
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_NotationalNotice,
                    "jqueryselector_CategoryCheckBox"));

                //Commission Order
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_CommissionOrder,
                    "jqueryselector_CategoryCheckBox"));

                //Consent
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_Consent,
                    "jqueryselector_CategoryCheckBox"));

                //Errata
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_Errata,
                    "jqueryselector_CategoryCheckBox"));

                //Tolling Order
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_TollingOrder,
                    "jqueryselector_CategoryCheckBox"));

                //Sunshine Notice
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_SunshineNotice,
                    "jqueryselector_CategoryCheckBox"));

                //Notice of Action Taken
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_NoticeofActionTaken,
                    "jqueryselector_CategoryCheckBox"));
            }
            else if (formTypeRadioButtonList.SelectedValue.Equals(Constants.PIWList_FormType_DirectPublicationForm))
            {

                cblDocumentCategory.Items.Add(allCheckBox);

                //Chairman Statement
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_ChairmanStatement,
                    "jqueryselector_CategoryCheckBox"));

                //Commissioner Statement
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_CommissionerStatement,
                    "jqueryselector_CategoryCheckBox"));

                //Delegated Letter - existing in Standard Form
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_DelegatedLetter,
                    "jqueryselector_CategoryCheckBox"));

                //EA
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_EA,
                    "jqueryselector_CategoryCheckBox"));

                //EIS
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_EIS,
                    "jqueryselector_CategoryCheckBox"));

                //Errata - existing in Agenda
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_Errata,
                    "jqueryselector_CategoryCheckBox"));

                //Inspection Report
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_InspectionReport,
                    "jqueryselector_CategoryCheckBox"));

                //Memo
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_Memo,
                    "jqueryselector_CategoryCheckBox"));

                //News Release
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_NewsRelease,
                    "jqueryselector_CategoryCheckBox"));

                //Notice of Action Taken
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_NoticeofActionTaken,
                    "jqueryselector_CategoryCheckBox"));

                //Project Update
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_ProjectUpdate,
                    "jqueryselector_CategoryCheckBox"));

                //Sunshine Act Meeting Notice
                cblDocumentCategory.Items.Add(createNewCheckBox(Constants.PIWList_DocCat_SunshineActMeetingNotice,
                    "jqueryselector_CategoryCheckBox"));
            }
            

            loadSelectedDocumentCategory();
        }

        public ListItem createNewCheckBox(string value, string jqueryClass)
        {
            ListItem checkBox = new ListItem()
            {
                Text = value,
                Value = value,
            };
            checkBox.Attributes.Add("class", jqueryClass);

            return checkBox;
        }

        protected void btnRun_OnClick(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    saveSelectedDocumentCategory();
                    displayData(clientContext);
                    PopulateDocumentCategory();
                }

            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, string.Empty, Page.Request.Url.OriginalString);
                if (exc is ServerUnauthorizedAccessException)
                {
                    helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_AccessDenied);
                }
                else
                {
                    helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
                }
            }
        }

        private bool isCount(Microsoft.SharePoint.Client.ListItem listItem, Dictionary<string, string> piwListInternalName, Dictionary<string, int> dicDocumentCategory)
        {
            string formType = listItem[piwListInternalName[Constants.PIWList_colName_FormType]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_FormType]].ToString()
                            : string.Empty; ;

            string documentCategory = listItem[piwListInternalName[Constants.PIWList_colName_DocumentCategory]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_DocumentCategory]].ToString()
                            : string.Empty;

            bool formTypeMatch = false;
            bool documentCategoryMatch = false;

            //Check the Form Type
            if (formTypeRadioButtonList.SelectedIndex == 0)//All formtype, no need to check any else, because no document category can be selected
            {
                return true;
            }
            else
            {
                formTypeMatch = formTypeRadioButtonList.SelectedValue.Equals(formType);
            }

            //Check the document category
            //Only check document category when form type is matched
            if (formTypeMatch)
            {
                if (cblDocumentCategory.SelectedIndex == 0)//All document cateogry
                {
                    documentCategoryMatch = true;
                }
                else
                {

                    documentCategoryMatch = dicDocumentCategory.ContainsKey(documentCategory);
                }
            }


            return (formTypeMatch && documentCategoryMatch);
        }

        private void saveSelectedDocumentCategory()
        {
            string selectedDocumentCategory = string.Empty;

            Dictionary<string, int> dicDocumentCategory = new Dictionary<string, int>();
            foreach (ListItem item in cblDocumentCategory.Items)
            {
                if (item.Selected)
                {
                    if (string.IsNullOrEmpty(selectedDocumentCategory))
                    {
                        selectedDocumentCategory = item.Value;
                    }
                    else
                    {
                        selectedDocumentCategory = selectedDocumentCategory + Constants.DocumentURLsSeparator + item.Value;
                    }
                }
            }

            SelectedDocumentCategory = selectedDocumentCategory;//save value to viewstate
        }

        private void loadSelectedDocumentCategory()
        {
            //because the property class of listitem does not saved and reload after post back, 
            //we need to manuall load it
            var selectedDocumentsCatArr = SelectedDocumentCategory.Split(new string[] { Constants.DocumentURLsSeparator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var selectedDocCat in selectedDocumentsCatArr)
            {
                foreach (ListItem item in cblDocumentCategory.Items)
                {
                    if (selectedDocCat.Equals(item.Value))
                    {
                        item.Selected = true;
                        break;
                    }
                }
            }

            
        }
    }


}