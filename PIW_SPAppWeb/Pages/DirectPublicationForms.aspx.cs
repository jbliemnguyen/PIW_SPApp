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
    public partial class DirectPublicationForms : System.Web.UI.Page
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
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
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
                dataTable.Columns.Add("InitiatorOffice", typeof(string));
                dataTable.Columns.Add("OwnerOffice", typeof(string));
                dataTable.Columns.Add("DueDate", typeof(string));
                dataTable.Columns.Add("Created", typeof(string));
                foreach (var listItem in listItemCollection)
                {
                    dataRow = dataTable.Rows.Add();

                    dataRow["Docket"] = listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_DocketNumber]].ToString()
                        : string.Empty;

                    //dataRow["URL"] = helper.getEditFormURL(formType, listItemId, Page.Request,filename);
                    dataRow["URL"] = listItem[piwListInternalName[Constants.PIWList_colName_EditFormURL]] != null
                        ? listItem[piwListInternalName[Constants.PIWList_colName_EditFormURL]].ToString()
                        : string.Empty;

                    dataRow["InitiatorOffice"] =
                        listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeWFInitator]].ToString()
                            : string.Empty;

                    dataRow["OwnerOffice"] =
                        listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeDocumentOwner]] != null
                            ? listItem[piwListInternalName[Constants.PIWList_colName_ProgramOfficeDocumentOwner]].ToString()
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

                    dataRow["Created"] = System.TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(listItem["Created"].ToString())).ToString();
                }

            }

            //Bound to gridview
            HyperLinkField hyperlinkField;

            string[] urls = new string[1] { "URL" };
            hyperlinkField = new HyperLinkField { HeaderText = "Docket Number", DataTextField = "Docket" };
            hyperlinkField.ControlStyle.Width = new Unit(200, UnitType.Pixel);
            hyperlinkField.DataNavigateUrlFields = urls;
            //hyperlinkField.Target = "_blank";
            gridView.Columns.Add(hyperlinkField);


            BoundField boundField = new BoundField
            {
                HeaderText = "Document",
                DataField = "DocumentURL",
                HtmlEncode = false,
            };
            boundField.ControlStyle.Width = new Unit(400, UnitType.Pixel);
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Form Status", DataField = "Status", Visible = false };
            gridView.Columns.Add(boundField);


            boundField = new BoundField { HeaderText = "Initiator Office", DataField = "InitiatorOffice" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Owner Office", DataField = "OwnerOffice" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Due Date", DataField = "DueDate" };
            gridView.Columns.Add(boundField);

            boundField = new BoundField { HeaderText = "Created Date", DataField = "Created" };
            gridView.Columns.Add(boundField);

            gridView.AutoGenerateColumns = false;
            DataView view = dataTable.DefaultView;

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
                Constants.PIWList_FormType_DirectPublicationForm
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
                
                RenderGridView(clientContext);
                lbLastUpdated.Text = "Last Updated: " + DateTime.Now.ToString("g");
            }
        }
    }
}