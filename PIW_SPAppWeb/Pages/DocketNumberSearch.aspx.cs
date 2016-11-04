using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = System.Web.UI.WebControls.ListItem;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;

namespace PIW_SPAppWeb.Pages
{
    public partial class DocketNumberSearch : System.Web.UI.Page
    {
        private SharePointHelper helper = new SharePointHelper();

        #region Events
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    if (Page.Request.QueryString["Docket"] != null)
                    {
                        tbDocketNumber.Text = Page.Request.QueryString["Docket"].ToString();
                        btnSearch_OnClick(null, null);
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

        protected void gridView_OnPageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gridView.PageIndex = e.NewPageIndex;
            btnSearch_OnClick(null, null);
        }

        protected void btnSearch_OnClick(object sender, EventArgs e)
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
                dataTable.Columns.Add("Docket", typeof(string));
                dataTable.Columns.Add("URL", typeof(string));
                dataTable.Columns.Add("DocumentURL", typeof(string));
                dataTable.Columns.Add("DocumentCategory", typeof(string));
                dataTable.Columns.Add("FormStatus", typeof(string));
                dataTable.Columns.Add("InitiatorOffice", typeof(string));
                dataTable.Columns.Add("FormType", typeof(string));
                dataTable.Columns.Add("CreatedDate", typeof(string));
                dataTable.Columns.Add("DueDate", typeof(string));

                foreach (var listItem in listItemCollection)
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

                    dataRow["DocumentCategory"] = listItem[piwListInternalName[Constants.PIWList_colName_DocumentCategory]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_DocumentCategory]].ToString()
                            : string.Empty;

