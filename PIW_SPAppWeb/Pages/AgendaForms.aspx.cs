using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;

namespace PIW_SPAppWeb.Pages
{
    public partial class AgendaForms : System.Web.UI.Page
    {
        private SharePointHelper helper;
        private string previousRowFormStatus = string.Empty;
        int intSubTotalIndex = 1;
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                displayData();
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

        private void RenderGridView(ClientContext clientContext)
        {
            DataTable dataTable = new DataTable();
            DataRow dataRow;
            //string filename = helper.getPageFileName(Page.Request);
            var piwListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            gridView.Columns.Clear();

            var listItemCollection = getPIWListItem(clientContext);
            if (listItemCollection.Count > 0)
            {
                dataTable.Columns.Add("Docket", typeof(string));
                dataTable.Columns.Add("URL", typeof(string));
                dataTable.Columns.Add("Status", typeof(string));
                //dataTable.Columns.Add("DocumentTitle", typeof(string));
                dataTable.Columns.Add("DocumentURL", typeof(string));
                dataTable.Columns.Add("DocumentCategory", typeof(string));
                dataTable.Columns.Add("InitiatorOffice", typeof(string));
                dataTable.Columns.Add("CitationNumber", typeof(string));




                dataTable.Columns.Add("DueDate", typeof(string));
                dataTable.Columns.Add("Created", typeof(string));
                dataTable.Columns.Add("RecallRejectComment", typeof(string));
                dataTable.Columns.Add("GroupOrder", typeof(string));

                foreach (var listItem in listItemCollection)
                {
                    dataRow = dataTable.Rows.Add();

                    dataRow["Docket"] = listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]].ToString()
                        : string.Empty;

                    //Form Type
                    //string formType = listItem[piwListInternalName[Constants.PIWList_colName_FormType]] != null
                    //    ? listItem[piwListInternalName[Constants.PIWList_colName_FormType]].ToString()
                    //    : string.Empty;

                    //dataRow["URL"] = helper.getEditFormURL(formType, listItemId, Page.Request,filename);
                    dataRow["URL"] = listItem[piwListInternalName[Constants.PIWList_colName_EditFormURL]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_EditFormURL]].ToString()
                        : string.Empty;

                    var formStatus = listItem[piwListInternalName[Constants.PIWList_colName_FormStatus]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_FormStatus]].ToString()
                        : string.Empty;
                    dataRow["Status"] = formStatus;

                    dataRow["GroupOrder"] = getGroupOrder(formStatus);

                    dataRow["InitiatorOffice"] =
                        listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString()
                            : string.Empty;

