using System;
using System.Collections.Generic;
using System.Configuration;
using System.Deployment.Internal;
using System.Linq;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb;
using PIW_SPAppWeb.Helper;
using FERC.eLibrary.Eps.Common;
using FERC.Common.Queues;

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
                    
                    SetAccessionNumberFromResponseQueue(clientContext,piwListInternalName);


                    var initiatedPublishedPiwListItem = getInitiatedPublishedPIWListItem(clientContext, piwListInternalName);
                    foreach (var piwListItem in initiatedPublishedPiwListItem)
                    {
                        //todo: temporary disable check elibrary availalbe
                        if (checkIfFormAvailableInELibrary(piwListItem, piwListInternalName))
                        {
                            UpdateListItem(clientContext, piwListItem, piwListInternalName, Constants.PIWList_FormStatus_PublishedToeLibrary, string.Empty);
                            helper.CreateLog(clientContext, "Running Scheduler Job - update status eLib available piwListItem ID: " + piwListItem["ID"], string.Empty);
                        }

                        //generate print req
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

        private static void UpdateListItem(ClientContext clientContext, ListItem listItem, Dictionary<string, string> piwListInternalName,
            string FormStatus, string accessionNumber)
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
            bool result = false;
            //if no accession number or published date, don't bother to go ahead, something's wrong
            if ((piwListItem[piwListInternalName[Constants.PIWList_colName_AccessionNumber]] != null) && 
                (piwListItem[piwListInternalName[Constants.PIWList_colName_PublishedDate]] != null))
            {
                //for testing purpose, check if the form is published more than 5 minutes ago, then set it status 
                DateTime publishedDate = System.TimeZone.CurrentTimeZone.ToLocalTime(
                    DateTime.Parse(piwListItem[piwListInternalName[Constants.PIWList_colName_PublishedDate]].ToString()));

                if (publishedDate.AddMinutes(5).CompareTo(DateTime.Now) < 0) //more than 5 minutes
                {
                    result = true;
                }
                else
                {
                    result = false;
                }

                //todo: connect to eORacle database and check the status of the form
                
            }
            

            return result;

        }

        //private static void setAccessionNumber(ClientContext clientContext, Dictionary<string, string> piwListInternalName)
        //{
        //    //todo: read the queue, find the piwlistItemID from the queue
        //    //get the piwList, set the accession number

        //    //For now, just query all published initiated item, and set accession number to 
        //    string accessionNumber = string.Empty;
        //    var initiatedPublishedPiwListItem = getInitiatedPublishedPIWListItem(clientContext, piwListInternalName);

        //    foreach (var piwListItem in initiatedPublishedPiwListItem)
        //    {
        //        accessionNumber = RandomString(13);
        //        UpdateListItem(clientContext, piwListItem, piwListInternalName, string.Empty, accessionNumber);
        //    }

        //}

        //private static Random random = new Random();
        //public static string RandomString(int length)
        //{
        //    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        //    return new string(Enumerable.Repeat(chars, length)
        //      .Select(s => s[random.Next(s.Length)]).ToArray());
        //}

        public static void SetAccessionNumberFromResponseQueue(ClientContext clientContext, Dictionary<string, string> piwListInternalName)
        {
            string responseQueue = ConfigurationManager.AppSettings["responsequeue"];
            QueueReader<QueueMessage<EpsResponse>> queueReader = new QueueReader<QueueMessage<EpsResponse>>(responseQueue);
            QueuePeeker<QueueMessage<EpsResponse>> queuePeeker = new QueuePeeker<QueueMessage<EpsResponse>>(responseQueue);

            if (queuePeeker.Peek())
            {
                do
                {
                    EpsResponse epsResponse = (EpsResponse)queuePeeker.Data.Body;
                    //SPListItem listItem = helper.GetPIWListItemByIDDisRegardIsActive(web, queuePeeker.Data.ClientID.ToString());
                    ListItem listItem = helper.GetPiwListItemById(clientContext, queuePeeker.Data.ClientID.ToString(), true);

                    if (listItem != null)
                    {

                        if (epsResponse.ResponseCode == EpsResponseCode.SUCCESS)
                        {
                            //update accession number
                            string accessionNumber = string.Empty;
                            if (epsResponse.AccessionInformation.Count > 1)
                            {
                                //more than 1 accession number, concat them with the security level
                                foreach (var accessionInformation in epsResponse.AccessionInformation)
                                {
                                    if (string.IsNullOrEmpty(accessionNumber))
                                    {
                                        accessionNumber = string.Format("{0} ({1})", accessionInformation.AccessionNumber,
                                                          accessionInformation.SecurityLevel);    
                                    }
                                    else
                                    {
                                        accessionNumber = string.Format("{0}, {1} ({2})",accessionNumber,accessionInformation.AccessionNumber,
                                                          accessionInformation.SecurityLevel);    
                                    }
                                    
                                }
                            }
                            else//only 1 accession, no need to display security level
                            {
                                accessionNumber = epsResponse.AccessionInformation[0].AccessionNumber;
                            }


                            listItem[piwListInternalName[Constants.PIWList_colName_AccessionNumber]] = accessionNumber;
                        }
                        else //error
                        {
                            // - set the published error
                            if (epsResponse.ErrorMessage.Length > 255)
                            {
                                listItem[piwListInternalName[Constants.PIWList_colName_PublishedError]] = epsResponse.ErrorMessage.Substring(0, 255);
                            }
                            else
                            {
                                listItem[piwListInternalName[Constants.PIWList_colName_PublishedError]] = epsResponse.ErrorMessage;
                            }

                            //string fileName = listItem[SPListSetting.col_PIWList_DocumentFileName].ToString();
                            //todo: send email to epubgrop
                            //SPUtility.SendEmail(web, false, false, PublishError_To_Email, "PIW Publication Failed: " + fileName, "Error Message: " + epsResponse.ErrorMessage);
                        }

                        listItem.Update();

                        //everything good is, remove the current item after finish processing
                        queueReader.Read();
                    }

                } while (queuePeeker.Peek());
                
                //Commit the changes
                clientContext.ExecuteQuery();

                //helper.CreateLog(clientContext, "Running Scheduler Job - assign accession number for piwListItem ID: " + piwListItem["ID"], string.Empty);

            }
        }

    }
}
