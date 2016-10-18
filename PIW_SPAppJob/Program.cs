using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb;
using PIW_SPAppWeb.Helper;

namespace PIW_SPAppJob
{
    class Program
    {
        static SharePointHelper helper = new SharePointHelper();
        static void Main(string[] args)
        {
            string spHostUrl = ConfigurationManager.AppSettings["spHostUrl"];
            try
            {
                using (var clientContext = helper.getElevatedClientContext(spHostUrl))
                {
                    clientContext.Load(clientContext.Web.CurrentUser);
                    clientContext.ExecuteQuery();
                    string CurrentUserLogInID = clientContext.Web.CurrentUser.LoginName;

                    var piwListInternalName = helper.getInternalColumnNames(clientContext, Constants.PIWListName);
                    var piwListItemCol = getInitiatedPublishedPIWListItem(clientContext, piwListInternalName);

                    foreach (var piwListItem in piwListItemCol)
                    {
                        if (checkIfFormAvailableInELibrary(piwListItem))
                        {
                            UpdateListItem(clientContext, piwListItem, piwListInternalName);
                            helper.CreateLog(clientContext, "Running Scheduler Job - update status eLib available piwListItem ID: " + piwListItem["ID"], string.Empty);
                        }
                        
                        if (helper.GenerateAndSubmitPrintReqForm(clientContext, piwListItem, CurrentUserLogInID))
                        {
                            helper.CreateLog(clientContext, "Running Scheduler Job - generate print req for piwListItem ID: " + piwListItem["ID"], string.Empty);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                using (var clientContext = helper.getElevatedClientContext(spHostUrl))
                    {
                        helper.LogError(clientContext, exc, string.Empty, String.Empty);
                    }
                
            }
        }

        private static void UpdateListItem(ClientContext clientContext,ListItem listItem,Dictionary<string,string> piwListInternalName)
        {
            //todo: set the accession number and published status

            //set the status
            listItem[piwListInternalName[Constants.PIWList_colName_FormStatus]] =
                Constants.PIWList_FormStatus_PublishedToeLibrary;
            listItem.Update();
            clientContext.ExecuteQuery();
        }

        private static ListItemCollection getInitiatedPublishedPIWListItem(ClientContext clientContext, Dictionary<string, string> piwListInternalName)
        {
            List piwList = clientContext.Web.Lists.GetByTitle(Constants.PIWListName);

            CamlQuery query = new CamlQuery();
            var args = new string[]
            {
                piwListInternalName[Constants.PIWList_colName_IsActive],
                piwListInternalName[Constants.PIWList_colName_FormStatus],
                Constants.PIWList_FormStatus_PublishInitiated
            };

            query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>			                                            
				                                        <And>
					                                        <Eq>
						                                        <FieldRef Name='{0}'/>
						                                        <Value Type='Bool'>True</Value>
					                                        </Eq>					                                            
						                                    <Eq>
							                                    <FieldRef Name='{1}'/>
							                                    <Value Type='Text'>{2}</Value>
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

        private static bool checkIfFormAvailableInELibrary(ListItem piwListItem)
        {
            //todo: connect to eORacle database and check the status of the form
            
            //for now, it return true for all items
            return true;
        }
    }
}
