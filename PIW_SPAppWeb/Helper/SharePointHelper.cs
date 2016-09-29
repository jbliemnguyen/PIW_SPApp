using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Web;
using System.Web.Caching;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FERC.eLibrary.Dvvo.Common;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.UserProfiles;
using File = Microsoft.SharePoint.Client.File;
using ListItem = Microsoft.SharePoint.Client.ListItem;
using FERC.FOL.ATMS.Remote.Interfaces;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;
using System.Text;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using Group = Microsoft.SharePoint.Client.Group;
using Page = System.Web.UI.Page;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

//using FERC.eLibrary.Dvvo.Facade;
//using FERC.eLibrary.Dvvo.Common;

namespace PIW_SPAppWeb.Helper
{
    public class SharePointHelper
    {
        #region PIW List
        //when item first created, it should have IsActive set to false
        //this flag will turn to true after it is first Saved/Submitted
        //We have to create ListItem first to accommodate Upload multiple documents right away
        public ListItem createNewPIWListItem(ClientContext clientContext, string formType, string currentUserLoginID)
        {
            List piwList = clientContext.Web.Lists.GetByTitle(Constants.PIWListName);
            var internalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            ListItem newItem = piwList.AddItem(itemCreateInfo);


            User user = clientContext.Web.EnsureUser(currentUserLoginID);
            clientContext.Load(user);

            PeopleManager peopleManager = new PeopleManager(clientContext);
            PersonProperties currentUserProperties = peopleManager.GetPropertiesFor(currentUserLoginID);
            clientContext.Load(currentUserProperties, p => p.Title);

            clientContext.ExecuteQuery();

            newItem[internalNameList[Constants.PIWList_colName_WorkflowInitiator]] = user;
            //newItem[internalNameList[Constants.PIWList_colName_DocumentOwner]] = user;

            //set the program office initiator to the value from user profile 
            if (!string.IsNullOrEmpty(currentUserProperties.Title))
            {
                string department = currentUserProperties.Title;
                newItem[internalNameList[Constants.PIWList_colName_ProgramOfficeWFInitator]] = department;
                //newItem[internalNameList[Constants.PIWList_colName_ProgramOfficeDocumentOwner]] = newItem[internalNameList[Constants.PIWList_colName_ProgramOfficeWFInitator]];
            }

            //set FormType
            newItem[internalNameList[Constants.PIWList_colName_FormType]] = formType;

            newItem.Update();
            clientContext.ExecuteQuery();

            return newItem;
        }

        public ListItem GetPiwListItemById(ClientContext clientContext, string id, bool ignoreIsActive)
        {
            var piwInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
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
            var piwListinternalName = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
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
            var piwListinternalName = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
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
                var citationListInternalCoumnNames = getInternalColumnNamesFromCache(clientContext, Constants.CitationNumberListName);
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
            var citationNumberInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.CitationNumberListName);
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

        public void SaveReOpenInfoAndStatusAndComment(ClientContext clientContext, ListItem listItem, string FormStatus, string PreviousFormStatus, string comment, string CurrentUserLogInName)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            //comment
            if (!string.IsNullOrEmpty(comment))
            {
                SetCommentURLHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, comment);
            }