                    dataRow["CitationNumber"] =
                        listItem[piwListInternalName[Constants.PIWList_colName_CitationNumber]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_CitationNumber]].ToString()
                            : string.Empty;
                    
                    dataRow["DueDate"] = listItem[piwListInternalName[Constants.PIWList_colName_DueDate]] != null
                        ? DateTime.Parse(listItem[piwListInternalName[Constants.PIWList_colName_DueDate]].ToString()).ToShortDateString()
                        : string.Empty;

                    var publicDocsURL = listItem[piwListInternalName[Constants.PIWList_colName_PublicDocumentURLs]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_PublicDocumentURLs]].ToString()
                        : string.Empty;
                    var CEIIDocsURL = listItem[piwListInternalName[Constants.PIWList_colName_CEIIDocumentURLs]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_CEIIDocumentURLs]].ToString()
                        : string.Empty;
                    var privilegedDocsURL =
                        listItem[piwListInternalName[Constants.PIWList_colName_PrivilegedDocumentURLs]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_PrivilegedDocumentURLs]].ToString()
                            : string.Empty;

                    dataRow["DocumentURL"] = helper.getDocumentURLsHTML(publicDocsURL, CEIIDocsURL, privilegedDocsURL, false);

                    dataRow["DocumentCategory"] =
                        listItem[piwListInternalName[Constants.PIWList_colName_DocumentCategory]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_DocumentCategory]].ToString()
                            : string.Empty;

                    dataRow["Created"] = System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem["Created"].ToString())).ToString();
                    
                    if ((formStatus == Constants.PIWList_FormStatus_Rejected) || (formStatus == Constants.PIWList_FormStatus_Recalled))
                    {
                        dataRow["RecallRejectComment"] =
                            listItem[piwListInternalName[Constants.PIWList_colName_RecallRejectComment]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_RecallRejectComment]].ToString()
                                : string.Empty;
                    }
                    else
                    {
                        dataRow["RecallRejectComment"] = string.Empty;
                    }
                    
                }

            }

            //Bound to gridview
            HyperLinkField hyperlinkField;

            string[] urls = new string[1] { "URL" };
            hyperlinkField = new HyperLinkField { HeaderText = "Docket Number", DataTextField = "Docket" };
            hyperlinkField.HeaderStyle.CssClass = "col-xs-2";
            hyperlinkField.ItemStyle.CssClass = "col-xs-2";
            hyperlinkField.DataNavigateUrlFields = urls;
            //hyperlinkField.Target = "_blank";
            gridView.Columns.Add(hyperlinkField);


            BoundField boundField = new BoundField
            {
                HeaderText = "Document",
                DataField = "DocumentURL",
                HtmlEncode = false,
            };
            boundField.HeaderStyle.CssClass = "col-xs-3";
            boundField.ItemStyle.CssClass = "col-xs-3";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Document Category", DataField = "DocumentCategory" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Form Status", DataField = "Status", Visible = false };
            gridView.Columns.Add(boundField);
            

            boundField = new BoundField { HeaderText = "Initiator Office", DataField = "InitiatorOffice" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Citation Number", DataField = "CitationNumber" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);
            
            boundField = new BoundField { HeaderText = "Due Date", DataField = "DueDate" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Created Date", DataField = "Created" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Recall/Reject Comment", DataField = "RecallRejectComment" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);
            
            gridView.AutoGenerateColumns = false;
            DataView view = dataTable.DefaultView;
            if (view.Count > 0) //without this check, exception happens if no row in the view
            {
                view.Sort = "GroupOrder";
            }

            gridView.DataSource = view;
            gridView.DataBind();
        }

        private ListItemCollection getPIWListItem(ClientContext clientContext)
        {
            List piwList = clientContext.Web.Lists.GetByTitle(Constants.PIWListName);
            var piwListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            CamlQuery query = new CamlQuery();
            var args = new string[]
            {
                piwListInternalName[Constants.PIWList_colName_IsActive],
                piwListInternalName[Constants.PIWList_colName_FormStatus],
                Constants.PIWList_FormStatus_PublishInitiated,
                Constants.PIWList_FormStatus_PublishedToeLibrary,
                piwListInternalName[Constants.PIWList_colName_FormType],
                Constants.PIWList_FormType_AgendaForm
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
						                                            <Neq>
							                                            <FieldRef Name='{1}'/>
							                                            <Value Type='Text'>{2}</Value>
						                                            </Neq>
						                                            <Neq>
							                                            <FieldRef Name='{1}'/>
							                                            <Value Type='Text'>{3}</Value>
						                                            </Neq>
					                                            </And>
				                                            </And>
				                                            <Eq>
					                                            <FieldRef Name='{4}'/>
					                                            <Value Type='Text'>{5}</Value>
				                                            </Eq>
			                                            </And>
		                                            </Where>
		                                            <OrderBy>
			                                            <FieldRef Name='{1}'/>
		                                            </OrderBy>
	                                            </Query>
                                            </View>", args);

            var piwListItems = piwList.GetItems(query);
            clientContext.Load(piwListItems);
            clientContext.ExecuteQuery();
            return piwListItems;

        }

        //create new column for Grouping order
        //Pending group should appear first on the list of issuance (not Edit, if by alphabetical)
        private string getGroupOrder(string formStatus)
        {
            string result = string.Empty;
            switch (formStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                    result = "1";
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    result = "2";
                    break;
                case Constants.PIWList_FormStatus_SecretaryReview:
                    result = "3";
                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    result = "4";
                    break;
                case Constants.PIWList_FormStatus_Edited:
                    result = "5";
                    break;
                case Constants.PIWList_FormStatus_Rejected:
                    result = "6";
                    break;
                case Constants.PIWList_FormStatus_Recalled:
                    result = "7";
                    break;
                default:
                    result = "8";
                    break;
            }

            return result;
        }

        protected void standardFormsGridView_RowCreated(object sender, GridViewRowEventArgs e)
        {
            if (DataBinder.Eval(e.Row.DataItem, "Status") != null)
            {
                string Status = DataBinder.Eval(e.Row.DataItem, "Status").ToString();
                if (!previousRowFormStatus.Equals(Status))
                {
                    GridView StandardFormgrdView = (GridView)sender;
                    GridViewRow row = new GridViewRow(0, 0, DataControlRowType.DataRow, DataControlRowState.Insert);
                    TableCell cell = new TableCell
                    {
                        Text = "Status : " + Status,
                        ColumnSpan = 10,
                        CssClass = "GroupHeaderStyle"
                    };
                    row.Cells.Add(cell);
                    StandardFormgrdView.Controls[0].Controls.AddAt(e.Row.RowIndex + intSubTotalIndex, row);
                    intSubTotalIndex++;
                }

                previousRowFormStatus = Status;
            }
        }

        protected void tmrRefresh_Tick(object sender, EventArgs e)
        {
            try
            {
                displayData();

            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, string.Empty, Page.Request.Url.OriginalString);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }

        }

        private void displayData()
        {
            helper = new SharePointHelper();
            using (var clientContext = helper.getElevatedClientContext(Context, Request))
            {
                ResetField();
                RenderGridView(clientContext);
                lbLastUpdated.Text = "Last Updated: " + DateTime.Now.ToString("g");
            }
        }

        private void ResetField()
        {
            previousRowFormStatus = string.Empty;
            intSubTotalIndex = 1;
        }
    }
}