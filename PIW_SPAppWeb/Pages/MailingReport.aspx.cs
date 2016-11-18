using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = Microsoft.SharePoint.Client.ListItem;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;

namespace PIW_SPAppWeb.Pages
{
    public partial class MailingRequired : System.Web.UI.Page
    {
        #region variables
        SharePointHelper helper = new SharePointHelper();
        private const string col_Docket = "Docket";
        private const string col_EditFormURL = "EditFormURL";
        private const string col_DocumentURL = "DocumentURL";
        private const string col_Initiator = "Initiator";
        private const string col_DocumentOwner = "Document Owner";
        private const string col_CreatedDate = "Created Date";
        private const string col_PublishedDate = "Published Date";
        private const string col_PrintRequestedDate = "Print Requested Date";
        private const string col_MailedDate = "Mailed Date";
        private const string col_NumberOfPages = "Number Of Pages";
        private const string col_NumberOfCopies = "Number Of Copies";
        private const string col_PrintReqURL = "PrintReqURL";

        #endregion
        #region Events
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (cbOnlyDisplayItemsNotMailed.Checked)
                {
                    lbReportName.Text = "OEP Mailing Pending";
                }
                else
                {
                    lbReportName.Text = "OEP Mailing Required";
                }
                    

                if (!Page.IsPostBack)
                {
                    tbToDate.Text = DateTime.Now.ToShortDateString();
                    tbFromDate.Text = DateTime.Now.AddDays(-15).ToShortDateString();

                    //Run the report in the first time
                    btnRunReport_OnClick(null, null);

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

        protected void gridView_OnPageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gridView.PageIndex = e.NewPageIndex;
            btnRunReport_OnClick(null, null);
        }

        protected void btnRunReport_OnClick(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    RenderGridView(clientContext);
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
        #endregion

        #region Utils
        private void RenderGridView(ClientContext clientContext)
        {
            DataTable dataTable = new DataTable();
            DataRow dataRow;
            var piwListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            gridView.Columns.Clear();

            var listItemCollection = getPIWListItem(clientContext);

            if (listItemCollection.Count > 0)
            {
                dataTable.Columns.Add(col_Docket, typeof(string));
                dataTable.Columns.Add(col_EditFormURL, typeof(string));
                dataTable.Columns.Add(col_DocumentURL, typeof(string));
                dataTable.Columns.Add(col_Initiator, typeof(string));
                dataTable.Columns.Add(col_DocumentOwner, typeof(string));
                dataTable.Columns.Add(col_CreatedDate, typeof(string));
                dataTable.Columns.Add(col_PublishedDate, typeof(string));
                dataTable.Columns.Add(col_PrintRequestedDate, typeof(string));
                dataTable.Columns.Add(col_MailedDate, typeof(string));
                dataTable.Columns.Add(col_NumberOfPages, typeof(string));
                dataTable.Columns.Add(col_NumberOfCopies, typeof(string));
                dataTable.Columns.Add(col_PrintReqURL, typeof(string));

                foreach (var listItem in listItemCollection)
                {

                    if (isCount(listItem, piwListInternalName))
                    {
                        dataRow = dataTable.Rows.Add();

                        dataRow[col_Docket] = listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]].ToString()
                            : string.Empty;

                        dataRow[col_EditFormURL] = listItem[piwListInternalName[Constants.PIWList_colName_EditFormURL]] != null
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

                        dataRow[col_DocumentURL] = helper.getDocumentURLsHTML(publicDocsURL, CEIIDocsURL,
                            privilegedDocsURL, false);

                        dataRow[col_Initiator] =
                            listItem[piwListInternalName[Constants.PIWList_colName_WorkflowInitiator]] != null
                                ? ((FieldUserValue)listItem[piwListInternalName[Constants.PIWList_colName_WorkflowInitiator]]).LookupValue
                                : string.Empty;
                        
                        //document owners
                        string documentOwnersStr = string.Empty;
                        if (listItem[piwListInternalName[Constants.PIWList_colName_DocumentOwner]] != null)
                        {
                            var documentOwners = (FieldUserValue[])listItem[piwListInternalName[Constants.PIWList_colName_DocumentOwner]];

                            foreach (var documentOwner in documentOwners)
                            {
                                if (string.IsNullOrEmpty(documentOwnersStr))
                                {
                                    documentOwnersStr = documentOwner.LookupValue;
                                }
                                else
                                {
                                    documentOwnersStr = documentOwnersStr + ", " + documentOwner.LookupValue;
                                }
                            }
                        }
                        dataRow[col_DocumentOwner] = documentOwnersStr;

                        dataRow[col_CreatedDate] =
                            listItem[piwListInternalName["Created"]] != null
                                ? System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem[piwListInternalName["Created"]].ToString())).ToString()
                                : string.Empty;
                        
                        dataRow[col_PublishedDate] =
                            listItem[piwListInternalName[Constants.PIWList_colName_PublishedDate]] != null
                                ? System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem[piwListInternalName[Constants.PIWList_colName_PublishedDate]].ToString())).ToString()
                                : string.Empty;

