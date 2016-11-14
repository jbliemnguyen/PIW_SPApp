using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = System.Web.UI.WebControls.ListItem;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;

namespace PIW_SPAppWeb.Pages
{
    public partial class LegalResourceReviewPendingItems : System.Web.UI.Page
    {
        #region variables, properties
        SharePointHelper helper = new SharePointHelper();
        private string previousRowFormStatus = string.Empty;
        int intSubTotalIndex = 1;

        private const string col_Docket = "Docket";
        private const string col_EditFormURL = "EditFormURL";
        private const string col_DocumentURL = "DocumentURL";
        //private const string col_Document = "Document";
        private const string col_CitationNumber = "Citation Number";
        private const string col_Initiator = "Initiator";
        private const string col_InitiatorOffice = "Initiator Office";
        private const string col_DocumentOwner = "Document Owner";
        private const string col_Description = "Description";
        private const string col_PublishedDate = "Published Date";
        private const string col_FormType = "Form Type";
        #endregion

        #region Events
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                
                if (!Page.IsPostBack)
                {
                    tbToDate.Text = DateTime.Now.ToShortDateString();
                    tbFromDate.Text = DateTime.Now.AddDays(-30).ToShortDateString();


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

        protected void btnRun_OnClick(object sender, EventArgs e)
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

        protected void gridView_OnRowCreated(object sender, GridViewRowEventArgs e)
        {
            if (DataBinder.Eval(e.Row.DataItem, col_FormType) != null)
            {
                string formType = DataBinder.Eval(e.Row.DataItem, col_FormType).ToString();
                if (!previousRowFormStatus.Equals(formType))
                {
                    GridView StandardFormgrdView = (GridView)sender;
                    GridViewRow row = new GridViewRow(0, 0, DataControlRowType.DataRow, DataControlRowState.Insert);
                    TableCell cell = new TableCell
                    {
                        Text = "Form Type: " + formType,
                        ColumnSpan = 10,
                        CssClass = "GroupHeaderStyle"
                    };
                    row.Cells.Add(cell);
                    StandardFormgrdView.Controls[0].Controls.AddAt(e.Row.RowIndex + intSubTotalIndex, row);
                    intSubTotalIndex++;
                }

                previousRowFormStatus = formType;
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
                dataTable.Columns.Add(col_CitationNumber, typeof(string));
                dataTable.Columns.Add(col_Initiator, typeof(string));
                dataTable.Columns.Add(col_InitiatorOffice, typeof(string));
                dataTable.Columns.Add(col_DocumentOwner, typeof(string));
                dataTable.Columns.Add(col_Description, typeof(string));
                dataTable.Columns.Add(col_PublishedDate, typeof(string));
                dataTable.Columns.Add(col_FormType, typeof(string));

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


                        dataRow[col_CitationNumber] = listItem[piwListInternalName[Constants.PIWList_colName_CitationNumber]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_CitationNumber]].ToString()
                            : string.Empty;

                        dataRow[col_Initiator] =
                            listItem[piwListInternalName[Constants.PIWList_colName_WorkflowInitiator]] != null
                                ? ((FieldUserValue)listItem[piwListInternalName[Constants.PIWList_colName_WorkflowInitiator]]).LookupValue
                                : string.Empty;

                        dataRow[col_InitiatorOffice] =
                            listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString()
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


                        dataRow[col_Description] = listItem[piwListInternalName[Constants.PIWList_colName_Description]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_Description]].ToString()
                            : string.Empty;

                        dataRow[col_PublishedDate] =
                            listItem[piwListInternalName[Constants.PIWList_colName_PublishedDate]] != null
                                ? System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem[piwListInternalName[Constants.PIWList_colName_PublishedDate]].ToString())).ToString()
                                : string.Empty;

