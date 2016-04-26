using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.UserProfiles;
using File = Microsoft.SharePoint.Client.File;
using ListItem = Microsoft.SharePoint.Client.ListItem;
using FERC.FOL.ATMS.Remote.Interfaces;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;
using System.Text;

//using FERC.FOL.ATMS.Structure;

namespace PIW_SPAppWeb.Helper
{
    public class SharePointHelper
    {

        #region PIW List
        //when item first created, it should have IsActive set to false
        //this flag will turn to true after it is first Saved/Submitted
        //We have to create ListItem first to accommodate Upload multiple documents right away
        public ListItem createNewPIWListItem(ClientContext context, string formType)
        {
            List piwList = context.Web.Lists.GetByTitle(Constants.PIWListName);
            var internalNameList = getInternalColumnNames(context, Constants.PIWListName);

            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            ListItem newItem = piwList.AddItem(itemCreateInfo);


            User user = context.Web.CurrentUser;
            context.Load(context.Web.CurrentUser);
            context.ExecuteQuery();

            newItem[internalNameList[Constants.PIWList_colName_WorkflowInitiator]] = user;

            //set FormType
            newItem[internalNameList[Constants.PIWList_colName_FormType]] = formType;

            newItem.Update();
            context.ExecuteQuery();

            return newItem;
        }

        public ListItem GetPiwListItemById(ClientContext clientContext, string id, bool ignoreIsActive)
        {
            var piwInternalNameList = getInternalColumnNames(clientContext, Constants.PIWListName);
            Web site = clientContext.Web;
            List piwList = site.Lists.GetByTitle(Constants.PIWListName);

            ListItem listItem = piwList.GetItemById(int.Parse(id));
            clientContext.Load(listItem);
            clientContext.ExecuteQuery();

            //****************************************************************

            if (!ignoreIsActive)
            {
                //If form is deleted, user won't be able to open
                if (!bool.Parse(listItem[piwInternalNameList[Constants.PIWList_colName_IsActive]].ToString()))
                {
                    //isActive = false, then check status, if status is Pending, it is OK to return item,
                    //otherwise, the item is deleted, throw exception

                    if (!listItem[piwInternalNameList[Constants.PIWList_colName_FormStatus]].ToString()
                            .Equals(Constants.PIWList_FormStatus_Pending))
                    {
                        //throw new ApplicationException("Workflow not exists");
                        return null;
                    }
                }
            }

            return listItem;

        }

        public ListItem SetCitationNumberFieldInPIWList(ClientContext clientContext, string piwListItemID, string citationNumber)
        {
            var piwListinternalName = getInternalColumnNames(clientContext, Constants.PIWListName);
            ListItem listItem = GetPiwListItemById(clientContext, piwListItemID, false);

            listItem[piwListinternalName[Constants.PIWList_colName_CitationNumber]] = citationNumber;
            listItem.Update();
            clientContext.ExecuteQuery();
            return listItem;
        }

        public ListItem deleteAssociatedCitationNumberListItem(ClientContext clientContext, string piwListItemID)
        {
            ListItemCollection citationList = GetCitationNumberListItemFromPiwListId(clientContext, piwListItemID);
            citationList[0].DeleteObject();

            //delete citation number field in piwlist
            var piwListinternalName = getInternalColumnNames(clientContext, Constants.PIWListName);
            ListItem listItem = GetPiwListItemById(clientContext, piwListItemID, false);

            listItem[piwListinternalName[Constants.PIWList_colName_CitationNumber]] = string.Empty;
            listItem.Update();
            clientContext.ExecuteQuery();
            return listItem;
        }

        public void ReleaseCitationNumberForDeletedListItem(ClientContext clientContext, string piwListItemId)
        {
            ListItemCollection citationList = GetCitationNumberListItemFromPiwListId(clientContext, piwListItemId);
            if (citationList.Count > 0)
            {
                var citationListInternalCoumnNames = getInternalColumnNames(clientContext, Constants.CitationNumberListName);
                citationList[0][citationListInternalCoumnNames[Constants.CitationNumberList_colName_Status]] = Constants.CitationNumber_DELETED_Status;
                citationList[0][citationListInternalCoumnNames[Constants.CitationNumberList_colName_DeletedDate]] = DateTime.Now.ToString();
                citationList[0][citationListInternalCoumnNames[Constants.CitationNumberList_colName_PIWList]] = string.Empty;

                citationList[0].Update();
                clientContext.ExecuteQuery();
            }
        }