                        dataRow[col_PrintRequestedDate] =
                            listItem[piwListInternalName[Constants.PIWList_colName_PrintReqDateRequested]] != null
                                ? System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem[piwListInternalName[Constants.PIWList_colName_PrintReqDateRequested]].ToString())).ToString()
                                : string.Empty;


                        dataRow[col_PrintReqURL] =
                            listItem[piwListInternalName[Constants.PIWList_colName_PrintReqFormURL]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_PrintReqFormURL]].ToString()
                                : string.Empty;

                        dataRow[col_MailedDate] =
                            listItem[piwListInternalName[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] != null
                                ? System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem[piwListInternalName[Constants.PIWList_colName_PrintReqMailJobCompleteDate]].ToString())).ToString()
                                : string.Empty;

                        dataRow[col_NumberOfPages] =
                            listItem[piwListInternalName[Constants.PIWList_colName_NumberOfPublicPages]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_NumberOfPublicPages]].ToString()
                                : string.Empty;


                        if ((listItem[piwListInternalName[Constants.PIWList_colName_PrintReqNumberofCopies]] != null) &&
                            (!listItem[piwListInternalName[Constants.PIWList_colName_PrintReqNumberofCopies]].ToString().Equals("0")))
                        {
                            dataRow[col_NumberOfCopies] =
                                listItem[piwListInternalName[Constants.PIWList_colName_PrintReqNumberofCopies]].ToString();
                        }
                        else
                        {
                            dataRow[col_NumberOfCopies] = string.Empty;
                        }
                        
                        
                    }
                }

                //bound to grid view
                gridView.Columns.Clear();

                HyperLinkField hyperlinkField;
                BoundField boundField;


                string[] urls = new string[1] { col_EditFormURL };
                hyperlinkField = new HyperLinkField { HeaderText = col_Docket, DataTextField = col_Docket, };
                hyperlinkField.HeaderStyle.CssClass = "col-md-2";
                hyperlinkField.ItemStyle.CssClass = "col-md-2";
                hyperlinkField.DataNavigateUrlFields = urls;
                hyperlinkField.Target = "_blank";
                gridView.Columns.Add(hyperlinkField);


                boundField = new BoundField
                {
                    HeaderText = "Document",
                    DataField = col_DocumentURL,
                    HtmlEncode = false,

                };
                boundField.HeaderStyle.CssClass = "col-md-2";
                boundField.ItemStyle.CssClass = "col-md-2";
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_Initiator, DataField = col_Initiator };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_DocumentOwner, DataField = col_DocumentOwner };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_CreatedDate, DataField = col_CreatedDate };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_PublishedDate, DataField = col_PublishedDate };
                gridView.Columns.Add(boundField);

                urls = new string[1] { col_PrintReqURL };
                hyperlinkField = new HyperLinkField { HeaderText = col_PrintRequestedDate, DataTextField = col_PrintRequestedDate, };
                hyperlinkField.HeaderStyle.CssClass = "col-md-2";
                hyperlinkField.ItemStyle.CssClass = "col-md-2";
                hyperlinkField.DataNavigateUrlFields = urls;
                hyperlinkField.Target = "_blank";
                gridView.Columns.Add(hyperlinkField);

                boundField = new BoundField { HeaderText = col_MailedDate, DataField = col_MailedDate };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_NumberOfPages, DataField = col_NumberOfCopies };
                gridView.Columns.Add(boundField);
                
                gridView.AutoGenerateColumns = false;
                DataView view = dataTable.DefaultView;
                gridView.PageSize = 100;
                gridView.AllowPaging = true;
                gridView.DataSource = view;
                gridView.DataBind();

                //display updated time
                lbLastUpdated.Text = "Last Updated: " + DateTime.Now.ToString("g");

            }
        }

        private bool isCount(ListItem listItem, Dictionary<string, string> piwListInternalName)
        {
            bool result = false;
            //count if there is print req submit
            if (listItem[piwListInternalName[Constants.PIWList_colName_PrintReqFormURL]] != null)
            {
                result = true;
            }
            else
            {
                //count if it is standard form
                if (listItem[piwListInternalName[Constants.PIWList_colName_FormType]].ToString()
                    .Equals(Constants.PIWList_FormType_StandardForm))
                {
                    result = true;
                }
                else
                {
                    //count if direct pub has "FOLA Service Required" is set
                    if (listItem[piwListInternalName[Constants.PIWList_colName_FormType]].ToString()
                        .Equals(Constants.PIWList_FormType_DirectPublicationForm))
                    {
                        var folaServiceRequired = listItem[piwListInternalName[Constants.PIWList_colName_FOLAServiceRequired]] != null
                                        ? listItem[piwListInternalName[Constants.PIWList_colName_FOLAServiceRequired]].ToString()
                                        : string.Empty;
                        if (folaServiceRequired.Equals("Yes"))
                        {
                            result = true;
                        }
                    }    
                }
            }

            if (result && cbOnlyDisplayItemsNotMailed.Checked)
            {
                //if we should count, then check if it has mailed date or not
                //if OnlyDisplayItemsNotMailed is checked, we only display item not mailed
                //it means item with maileddate is not counted.
                var hasMailedDate = listItem[piwListInternalName[Constants.PIWList_colName_PrintReqMailJobCompleteDate]] != null;
                if (hasMailedDate)
                {
                    result = false;
                }
            }

            

            return result;

        }


        private ListItemCollection getPIWListItem(ClientContext clientContext)
        {
            List piwList = clientContext.Web.Lists.GetByTitle(Constants.PIWListName);
            var piwListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);


            string fromPublishedDate = DateTime.Parse(tbFromDate.Text).ToString("yyyy-MM-ddTHH:mm:ssZ");
            string toPublishedDate = DateTime.Parse(tbToDate.Text).ToString("yyyy-MM-ddTHH:mm:ssZ");
            string office = "OEP";

            CamlQuery query = new CamlQuery();


            var args = new string[]
                {
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
                                                                <Geq>
                                                                    <FieldRef Name='{0}'/>
                                                                    <Value Type='DateTime' IncludeTimeValue='False'>{1}</Value>
                                                                </Geq>
                                                                <Leq>
                                                                    <FieldRef Name='{0}'/>
                                                                    <Value Type='DateTime' IncludeTimeValue='False'>{2}</Value>
                                                                </Leq>
                                                            </And>
                                                            <Eq>
                                                                <FieldRef Name='{3}'/>
                                                                <Value Type='Text'>{4}</Value>
                                                            </Eq>
                                                        </And>                                                    
                                                    </Where>
                                                    <OrderBy>
                                                      <FieldRef Name='{0}' Ascending='False'/>
                                                    </OrderBy>
                                                </Query>
                                            </View>", args);

            var piwListItems = piwList.GetItems(query);
            clientContext.Load(piwListItems);
            clientContext.ExecuteQuery();
            return piwListItems;
        }
        #endregion
        #region requirements
