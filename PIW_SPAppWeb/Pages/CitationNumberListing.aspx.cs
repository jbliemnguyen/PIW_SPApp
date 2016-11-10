using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.IdentityModel.Protocols.WSFederation.Metadata;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = Microsoft.SharePoint.Client.ListItem;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;

namespace PIW_SPAppWeb.Pages
{
    public partial class CitationNumberListing : System.Web.UI.Page
    {
        #region variables
        //variable declaration
        private SharePointHelper helper = new SharePointHelper();
        int index = 0;

        //constant setting
        private string col_CitationNumber = "Citation Number";
        private string col_Docket = "Docket Number";
        private string col_Initiator = "Initiator(s)";
        private string col_Status = "Status";
        private string col_AssignedDate = "Assigned Date";
        private string col_DeletedDate = "Deleted";
        private string col_ReAssignedDate = "Reassigned";
        //private string col_DocumentTitle = "Document Title";
        private string col_CreatedDate = "PIW Form Created";
        private string col_EditFormURL = "PIWURL";
        private string col_DocumentURL = "Document URL";
        private string col_Index = "Index";
        #endregion

        #region Events
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    if (!Page.IsPostBack)
                    {
                        populateAllQuarterNumber(clientContext);
                    }
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

        protected void btnRun_OnClick(object sender, EventArgs e)
        {
            try
            {
                using (var clientContext = helper.getElevatedClientContext(Context, Request))
                {
                    var dataTable = getDataTable(clientContext);
                    RenderGridView(dataTable);
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

        

        public void sPGridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView rowView = (DataRowView)e.Row.DataItem;
                string status = rowView[col_Status].ToString();
                if (status == "skipped")
                {
                    e.Row.BackColor = System.Drawing.Color.YellowGreen;
                }
                else if (status == "deleted")
                {
                    e.Row.BackColor = System.Drawing.Color.Tomato;
                }
            }
        }
        #endregion

        #region Utils

        private void populateAllQuarterNumber(ClientContext clientContext)
        {
            List citationNumberList = clientContext.Web.Lists.GetByTitle(Constants.CitationNumberListName);
            var citationNumberListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.CitationNumberListName);

            CamlQuery camlQuery = new CamlQuery();
            camlQuery.ViewXml = string.Format(@"<View>	      
                                                    <ViewFields>
                                                        <FieldRef Name='{0}'/>                                       
                                                    </ViewFields>
                                                    <Query>
                                                        <OrderBy>
			                                                <FieldRef Name='{0}' Ascending='FALSE'/>
		                                                </OrderBy>
                                                    </Query>
                                                </View>", citationNumberListInternalName[Constants.CitationNumberList_colName_QuarterNumber]);

            ListItemCollection collListItem = citationNumberList.GetItems(camlQuery);

            clientContext.Load(collListItem);
            clientContext.ExecuteQuery();
            Dictionary<string, int> quarterDicCheck = new Dictionary<string, int>();

            foreach (ListItem oListItem in collListItem)
            {
                var quarter = oListItem[citationNumberListInternalName[Constants.CitationNumberList_colName_QuarterNumber]].ToString();
                if (!quarterDicCheck.ContainsKey(quarter))
                {
                    quarterDicCheck.Add(quarter, 1);
                    ddlQuarter.Items.Add(quarter);
                }

            }
        }

        private DataTable getDataTable(ClientContext clientContext)
        {
            var citationNumberInternalNameList = helper.getInternalColumnNamesFromCache(clientContext, Constants.CitationNumberListName);
            var piwListInternalNameList = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            var quarter = int.Parse(ddlQuarter.SelectedValue);
            var documentCategory = int.Parse(ddlCitationNumberCategory.SelectedValue);

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(col_CitationNumber, typeof (string));
            dataTable.Columns.Add(col_Docket, typeof (string));
            dataTable.Columns.Add(col_EditFormURL, typeof (string));
            dataTable.Columns.Add(col_Initiator, typeof (string));
            dataTable.Columns.Add(col_Status, typeof (string));
            dataTable.Columns.Add(col_AssignedDate, typeof (string));
            dataTable.Columns.Add(col_DeletedDate, typeof (string));
            dataTable.Columns.Add(col_ReAssignedDate, typeof (string));
            dataTable.Columns.Add(col_DocumentURL, typeof (string));
            dataTable.Columns.Add(col_CreatedDate, typeof (string));
            dataTable.Columns.Add(col_Index, typeof (int)); //this column is used for sorting only

            CitationNumber citation = new CitationNumber();
            
            var listItemCollection = citation.getListItemByQuarterNumberAndDocumentCategory(clientContext, quarter,documentCategory);

            if (listItemCollection.Count > 0)
            {
                if (cbAllDate.Checked)
                {
                    for (int i = 0; i < listItemCollection.Count; i++)
                            {
                                int currentNumber = int.Parse(listItemCollection[i][citationNumberInternalNameList[Constants.CitationNumberList_colName_SequenceNumber]].ToString());
                                int nextNumber = 0;//make sure we have number "1" if it is on gap
                                if (i < (listItemCollection.Count - 1))
                                {
                                    nextNumber = int.Parse(listItemCollection[i + 1][citationNumberInternalNameList[Constants.CitationNumberList_colName_SequenceNumber]].ToString());
                                }


                                //add current number to result
                                string fullCitationNumber = CitationNumber.ToString(quarter, documentCategory, currentNumber);
                                AddRowInDataTable(clientContext, dataTable, listItemCollection[i], fullCitationNumber,citationNumberInternalNameList,piwListInternalNameList);

                                //add gap number to result (if any)
                                if (currentNumber > (nextNumber + 1))//there is gap
                                {
                                    for (int j = currentNumber - 1; j > nextNumber; j--)
                                    {
                                        //add all gap number
                                        fullCitationNumber = CitationNumber.ToString(quarter, documentCategory, j);
                                        AddRowInDataTable(clientContext, dataTable, null, fullCitationNumber,citationNumberInternalNameList,piwListInternalNameList);
                                    }
                                }
                            }
                }
                else
                {
                    DateTime actionDate = DateTime.Parse(tbActionDate.Text);
                    for (int i = 0; i < listItemCollection.Count; i++)
                    {
                        if (isSameActionDate(listItemCollection[i], actionDate,citationNumberInternalNameList))
                        {
                            string fullCitationNumber = listItemCollection[i][citationNumberInternalNameList[Constants.CitationNumberList_colName_Title]].ToString();
                            AddRowInDataTable(clientContext, dataTable, listItemCollection[i], fullCitationNumber,citationNumberInternalNameList,piwListInternalNameList);
                        }
                    }
                }
            }

            return dataTable;

        }

        public ListItemCollection getListItemByQuarterNumberAndDocumentCategory(ClientContext clientContext, string quarterNumber, string documentCategoryNumber)
        {
            //Note:
            //This method work fine, KEEP IT for future reference
            //we are not using join in caml becuase when joining, the fields (called projectedfields) from foreign list has some limitation
            //the multiple lines field and choice cannot be included in the query
            //call the fields we need from piw list are multiple line of text. Thus we need to do multiple query to get information.

            List citationNumberList = clientContext.Web.Lists.GetByTitle(Constants.CitationNumberListName);
            var citationNumberInternalNameList = helper.getInternalColumnNamesFromCache(clientContext, Constants.CitationNumberListName);
            var piwListInternalNameList = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            var query = new CamlQuery();
            var args = new string[]
            {
                citationNumberInternalNameList[Constants.CitationNumberList_colName_QuarterNumber],
                quarterNumber,
                citationNumberInternalNameList[Constants.CitationNumberList_colName_DocumentCategoryNumber],
                documentCategoryNumber,
                citationNumberInternalNameList[Constants.CitationNumberList_colName_SequenceNumber],
                Constants.PIWListName,
                citationNumberInternalNameList[Constants.CitationNumberList_colName_PIWList],
                piwListInternalNameList[Constants.PIWList_colName_ProgramOfficeWFInitator]
            };

            query.ViewXml = string.Format(@"<View>
	                                            <Query>		
                                                    <Where>
			                                            <And>
				                                            <Eq>
					                                            <FieldRef Name='{0}'/>
					                                            <Value Type='Number'>{1}</Value>
				                                            </Eq>
				                                            <Eq>
					                                            <FieldRef Name='{2}'/>
					                                            <Value Type='Number'>{3}</Value>
				                                            </Eq>
			                                            </And>
		                                            </Where>                                            
		                                            <OrderBy>
			                                            <FieldRef Name='{4}' Ascending='False'/>
		                                            </OrderBy>
	                                            </Query>                                                                                                   
                                                <Joins>
                                                   <Join Type='INNER' ListAlias='{5}'>
                                                       <Eq>
                                                           <FieldRef Name='{6}' RefType='Id' />      
                                                           <FieldRef List='{5}' Name='ID'/>                                                     
                                                       </Eq>
                                                   </Join>
                                                </Joins>                                            
                                                <ProjectedFields>
                                                    <Field Name='PIWFormType' Type='Lookup' List='{5}' ShowField='{7}' />                                                    
                                                </ProjectedFields>
                                            </View>", args);

            var citationListItems = citationNumberList.GetItems(query);

            clientContext.Load(citationListItems);
            clientContext.ExecuteQuery();

            lbTest.Text = "Number of items: " + citationListItems.Count;
            foreach (ListItem oListItem in citationListItems)
            {
                //    var quarter = oListItem["PIWTitle"].ToString();
            }

            return citationListItems;

        }

        private void AddRowInDataTable(ClientContext clientContext, DataTable dataTable, ListItem citationNumberListItem, string fullCitationNumber,
            Dictionary<string, string> citationNumberInternalNameList, Dictionary<string, string> piwListInternalNameList)
        {

            index++;
            if (citationNumberListItem == null)//skip item (citation on gap)
            {
                DataRow row = dataTable.Rows.Add();
                row[col_CitationNumber] = fullCitationNumber;
                row[col_Status] = "skipped";
                row[col_Index] = index;

            }
            else
            {
                ListItem piwListItem = null;
                DataRow row = dataTable.Rows.Add();
                //get piwlist from citation item
                //bad performance --> we can NOT do join 
                //actually it is not too bad becuase the filter limit the number of citation number returned

                if (citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_PIWList]] != null)//not deleted item (there is piwlist associated)
                {
                    string piwListItemID = (citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_PIWList]] as FieldLookupValue).
                        LookupId.ToString();
                    piwListItem = helper.GetPiwListItemById(clientContext, piwListItemID, true);

                    //data from piwlist
                    row[col_Docket] = piwListItem[piwListInternalNameList[Constants.PIWList_colName_DocketNumber]] != null
                            ? piwListItem[piwListInternalNameList[Constants.PIWList_colName_DocketNumber]].ToString()
                            : string.Empty;

                    row[col_EditFormURL] = piwListItem[piwListInternalNameList[Constants.PIWList_colName_EditFormURL]] != null
                            ? piwListItem[piwListInternalNameList[Constants.PIWList_colName_EditFormURL]].ToString()
                            : string.Empty;

                    var publicDocsURL = piwListItem[piwListInternalNameList[Constants.PIWList_colName_PublicDocumentURLs]] != null ?
                        piwListItem[piwListInternalNameList[Constants.PIWList_colName_PublicDocumentURLs]].ToString() : string.Empty;
                    var CEIIDocsURL = piwListItem[piwListInternalNameList[Constants.PIWList_colName_CEIIDocumentURLs]] != null ?
                        piwListItem[piwListInternalNameList[Constants.PIWList_colName_CEIIDocumentURLs]].ToString() : string.Empty;
                    var privilegedDocsURL = piwListItem[piwListInternalNameList[Constants.PIWList_colName_PrivilegedDocumentURLs]] != null ?
                        piwListItem[piwListInternalNameList[Constants.PIWList_colName_PrivilegedDocumentURLs]].ToString() : string.Empty;


                    row[col_DocumentURL] = helper.getDocumentURLsHTML(publicDocsURL, CEIIDocsURL, privilegedDocsURL, false);

                    row[col_Initiator] = piwListItem[piwListInternalNameList[Constants.PIWList_colName_WorkflowInitiator]] != null ?
                        ((FieldUserValue)piwListItem[piwListInternalNameList[Constants.PIWList_colName_WorkflowInitiator]]).LookupValue : string.Empty;

                    row[col_CreatedDate] = System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(piwListItem["Created"].ToString()));
                }