            //clear accession number
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_AccessionNumber]] = string.Empty;

            //set ReOpen flag
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_Re_Opened]] = true;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SaveFormStatusAndComment(ClientContext clientContext, ListItem listItem, string FormStatus, string PreviousFormStatus, enumAction action, string comment, string CurrentUserLogInName)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            if ((action == enumAction.Recall) || (action == enumAction.Reject))
            {

                if (!string.IsNullOrEmpty(comment))
                {
                    //recall / reject comment field-single line
                    if (comment.Length <= 255)
                    {
                        listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] = comment;
                    }
                    else
                    {
                        listItem[piwListInternalColumnNames[Constants.PIWList_colName_RecallRejectComment]] =
                            comment.Substring(0, 255);
                    }
                }
            }

            //comment
            if (!string.IsNullOrEmpty(comment))
            {
                SetCommentURLHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, comment);
            }

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SaveDeleteInfoAndStatusAndComment(ClientContext clientContext, ListItem listItem, string FormStatus, string PreviousFormStatus, string comment, string CurrentUserLogInName)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            clientContext.Load(clientContext.Web.CurrentUser, user => user.Id);
            clientContext.ExecuteQuery();

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            //comment
            if (!string.IsNullOrEmpty(comment))
            {
                SetCommentURLHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, comment);
            }

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_IsActive]] = false;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_CitationNumber]] = string.Empty;


            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SavePublishingInfoAndStatusAndComment(ClientContext clientContext, ListItem listItem, string FormStatus, string PreviousFormStatus, string comment, string CurrentUserLogInName)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            clientContext.Load(clientContext.Web.CurrentUser, user => user.Id);
            clientContext.ExecuteQuery();

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            //comment
            if (!string.IsNullOrEmpty(comment))
            {
                SetCommentURLHTML(listItem, piwListInternalColumnNames, CurrentUserLogInName, comment);
            }

            //publisher
            FieldUserValue publisher = new FieldUserValue { LookupId = clientContext.Web.CurrentUser.Id };
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PublishedBy]] = publisher;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SaveLegalResourcesAndReviewAndStatus(ClientContext clientContext, ListItem listItem, string formStatus, string previousFormStatus, string completionDate, string note)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = formStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = previousFormStatus;

            //legal resource completion date and note
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_LegalResourcesAndReviewGroupCompleteDate]] = completionDate;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_LegalResourcesAndReviewGroupNote]] = note;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void InitiatePrintReqForm(ClientContext clientContext, ListItem listItem, string CurrentUserLogInID)
        {
            string PrintReqStatus = Constants.PrintReq_FormStatus_PrintReqGenerated;
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            string listItemID = listItem["ID"].ToString();

            string docketNumber = listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocketNumber]] != null?
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocketNumber]].ToString():string.Empty;

            string FormStatus = listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] != null
                ? listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]].ToString(): string.Empty;
            
            int numberOfPublicPages = listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfPublicPages]] != null
                    ? int.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfPublicPages]].ToString()): 0;


            FOLAMailingList folaMailingList = new FOLAMailingList();
            int numberOfFOLAAddress = folaMailingList.GenerateFOLAMailingExcelFile(clientContext, docketNumber, listItemID);


            //number of supplemental mailing list address
            int numberOfSupplementalMailingListAddress = 0;
            if (listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfSupplementalMailingListAddress]] != null)
            {
                numberOfSupplementalMailingListAddress = int.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfSupplementalMailingListAddress]].ToString());
            }



            //create history list
            if ((numberOfSupplementalMailingListAddress + numberOfFOLAAddress) > 0)
            {

                int printPriority = getPrintPriority(listItem);
                int numberofCopies = numberOfFOLAAddress + numberOfSupplementalMailingListAddress;
                DateTime dateRequested = DateTime.Now;
                //update piw list
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfFOLAMailingListAddress]] = numberOfFOLAAddress;
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]] = numberofCopies;
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqStatus]] = PrintReqStatus;
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqDateRequested]] = dateRequested.ToString();
                
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqPrintPriority]] = printPriority;

                listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqDateRequired]] = getDateRequired(printPriority, dateRequested, numberofCopies * numberOfPublicPages);

                listItem.Update();
                clientContext.ExecuteQuery();

                //todo: send email


                //history list
                //get current user
                User currentUser = clientContext.Web.EnsureUser(CurrentUserLogInID);
                clientContext.Load(currentUser);
                clientContext.ExecuteQuery();
                //add history list for the main form 
                CreatePIWListHistory(clientContext, listItemID, "Print Requisition Generated/Submitted.",
                        FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                //Add history list for the print req form
                if (getHistoryListByPIWListID(clientContext, listItemID, Constants.PIWListHistory_FormTypeOption_PrintReq).Count == 0)
                {
                    string message = "Print Requisition Generated/Submitted.";
                    CreatePIWListHistory(clientContext, listItemID, message,
                        PrintReqStatus, Constants.PIWListHistory_FormTypeOption_PrintReq, currentUser);
                }
                
            }
        }

        private DateTime getDateRequired(int PrintPriority,DateTime dateRequested,int totalPrintPages)
        {
            return dateRequested.AddDays(2);
        }

        private int getPrintPriority(ListItem listItem)
        {
            return 1;
        }

        public void ReGenerateFOLAMailingList(ClientContext clientContext, string listItemID, string CurrentUserLogInID)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            ListItem listItem = GetPiwListItemById(clientContext, listItemID, false);
            string docketNumber = listItem[piwListInternalColumnNames[Constants.PIWList_colName_DocketNumber]].ToString();

            //number of supplemental mailing list address
            int numberofSupplementalMailingListAddress = 0;
            if (listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfSupplementalMailingListAddress]] != null)
            {
                numberofSupplementalMailingListAddress = int.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfSupplementalMailingListAddress]].ToString());
            }

            FOLAMailingList folaMailingList = new FOLAMailingList();
            int numberOfFOLAMailingListAddress = folaMailingList.GenerateFOLAMailingExcelFile(clientContext, docketNumber, listItemID);


            //update number of fola address and number of copies to piwlist
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfFOLAMailingListAddress]] = numberOfFOLAMailingListAddress;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PrintReqNumberofCopies]] = numberOfFOLAMailingListAddress + numberofSupplementalMailingListAddress;
            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SaveNumberOfPublicPagesAndSupplementalMailingListAddress(ClientContext clientContext, ListItem listItem, int numberOfPublicPages, int numberOfSupplementalMailingListAddress)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            if (numberOfPublicPages > 0)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfPublicPages]] = numberOfPublicPages;
            }

            if (numberOfSupplementalMailingListAddress > 0)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfSupplementalMailingListAddress]] = numberOfSupplementalMailingListAddress;
            }

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        #endregion

        #region PIW Documents
        public void CreatePIWDocumentsSubFolder(ClientContext clientContext, string folderName)
        {
            List list = clientContext.Web.Lists.GetByTitle(Constants.PIWDocuments_DocumentLibraryName);
            ListItemCreationInformation info = new ListItemCreationInformation
            {
                UnderlyingObjectType = FileSystemObjectType.Folder,
                LeafName = folderName.Trim()
            };
            ListItem newItem = list.AddItem(info);
            newItem["Title"] = folderName;
            newItem.Update();
            clientContext.ExecuteQuery();
        }



        public string UploadDocumentContentStream(ClientContext clientContext, Stream fileStream, string libraryName, string subFolder, string fileName, string securityLevel, string docType, bool overwrite)
        {
            var internalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWDocuments_DocumentLibraryName);

            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();

            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, libraryName, subFolder);
            Folder uploadSubFolder = clientContext.Web.GetFolderByServerRelativeUrl(uploadSubFolderURL);
            clientContext.ExecuteQuery();//file not found exception if the folder is not exist, let it crash because it is totally wrong somewhere
            fileStream.Seek(0, SeekOrigin.Begin);
            FileCreationInformation flciNewFile = new FileCreationInformation
            {
                ContentStream = fileStream,
                Url = Path.GetFileName(fileName),
                Overwrite = overwrite
            };

            File uploadFile = uploadSubFolder.Files.Add(flciNewFile);
            clientContext.Load(uploadFile);

            uploadFile.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_SecurityLevel]] = securityLevel;
            uploadFile.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_DocType]] = docType;
            uploadFile.ListItemAllFields.Update();

            clientContext.ExecuteQuery();
            return string.Format("{0}/{1}", uploadSubFolderURL, uploadFile.Name);

        }

        public List<File> getDocumentsByDocType(ClientContext clientContext, string uploadSubFolderURL, string docType)
        {
            var internalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWDocuments_DocumentLibraryName);
            Folder folder = clientContext.Web.GetFolderByServerRelativeUrl(uploadSubFolderURL);

            FileCollection fileCol = folder.Files;
            clientContext.Load(fileCol);
            clientContext.Load(fileCol, includes => includes.Include(i => i.ListItemAllFields));
            clientContext.ExecuteQuery();//file not found exception if the folder is not exist, let it crash because it is totally wrong somewhere

            //Sort
            var files = fileCol.OrderBy(f => f.TimeCreated);

            var issuanceFiles = new List<File>();

            if (string.IsNullOrEmpty(docType))//get all docs
            {
                issuanceFiles = files.ToList();
            }
            else
            {
                foreach (var file in files)
                {
                    if (file.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_DocType]] != null &&
                        file.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_DocType]].Equals(docType))
                    {
                        issuanceFiles.Add(file);
                    }
                }
            }


            return issuanceFiles;
        }

        public System.Data.DataTable getDocumentsTableByDocType(ClientContext clientContext, string subFoder, string libraryName,
            out string PublicDocumentURLs, out string CEIIDocumentURLs, out string PrivilegedDocumentURLs, string docType)
        {
            PublicDocumentURLs = string.Empty;
            CEIIDocumentURLs = string.Empty;
            PrivilegedDocumentURLs = string.Empty;

            var result = new System.Data.DataTable();
            result.Columns.Add("ID");
            result.Columns.Add("Name");
            result.Columns.Add("URL");
            result.Columns.Add("DownloadURL");
            result.Columns.Add("Security Level");
            //result.Columns.Add("EPS Passed");
            //result.Columns.Add("EPS Error");

            var internalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWDocuments_DocumentLibraryName);

            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();

            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, libraryName, subFoder);
            //string downloadURL = string.Format("{0}/_layouts/download.aspx?SourceURL=", clientContext.Web.Url);

            var documentList = getDocumentsByDocType(clientContext, uploadSubFolderURL, docType);

            foreach (File file in documentList)
            {
                System.Data.DataRow row = result.NewRow();

                string url = uploadSubFolderURL + "/" + file.Name;
                ;
                row["ID"] = file.ListItemAllFields["ID"];
                row["Name"] = file.Name + " (View Only)";
                row["URL"] = url;
                //row["DownloadURL"] = downloadURL + url;
                row["DownloadURL"] = url + "?web=0";
                row["Security Level"] =
                    file.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_SecurityLevel]];

                result.Rows.Add(row);

                if (row["Security Level"].ToString().Equals(Constants.ddlSecurityControl_Option_Public))
                {
                    if (string.IsNullOrEmpty(PublicDocumentURLs))
                    {
                        PublicDocumentURLs = row["URL"].ToString();
                    }
                    else
                    {
                        PublicDocumentURLs = PublicDocumentURLs + Constants.DocumentURLsSeparator + row["URL"];
                    }
                }
                else if (row["Security Level"].ToString().Equals(Constants.ddlSecurityControl_Option_CEII))
                {
                    if (string.IsNullOrEmpty(CEIIDocumentURLs))
                    {
                        CEIIDocumentURLs = row["URL"].ToString();
                    }
                    else
                    {
                        CEIIDocumentURLs = CEIIDocumentURLs + Constants.DocumentURLsSeparator + row["URL"];
                    }
                }
                else if (row["Security Level"].ToString().Equals(Constants.ddlSecurityControl_Option_Privileged))
                {
                    if (string.IsNullOrEmpty(PrivilegedDocumentURLs))
                    {
                        PrivilegedDocumentURLs = row["URL"].ToString();
                    }
                    else
                    {
                        PrivilegedDocumentURLs = PrivilegedDocumentURLs + Constants.DocumentURLsSeparator + row["URL"];
                    }
                }

            }


            return result;
        }

        public string RemoveDocument(ClientContext clientContext, string subFolder, string libraryName, string Id)
        {
            string removedFileName = string.Empty;
            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();
            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, libraryName, subFolder);
            var documentList = getDocumentsByDocType(clientContext, uploadSubFolderURL, string.Empty);

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

        public void CreatePIWListHistory(ClientContext clientContext, string listItemID, string action, string FormStatus, string FormType, User currentUser)
        {
            List piwlisthistory = clientContext.Web.Lists.GetByTitle(Constants.PIWListHistory_ListName);
            var piwlistHistoryInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWListHistory_ListName);

            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            ListItem newItem = piwlisthistory.AddItem(itemCreateInfo);

            newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_User]] = currentUser;

            newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_Action]] = action;
            newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_FormStatus]] = FormStatus;
            newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_FormType]] = FormType;

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

        public ListItemCollection getHistoryListByPIWListID(ClientContext clientContext, string piwListItemID, string FormType)
        {
            List historyList = clientContext.Web.Lists.GetByTitle(Constants.PIWListHistory_ListName);
            var historyListInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWListHistory_ListName);
            var args = new string[]
            {
                historyListInternalNameList[Constants.PIWListHistory_colName_PIWList],
                piwListItemID.ToString(),
                historyListInternalNameList[Constants.PIWListHistory_colName_FormType],
                FormType
            };
            CamlQuery query = new CamlQuery();
            query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>
                                                        <And>
			                                                <Eq>
				                                                <FieldRef Name='{0}' LookupId='TRUE' />
				                                                <Value Type='Lookup'>{1}</Value>
			                                                </Eq>
                                                            <Eq>
						                                         <FieldRef Name='{2}'/>
						                                         <Value Type='Text'>{3}</Value>
					                                        </Eq>
                                                        </And>			
		                                            </Where>		
	                                            </Query>
                                            </View>", args);

            var historyListItems = historyList.GetItems(query);

            clientContext.Load(historyListItems);
            clientContext.ExecuteQuery();

            return historyListItems;



        }

        public System.Data.DataTable getHistoryListTable(ClientContext clientContext, string piwListItemID, string FormType)
        {
            var historyList = getHistoryListByPIWListID(clientContext, piwListItemID, FormType);
            var historyListInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWListHistory_ListName);
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
                    ? ((FieldUserValue)historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_User]]).LookupValue : string.Empty;

                row["Action"] = historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Action]] != null
                    ? historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_Action]].ToString() : string.Empty;

                row["FormStatus"] = historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_FormStatus]] != null
                    ? historyItem[historyListInternalNameList[Constants.PIWListHistory_colName_FormStatus]].ToString() : string.Empty;

                result.Rows.Add(row);
            }

            return result;
        }

        public void PopulateHistoryList(ClientContext clientContext, string listItemId, Repeater rpHistoryList, string FormType)
        {
            System.Data.DataTable table = getHistoryListTable(clientContext, listItemId, FormType);
            rpHistoryList.DataSource = table;
            rpHistoryList.DataBind();
        }
        #endregion

        #region Utils
        public void OpenDocument(Page page, string documentPath)
        {
            if (string.IsNullOrEmpty(documentPath))
            {
                return;
            }

            page.ClientScript.RegisterStartupScript(this.GetType(), "documentWindow", String.Format("<script>window.open('{0}');</script>", documentPath));
        }

        public void AddCitationNumberToDocument(ClientContext clientContext, string citationNumber, string listItemID, string fileName)
        {
            var documentServerRelativeURL = getDocumentServerRelativeURL(clientContext, listItemID, fileName);

            //var newclientContext = new ClientContext(Request.QueryString["SPHostUrl"]);
            FileInformation fileInformation = File.OpenBinaryDirect(clientContext, documentServerRelativeURL);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                fileInformation.Stream.CopyTo(memoryStream);
                using (WordprocessingDocument doc = WordprocessingDocument.Open(memoryStream, true))
                {
                    // Insert a new paragraph at the beginning of the document.
                    var paragraph = GenerateCitParagraph(citationNumber);
                    doc.MainDocumentPart.Document.Body.InsertAt(paragraph, 0);
                }
                // Seek to beginning before writing to the SharePoint server.
                memoryStream.Seek(0, SeekOrigin.Begin);

                File.SaveBinaryDirect(clientContext, documentServerRelativeURL, memoryStream, true);
            }
        }

        public void RemoveCitationNumberFromDocument(ClientContext clientContext, string citationNumber, string listItemID, string fileName)
        {
            var documentServerRelativeURL = getDocumentServerRelativeURL(clientContext, listItemID, fileName);

            FileInformation fileInformation = File.OpenBinaryDirect(clientContext, documentServerRelativeURL);
            bool foundCitationNumberInDocument = false;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                fileInformation.Stream.CopyTo(memoryStream);
                using (WordprocessingDocument doc = WordprocessingDocument.Open(memoryStream, true))
                {
                    MainDocumentPart mainpart = doc.MainDocumentPart;
                    //IEnumerable<OpenXmlElement> elems = mainpart.Document.Body.Descendants().ToList();

                    IEnumerable<OpenXmlElement> elems = mainpart.Document.Body.Descendants<Paragraph>().ToList()[0].Descendants().ToList();


                    foreach (OpenXmlElement elem in elems)
                    {
                        if (elem is Text && elem.InnerText.Contains(citationNumber))
                        {
                            Run run = (Run)elem.Parent;
                            Paragraph p = (Paragraph)run.Parent;
                            p.RemoveAllChildren();
                            p.Remove();
                            foundCitationNumberInDocument = true;
                            break;
                        }
                    }
                }

                if (foundCitationNumberInDocument)
                {
                    // Seek to beginning before writing to the SharePoint server.
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    File.SaveBinaryDirect(clientContext, documentServerRelativeURL, memoryStream, true);
                }

            }
        }
        public string getDocumentServerRelativeURL(ClientContext clientContext, string listItemID, string fileName)
        {
            clientContext.Load(clientContext.Web);
            clientContext.ExecuteQuery();

            return string.Format("{0}/{1}/{2}/{3}", clientContext.Web.ServerRelativeUrl,
                    Constants.PIWDocuments_DocumentLibraryName, listItemID, fileName);

        }

        public string getFolderServerRelativeURL(ClientContext clientContext, string listItemID)
        {
            clientContext.Load(clientContext.Web);
            clientContext.ExecuteQuery();

            return string.Format("{0}/{1}/{2}", clientContext.Web.ServerRelativeUrl,
                    Constants.PIWDocuments_DocumentLibraryName, listItemID);
        }

        public string getDocumentServerRelativeURLFromURL(ClientContext clientContext, string listItemID, string fileURL)
        {
            string fileName = getFileNameFromURL(fileURL);
            return getDocumentServerRelativeURL(clientContext, listItemID, fileName);
        }

        /// <summary>
        /// Convert dictionary of documents full URL to document server relative url
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="listItemID"></param>
        /// <param name="fileURLs"></param>
        /// <returns></returns>
        public Dictionary<string, string> getDocumentServerRelativeURL(ClientContext clientContext, string listItemID, Dictionary<string, string> fileURLs)
        {
            var result = new Dictionary<string, string>();
            clientContext.Load(clientContext.Web);
            clientContext.ExecuteQuery();

            foreach (KeyValuePair<string, string> kvp in fileURLs)
            {
                var documentServerRelativeURL = string.Format("{0}/{1}/{2}/{3}", clientContext.Web.ServerRelativeUrl,
                    Constants.PIWDocuments_DocumentLibraryName, listItemID, getFileNameFromURL(kvp.Key));
                if (!result.ContainsKey(documentServerRelativeURL))
                {
                    result.Add(documentServerRelativeURL, kvp.Value);
                }
            }

            return result;
        }


        public Paragraph GenerateCitParagraph(string text)
        {
            //citation paragraph will be bold, centered and size 13, font size by default will be Times New Romain
            Paragraph paragraph1 = new Paragraph() { };

            ParagraphProperties paragraphProperties1 = new ParagraphProperties();

            Justification justification1 = new Justification()
            {
                Val = JustificationValues.Center,

            };

            paragraphProperties1.Append(justification1);

            Run run1 = new Run();

            RunProperties runProperties1 = new RunProperties();

            //RunFonts runFonts1 = new RunFonts() { Ascii = "Times New Roman"};
            Bold bold1 = new Bold();
            DocumentFormat.OpenXml.Wordprocessing.FontSize fontSize1 = new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "26" };//font size 13 - half size paramater

            runProperties1.Append(bold1);
            runProperties1.Append(fontSize1);

            Text text1 = new Text();
            text1.Text = text;

            run1.Append(runProperties1);
            run1.Append(text1);

            paragraph1.Append(paragraphProperties1);
            paragraph1.Append(run1);
            return paragraph1;
        }

        public string UploadIssuanceDocument(ClientContext clientContext, FileUpload fileUpload, string listItemId, Repeater rpDocumentList, Label lbUploadedDocumentError,
            Label lbRequiredUploadedDocumentError, string FormStatus, string securityControlValue, string docType, string currentloginID)
        {
            var uploadedFileURL = string.Empty;
            using (var fileStream = fileUpload.PostedFile.InputStream)
            {
                string fileName = fileUpload.FileName;
                var extension = Path.GetExtension(fileName);
                if (extension != null && extension.Equals(".doc", StringComparison.CurrentCultureIgnoreCase))
                {
                    lbUploadedDocumentError.Text = ".doc file is not supported, please upload .docx file";
                    lbUploadedDocumentError.Visible = true;
                }
                else
                {
                    lbUploadedDocumentError.Visible = false;
                    lbUploadedDocumentError.Text = string.Empty;

                    //validate the upload file
                    //copy file
                    string desctinationURNFolder = string.Format("{0}\\{1}\\{2}",
                        ConfigurationManager.AppSettings["PIWDocuments"], listItemId, Constants.ValidationFolder);
                    string fullPathFileName = desctinationURNFolder + "\\" + fileName;

                    CopyFile(fileStream, fileName, desctinationURNFolder);

                    EPSPublicationHelper epsHelper = new EPSPublicationHelper();
                    var validationResult = epsHelper.ValidateDocument(fullPathFileName, null, string.Empty);
                    if (validationResult.ErrorList.Count > 0)
                    {
                        //set validation error
                        lbUploadedDocumentError.Text = validationResult.ErrorList[0].Description;
                        lbUploadedDocumentError.Visible = true;
                    }
                    else
                    {
                        uploadedFileURL = UploadDocumentContentStream(clientContext, fileStream, Constants.PIWDocuments_DocumentLibraryName,
                            listItemId, fileName, securityControlValue, docType, false);

                        //set permission if CEII or Privileged
                        //AssignPermissionForCEIIAndPrivilegedDocument(clientContext, listItemId, uploadedFileURL, securityControlValue);


                        //clear validation error
                        lbRequiredUploadedDocumentError.Visible = false;
                        lbUploadedDocumentError.Visible = false;
                        lbUploadedDocumentError.Text = string.Empty;

                        //get current user
                        User currentUser = clientContext.Web.EnsureUser(currentloginID);
                        clientContext.Load(currentUser);
                        clientContext.ExecuteQuery();

                        //history list
                        if (getHistoryListByPIWListID(clientContext, listItemId, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                        {
                            CreatePIWListHistory(clientContext, listItemId, "Workflow Item created.", FormStatus,
                                Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                        }

                        CreatePIWListHistory(clientContext, listItemId,
                            string.Format("Document file {0} uploaded/associated with Workflow Item.", fileName), FormStatus,
                            Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);

                    }
                }
            }

            return uploadedFileURL;
        }

        public bool UploadSupplementalMailingListDocument(ClientContext clientContext, FileUpload fileUpload, string listItemId,
            Repeater rpDocumentList, Label lbUploadedDocumentError, string FormStatus, string securityControlValue, string docType, string currentLoginID)
        {
            bool result = false;
            using (var fileStream = fileUpload.PostedFile.InputStream)
            {
                string fileName = fileUpload.FileName;
                var extension = Path.GetExtension(fileName);
                if (extension != null && (!extension.Equals(".xlsx", StringComparison.CurrentCultureIgnoreCase)))
                {
                    lbUploadedDocumentError.Text = "Please upload excel file with .xlsx extension";
                    lbUploadedDocumentError.Visible = true;
                }
                else
                {
                    UploadDocumentContentStream(clientContext, fileStream, Constants.PIWDocuments_DocumentLibraryName, listItemId, fileName, securityControlValue, docType, false);

                    //clear validation error
                    lbUploadedDocumentError.Visible = false;
                    lbUploadedDocumentError.Text = string.Empty;

                    //get current user
                    User currentUser = clientContext.Web.EnsureUser(currentLoginID);
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();

                    //history list
                    if (getHistoryListByPIWListID(clientContext, listItemId, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                    {
                        CreatePIWListHistory(clientContext, listItemId, "Workflow Item created.", FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                    }

                    CreatePIWListHistory(clientContext, listItemId,
                        string.Format("Supplemental Mailing List file {0} uploaded/associated with Workflow Item.", fileName), FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm, currentUser);
                    result = true;
                }
            }

            return result;
        }

        public void PopulateIssuanceDocumentList(ClientContext clientContext, string listItemId, Repeater rpDocumentList,
            out string publicDocumentURLs, out string cEIIDocumentURLs, out string PrivilegeDocumentURLs)
        {
            System.Data.DataTable table = getDocumentsTableByDocType(clientContext, listItemId, Constants.PIWDocuments_DocumentLibraryName,
                out publicDocumentURLs, out cEIIDocumentURLs, out PrivilegeDocumentURLs, Constants.PIWDocuments_DocTypeOption_Issuance);
            rpDocumentList.DataSource = table;
            rpDocumentList.DataBind();
        }
        public void PopulateSupplementalMailingListDocumentList(ClientContext clientContext, string listItemId, Repeater rpDocumentList, HtmlGenericControl supplementalMailingListFieldSet)
        {
            string publicDocumentUrLs;
            string CEIIDocumentURLs;
            string PrivilegedDocumentURLs;
            System.Data.DataTable table = getDocumentsTableByDocType(clientContext, listItemId, Constants.PIWDocuments_DocumentLibraryName,
                out publicDocumentUrLs, out CEIIDocumentURLs, out PrivilegedDocumentURLs, Constants.PIWDocuments_DocTypeOption_SupplementalMailingList);
            rpDocumentList.DataSource = table;
            rpDocumentList.DataBind();

            //only allow ONE supplemental mailing list uploaded
            supplementalMailingListFieldSet.Visible = table.Rows.Count == 0;

        }

        public void GenerateCitation(ClientContext clientContext, DropDownList ddDocumentCategory, TextBox tbCitationNumber, DropDownList ddAvailableCitationNumbers, bool isAgendaForm)
        {
            if (ddDocumentCategory.SelectedIndex > 0)
            {
                int documentCategoryNumber = getDocumentCategoryNumber(ddDocumentCategory.SelectedValue, isAgendaForm);

                CitationNumber citationNumberHelper = new CitationNumber(documentCategoryNumber, DateTime.Now);

                tbCitationNumber.Text = citationNumberHelper.GetNextCitationNumber(clientContext);

                var availableCitationNumbers = citationNumberHelper.getAllAvailableCitationNumber(clientContext);
                if (availableCitationNumbers.Count > 1) //more than 1, 1 is already displayed in textbox
                {
                    ddAvailableCitationNumbers.Visible = true;
                    ddAvailableCitationNumbers.Items.Clear();
                    ddAvailableCitationNumbers.Items.Add("-- Available Citation # --");

                    foreach (string s in availableCitationNumbers)
                    {
                        ddAvailableCitationNumbers.Items.Add(s);
                    }
                }
                else
                {
                    ddAvailableCitationNumbers.Visible = false;
                }
            }
        }

        /// <summary>
        /// check if a docket is existing in ATMS
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
                    //Add docket to dictionary
                    if (!docketDictionary.ContainsKey(docketFullTrimmed))
                    {
                        //FullDocket: ER14-543-000 or EL02-60-007
                        int docketLength = docketFullTrimmed.LastIndexOf("-");

                        bool validDocket = true;
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

        //public IDvvoRemoteBusiness DvvoProxy
        //{
        //    get
        //    {
        //        if (dvvoProxy == null)
        //        {
        //            dvvoProxy =
        //                (IDvvoRemoteBusiness)Activator.GetObject(typeof(IDvvoRemoteBusiness),
        //                ConfigurationManager.AppSettings["eLibRemoteServiceDvvoURI"].ToString());
        //        }
        //        return dvvoProxy;
        //    }
        //}
        public IDvvoRemoteBusiness getDVVORemoteObject()
        {
            string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            IChannel[] myIChannelArray = ChannelServices.RegisteredChannels;
            if (myIChannelArray.Length == 0)
                System.Runtime.Remoting.RemotingConfiguration.Configure(configPath, true);

            IDvvoRemoteBusiness m_RemoteObject = (IDvvoRemoteBusiness)
                             Activator.GetObject(typeof(IDvvoRemoteBusiness), ConfigurationManager.AppSettings["eLibRemoteServiceDvvoURI"]);

            return m_RemoteObject;
        }


        public bool DocketExist(string docket, string subdocket, IWorkSetOps m_RemoteObject)
        {
            var atmsDocket = m_RemoteObject.GetWorkSetsByLabel(docket, subdocket, false, true);
            return (atmsDocket.Count > 0);
        }

        public Dictionary<string, string> getInternalColumnNamesFromCache(ClientContext clientContext, string listName)
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
                    var internalColumnList = getInternalColumnNames(clientContext, listName);

                    //Add the new object to cache
                    cache.Insert(listName, internalColumnList, null, DateTime.Now.AddHours(10), Cache.NoSlidingExpiration);
                    return internalColumnList;
                }
            }

        }

        public Dictionary<string, string> getInternalColumnNames(ClientContext clientContext, string listName)
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
            return internalColumnList;
        }

        public void LogError(HttpContext httpContext, HttpRequest httpRequest, Exception exc, string listItemID, string pageName)
        {
            //This is expected exception after Page.Redirect --> ignore it??? TEst it
            if (exc is System.Threading.ThreadAbortException)
            {
                return;
            }

            //create new log error - this should have its own clientContext
            using (var clientContext = getElevatedClientContext(httpContext, httpRequest))
            {
                List errorLogList = clientContext.Web.Lists.GetByTitle(Constants.ErrorLogListName);
                var errorLogInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.ErrorLogListName);

                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = errorLogList.AddItem(itemCreateInfo);

                //set current user name
                clientContext.Load(clientContext.Web.CurrentUser);
                clientContext.ExecuteQuery();
                newItem[errorLogInternalNameList[Constants.ErrorLog_colName_User]] = clientContext.Web.CurrentUser;

                newItem[errorLogInternalNameList[Constants.ErrorLog_colName_ErrorPageName]] = pageName;

                string message = string.Empty;
                if (exc.InnerException != null)
                {
                    message = exc.Message + " - Inner Exception: " + exc.InnerException.Message;
                }
                else
                {
                    message = exc.Message;
                }

                if (exc.StackTrace != null)
                {
                    message = message + "Stack Trace: " + exc.StackTrace;
                }

                message = message + "Type: " + exc.GetType();

                newItem[errorLogInternalNameList[Constants.ErrorLog_colName_ErrorMessage]] = message;

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

        //public bool IsUserMemberOfGroup(ClientContext clientContext, User user, string groupName)
        //{
        //    //Load group
        //    clientContext.Load(user.Groups);
        //    clientContext.ExecuteQuery();
        //    //user.Groups.Cast<Group>().Any()
        //    return user.Groups.Cast<Group>().Any(g => g.Title == groupName);
        //}

        public bool IsUserMemberOfGroup(ClientContext clientContext, string userLoginID, string[] groupNames)
        {
            //Load group
            var user = clientContext.Web.EnsureUser(userLoginID);
            clientContext.Load(user.Groups);
            clientContext.ExecuteQuery();
            bool result = false;
            var GroupCollection = user.Groups.Cast<Group>();
            foreach (string groupName in groupNames)
            {
                result = GroupCollection.Any(g => g.Title == groupName);
                if (result)
                {
                    break;
                }
            }
            //user.Groups.Cast<Group>().Any()
            return result;
        }

        /// <summary>
        /// If user is not belong to specific group authozied for submit and approve the form,
        /// They can only view the form after it is initiated publication.
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="CurrentUserLogInID"></param>
        /// <param name="groups">authorized group to submit and approve the form</param>
        /// <param name="formStatus"></param>
        /// <returns></returns>
        public bool CanUserViewForm(ClientContext clientContext, string CurrentUserLogInID, string[] groups, string formStatus)
        {
            bool result = IsUserMemberOfGroup(clientContext, CurrentUserLogInID, groups);
            if (!result)
            {
                if (!string.IsNullOrEmpty(formStatus))
                {
                    //if user is not member of viewable group, they can only view form after it is initiated publication
                    result = (formStatus.Equals(Constants.PIWList_FormStatus_PublishInitiated) ||
                              formStatus.Equals(Constants.PIWList_FormStatus_PublishedToeLibrary));
                }
            }

            return result;
        }
        /// <summary>
        /// Return the first docket number found in input
        /// If no docket found, return the whole input
        /// </summary>
        /// <param name="filename"></param>
        public string ExtractDocket(string filename)
        {
            const string pattern = @"^(\w+)-(\d+)-\d\d\d";
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

        public int getDocumentCategoryNumber(string documentCategory, bool isAgendaForm)
        {
            int documentCategoryNumber = 0;
            if (isAgendaForm)
            {
                documentCategoryNumber = 61;
            }
            else
            {
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
                        throw new Exception("getDocumentCategoryNumber - Unknown document category: " + documentCategory);
                }
            }

            return documentCategoryNumber;
        }

        /// <summary>
        /// Check if form is not saved/changed after it is opened
        /// for concurrency checking
        public bool CheckIfListItemChanged(ClientContext clientContext, ListItem listItem, DateTime viewModifiedDateTime)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
            if (listItem[piwListInternalColumnNames[Constants.PIWList_colName_Modified]] != null)
            {
                DateTime currentModifiedDateTime = DateTime.Parse(listItem[piwListInternalColumnNames[Constants.PIWList_colName_Modified]].ToString());
                return DateTime.Compare(currentModifiedDateTime, viewModifiedDateTime) != 0;
            }
            return false;
        }

        public void RedirectToSourcePage(HttpRequest request, HttpResponse response)
        {
            //redirect to source page
            //Attention: Source page is short name, not the entire URL
            //https://dev.spapps.ferc.gov/PIW_SPAppWeb/pages/EditStandardForm.aspx?SPHostUrl=https%3a%2f%2ffdc1s-sp23wfed2.ferc.gov%2fpiw&SPLanguage=en-US&SPClientTag=0&SPProductNumber=15.0.4727.1000&SPAppWebUrl=https%3a%2f%2fapp-3f613e5e650fd4.dev.spapps.ferc.gov%2fpiw%2fPIW_SPApp&ID=41&Source=StandardForm.aspx
            string sourcePage = request.QueryString["Source"];
            if (string.IsNullOrEmpty(sourcePage))//if source is not provided in url, use the default setting
            {
                sourcePage = getDefaultSourcePage(getPageFileName(request));
            }


            RedirectToAPage(request, response, sourcePage);

        }

        public string getPageFileName(HttpRequest request)
        {
            string filepath = request.FilePath;
            return getFileNameFromURL(filepath);
        }

        /// <summary>
        /// get the file name from the URL: //https://dev.spapps.ferc.gov/PIW_SPAppWeb/pages/documentName.docx
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string getFileNameFromURL(string url)
        {
            return url.Substring(url.LastIndexOf("/") + 1);
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

            var newURLPage = GetPageUrl(request, PageName, string.Empty);

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
        private string GetPageUrl(HttpRequest request, string PageName, string sourcePage)
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
                request.QueryString["SPAppWebUrl"],
                sourcePage
            };

            var fullPageURL = string.Format("{0}?SPHostUrl={1}&SPLanguage={2}&SPClientTag={3}&SPProductNumber={4}&SPAppWebUrl={5}&Source={6}", args);
            return fullPageURL;
        }

        /// <summary>
        /// return default source page if the source page is not provided
        /// </summary>
        /// <param name="pageName"></param>
        /// <returns></returns>
        private string getDefaultSourcePage(string pageName)
        {
            string result = string.Empty;

            switch (pageName)
            {
                case Constants.Page_EditStandardForm:
                    result = Constants.Page_StandardForms;
                    break;
                case Constants.Page_EditAgendaForm:
                    result = Constants.Page_AgendaForms;
                    break;
                case Constants.Page_EditDirectPublicationForm:
                    result = Constants.Page_DirectPublicationForms;
                    break;
            }

            return result;
        }

        public void RefreshPage(HttpRequest request, HttpResponse response)
        {
            string PageURL = request.Url.ToString();
            if (!string.IsNullOrEmpty(PageURL))
            {
                response.Redirect(PageURL, false);
            }
        }


        public string getEditFormURL(string formType, string listItemId, HttpRequest request, string sourcePage)
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
            else if (formType == Constants.PIWList_FormType_PrintReqForm)
            {
                PageFileName = Constants.Page_EditPrintReqForm;
            }

            result = String.Format("{0}&ID={1}", GetPageUrl(request, PageFileName, sourcePage), listItemId);
            return result;
        }

        public void CopyFile(Stream stream, string fileName, string DestinationURNFolder)
        {
            if (!Directory.Exists(DestinationURNFolder))
            {
                Directory.CreateDirectory(DestinationURNFolder);
            }
            string fileNameFullURN = DestinationURNFolder + "\\" + fileName;
            using (var fileStream = System.IO.File.Create(fileNameFullURN))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            }

        }

        public ClientContext getElevatedClientContext(HttpContext context, HttpRequest request)
        {
            return new ClientContext(request.QueryString["SPHostUrl"]);
        }

        public ClientContext getElevatedClientContext(string spHostUrl)
        {
            return new ClientContext(spHostUrl);
        }

        public ClientContext getCurrentLoginClientContext(HttpContext context, HttpRequest request)
        {
            return SharePointContextProvider.Current.GetSharePointContext(context).CreateUserClientContextForSPHost();
        }

        public User[] getUsersFromField(ClientContext clientContext, FieldUserValue[] fuv)
        {
            User[] users = new User[fuv.Length];
            for (int i = 0; i < users.Length; i++)
            {
                User user = clientContext.Web.GetUserById(fuv[i].LookupId);
                clientContext.Load(user);
                clientContext.ExecuteQuery();
                users[i] = user;
            }

            return users;
        }

        /// <summary>
        /// this method prepare the html code to display the document list in report or in email.
        /// If report, it display using bootstrap icon, if email it display using the html list item
        /// </summary>
        /// <param name="publicDocsURLs"></param>
        /// <param name="CEIIDocsURLs"></param>
        /// <param name="PrivilegedDocsURLs"></param>
        /// <param name="forEmail"></param>
        /// <returns></returns>
        public string getDocumentURLsHTML(string publicDocsURLs, string CEIIDocsURLs, string PrivilegedDocsURLs, bool forEmail)
        {
            //build seperator array
            StringBuilder result = new StringBuilder();
            //string pattern = "<span class='glyphicon glyphicon-menu-right' style='font-size:0.7em; color:#337ab7'></span> <a href='{0}'>{1}</a>";
            string pattern = "<li><a href='{0}'>{1}</a></li>";
            if (forEmail)
            {
                pattern = "<li>{1}</li>";
            }

            string allowDownload = "?web=0";

            //Public
            var urlArray = publicDocsURLs.Split(new string[] { Constants.DocumentURLsSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var url in urlArray)
            {
                if (string.IsNullOrEmpty(result.ToString()))
                {
                    result.Append(string.Format(pattern, url + allowDownload, getFileNameFromURL(url) + " (Public)"));
                }
                else
                {
                    result.Append(string.Format(pattern, url + allowDownload, getFileNameFromURL(url) + " (Public)"));
                }

            }

            //CEII
            urlArray = CEIIDocsURLs.Split(new string[] { Constants.DocumentURLsSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var url in urlArray)
            {
                if (string.IsNullOrEmpty(result.ToString()))
                {
                    result.Append(string.Format(pattern, url + allowDownload, getFileNameFromURL(url) + " (CEII)"));
                }
                else
                {
                    result.Append(string.Format(pattern, url + allowDownload, getFileNameFromURL(url) + " (CEII)"));
                }

            }

            //Priviledge
            urlArray = PrivilegedDocsURLs.Split(new string[] { Constants.DocumentURLsSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var url in urlArray)
            {
                if (string.IsNullOrEmpty(result.ToString()))
                {
                    result.Append(string.Format(pattern, url + allowDownload, getFileNameFromURL(url) + " (Privileged)"));
                }
                else
                {
                    result.Append(string.Format(pattern, url + allowDownload, getFileNameFromURL(url) + " (Privileged)"));
                }

            }

            //for report, it is cleaner (more condense) if we don't use <ul> tag
            if (forEmail)
            {
                result.Insert(0, "<ul>");
                result.Append("</ul>");
            }
            return result.ToString();
        }

        public void SetCommentURLHTML(ListItem listItem, Dictionary<string, string> piwListInternalColumnNames, string userName, string comment)
        {
            if (listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] == null)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("<li>{0} ({1}): {2}</li>", userName,
                    DateTime.Now.ToString("G"), comment);
            }
            else
            {
                //append
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]] = String.Format("<li>{0} ({1}): {2}</li><br>{3}",
                    userName, DateTime.Now.ToString("G"), comment, listItem[piwListInternalColumnNames[Constants.PIWList_colName_Comment]]);
            }
        }

        public void CreateLog(ClientContext clientContext, string Title, string Message)
        {
            List log = clientContext.Web.Lists.GetByTitle(Constants.LogListName);
            var logInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.LogListName);

            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            ListItem newItem = log.AddItem(itemCreateInfo);

            newItem[logInternalNameList[Constants.Log_colName_Title]] = Title;

            newItem[logInternalNameList[Constants.Log_colName_Message]] = Message;

            newItem.Update();
            clientContext.ExecuteQuery();//we need to create item first before set lookup field.
        }


        public void CreateEmailLog(ClientContext clientContext, string toAddress, string subject, string content)
        {
            List emailLog = clientContext.Web.Lists.GetByTitle(Constants.EmailLogListName);
            var logInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.EmailLogListName);

            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            ListItem newItem = emailLog.AddItem(itemCreateInfo);
            newItem[logInternalNameList[Constants.EmailLog_colName_To]] = toAddress;
            newItem[logInternalNameList[Constants.EmailLog_colName_Title]] = subject;
            newItem[logInternalNameList[Constants.EmailLog_colName_Content]] = content;

            newItem.Update();
            clientContext.ExecuteQuery();//we need to create item first before set lookup field.
        }

        public string RemoveDuplicateDocket(string docketInput)
        {
            string[] dockets = docketInput.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            string finalDocket = string.Empty;
            foreach (string fullDocket in dockets)
            {
                string docketTrimmed = fullDocket.Trim();
                if (string.IsNullOrEmpty(finalDocket))
                {
                    finalDocket = docketTrimmed;
                }
                else
                {
                    if (finalDocket.IndexOf(docketTrimmed, StringComparison.OrdinalIgnoreCase) < 0)//no duplicated
                    {
                        finalDocket = finalDocket + "," + docketTrimmed;
                    }
                }


            }
            return finalDocket;
        }

        #endregion

        #region Permission for Document List Item

        public void UpdatePermissionBaseOnFormStatus(ClientContext clientContext, string listitemID, string formStatus, string formType)
        {
            switch (formStatus)//this is the nextformstatus after wf is executed
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Recalled:
                case Constants.PIWList_FormStatus_Rejected:
                case Constants.PIWList_FormStatus_ReOpen:
                    if (formType.Equals(Constants.PIWList_FormType_StandardForm))
                    {
                        AssignUniqueRoles(clientContext, listitemID, Constants.Role_ContributeNoDelete, Constants.Role_ContributeNoDelete,
                            Constants.Role_ContributeNoDelete, Constants.Role_Read, Constants.Role_Read);
                    }
                    else if (formType.Equals(Constants.PIWList_FormType_AgendaForm))
                    {
                        AssignUniqueRoles(clientContext, listitemID, Constants.Role_Read, Constants.Role_ContributeNoDelete,
                            Constants.Role_ContributeNoDelete, Constants.Role_Read, Constants.Role_Read);
                    }
                    else if (formType.Equals(Constants.PIWList_FormType_DirectPublicationForm))
                    {
                        AssignUniqueRoles(clientContext, listitemID, Constants.Role_Read, Constants.Role_Read,
                            Constants.Role_Read, Constants.Role_ContributeNoDelete, Constants.Role_ContributeNoDelete);
                    }
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    AssignUniqueRoles(clientContext, listitemID, Constants.Role_Read, Constants.Role_Read,
                            Constants.Role_Read, Constants.Role_Read, Constants.Role_Read);
                    break;
                case Constants.PIWList_FormStatus_Edited://no change when form moved to Edit mode
                    //Do nothings in Edit
                    break;
                case Constants.PIWList_FormStatus_OSECVerification:
                case Constants.PIWList_FormStatus_PrePublication:
                    AssignUniqueRoles(clientContext, listitemID, Constants.Role_Read, Constants.Role_ContributeNoDelete,
                            Constants.Role_ContributeNoDelete, Constants.Role_Read, Constants.Role_Read);
                    break;
                case Constants.PIWList_FormStatus_SecretaryReview:
                    AssignUniqueRoles(clientContext, listitemID, Constants.Role_Read, Constants.Role_Read,
                            Constants.Role_ContributeNoDelete, Constants.Role_Read, Constants.Role_Read);
                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    if (formType.Equals(Constants.PIWList_FormType_StandardForm))
                    {
                        AssignUniqueRoles(clientContext, listitemID, Constants.Role_Read, Constants.Role_ContributeNoDelete,
                            Constants.Role_ContributeNoDelete, Constants.Role_Read, Constants.Role_Read);
                    }
                    else if (formType.Equals(Constants.PIWList_FormType_AgendaForm))
                    {
                        AssignUniqueRoles(clientContext, listitemID, Constants.Role_Read, Constants.Role_Read,
                            Constants.Role_ContributeNoDelete, Constants.Role_Read, Constants.Role_Read);
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                case Constants.PIWList_FormStatus_Deleted:
                    AssignUniqueRoles(clientContext, listitemID, Constants.Role_Read, Constants.Role_Read,
                            Constants.Role_Read, Constants.Role_Read, Constants.Role_Read);
                    break;
                default:
                    throw new Exception("UpdatePermissionBasedOnFormStatus method - UnKnown Form Status: " + formStatus);

            }
        }

        public void AssignUniqueRoles(ClientContext clientContext, string listitemID, string PIWUsersRole, string PIWOSECRole, string PIWSecReviewRole, string PIWDirectPublicationRole, string PIWDirectPublicationSubmissionOnlyRole)
        {
            var folderServerRelativeURL = getFolderServerRelativeURL(clientContext, listitemID);
            var folder = clientContext.Web.GetFolderByServerRelativeUrl(folderServerRelativeURL);

            folder.ListItemAllFields.BreakRoleInheritance(true, false);//don't change the subscope becuase of CEII and Prividledge has their own permission

            //PIWUser group
            if (!string.IsNullOrEmpty(PIWUsersRole))
            {
                var group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_PIWUsers);

                AssignRoleForGroup(clientContext, group, PIWUsersRole, folder);
            }

            //PIWOSEC group
            if (!string.IsNullOrEmpty(PIWOSECRole))
            {
                var group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_OSEC);

                AssignRoleForGroup(clientContext, group, PIWOSECRole, folder);
            }

            //PIWSecReview group
            if (!string.IsNullOrEmpty(PIWSecReviewRole))
            {
                var group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_SecReview);

                AssignRoleForGroup(clientContext, group, PIWSecReviewRole, folder);
            }

            //PIWDirectPublication group
            if (!string.IsNullOrEmpty(PIWDirectPublicationRole))
            {
                var group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_PIWDirectPublication);

                AssignRoleForGroup(clientContext, group, PIWDirectPublicationRole, folder);
            }

            //PIWDirectPublicationSubmissionOnly group
            if (!string.IsNullOrEmpty(PIWDirectPublicationSubmissionOnlyRole))
            {
                var group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_PIWDirectPublicationSubmitOnly);

                AssignRoleForGroup(clientContext, group, PIWDirectPublicationSubmissionOnlyRole, folder);
            }

            //CEII and Privileged
            ListItem listItem = GetPiwListItemById(clientContext, listitemID, true);
            if (listItem != null)
            {
                var piwlistInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);
                string CEIIUrls = listItem[piwlistInternalNameList[Constants.PIWList_colName_CEIIDocumentURLs]] != null
                    ? listItem[piwlistInternalNameList[Constants.PIWList_colName_CEIIDocumentURLs]].ToString() : string.Empty;
                string PrivilegedUrls = listItem[piwlistInternalNameList[Constants.PIWList_colName_PrivilegedDocumentURLs]] != null
                    ? listItem[piwlistInternalNameList[Constants.PIWList_colName_PrivilegedDocumentURLs]].ToString() : string.Empty;

                //todo: get the library
                AssignPermissionForCEIIAndPrivilegedDocument(clientContext, listitemID, CEIIUrls, PrivilegedUrls, "Gas");
            }


            //update
            folder.Update();
            clientContext.ExecuteQuery();

        }

        public void AssignPermissionForCEIIAndPrivilegedDocument(ClientContext clientContext, string listItemID, string fileURL, string securityControl)
        {
            Group group = null;

            if (securityControl.Equals(Constants.ddlSecurityControl_Option_CEII))
            {
                group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_FAWSGG_elc_FERCStaff_Privileged_CEII_RO);
            }
            else if (securityControl.Equals(Constants.ddlSecurityControl_Option_Privileged))
            {
                group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_PIW_FOL_Submission_Privileged_ReadOnly);
            }

            if (group != null)
            {
                AssignRoleForDocument(clientContext, listItemID, group, Constants.Role_Read, fileURL);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="listItemID"></param>
        /// <param name="CEIIUrls">urls saved in splist, text seperated by _##_</param>
        /// <param name="PrivilegedUrls"></param>
        /// <param name="library"></param>
        public void AssignPermissionForCEIIAndPrivilegedDocument(ClientContext clientContext, string listItemID,
            string CEIIUrls, string PrivilegedUrls, string library)
        {
            Group group = null;

            //CEII
            if (!string.IsNullOrEmpty(CEIIUrls))
            {
                string[] urls = CEIIUrls.Split(new string[] { Constants.DocumentURLsSeparator },
                                        StringSplitOptions.RemoveEmptyEntries);
                group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_FAWSGG_elc_FERCStaff_Privileged_CEII_RO);
                foreach (string url in urls)
                {
                    if (group != null)
                    {
                        AssignRoleForDocument(clientContext, listItemID, group, Constants.Role_Read, url);
                    }
                }
            }

            //Privileged
            if (!string.IsNullOrEmpty(PrivilegedUrls))
            {
                string[] urls = PrivilegedUrls.Split(new string[] { Constants.DocumentURLsSeparator },
                                        StringSplitOptions.RemoveEmptyEntries);
                //get group
                switch (library)
                {
                    case Constants.PrivilegedLibrary_General:
                        group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_FAWSGG_elc_FERCStaff_Privileged_General_RO);
                        break;
                    case Constants.PrivilegedLibrary_Gas:
                        group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_FAWSGG_elc_FERCStaff_Privileged_GAS_RO);
                        break;
                    case Constants.PrivilegedLibrary_Electric:
                        group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_FAWSGG_elc_FERCStaff_Privileged_electric_RO);
                        break;
                    case Constants.PrivilegedLibrary_Hydro:
                        group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_FAWSGG_elc_FERCStaff_Privileged_hydro_RO);
                        break;
                    case Constants.PrivilegedLibrary_Oil:
                        group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_FAWSGG_elc_FERCStaff_Privileged_Oil_RO);
                        break;
                    case Constants.PrivilegedLibrary_RuleMaking:
                        group = clientContext.Web.SiteGroups.GetByName(Constants.Grp_FAWSGG_elc_FERCStaff_Privileged_RuleMaking_RO);
                        break;
                    default:
                        group = null;
                        break;

                }

                foreach (string url in urls)
                {
                    if (group != null)
                    {
                        AssignRoleForDocument(clientContext, listItemID, group, Constants.Role_Read, url);
                    }
                }
            }

            //clientContext.ExecuteQuery();
        }

        public Dictionary<string, string> getAllDocumentUrls(Repeater rpDocumentList)
        {
            Dictionary<string, string> issuanceDocuments = new Dictionary<string, string>();
            foreach (RepeaterItem row in rpDocumentList.Items)
            {
                var url = ((HyperLink)row.FindControl("hplEdit")).NavigateUrl;
                var securityLevel = ((Label)row.FindControl("lbSecurityLevel")).Text;
                if (!issuanceDocuments.ContainsKey(url))
                {
                    issuanceDocuments.Add(url, securityLevel);
                }
            }

            return issuanceDocuments;
        }

        public void AssignRoleForGroup(ClientContext clientContext, Group group, string role, Folder folder)
        {
            var web = clientContext.Web;

            //remove existing group role
            folder.ListItemAllFields.RoleAssignments.Groups.Remove(group);

            var rolebindingCol = new RoleDefinitionBindingCollection(clientContext);
            rolebindingCol.Add(web.RoleDefinitions.GetByName(role));

            folder.ListItemAllFields.RoleAssignments.Add(group, rolebindingCol);

        }

        public void AssignRoleForDocument(ClientContext clientContext, string listItemID, Group group, string role, string fileURL)
        {
            var web = clientContext.Web;
            string documentServerRelativeUrlUrl = getDocumentServerRelativeURLFromURL(clientContext, listItemID, fileURL);
            File document = web.GetFileByServerRelativeUrl(documentServerRelativeUrlUrl);
            //remove existing group role
            //folder.ListItemAllFields.RoleAssignments.Groups.Remove(group);
            document.ListItemAllFields.BreakRoleInheritance(false, true);//clear all permission from parent

            var rolebindingCol = new RoleDefinitionBindingCollection(clientContext);
            rolebindingCol.Add(web.RoleDefinitions.GetByName(role));

            document.ListItemAllFields.RoleAssignments.Add(group, rolebindingCol);

        }

        #endregion

    }

}