//        Report 1: OEP Mailing Required
//Purpose: This report will show ALL of OEP’s items that require mailing and enable us to see all of the steps involved for all issuances over a given time period.
//Criteria:
//•         Filter by date range (like the existing Print Requisition Report)
//•         Only OEP Items
//•         All OEP documents submitted via the Standard Form
//•         All OEP Direct Publication Documents where the Print Req Required box is checked
//•         Any other OEP item where a print request was submitted

//Column Headers to include:
//•         Docket Number (Link to PIW FORM)
//•         Document Title (Link to Document)
//•         Initiator
//•         Document Owner
//•         Submitted Date- Date and time data for ACTION- Workflow Item Submitted
//•         Publish Date- Date and time data for ACTION- Workflow Item Publication to eLibrary Data Entry Initiated
//•         Print Requested- Date and time data for ACTION- Print Requisition Form submitted
//•         Mailed Date- Date and time data for ACTION- Mailing of issuance Recorded in PIW (Link to Print Request Form)
//•         # Pages- same as on Print Requisition Report
//•         # Copies- same as on Print Requisition Report


//Report 2: OEP Mailing Pending
//Purpose: This report will allow OEP to quickly see which items have not been mailed.  
//Criteria and Column Headers: Same as above except no date filter and only show those items that have not been mailed. It does not matter whether the other actions have been completed-but if the item has not been marked as mailed - it should appear on this report.

        #endregion
    }
}