        public ListItemCollection GetCitationNumberListItemFromPiwListId(ClientContext clientContext, string piwListItemID)
        {
            List citationNumberList = clientContext.Web.Lists.GetByTitle(Constants.CitationNumberListName);
            var citationNumberInternalNameList = getInternalColumnNames(clientContext, Constants.CitationNumberListName);
            CamlQuery query = new CamlQuery();
            query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>
			                                            <Eq>
				                                            <FieldRef Name='{0}' LookupId='TRUE' />
				                                            <Value Type='Lookup'>{1}</Value>
			                                            </Eq>			
		                                            </Where>		
	                                            </Query>
                                            </View>", citationNumberInternalNameList[Constants.CitationNumberList_colName_PIWList], piwListItemID);

            var citationListItems = citationNumberList.GetItems(query);

            clientContext.Load(citationListItems);
            clientContext.ExecuteQuery();

            return citationListItems;

        }
        #endregion

        #region PIW Documents
        public void CreatePIWDocumentsSubFolder(ClientContext clientContext, string folderName)
        {
            try
            {
                List list = clientContext.Web.Lists.GetByTitle(Constants.PIWDocuments_DocumentLibraryName);
                ListItemCreationInformation info = new ListItemCreationInformation();
                info.UnderlyingObjectType = FileSystemObjectType.Folder;
                info.LeafName = folderName.Trim();//Trim for spaces.Just extra check
                ListItem newItem = list.AddItem(info);
                newItem["Title"] = folderName;
                newItem.Update();
                clientContext.ExecuteQuery();

            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }



        public string UploadDocumentContentStream(ClientContext clientContext, Stream fileStream, string libraryName, string subFolder, string fileName, string securityLevel)
        {

            //Dictionary<string, string> internalNameList = PopulateInternalNameList(clientContext, Constants.PIWDocuments_DocumentLibraryName);
            var internalNameList = getInternalColumnNames(clientContext, Constants.PIWDocuments_DocumentLibraryName);




            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();

            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, libraryName, subFolder);


            Folder uploadSubFolder = clientContext.Web.GetFolderByServerRelativeUrl(uploadSubFolderURL);
            clientContext.ExecuteQuery();//file not found exception if the folder is not exist, let it crash because it is totally wrong somewhere

            FileCreationInformation flciNewFile = new FileCreationInformation
            {
                ContentStream = fileStream,
                Url = System.IO.Path.GetFileName(fileName),
                Overwrite = false
            };

            Microsoft.SharePoint.Client.File uploadFile = uploadSubFolder.Files.Add(flciNewFile);
            clientContext.Load(uploadFile);

            uploadFile.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_SecurityLevel]] = securityLevel;
            uploadFile.ListItemAllFields.Update();

