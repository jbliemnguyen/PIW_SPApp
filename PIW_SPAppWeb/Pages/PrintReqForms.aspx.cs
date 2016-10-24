using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;

namespace PIW_SPAppWeb.Pages
{
    public partial class PrintReqForms : System.Web.UI.Page
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
                dataTable.Columns.Add("InitiatorOffice", typeof(string));
                dataTable.Columns.Add("DateRequested", typeof(string));
                dataTable.Columns.Add("DateRequired", typeof(string));
                dataTable.Columns.Add("NumberOfPages", typeof(string));
                dataTable.Columns.Add("NumberOfCopies", typeof(string));
                dataTable.Columns.Add("GroupOrder", typeof(string));
                
                

                foreach (var listItem in listItemCollection)
                {
                    dataRow = dataTable.Rows.Add();

                    dataRow["Docket"] = listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]].ToString()
                        : string.Empty;
                    
                    dataRow["URL"] = listItem[piwListInternalName[Constants.PIWList_colName_PrintReqFormURL]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_PrintReqFormURL]].ToString()
                        : string.Empty;

                    var formStatus = listItem[piwListInternalName[Constants.PIWList_colName_PrintReqStatus]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_PrintReqStatus]].ToString()
                        : string.Empty;
                    dataRow["Status"] = formStatus;

                    dataRow["GroupOrder"] = getGroupOrder(formStatus);

                    dataRow["InitiatorOffice"] =
                        listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString()
                            : string.Empty;

                    dataRow["DateRequested"] = listItem[piwListInternalName[Constants.PIWList_colName_PrintReqDateRequested]] != null
                        ? DateTime.Parse(listItem[piwListInternalName[Constants.PIWList_colName_PrintReqDateRequested]].ToString()).ToShortDateString()
                        : string.Empty;

                    dataRow["DateRequired"] = listItem[piwListInternalName[Constants.PIWList_colName_PrintReqDateRequired]] != null
                        ? DateTime.Parse(listItem[piwListInternalName[Constants.PIWList_colName_PrintReqDateRequired]].ToString()).ToShortDateString()
                        : string.Empty;

                    dataRow["NumberOfPages"] = listItem[piwListInternalName[Constants.PIWList_colName_NumberOfPublicPages]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_NumberOfPublicPages]].ToString(): string.Empty;

                    dataRow["NumberOfCopies"] = listItem[piwListInternalName[Constants.PIWList_colName_PrintReqNumberofCopies]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_PrintReqNumberofCopies]].ToString() : string.Empty;
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
            

            BoundField boundField = new BoundField { HeaderText = "Form Status", DataField = "Status", Visible = false };
            gridView.Columns.Add(boundField);
            
            boundField = new BoundField { HeaderText = "Initiator Office", DataField = "InitiatorOffice" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Date Requested", DataField = "DateRequested" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Date Required", DataField = "DateRequired" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Number of Pages", DataField = "NumberOfPages" };
            boundField.HeaderStyle.CssClass = "col-xs-1";
            boundField.ItemStyle.CssClass = "col-xs-1";
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Number of Copies", DataField = "NumberOfCopies" };
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
                piwListInternalName[Constants.PIWList_colName_PrintReqNumberofCopies],
                piwListInternalName[Constants.PIWList_colName_PrintReqStatus],
                Constants.PIWList_FormStatus_PrintReqMailJobCompleted
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
						                                        <Gt>
							                                        <FieldRef Name='{1}'/>
							                                        <Value Type='Number'>0</Value>
						                                        </Gt>
				                                            </And>
				                                            <Neq>
					                                            <FieldRef Name='{2}'/>
					                                            <Value Type='Text'>{3}</Value>
				                                            </Neq>
			                                            </And>
		                                            </Where>
		                                            <OrderBy>
			                                            <FieldRef Name='{2}'/>
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
        private string getGroupOrder(string printReqFormStatus)
        {
            string result = string.Empty;
            switch (printReqFormStatus)
            {
                case Constants.PIWList_FormStatus_Submitted:
                    result = "1";
                    break;
                case Constants.PIWList_FormStatus_PrintReqAccepted:
                    result = "2";
                    break;
                case Constants.PIWList_FormStatus_PrintReqPrintJobCompleted:
                    result = "3";
                    break;
                case Constants.PIWList_FormStatus_Rejected:
                    result = "4";
                    break;
                default:
                    result = "5";
                    break;
            }

            return result;
        }

        protected void gridView_RowCreated(object sender, GridViewRowEventArgs e)
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