                        //form type: OSEC Forms = Agenda Form, otherwise: Program Office Forms
                        string formType = listItem[piwListInternalName[Constants.PIWList_colName_FormType]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_FormType]].ToString() : string.Empty;
                        if (formType.Equals(Constants.PIWList_FormType_AgendaForm))
                        {
                            dataRow[col_FormType] = "OSEC Forms";
                        }
                        else
                        {
                            dataRow[col_FormType] = "Program Office Forms";
                        }
                    }
                }

                //bound to grid view
                gridView.Columns.Clear();

                HyperLinkField hyperlinkField;


                string[] urls = new string[1] { col_EditFormURL };
                hyperlinkField = new HyperLinkField { HeaderText = col_Docket, DataTextField = col_Docket, };
                hyperlinkField.HeaderStyle.CssClass = "col-md-2";
                hyperlinkField.ItemStyle.CssClass = "col-md-2";
                hyperlinkField.DataNavigateUrlFields = urls;
                hyperlinkField.Target = "_blank";
                gridView.Columns.Add(hyperlinkField);


                BoundField boundField = new BoundField
                {
                    HeaderText = "Document",
                    DataField = col_DocumentURL,
                    HtmlEncode = false,

                };
                boundField.HeaderStyle.CssClass = "col-md-2";
                boundField.ItemStyle.CssClass = "col-md-2";
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_CitationNumber, DataField = col_CitationNumber };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_Initiator, DataField = col_Initiator };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_InitiatorOffice, DataField = col_InitiatorOffice };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_DocumentOwner, DataField = col_DocumentOwner };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_Description, DataField = col_Description };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = "Initiator", DataField = "Initiator" };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = col_PublishedDate, DataField = col_PublishedDate };
                gridView.Columns.Add(boundField);

                gridView.AutoGenerateColumns = false;
                DataView view = dataTable.DefaultView;
                gridView.DataSource = view;
                gridView.DataBind();

                //display updated time
                lbLastUpdated.Text = "Last Updated: " + DateTime.Now.ToString("g");

            }
        }

        private bool isCount(Microsoft.SharePoint.Client.ListItem listItem, Dictionary<string, string> piwListInternalName)
        {
            return true;
        }

        private ListItemCollection getPIWListItem(ClientContext clientContext)
        {
            List piwList = clientContext.Web.Lists.GetByTitle(Constants.PIWListName);
            var piwListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);


            string fromPublishedDate = DateTime.Parse(tbFromDate.Text).ToString("yyyy-MM-ddTHH:mm:ssZ");
            string toPublishedDate = DateTime.Parse(tbToDate.Text).ToString("yyyy-MM-ddTHH:mm:ssZ");

            CamlQuery query = new CamlQuery();


            var args = new string[]
                {
                    piwListInternalName[Constants.PIWList_colName_IsActive],
                    piwListInternalName[Constants.PIWList_colName_LegalResourcesAndReviewGroupCompleteDate],
                    piwListInternalName[Constants.PIWList_colName_PublishedDate],
                    fromPublishedDate,
                    toPublishedDate,
                    piwListInternalName[Constants.PIWList_colName_FormType],

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
                                                                <IsNull>
                                                                    <FieldRef Name='{1}'/>
                                                                </IsNull>
                                                            </And>    
					                                        <And>
						                                        <Geq>
                                                                    <FieldRef Name='{2}'/>
                                                                    <Value Type='DateTime' IncludeTimeValue='False'>{3}</Value>
                                                                </Geq>
                                                                <Leq>
                                                                    <FieldRef Name='{2}'/>
                                                                    <Value Type='DateTime' IncludeTimeValue='False'>{4}</Value>
                                                                </Leq>
					                                        </And>
				                                        </And>
		                                            </Where>
		                                            <OrderBy>
                                                        <FieldRef Name='{5}' Ascending='True'/>
			                                            <FieldRef Name='{2}' Ascending='False'/>
		                                            </OrderBy>
	                                            </Query>
                                            </View>", args);

            var piwListItems = piwList.GetItems(query);
            clientContext.Load(piwListItems);
            clientContext.ExecuteQuery();
            return piwListItems;
        }
        #endregion


    }


}