            clientContext.ExecuteQuery();
            return uploadFile.Name;

        }

        private FileCollection getAllDocuments(ClientContext ctx, string uploadSubFolderURL, bool includeListItemAllFields)
        {
            Folder folder = ctx.Web.GetFolderByServerRelativeUrl(uploadSubFolderURL);

            FileCollection files = folder.Files;
            ctx.Load(files);
            ctx.Load(files, includes => includes.Include(i => i.ListItemAllFields.Id));

            if (includeListItemAllFields)
            {
                ctx.Load(files, includes => includes.Include(i => i.ListItemAllFields));
            }

            ctx.ExecuteQuery();//file not found exception if the folder is not exist, let it crash because it is totally wrong somewhere
            return files;
        }

        public System.Data.DataTable getAllDocumentsTable(ClientContext clientContext, string subFoder, string libraryName)
        {
            var result = new System.Data.DataTable();
            result.Columns.Add("ID");
            result.Columns.Add("Name");
            result.Columns.Add("URL");
            result.Columns.Add("Security Level");
            result.Columns.Add("EPS Passed");
            result.Columns.Add("EPS Error");


            //Dictionary<string, string> internalNameList = PopulateInternalNameList(clientContext, Constants.PIWDocuments_DocumentLibraryName);
            var internalNameList = getInternalColumnNames(clientContext, Constants.PIWDocuments_DocumentLibraryName);

            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();

            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, libraryName, subFoder);

            var documentList = getAllDocuments(clientContext, uploadSubFolderURL, true);

            foreach (File file in documentList)
            {
                System.Data.DataRow row = result.NewRow();
                row["ID"] = file.ListItemAllFields["ID"];
                row["Name"] = file.Name;
                row["URL"] = uploadSubFolderURL + "/" + row["Name"];
                row["Security Level"] =
                    file.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_SecurityLevel]];
                row["EPS Passed"] =
                    getEPSPassedIconHTML(file.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_EPSPassed]].ToString());
                row["EPS Error"] =
                    file.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_EPSError]];
                result.Rows.Add(row);

            }

            return result;
        }

        private string getEPSPassedIconHTML(string epsPassedStatus)
        {
            if (epsPassedStatus.Equals(Constants.PIWDocuments_EPSPassed_Option_True))
            {
                return @"<span class='glyphicon glyphicon-ok' style='color: green'></span>";
            }
            else if (epsPassedStatus.Equals(Constants.PIWDocuments_EPSPassed_Option_False))
            {
                return @"<span class='glyphicon glyphicon-remove' style='color: red'></span>";
            }
            else
            {
                return @"<img src='..\Scripts\spinner\spinner-large.gif' style='width:18px;height:18px;'></span>";
            }
        }

        public string RemoveDocument(ClientContext clientContext, string subFolder, string libraryName, string Id)
        {
            string removedFileName = string.Empty;
            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();
            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, libraryName, subFolder);
            var documentList = getAllDocuments(clientContext, uploadSubFolderURL, false);

            foreach (File file in documentList)
            {
                if (file.ListItemAllFields.Id.ToString().Equals(Id))
                {
                    removedFileName = file.Name;
                    file.DeleteObject();
                    clientContext.ExecuteQuery();
                    break;
                }
            }
            return removedFileName;


        }
        #endregion

        #region PIWListHistory

        public void CreatePIWListHistory(ClientContext clientContext, string listItemID, string action, string FormStatus)
        {
            List piwlisthistory = clientContext.Web.Lists.GetByTitle(Constants.PIWListHistory_ListName);
            var piwlistHistoryInternalNameList = getInternalColumnNames(clientContext, Constants.PIWListHistory_ListName);

            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            ListItem newItem = piwlisthistory.AddItem(itemCreateInfo);

            clientContext.Load(clientContext.Web.CurrentUser);
            clientContext.ExecuteQuery();
            newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_User]] = clientContext.Web.CurrentUser;

            newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_Action]] = action;
            newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_FormStatus]] = FormStatus;

            newItem.Update();
            clientContext.ExecuteQuery();//we need to create item first before set lookup field.

            if (!string.IsNullOrEmpty(listItemID))
            {
                //get piwListItem reference
                FieldLookupValue lv = new FieldLookupValue { LookupId = int.Parse(listItemID) };
                newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_PIWList]] = lv;
                newItem.Update();
                clientContext.ExecuteQuery();
            }

        }

        public ListItemCollection getHistoryListByPIWListID(ClientContext clientContext, string piwListItemID)
        {
            List historyList = clientContext.Web.Lists.GetByTitle(Constants.PIWListHistory_ListName);
            var historyListInternalNameList = getInternalColumnNames(clientContext, Constants.PIWListHistory_ListName);
            CamlQuery query = new CamlQuery();
            query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>
			                                            <Eq>
				                                            <FieldRef Name='{0}' LookupId='TRUE' />
				                                            <Value Type='Lookup'>{1}</Value>
			                                            </Eq>			
		                                            </Where>		
	                                            </Query>
                                            </View>", historyListInternalNameList[Constants.PIWListHistory_colName_PIWList], piwListItemID);

            var historyListItems = historyList.GetItems(query);

            clientContext.Load(historyListItems);
            clientContext.ExecuteQuery();

            return historyListItems;



        }

        public System.Data.DataTable getHistoryListTable(ClientContext clientContext, string piwListItemID)
        {
            var historyList = getHistoryListByPIWListID(clientContext,piwListItemID);
            var historyListInternalNameList = getInternalColumnNames(clientContext, Constants.PIWListHistory_ListName);
            //TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(System.TimeZone.CurrentTimeZone.ToLocalTime());
            var result = new System.Data.DataTable();
            result.Columns.Add("Created");
            result.Columns.Add("User");
            result.Columns.Add("Action");
            result.Columns.Add("FormStatus");
            foreach (ListItem historyItem in historyList)
            {
                System.Data.DataRow row = result.NewRow();

                if (historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Created]] != null)
                {
                    var createdUTC = DateTime.Parse(historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Created]].ToString());


                    DateTime created = System.TimeZone.CurrentTimeZone.ToLocalTime(createdUTC);
                    row["Created"] = created;

                }
                else
                {
                    row["Created"] = string.Empty;
                }

                row["User"] = historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_User]] != null
                    ? ((FieldUserValue) historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_User]]).LookupValue: string.Empty;

                row["Action"] = historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Action]] != null
                    ? historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Action]].ToString(): string.Empty;

                row["FormStatus"] = historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_FormStatus]] != null
                    ? historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_FormStatus]].ToString(): string.Empty;

                result.Rows.Add(row);
            }

            

            return result;





            //StringBuilder html = new StringBuilder("<table border='1' cellpadding='10'>");
            //html.Append("<tr style='font-weight:bold'><td>Date and Time</td><td>User</td><td>Action</td><td>Post-Action PIW Status</td></tr>");
            //foreach (ListItem historyItem in historyList)
            //{
            //    html.AppendLine("<tr>");

            //    html.Append("<td>");
            //    html.Append(historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Created]] != null ? historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Created]].ToString() : string.Empty);
            //    html.Append("</td>");

            //    html.Append("<td>");
            //    html.Append(historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_User]] != null ? ((FieldUserValue)historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_User]]).LookupValue : string.Empty);
            //    html.Append("</td>");

            //    html.Append("<td>");
            //    html.Append(historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Action]] != null ? historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Action]].ToString() : string.Empty);
            //    html.Append("</td>");

            //    html.Append("<td>");
            //    html.Append(historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_FormStatus]] != null ? historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_FormStatus]].ToString() : string.Empty);
            //    html.Append("</td>");

            //    html.AppendLine("</tr>");
            //}
            //html.AppendLine("</table>");

            //return html.ToString();
        }
        #endregion

        #region Utils

        /// <summary>
        /// check if a docket is existing in P8 
        /// result is set back it its corresponding docket inside the dictionary parameter
        /// </summary>
        public void CheckDocketNumber(string strdocket, ref string errorMessage, bool isCNF, bool isByPass)
        {
            //this will temporary remove the docket number validation
            if (isByPass)
            {
                return;
            }

            if (isCNF)
            {
                return;
            }

            if (strdocket.Equals("non-docket", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string[] dockets = strdocket.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, bool> docketDictionary = new Dictionary<string, bool>();

            try
            {
                var m_RemoteObject = getWorkSetRemoteObject();
                foreach (string fullDocket in dockets)
                {
                    string docketFullTrimmed = fullDocket.Trim();
                    bool validDocket = true;
                    //Add docket to dictionary
                    if (!docketDictionary.ContainsKey(docketFullTrimmed))
                    {
                        //FullDocket: ER14-543-000 or EL02-60-007
                        int docketLength = docketFullTrimmed.LastIndexOf("-");

                        if (docketLength < 0)//invalid
                        {
                            validDocket = false;
                        }
                        else
                        {
                            string docket = docketFullTrimmed.Substring(0, docketLength);
                            string subdocket = docketFullTrimmed.Substring(docketLength + 1, docketFullTrimmed.Length - docket.Length - 1);
                            validDocket = DocketExist(docket, subdocket, m_RemoteObject);
                        }

                        if (!validDocket)
                        {
                            if (string.IsNullOrEmpty(errorMessage))//first invalid docket
                            {
                                errorMessage = "invalid Docket: " + fullDocket;
                            }
                            else
                            {
                                errorMessage = errorMessage + ", " + docketFullTrimmed;
                            }
                        }

                        docketDictionary.Add(docketFullTrimmed, false);//add docket to dictionary to avoid check again if user put them twice                        
                    }
                }
            }
            catch (Exception exc)
            {
                //LogError(Context,exc, string.Empty, "ATMS Connection");
                errorMessage = Constants.ATMSRemotingServiceConnectionError;
            }
        }

        public IWorkSetOps getWorkSetRemoteObject()
        {
            string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            IChannel[] myIChannelArray = ChannelServices.RegisteredChannels;
            if (myIChannelArray.Length == 0)
                System.Runtime.Remoting.RemotingConfiguration.Configure(configPath, true);

            IWorkSetOps m_RemoteObject = (IWorkSetOps)
                             Activator.GetObject(typeof(IWorkSetOps),
                                                 ConfigurationManager.AppSettings["atmsRemoteURL"] + "/WorkSetOps");

            return m_RemoteObject;
        }

        public bool DocketExist(string docket, string subdocket, IWorkSetOps m_RemoteObject)
        {
            var atmsDocket = m_RemoteObject.GetWorkSetsByLabel(docket, subdocket, false, true);
            return (atmsDocket.Count > 0);
        }

        public Dictionary<string, string> getInternalColumnNames(ClientContext clientContext, string listName)
        {
            {
                //HttpRuntime httpRT = new HttpRuntime();
                Cache cache = HttpRuntime.Cache;

                if (cache[listName] != null)
                {
                    return (Dictionary<string, string>)cache[listName];
                }
                else
                {
                    //Query the new list from SharePoint
                    var internalColumnList = new Dictionary<string, string>();
                    List list = clientContext.Web.Lists.GetByTitle(listName);

                    FieldCollection fields = list.Fields;

                    clientContext.Load(fields);
                    clientContext.ExecuteQuery();

                    foreach (var field in fields)
                    {
                        if (!internalColumnList.ContainsKey(field.Title))
                        {
                            internalColumnList.Add(field.Title, field.InternalName);
                        }

                    }

                    //Add the new object to cache
                    cache.Insert(listName, internalColumnList, null, DateTime.Now.AddHours(10), System.Web.Caching.Cache.NoSlidingExpiration);
                    return internalColumnList;
                }
            }

        }

        public void LogError(HttpContext httpContext, Exception exc, string listItemID, string pageName)
        {
            //This is expected exception after Page.Redirect --> ignore it??? TEst it
            if (exc is System.Threading.ThreadAbortException)
            {
                return;
            }

            //create new log error - this should have its own clientContext
            using (var clientContext = SharePointContextProvider.Current.GetSharePointContext(httpContext).CreateUserClientContextForSPHost())
            {
                List errorLogList = clientContext.Web.Lists.GetByTitle(Constants.ErrorLogListName);
                var errorLogInternalNameList = getInternalColumnNames(clientContext, Constants.ErrorLogListName);

                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = errorLogList.AddItem(itemCreateInfo);

                //set current user name
                clientContext.Load(clientContext.Web.CurrentUser);
                clientContext.ExecuteQuery();
                newItem[errorLogInternalNameList[Constants.ErrorLog_colName_User]] = clientContext.Web.CurrentUser;

                newItem[errorLogInternalNameList[Constants.ErrorLog_colName_ErrorPageName]] = pageName;

                if (exc.InnerException != null)
                {
                    newItem[errorLogInternalNameList[Constants.ErrorLog_colName_ErrorMessage]] = exc.Message + " - Inner Exception: " + exc.InnerException.Message;
                }
                else
                {
                    newItem[errorLogInternalNameList[Constants.ErrorLog_colName_ErrorMessage]] = exc.Message;
                }

                newItem.Update();
                clientContext.ExecuteQuery();//we need to create item first before set lookup field.


                if (!string.IsNullOrEmpty(listItemID))
                {
                    //get piwListItem reference
                    FieldLookupValue lv = new FieldLookupValue { LookupId = int.Parse(listItemID) };
                    newItem[errorLogInternalNameList[Constants.ErrorLog_colName_PIWListItem]] = lv;
                    newItem.Update();
                    clientContext.ExecuteQuery();
                }

            }
        }

        public bool IsUserMemberOfGroup(ClientContext clientContext, User user, string groupName)
        {
            //Load group
            clientContext.Load(user.Groups);
            clientContext.ExecuteQuery();
            return user.Groups.Cast<Group>()
              .Any(g => g.Title == groupName);
        }

        /// <summary>
        /// Return the first docket number found in input
        /// If no docket found, return the whole input
        /// </summary>
        /// <param name="filename"></param>
        public string ExtractDocket(string filename)
        {
            string pattern = @"^(\w+)-(\d+)-\d\d\d";
            string docket = string.Empty;

            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(filename, pattern);
            if (match.Success)
            {
                docket = match.Value;
            }
            else
            {
                docket = filename.Substring(0, filename.LastIndexOf("."));
            }

            return docket;
        }

        public int getDocumentCategoryNumber(string documentCategory)
        {
            int documentCategoryNumber = 0;
            switch (documentCategory)
            {
                case Constants.PIWList_DocCat_DelegatedErrata:
                case Constants.PIWList_DocCat_DelegatedLetter:
                case Constants.PIWList_DocCat_DelegatedNotice:
                case Constants.PIWList_DocCat_DelegatedOrder:
                    documentCategoryNumber = 62;
                    break;
                case Constants.PIWList_DocCat_OALJ:
                case Constants.PIWList_DocCat_OALJErrata:
                    documentCategoryNumber = 63;
                    break;
                case Constants.PIWList_DocCat_NoticeErrata:
                case Constants.PIWList_DocCat_Notice:
                    documentCategoryNumber = 61;
                    break;
                default:
                    throw new Exception("Unknown document category: " + documentCategory);
                    break;
            }

            return documentCategoryNumber;
        }

        /// <summary>
        /// Check if form is not saved/changed after it is opened
        /// for concurrency checking
        /// </summary>
        /// <param name="listItem"></param>
        /// <param name="expectingFormStatus"></param>
        /// <returns></returns>
        public bool CheckIfListItemChanged(ClientContext clientContext, ListItem listItem, DateTime viewModifiedDateTime)
        {
            var piwListInternalColumnNames = getInternalColumnNames(clientContext, Constants.PIWListName);
            DateTime currentModifiedDateTime;
            if (listItem[piwListInternalColumnNames[Constants.PIWList_colName_Modified]] != null)
            {
                currentModifiedDateTime = DateTime.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_Modified]].ToString());
                return DateTime.Compare(currentModifiedDateTime, viewModifiedDateTime) != 0;
            }
            return false;
        }

        public void RedirectToSourcePage(HttpRequest request, HttpResponse response)
        {
            //redirect to source page
            //https://dev.spapps.ferc.gov/PIW_SPAppWeb/pages/EditStandardForm.aspx?SPHostUrl=https%3a%2f%2ffdc1s-sp23wfed2.ferc.gov%2fpiw&SPLanguage=en-US&SPClientTag=0&SPProductNumber=15.0.4727.1000&SPAppWebUrl=https%3a%2f%2fapp-3f613e5e650fd4.dev.spapps.ferc.gov%2fpiw%2fPIW_SPApp&ID=41&Source=StandardForm.aspx
            string sourcePage = request.QueryString["Source"];
            RedirectToAPage(request,response,sourcePage);

        }

        /// <summary>
        /// redirect the page to a specific page
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="PageName">PIWList.aspx</param>
        public void RedirectToAPage(HttpRequest request, HttpResponse response, string PageName)
        {
            //https://dev.spapps.ferc.gov/PIW_SPAppWeb/pages/EditStandardForm.aspx
            
            var newURLPage = GetPageUrl(request, PageName);

            if (!string.IsNullOrEmpty(newURLPage))
            {
                response.Redirect(newURLPage, false);
            }
        }


        /// <summary>
        /// return full URL of a page, with all sharepont app settings
        /// </summary>
        /// <param name="request">HTTPRequest</param>
        /// <param name="PageName">FileName of Page, ie: EditStandardForm.aspx</param>
        /// <returns></returns>
        private string GetPageUrl(HttpRequest request, string PageName)
        {
            const string pattern = "/pages/";
            int length = request.Url.ToString().IndexOf(pattern, StringComparison.CurrentCultureIgnoreCase) + pattern.Length;
            string newURLPage = request.Url.ToString().Substring(0, length) + PageName;

            var args = new string[]
            {
                newURLPage,
                request.QueryString["SPHostUrl"],
                request.QueryString["SPLanguage"],
                request.QueryString["SPClientTag"],
                request.QueryString["SPProductNumber"],
                request.QueryString["SPAppWebUrl"]
            };

            var fullPageURL = string.Format("{0}?SPHostUrl={1}&SPLanguage={2}$SPClientTag={3}&SPProductNumber={4}&SPAppWebUrl={5}",args);
            return fullPageURL;
        }

        public void RefreshPage(HttpRequest request, HttpResponse response)
        {
            string PageURL = request.Url.ToString();
            if (!string.IsNullOrEmpty(PageURL))
            {
                response.Redirect(PageURL, false);
            }
        }


        public string getEditFormURL(string formType,string listItemId,HttpRequest  request )
        {
            string result = string.Empty;
            string PageFileName = string.Empty;

            if (formType == Constants.PIWList_FormType_StandardForm)
            {
                PageFileName = Constants.Page_EditStandardForm;
            }
            else if (formType == Constants.PIWList_FormType_AgendaForm)
            {
                PageFileName = Constants.Page_EditAgendaForm;
            }
            else if (formType == Constants.PIWList_FormType_DirectPublicationForm)
            {
                PageFileName = Constants.Page_EditDirectPublicationForm;
            }

            result = String.Format("{0}&ID={1}",GetPageUrl(request, PageFileName),listItemId);
            return result;
        }
        
        #endregion





    }

}