                    dataRow["FormStatus"] = listItem[piwListInternalName[Constants.PIWList_colName_FormStatus]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_FormStatus]].ToString()
                            : string.Empty;


                    dataRow["InitiatorOffice"] = listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null
                                ? listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString() : string.Empty;

                    dataRow["FormType"] = listItem[piwListInternalName[Constants.PIWList_colName_FormType]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_FormType]].ToString()
                            : string.Empty;

                    dataRow["CreatedDate"] = listItem["Created"] != null
                            ? listItem["Created"].ToString()
                            : string.Empty;

                    dataRow["DueDate"] = listItem[piwListInternalName[Constants.PIWList_colName_DueDate]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_DueDate]].ToString()
                            : string.Empty;
                }

                //Bound to gridview
                HyperLinkField hyperlinkField;

                string[] urls = new string[1] { "URL" };
                hyperlinkField = new HyperLinkField { HeaderText = "Docket Number", DataTextField = "Docket", };
                hyperlinkField.HeaderStyle.CssClass = "col-md-2";
                hyperlinkField.ItemStyle.CssClass = "col-md-2";
                hyperlinkField.DataNavigateUrlFields = urls;
                hyperlinkField.Target = "_blank";
                gridView.Columns.Add(hyperlinkField);

                BoundField boundField = new BoundField
                {
                    HeaderText = "Document",
                    DataField = "DocumentURL",
                    HtmlEncode = false,
                };
                boundField.HeaderStyle.CssClass = "col-md-2";
                boundField.ItemStyle.CssClass = "col-md-2";
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = "Document Category", DataField = "DocumentCategory" };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = "Form Status", DataField = "FormStatus" };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = "Initiator Office", DataField = "InitiatorOffice" };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = "FormType", DataField = "FormType" };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = "Created Date", DataField = "CreatedDate" };
                gridView.Columns.Add(boundField);

                boundField = new BoundField { HeaderText = "Due Date", DataField = "DueDate" };
                gridView.Columns.Add(boundField);

                gridView.AllowPaging = true;
                gridView.PageSize = 25;
                gridView.DataSource = dataTable.DefaultView; ;
                gridView.DataBind();

                //display updated time
                lbLastUpdated.Text = "Last Updated: " + DateTime.Now.ToString("g");
            }
        }

        private ListItemCollection getPIWListItem(ClientContext clientContext)
        {
            List piwList = clientContext.Web.Lists.GetByTitle(Constants.PIWListName);
            var piwListInternalName = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            string office = ddProgramOfficeWorkflowInitiator.SelectedIndex > 0 ? ddProgramOfficeWorkflowInitiator.SelectedValue : string.Empty;
            string docket = tbDocketNumber.Text.Trim();

            if (string.IsNullOrEmpty(office)) //All Office
            {
                CamlQuery query = new CamlQuery();
                var args = new string[]
                {
                    piwListInternalName[Constants.PIWList_colName_IsActive],
                    piwListInternalName[Constants.PIWList_colName_DocketNumber],
                    docket,
                    "Created"
                };

                query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>			                                            
				                                        <And>
					                                        <Eq>
						                                        <FieldRef Name='{0}'/>
						                                        <Value Type='Bool'>True</Value>
					                                        </Eq>					                                        
						                                    <Contains>
					                                            <FieldRef Name='{1}'/>
					                                            <Value Type='Text'>{2}</Value>
				                                            </Contains>
				                                        </And>
		                                            </Where>
		                                            <OrderBy>
			                                            <FieldRef Name='{3}' Ascending='False'/>
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
                    piwListInternalName[Constants.PIWList_colName_DocketNumber],
                    docket,
                    piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator],
                    office,
                    "Created"
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
						                                        <Contains>
					                                                <FieldRef Name='{1}'/>
					                                                <Value Type='Text'>{2}</Value>
				                                                </Contains>
				                                            </And>
                                                            <Eq>
						                                        <FieldRef Name='{3}'/>
						                                        <Value Type='Text'>{4}</Value>
					                                        </Eq>
					                                    </And>
		                                            </Where>
		                                            <OrderBy>
			                                            <FieldRef Name='{5}' Ascending='False'/>
		                                            </OrderBy>
	                                            </Query>
                                            </View>", args);

                var piwListItems = piwList.GetItems(query);
                clientContext.Load(piwListItems);
                clientContext.ExecuteQuery();
                return piwListItems;

            }
        }

        //        private ListItemCollection getListItems(ClientContext clientContext, string office, string docket,
        //            bool isMyItems,
        //            Dictionary<string, string> piwListInternalName, List piwList)
        //        {
        //            if (string.IsNullOrEmpty(office)) //All Office
        //            {
        //                CamlQuery query = new CamlQuery();
        //                var args = new string[]
        //                {
        //                    piwListInternalName[Constants.PIWList_colName_IsActive],
        //                    piwListInternalName[Constants.PIWList_colName_DocketNumber],
        //                    docket,
        //                    "Created"
        //                };

        //                query.ViewXml = string.Format(@"<View>
        //	                                            <Query>
        //		                                            <Where>			                                            
        //				                                        <And>
        //					                                        <Eq>
        //						                                        <FieldRef Name='{0}'/>
        //						                                        <Value Type='Bool'>True</Value>
        //					                                        </Eq>					                                        
        //						                                    <Contains>
        //					                                            <FieldRef Name='{1}'/>
        //					                                            <Value Type='Text'>{2}</Value>
        //				                                            </Contains>
        //				                                        </And>
        //		                                            </Where>
        //		                                            <OrderBy>
        //			                                            <FieldRef Name='{3}' Ascending='False'/>
        //		                                            </OrderBy>
        //	                                            </Query>
        //                                            </View>", args);

        //                var piwListItems = piwList.GetItems(query);
        //                clientContext.Load(piwListItems);
        //                clientContext.ExecuteQuery();
        //                return piwListItems;
        //            }
        //            else
        //            {
        //                CamlQuery query = new CamlQuery();
        //                var args = new string[]
        //                {
        //                    piwListInternalName[Constants.PIWList_colName_IsActive],
        //                    piwListInternalName[Constants.PIWList_colName_DocketNumber],
        //                    docket,
        //                    piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator],
        //                    office,
        //                    "Created"
        //                };

        //                query.ViewXml = string.Format(@"<View>
        //	                                            <Query>
        //		                                            <Where>	
        //                                                        <And>		                                            
        //				                                            <And>
        //					                                            <Eq>
        //						                                            <FieldRef Name='{0}'/>
        //						                                            <Value Type='Bool'>True</Value>
        //					                                            </Eq>					                                        
        //						                                        <Contains>
        //					                                                <FieldRef Name='{1}'/>
        //					                                                <Value Type='Text'>{2}</Value>
        //				                                                </Contains>
        //				                                            </And>
        //                                                            <Eq>
        //						                                        <FieldRef Name='{3}'/>
        //						                                        <Value Type='Text'>{4}</Value>
        //					                                        </Eq>
        //					                                    </And>
        //		                                            </Where>
        //		                                            <OrderBy>
        //			                                            <FieldRef Name='{5}' Ascending='False'/>
        //		                                            </OrderBy>
        //	                                            </Query>
        //                                            </View>", args);

        //                var piwListItems = piwList.GetItems(query);
        //                clientContext.Load(piwListItems);
        //                clientContext.ExecuteQuery();
        //                return piwListItems;
        //            }
        //        }



        #endregion


    }
}