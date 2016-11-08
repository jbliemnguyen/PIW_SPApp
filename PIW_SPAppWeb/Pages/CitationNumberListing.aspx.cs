using System;
using System.Collections.Generic;
using System.Data;
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
        private string col_DeletedDate = "Deleted Date";
        private string col_ReAssignedDate = "Reassigned Date";
        private string col_DocumentTitle = "Document Title";
        private string col_CreatedDate = "Document Created Date";
        private string col_PIWURL = "PIWURL";
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
            throw new NotImplementedException();
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
            Dictionary<string,int> quarterDicCheck = new Dictionary<string, int>();

            foreach (ListItem oListItem in collListItem)
            {
                var quarter = oListItem[citationNumberListInternalName[Constants.CitationNumberList_colName_QuarterNumber]].ToString();
                if (!quarterDicCheck.ContainsKey(quarter))
                {
                    quarterDicCheck.Add(quarter,1);
                    ddlQuarter.Items.Add(quarter);    
                }
                
            }
        }

        private DataTable getDataTable(ClientContext clientContext)
        {
            var quarter = ddlQuarter.SelectedValue;
            var documentCategory = ddlCitationNumberCategory.SelectedValue;

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(col_CitationNumber, typeof (string));
            dataTable.Columns.Add(col_Docket, typeof (string));
            dataTable.Columns.Add(col_PIWURL, typeof (string));
            dataTable.Columns.Add(col_Initiator, typeof (string));
            dataTable.Columns.Add(col_Status, typeof (string));
            dataTable.Columns.Add(col_AssignedDate, typeof (string));
            dataTable.Columns.Add(col_DeletedDate, typeof (string));
            dataTable.Columns.Add(col_ReAssignedDate, typeof (string));
            dataTable.Columns.Add(col_DocumentTitle, typeof (string));
            dataTable.Columns.Add(col_DocumentURL, typeof (string));
            dataTable.Columns.Add(col_CreatedDate, typeof (string));
            dataTable.Columns.Add(col_Index, typeof (int)); //this column is used for sorting only

            
            var listItemCollection = getListItemByQuarterNumberAndDocumentCategory(clientContext, quarter,documentCategory);

            if (listItemCollection.Count > 0)
            {
                if (cbAllDate.Checked)
                {

                }
                else
                {
                    
                }
            }

            return dataTable;

        }

        public ListItemCollection getListItemByQuarterNumberAndDocumentCategory(ClientContext clientContext, string quarterNumber, string documentCategoryNumber)
        {
            List citationNumberList = clientContext.Web.Lists.GetByTitle(Constants.CitationNumberListName);
            var citationNumberInternalNameList = helper.getInternalColumnNamesFromCache(clientContext, Constants.CitationNumberListName);
            var query = new CamlQuery();
            var args = new string[]
            {
                citationNumberInternalNameList[Constants.CitationNumberList_colName_QuarterNumber],
                quarterNumber,
                citationNumberInternalNameList[Constants.CitationNumberList_colName_DocumentCategoryNumber],
                documentCategoryNumber,
                citationNumberInternalNameList[Constants.CitationNumberList_colName_SequenceNumber],
                Constants.PIWListName,
                citationNumberInternalNameList[Constants.CitationNumberList_colName_PIWList]
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
                                                <ViewFields>
                                                    <Field Name='PIWTitle' />
                                                </ViewFields>
                                                <ProjectedFields>
                                                    <Field Name='PIWTitle' Type='Lookup' 
                                                           List='{5}' ShowField='Title' />                                                    
                                                </ProjectedFields>
                                                <Joins>
                                                   <Join Type='INNER' ListAlias='{5}'>
                                                       <Eq>
                                                           <FieldRef Name='{6}' RefType='Id' />      
                                                           <FieldRef List='{5}' Name='ID'/>                                                     
                                                       </Eq>
                                                   </Join>
                                                </Joins>                                            
                                            </View>", args);

            var citationListItems = citationNumberList.GetItems(query);

            clientContext.Load(citationListItems);
            clientContext.ExecuteQuery();

            return citationListItems;

        }

        //private void AddRowInDataTable(SPWeb web, DataTable dataTable, ListItem listItem, string fullCitationNumber)
        //{
        //    index++;
        //    if (listItem == null)//skip item (citation on gap)
        //    {
        //        DataRow row = dataTable.Rows.Add();
        //        row[col_CitationNumber] = fullCitationNumber;
        //        row[col_Status] = "skipped";
        //        row[col_Index] = index;

        //    }
        //    else
        //    {
        //        SPListItem piwListItem = null;
        //        //get piwlist from citation item
        //        //bad performance --> in Sharepoint 2010, we can do a join between 2 list (citation list and piwlist)
        //        //actually it is not too bad becuase the filter limit the number of list item less than 500 per 1 display
        //        if (listItem[SPListSetting.col_CitationNumberList_PIWList] != null)
        //        {
        //            piwListItem = helper.getPIWListItemByIDDisRegardIsActive((new SPFieldLookupValue(listItem[SPListSetting.col_CitationNumberList_PIWList].ToString())).LookupValue);

        //            DataRow row = dataTable.Rows.Add();
        //            row[col_CitationNumber] = fullCitationNumber;
        //            row[col_Docket] = piwListItem[SPListSetting.col_PIWList_Docket] != null ? piwListItem[SPListSetting.col_PIWList_Docket] : string.Empty;
        //            row[col_PIWURL] = helper.getPIWURL(piwListItem, string.Empty);
        //            row[col_Initiator] = helper.getUserNameFromLoginIDs(web, piwListItem[SPListSetting.col_PIWList_WFInitiatorID].ToString());
        //            row[col_Status] = listItem[SPListSetting.col_CitationNumberList_Status] != null ? listItem[SPListSetting.col_CitationNumberList_Status].ToString() : string.Empty;
        //            row[col_AssignedDate] = listItem[SPListSetting.col_CitationNumberList_AssignedDate] != null ? listItem[SPListSetting.col_CitationNumberList_AssignedDate].ToString() : string.Empty;
        //            row[col_DeletedDate] = listItem[SPListSetting.col_CitationNumberList_DeletedDate] != null ? listItem[SPListSetting.col_CitationNumberList_DeletedDate].ToString() : string.Empty;
        //            row[col_ReAssignedDate] = listItem[SPListSetting.col_CitationNumberList_ReAssignedDate] != null ? listItem[SPListSetting.col_CitationNumberList_ReAssignedDate].ToString() : string.Empty;
        //            row[col_DocumentTitle] = piwListItem[SPListSetting.col_PIWList_DocumentTitle] != null ? piwListItem[SPListSetting.col_PIWList_DocumentTitle].ToString() : string.Empty;
        //            row[col_DocumentURL] = helper.getDocumentURL(piwListItem);
        //            row[col_CreatedDate] = piwListItem[SPListSetting.col_PIWList_Created] != null ? piwListItem[SPListSetting.col_PIWList_Created].ToString() : string.Empty;


        //            row[col_Index] = index;

        //        }
        //        else//deleted item
        //        {
        //            DataRow row = dataTable.Rows.Add();
        //            row[col_CitationNumber] = fullCitationNumber;
        //            row[col_Docket] = listItem[SPListSetting.col_CitationNumberList_PIWList] != null ? listItem[SPListSetting.col_CitationNumberList_PIWList].ToString() : string.Empty;
        //            row[col_Status] = listItem[SPListSetting.col_CitationNumberList_Status] != null ? listItem[SPListSetting.col_CitationNumberList_Status].ToString() : string.Empty;
        //            row[col_AssignedDate] = listItem[SPListSetting.col_CitationNumberList_AssignedDate] != null ? listItem[SPListSetting.col_CitationNumberList_AssignedDate].ToString() : string.Empty;
        //            row[col_DeletedDate] = listItem[SPListSetting.col_CitationNumberList_DeletedDate] != null ? listItem[SPListSetting.col_CitationNumberList_DeletedDate].ToString() : string.Empty;
        //            row[col_ReAssignedDate] = listItem[SPListSetting.col_CitationNumberList_ReAssignedDate] != null ? listItem[SPListSetting.col_CitationNumberList_ReAssignedDate].ToString() : string.Empty;
        //            row[col_Index] = index;
        //        }
        //    }
        //}
        

        #endregion

        
    }
}