                row[col_CitationNumber] = fullCitationNumber;
                row[col_Status] = citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_Status]] != null ?
                        citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_Status]].ToString() : string.Empty;

                row[col_AssignedDate] = citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_AssignedDate]] != null ?
                    citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_AssignedDate]].ToString() : string.Empty;

                row[col_DeletedDate] = citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_DeletedDate]] != null ?
                    DateTime.Parse(citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_DeletedDate]].ToString()).ToShortDateString() : string.Empty;
                    

                row[col_ReAssignedDate] = citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_ReAssignedDate]] != null ?
                    DateTime.Parse(citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_ReAssignedDate]].ToString()).ToShortDateString() : string.Empty;
                    

                row[col_Index] = index;
            }
        }

        private bool isSameActionDate(ListItem citationNumberListItem, DateTime ActionDate, Dictionary<string, string> citationNumberInternalNameList)
        {
            string strAssignedDate = citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_AssignedDate]] != null ?
                    citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_AssignedDate]].ToString() : string.Empty;

            string strReAssignedDate = citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_ReAssignedDate]] != null ?
                    citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_ReAssignedDate]].ToString() : string.Empty;

            string strDeletedDate = citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_DeletedDate]] != null ?
                    citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_DeletedDate]].ToString() : string.Empty;

            DateTime? assignedDate = null;
            DateTime? reAssignedDate = null;
            DateTime? deletedDate = null;

            if (!string.IsNullOrEmpty(strReAssignedDate))
            {
                reAssignedDate = DateTime.Parse(strReAssignedDate);
            }

            if (!string.IsNullOrEmpty(strAssignedDate))
            {
                assignedDate = DateTime.Parse(strAssignedDate);
            }

            if (!string.IsNullOrEmpty(strDeletedDate))
            {
                deletedDate = DateTime.Parse(strDeletedDate);
            }


            //If any date (assigned, deleted, reassigned) got same value of searched date, 
            //return true to add this record to search result
            if (reAssignedDate.HasValue)
            {
                if (ActionDate.Date.Equals(reAssignedDate.Value.Date))
                {
                    return true;
                }
            }

            if (assignedDate.HasValue)
            {
                if (ActionDate.Date.Equals(assignedDate.Value.Date))
                {
                    return true;
                }
            }

            if (deletedDate.HasValue)
            {
                if (ActionDate.Date.Equals(deletedDate.Value.Date))
                {
                    return true;
                }
            }

            return false;
        }

        private void RenderGridView(DataTable dataTable)
        {
            BoundField boundField;
            HyperLinkField hyperlinkField;

            gridView.Columns.Clear();
            
            boundField = new BoundField { HeaderText = col_CitationNumber, DataField = col_CitationNumber };
            boundField.HeaderStyle.CssClass = "col-md-2";
            boundField.ItemStyle.CssClass = "col-md-2";
            gridView.Columns.Add(boundField);

            //docket link to piwform
            string[] urls = new string[1] { col_EditFormURL };
            hyperlinkField = new HyperLinkField { HeaderText = col_Docket, DataTextField = col_Docket, };
            hyperlinkField.HeaderStyle.CssClass = "col-md-2";
            hyperlinkField.ItemStyle.CssClass = "col-md-2";
            hyperlinkField.DataNavigateUrlFields = urls;
            hyperlinkField.Target = "_blank";
            gridView.Columns.Add(hyperlinkField);

            //initiator
            boundField = new BoundField { HeaderText = "Initiator", DataField = col_Initiator };
            gridView.Columns.Add(boundField);

            //status
            boundField = new BoundField { HeaderText = col_Status, DataField = col_Status };
            gridView.Columns.Add(boundField);

            //Assigned Date
            boundField = new BoundField { HeaderText = col_AssignedDate, DataField = col_AssignedDate };
            gridView.Columns.Add(boundField);

            //deleted date
            boundField = new BoundField { HeaderText = col_DeletedDate, DataField = col_DeletedDate };
            gridView.Columns.Add(boundField);

            //reassigned date
            boundField = new BoundField { HeaderText = col_ReAssignedDate, DataField = col_ReAssignedDate };
            gridView.Columns.Add(boundField);

            //documents
            boundField = new BoundField
            {
                HeaderText = "Document",
                DataField = col_DocumentURL,
                HtmlEncode = false,

            };
            boundField.HeaderStyle.CssClass = "col-md-2";
            boundField.ItemStyle.CssClass = "col-md-2";
            gridView.Columns.Add(boundField);


            //Created
            boundField = new BoundField { HeaderText = col_CreatedDate, DataField = col_CreatedDate };
            gridView.Columns.Add(boundField);

            gridView.AutoGenerateColumns = false;
            DataView view = dataTable.DefaultView;
            //view.Sort = col_Index + " DESC";//resort in reverse direction, so last citation number will apprear first
            gridView.DataSource = view;
            gridView.DataBind();
        }

        #endregion


    }
}