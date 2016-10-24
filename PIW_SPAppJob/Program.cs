using System;
using System.Collections.Generic;
using System.Configuration;
using System.Deployment.Internal;
using System.Linq;
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
                    

                    //todo: set accession number here, not in below code, use for testing only
                    //setAccessionNumber(clientContext,piwListInternalName);


                    var initiatedPublishedPiwListItem = getInitiatedPublishedPIWListItem(clientContext, piwListInternalName);
                    foreach (var piwListItem in initiatedPublishedPiwListItem)
                    {
                        //check elibrary availalbe
                        if (checkIfFormAvailableInELibrary(piwListItem, piwListInternalName))
                        {
                            UpdateListItem(clientContext, piwListItem, piwListInternalName,Constants.PIWList_FormStatus_PublishedToeLibrary,string.Empty);
                            helper.CreateLog(clientContext, "Running Scheduler Job - update status eLib available piwListItem ID: " + piwListItem["ID"], string.Empty);
                        }
                        
                        //generate print req
                        if (helper.GenerateAndSubmitPrintReqForm(clientContext, piwListItem, CurrentUserLogInID))
                        {
                            helper.CreateLog(clientContext, "Running Scheduler Job - generate print req for piwListItem ID: " + piwListItem["ID"], string.Empty);
                        }

                        //todo: not set accession number here, but in above code, use for testing only
                        string accessionNumber = RandomString(13);
                        UpdateListItem(clientContext, piwListItem, piwListInternalName, string.Empty, accessionNumber);
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

        private static void UpdateListItem(ClientContext clientContext,ListItem listItem,Dictionary<string,string> piwListInternalName,
            string FormStatus,string accessionNumber)
        {
            //accession number
            if (!string.IsNullOrEmpty(accessionNumber))
            {
                listItem[piwListInternalName[Constants.PIWList_colName_AccessionNumber]] = accessionNumber;
            }

            //set the status
            if (!string.IsNullOrEmpty(FormStatus))
            {
                listItem[piwListInternalName[Constants.PIWList_colName_FormStatus]] = FormStatus;
            }
            
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

        private static bool checkIfFormAvailableInELibrary(ListItem piwListItem, Dictionary<string, string> piwListInternalName)
        {
            //todo: connect to eORacle database and check the status of the form
            
            //for now, it return true for all items
            if (piwListItem[piwListInternalName[Constants.PIWList_colName_AccessionNumber]] != null)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        private static void setAccessionNumber(ClientContext clientContext,Dictionary<string,string> piwListInternalName )
        {
            //todo: read the queue, find the piwlistItemID from the queue
            //get the piwList, set the accession number

            //For now, just query all published initiated item, and set accession number to 
            string accessionNumber = string.Empty;
            var initiatedPublishedPiwListItem = getInitiatedPublishedPIWListItem(clientContext, piwListInternalName);

            foreach (var piwListItem in initiatedPublishedPiwListItem)
            {
                accessionNumber = RandomString(13);
                UpdateListItem(clientContext,piwListItem,piwListInternalName,string.Empty,accessionNumber);
            }

        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
