using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.UserProfiles;
using File = Microsoft.SharePoint.Client.File;
using ListItem = Microsoft.SharePoint.Client.ListItem;

namespace PIW_SPAppWeb.Helper
{
    public class SharePointHelper
    {
        //variable        
        public string listItemID;

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

        public SharePointHelper() { }

        public SharePointHelper(string listItemID)
        {
            this.listItemID = listItemID;
        }

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

            //newItem[internalNameList[Constants.PIWList_colName_FormStatus]] = formStatus;


            //if (!string.IsNullOrEmpty(previousFormStatus))
            //{
            //    newItem[internalNameList[Constants.PIWList_colName_PreviousFormStatus]] = previousFormStatus;
            //}


            User user = context.Web.CurrentUser;
            context.Load((user));
            context.ExecuteQuery();
            newItem[internalNameList[Constants.PIWList_colName_WorkflowInitiator]] = user;

            //set FormType
            newItem[internalNameList[Constants.PIWList_colName_FormType]] = formType;

            newItem.Update();
            context.ExecuteQuery();



            return newItem;
        }

        public ListItem GetPiwListItemById(ClientContext clientContext,string id,bool ignoreIsActive)
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

                    if (
                        !listItem[piwInternalNameList[Constants.PIWList_colName_FormStatus]].ToString()
                            .Equals(Constants.PIWList_FormStatus_Pending))
                    {
                        throw new ApplicationException("Workflow not exists");
                    }
                }
            }

            return listItem;
                
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



        public void UploadDocumentContentStream(ClientContext clientContext, Stream fileStream, string libraryName, string subFolder, string fileName, string securityLevel)
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


            //clientContext.Load(files, includes => includes.Include(
            //    i => i.ListItemAllFields,
            //    i => i.ListItemAllFields["ID"],
            //    i => i.ListItemAllFields[internalColumnNames[Constants.PIWDocuments_colName_SecurityLevel]],
            //    i => i.ListItemAllFields[internalColumnNames[Constants.PIWDocuments_colName_EPSPassed]],
            //    i => i.ListItemAllFields[internalColumnNames[Constants.PIWDocuments_colName_EPSError]]
            //    ));



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

        public void RemoveDocument(ClientContext clientContext, string subFolder, string libraryName, string Id)
        {
            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();
            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, libraryName, subFolder);
            var documentList = getAllDocuments(clientContext, uploadSubFolderURL, false);

            foreach (File file in documentList)
            {
                if (file.ListItemAllFields.Id.ToString().Equals(Id))
                {
                    file.DeleteObject();
                    clientContext.ExecuteQuery();
                    break;
                }
            }


        }
        #endregion

        #region Utilities

        public void LogError(HttpContext httpContext, Exception exc, string listItemID, string pageName)
        {
            //This is expected exception after Page.Redirect --> ignore it??? TEst it
            if (exc is System.Threading.ThreadAbortException)
            {
                return;
            }

            //create new log error - this should have its own clientContext
            var spContext = SharePointContextProvider.Current.GetSharePointContext(httpContext);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                List errorLogList = clientContext.Web.Lists.GetByTitle(Constants.ErrorLogListName);
                var errorLogInternalNameList = getInternalColumnNames(clientContext, Constants.ErrorLogListName);

                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = errorLogList.AddItem(itemCreateInfo);

                newItem[errorLogInternalNameList[Constants.col_ErrorLog_ErrorPageName]] = pageName;

                //set current user name
                clientContext.Load(clientContext.Web.CurrentUser);
                clientContext.ExecuteQuery();
                newItem[errorLogInternalNameList[Constants.col_ErrorLog_User]] = clientContext.Web.CurrentUser;

                if (exc.InnerException != null)
                {
                    newItem[errorLogInternalNameList[Constants.col_ErrorLog_ErrorMessage]] = exc.Message + " - Inner Exception: " + exc.InnerException.Message;
                }
                else
                {
                    newItem[errorLogInternalNameList[Constants.col_ErrorLog_ErrorMessage]] = exc.Message;
                }
                
                newItem.Update();
                clientContext.ExecuteQuery();//we need to create item first before set lookup field.


                if (!string.IsNullOrEmpty(listItemID))
                {
                    //get piwListItem reference
                    FieldLookupValue lv = new FieldLookupValue {LookupId = int.Parse(listItemID)};
                    newItem[errorLogInternalNameList[Constants.col_ErrorLog_PIWListItem]] = lv;
                    newItem.Update();
                    clientContext.ExecuteQuery();
                }

            }
        }

        #endregion
    }


}