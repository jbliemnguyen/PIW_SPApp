using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Web;
using System.Web.Caching;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.UserProfiles;
using File = Microsoft.SharePoint.Client.File;
using ListItem = Microsoft.SharePoint.Client.ListItem;
using FERC.FOL.ATMS.Remote.Interfaces;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;
using System.Text;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using FontSize = System.Web.UI.WebControls.FontSize;
using Group = Microsoft.SharePoint.Client.Group;
using Page = System.Web.UI.Page;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

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
            var internalNameList = getInternalColumnNamesFromCache(context, Constants.PIWListName);

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

        public void SaveReOpenInfoAndStatus(ClientContext clientContext, ListItem listItem, string FormStatus, string PreviousFormStatus)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            //clear secreview action and comment
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_SecReviewAction]] = string.Empty;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_SecReviewComment]] = string.Empty;

            //clear accession number
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_AccessionNumber]] = string.Empty;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SaveFormStatus(ClientContext clientContext, ListItem listItem, string FormStatus, string PreviousFormStatus)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SaveDeleteInfoAndStatus(ClientContext clientContext, ListItem listItem, string FormStatus, string PreviousFormStatus)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            clientContext.Load(clientContext.Web.CurrentUser, user => user.Id);
            clientContext.ExecuteQuery();

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_IsActive]] = false;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_CitationNumber]] = string.Empty;


            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SavePublishingInfoAndStatus(ClientContext clientContext, ListItem listItem, string FormStatus, string PreviousFormStatus)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            clientContext.Load(clientContext.Web.CurrentUser, user => user.Id);
            clientContext.ExecuteQuery();

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_FormStatus]] = FormStatus;
            listItem[piwListInternalColumnNames[Constants.PIWList_colName_PreviousFormStatus]] = PreviousFormStatus;

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

        public void SaveNumberOfFOLAMailingListAddress(ClientContext clientContext, ListItem listItem, int numberOfFOLAMailingListAddress)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfFOLAMailingListAddress]] = numberOfFOLAMailingListAddress;

            listItem.Update();
            clientContext.ExecuteQuery();
        }

        public void SaveNumberOfPublicPagesAndSupplementalMailingListAddress(ClientContext clientContext, ListItem listItem, int numberOfPublicPages, int numberOfSupplementalMailingListAddress)
        {
            var piwListInternalColumnNames = getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            if (numberOfPublicPages > 0)
            {
                listItem[piwListInternalColumnNames[Constants.PIWList_colName_NumberOfPublicPages]] =
                    numberOfPublicPages;
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

            FileCollection files = folder.Files;
            clientContext.Load(files);
            clientContext.Load(files, includes => includes.Include(i => i.ListItemAllFields));
            clientContext.ExecuteQuery();//file not found exception if the folder is not exist, let it crash because it is totally wrong somewhere

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

        public System.Data.DataTable getDocumentsTableByDocType(ClientContext clientContext, string subFoder, string libraryName, out StringBuilder DocumentURLs, string docType)
        {
            DocumentURLs = new StringBuilder();
            var result = new System.Data.DataTable();
            result.Columns.Add("ID");
            result.Columns.Add("Name");
            result.Columns.Add("URL");
            result.Columns.Add("Security Level");
            //result.Columns.Add("EPS Passed");
            //result.Columns.Add("EPS Error");

            var internalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWDocuments_DocumentLibraryName);

            clientContext.Load(clientContext.Web, web => web.Url);
            clientContext.ExecuteQuery();

            string uploadSubFolderURL = string.Format("{0}/{1}/{2}", clientContext.Web.Url, libraryName, subFoder);

            var documentList = getDocumentsByDocType(clientContext, uploadSubFolderURL, docType);

            foreach (File file in documentList)
            {
                System.Data.DataRow row = result.NewRow();
                row["ID"] = file.ListItemAllFields["ID"];
                row["Name"] = file.Name;
                row["URL"] = uploadSubFolderURL + "/" + row["Name"];
                row["Security Level"] =
                    file.ListItemAllFields[internalNameList[Constants.PIWDocuments_colName_SecurityLevel]];

                result.Rows.Add(row);

                if (DocumentURLs.Length == 0)
                {
                    DocumentURLs.Append(row["URL"]);
                }
                else
                {
                    DocumentURLs.Append(Constants.DocumentURLsSeparator + row["URL"]);
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

        public void CreatePIWListHistory(ClientContext clientContext, string listItemID, string action, string FormStatus,string FormType)
        {
            List piwlisthistory = clientContext.Web.Lists.GetByTitle(Constants.PIWListHistory_ListName);
            var piwlistHistoryInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.PIWListHistory_ListName);

            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            ListItem newItem = piwlisthistory.AddItem(itemCreateInfo);

            clientContext.Load(clientContext.Web.CurrentUser);
            clientContext.ExecuteQuery();
            newItem[piwlistHistoryInternalNameList[Constants.PIWListHistory_colName_User]] = clientContext.Web.CurrentUser;

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

        public ListItemCollection getHistoryListByPIWListID(ClientContext clientContext, string piwListItemID,string FormType)
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

        public System.Data.DataTable getHistoryListTable(ClientContext clientContext, string piwListItemID,string FormType)
        {
            var historyList = getHistoryListByPIWListID(clientContext, piwListItemID,FormType);
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

        public void PopulateHistoryList(ClientContext clientContext, string listItemId, Repeater rpHistoryList,string FormType)
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
                    IEnumerable<OpenXmlElement> elems = mainpart.Document.Body.Descendants().ToList();

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

        public string UploadIssuanceDocument(ClientContext clientContext, FileUpload fileUpload, string listItemId, Repeater rpDocumentList, Label lbUploadedDocumentError, Label lbRequiredUploadedDocumentError, string FormStatus, string securityControlValue, string docType)
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


                        //clear validation error
                        lbRequiredUploadedDocumentError.Visible = false;
                        lbUploadedDocumentError.Visible = false;
                        lbUploadedDocumentError.Text = string.Empty;

                        //history list
                        if (getHistoryListByPIWListID(clientContext, listItemId, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                        {
                            CreatePIWListHistory(clientContext, listItemId, "Workflow Item created", FormStatus,Constants.PIWListHistory_FormTypeOption_EditForm);
                        }

                        CreatePIWListHistory(clientContext, listItemId,
                            string.Format("Document file {0} uploaded/associated with Workflow Item", fileName), FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm);

                    }
                }
            }

            return uploadedFileURL;
        }

        public bool UploadSupplementalMailingListDocument(ClientContext clientContext, FileUpload fileUpload, string listItemId,
            Repeater rpDocumentList, Label lbUploadedDocumentError,
            string FormStatus, string securityControlValue, string docType)
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
                    UploadDocumentContentStream(clientContext, fileStream,Constants.PIWDocuments_DocumentLibraryName,listItemId, fileName, securityControlValue, docType, false);

                    //clear validation error
                    lbUploadedDocumentError.Visible = false;
                    lbUploadedDocumentError.Text = string.Empty;

                    //history list
                    if (getHistoryListByPIWListID(clientContext, listItemId, Constants.PIWListHistory_FormTypeOption_EditForm).Count == 0)
                    {
                        CreatePIWListHistory(clientContext, listItemId, "Workflow Item created", FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm);
                    }

                    CreatePIWListHistory(clientContext, listItemId,
                        string.Format("Supplemental Mailing List file {0} uploaded/associated with Workflow Item", fileName), FormStatus, Constants.PIWListHistory_FormTypeOption_EditForm);
                    result = true;
                }
            }

            return result;
        }

        public string PopulateIssuanceDocumentList(ClientContext clientContext, string listItemId, Repeater rpDocumentList)
        {
            StringBuilder documentURLs;
            System.Data.DataTable table = getDocumentsTableByDocType(clientContext, listItemId, Constants.PIWDocuments_DocumentLibraryName, out documentURLs, Constants.PIWDocuments_DocTypeOption_Issuance);
            rpDocumentList.DataSource = table;
            rpDocumentList.DataBind();
            return documentURLs.ToString();

        }
        public void PopulateSupplementalMailingListDocumentList(ClientContext clientContext, string listItemId, Repeater rpDocumentList, HtmlGenericControl supplementalMailingListFieldSet)
        {
            StringBuilder documentURLs;
            System.Data.DataTable table = getDocumentsTableByDocType(clientContext, listItemId, Constants.PIWDocuments_DocumentLibraryName, out documentURLs, Constants.PIWDocuments_DocTypeOption_SupplementalMailingList);
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
                    cache.Insert(listName, internalColumnList, null, DateTime.Now.AddHours(10), Cache.NoSlidingExpiration);
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
                var errorLogInternalNameList = getInternalColumnNamesFromCache(clientContext, Constants.ErrorLogListName);

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

        public bool IsCurrentUserMemberOfGroup(ClientContext clientContext, string groupName)
        {
            var currentUser = clientContext.Web.CurrentUser;
            clientContext.Load(currentUser);
            clientContext.ExecuteQuery();

            return IsUserMemberOfGroup(clientContext, currentUser, groupName);
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
                        //Agenda
                        //case Constants.PIWList_DocCat_NotationalOrder:
                        //case Constants.PIWList_DocCat_NotationalNotice:
                        //case Constants.PIWList_DocCat_CommissionOrder:
                        //case Constants.PIWList_DocCat_Consent:
                        //case Constants.PIWList_DocCat_Errata:
                        //case Constants.PIWList_DocCat_TollingOrder:
                        //case Constants.PIWList_DocCat_SunshineNotice:
                        //case Constants.PIWList_DocCat_NoticeofActionTaken:
                        documentCategoryNumber = 61;
                        break;
                    default:
                        throw new Exception("Unknown document category: " + documentCategory);
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
        
        #endregion

        #region FOLA and eLibrary connection

        #endregion

    }

}


