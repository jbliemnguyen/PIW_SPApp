using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FERC.Common.Queues;
using FERC.eLibrary.Eps.Common;
//using FERC.eLibrary.Eps.Data;
using FERC.MSOffice;
using FERC.MSOfficeAutomation;
using iTextSharp.text.pdf;
using Microsoft.Office.Interop.Word;
using Microsoft.SharePoint.Client;
using Document = FERC.eLibrary.Eps.Common.Document;
using File = Microsoft.SharePoint.Client.File;
using Row = Microsoft.Office.Interop.Word.Row;

namespace PIW_SPAppWeb.Helper
{
    public class EPSPublicationHelper
    {
        private SharePointHelper helper = new SharePointHelper();

        public bool Publish(ClientContext clientContext, Dictionary<string, string> documentWithFullURLs, string supplementalMailingListFileName, ListItem piwListItem)
        {
            int totalPublicDocPages = 0;
            string submissionQueue = ConfigurationManager.AppSettings["submissionqueue"];
            string responseQueue = ConfigurationManager.AppSettings["responsequeue"];
            string fileStoragePath = ConfigurationManager.AppSettings["PIWDocuments"];


            var internalColumnNameList = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            string listItemId = piwListItem["ID"].ToString();
            string docketNumber = piwListItem[internalColumnNameList[Constants.PIWList_colName_DocketNumber]] != null
                ? piwListItem[internalColumnNameList[Constants.PIWList_colName_DocketNumber]].ToString()
                : string.Empty;

            string description = piwListItem[internalColumnNameList[Constants.PIWList_colName_Description]] != null
                ? piwListItem[internalColumnNameList[Constants.PIWList_colName_Description]].ToString()
                : string.Empty;

            string fercCitation = piwListItem[internalColumnNameList[Constants.PIWList_colName_CitationNumber]] != null
                ? piwListItem[internalColumnNameList[Constants.PIWList_colName_CitationNumber]].ToString()
                : string.Empty;

            string destinationUrnFolder = string.Format("{0}\\{1}", fileStoragePath, listItemId);

            DateTime dueDate;
            if (piwListItem[internalColumnNameList[Constants.PIWList_colName_DueDate]] != null)
            {
                if (
                    !string.IsNullOrEmpty(
                        piwListItem[internalColumnNameList[Constants.PIWList_colName_DueDate]].ToString().Trim()))
                {
                    dueDate =
                        DateTime.Parse(
                            piwListItem[internalColumnNameList[Constants.PIWList_colName_DueDate]].ToString().Trim()).Date;
                }
            }



            //start publishing
            Publication publication = new Publication(EpsCallingApplication.PIW, EpsCatCode.ISSUANCE);
            publication.HasFamily = (documentWithFullURLs.Count > 1);
            //if more than 1 document, set the HasFamily to true so parent/child relationship canbe set in EPS
            if (!docketNumber.Equals("non-docket", StringComparison.OrdinalIgnoreCase))
            {
                //docket list                                                                     
                string[] dockets = docketNumber.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string docket in dockets)
                {
                    publication.AssociatedDockets.Add(docket.Trim());
                }
            }


            //affiliation list
            AffiliationInfo affiliationInfo = new AffiliationInfo(Constants.Affiliation_FirstName,
                Constants.Affiliation_LastName,
                Constants.Affiliation_MiddleInitial, Constants.Affiliation_Organization, AuthRecipRole.AUTHOR);
            publication.AffiliationsList.Add(affiliationInfo);

            var documentsWithServerRelativeURL = helper.getDocumentServerRelativeURL(clientContext, listItemId,
                documentWithFullURLs);

            //Copy all documentWithFullURLs and calcuate number of public pages
            foreach (KeyValuePair<string, string> file in documentsWithServerRelativeURL)
            {
                int filePages = 0;
                string fileURN = CopyFile(clientContext, file.Key, destinationUrnFolder, ref filePages);

                //count the page if the document is Public, used for Printing
                if (file.Value.Equals(Constants.ddlSecurityControl_Option_Public))
                {
                    totalPublicDocPages = totalPublicDocPages + filePages;
                }

                //Document
                Document document = new Document();
                document.AvailabilityCode = getEPSAvailabilityCode(file.Value);
                document.OfficialFlag = Constants.document_OfficialFlag;
                document.FileDate = DateTime.Now;
                document.ReceivedDate = DateTime.Now;
                document.IssueDate = DateTime.Now;
                document.Description = description;
                //142 FERC ¶ 62,014 is passed as 142FERC62,014
                document.FERCCitation = fercCitation.Replace("¶", string.Empty).Replace(" ", string.Empty);

                //File
                string fileExtension = string.Empty;
                long fileSize = 0;

                FileInfo fileInfo = new FileInfo(fileURN);
                fileExtension = fileInfo.Extension;
                fileSize = fileInfo.Length;

                EpsFile epsFile = new EpsFile(fileURN, fileExtension, fileSize);
                Transmittal transmittal = new Transmittal();
                transmittal.Filelist.Add(epsFile);

                document.TransmittalBatch.Add(transmittal);

                publication.DocumentList.Add(document);
            }



            //Send publication to EPS
            QueueSender<QueueMessage<Publication>> qs = new QueueSender<QueueMessage<Publication>>(submissionQueue);
            qs.Send(new QueueMessage<Publication>(responseQueue, int.Parse(listItemId), publication));

            //Set number of pages - total public doc pages and supplemental mailing list numnber of rows(addresses)
            int supplementalMailingListNumberOfPages = 0;
            if (!string.IsNullOrEmpty(supplementalMailingListFileName))
            {
                supplementalMailingListNumberOfPages = getNumberOfRowsFromSupplementalMailingListExcelFile(clientContext, listItemId, supplementalMailingListFileName);
            }
            helper.SaveNumberOfPublicPagesAndSupplementalMailingListAddress(clientContext,piwListItem,totalPublicDocPages,supplementalMailingListNumberOfPages);

            //generate fola excel mailing list file
            FOLAMailingList folaMailingList = new FOLAMailingList();
            folaMailingList.GenerateFOLAMailingExcelFile(clientContext, docketNumber, listItemId);
            

            return true;
        }

        public string getEPSAvailabilityCode(string ddldocumentSecurity)
        {
            string result = string.Empty;
            switch (ddldocumentSecurity)
            {
                case Constants.ddlSecurityControl_Option_Public:
                    result = Constants.PIWDocuments_EPSSecurityLevel_Option_Public;
                    break;
                case Constants.ddlSecurityControl_Option_CEII:
                    result = Constants.PIWDocuments_EPSSecurityLevel_Option_CEII;
                    break;
                case Constants.ddlSecurityControl_Option_Privileged:
                    result = Constants.PIWDocuments_EPSSecurityLevel_Option_NonPublic;
                    break;
                default:
                    break;
            }
            return result;

        }
        public string CopyFile(ClientContext clientContext, string sourceFileURL, string DestinationURNFolder, ref int pages)
        {
            if (!Directory.Exists(DestinationURNFolder))
            {
                Directory.CreateDirectory(DestinationURNFolder);
            }


            FileInformation fileInfo = File.OpenBinaryDirect(clientContext, sourceFileURL);
            string fileName = helper.getFileNameFromURL(sourceFileURL);
            var destinationFileURN = DestinationURNFolder + "\\" + fileName;
            using (var fileStream = System.IO.File.Create(destinationFileURN))
            {
                fileInfo.Stream.CopyTo(fileStream);
            }

            pages = getPublishedIssuanceNumberOfPages(destinationFileURN);

            return destinationFileURN;

        }

        public EpsResult ValidateDocument(string fullPathFileName, int? documentOfficialFlag,
            string documentAvailability)
        {
            if (documentOfficialFlag == null)
            {
                documentOfficialFlag = 1;
            }

            if (string.IsNullOrEmpty(documentAvailability))
            {
                documentAvailability = "P";
            }

            var publication = PopulatePublication(documentOfficialFlag.Value, documentAvailability, string.Empty,
                string.Empty, fullPathFileName);

            return HasMSWordModifications(publication);
        }

        private Publication PopulatePublication(int documentOfficialFlag, string documentAvailability,
            string description, string fercCitation, string fileURN)
        {
            Publication publication = new Publication(EpsCallingApplication.PIW, EpsCatCode.ISSUANCE);

            //Document
            Document document = new Document
            {
                AvailabilityCode = documentAvailability,
                OfficialFlag = documentOfficialFlag,
                FileDate = DateTime.Now,
                ReceivedDate = DateTime.Now,
                IssueDate = DateTime.Now,
                Description = description,
                FERCCitation = fercCitation.Replace("¶", string.Empty).Replace(" ", string.Empty)
            };
            //142 FERC ¶ 62,014 is passed as 142FERC62,014

            //File
            //string fileURN = fileStoragePath + piWlistItemId + @"\" + fileName;
            string fileExtension = string.Empty;
            long fileSize = 0;

            FileInfo fileInfo = new FileInfo(fileURN);
            fileExtension = fileInfo.Extension;
            fileSize = fileInfo.Length;



            EpsFile epsFile = new EpsFile(fileURN, fileExtension, fileSize);

            Transmittal transmittal = new Transmittal();
            transmittal.Filelist.Add(epsFile);

            document.TransmittalBatch.Add(transmittal);

            publication.DocumentList.Add(document);

            return publication;

        }

        public EpsResult HasMSWordModifications(Publication pub)
        {
            EpsResult result = new EpsResult();

            foreach (var doc in pub.DocumentList)
            {
                foreach (var transmittal in doc.TransmittalBatch)
                {
                    foreach (var file in transmittal.Filelist)
                    {
                        switch (file.Extension.ToUpper())
                        {
                            case "DOCX":
                                if (FERC.MSOffice.XMLDocument.WordDocHasField(file.FullName, FieldTypes.DATE))
                                {
                                    result.ErrorList.Add((int)EpsResponseCode.FAILURE, "Has Macros: " + file.FileName);
                                }


                                if (FERC.MSOffice.XMLDocument.WordDocHasRevisions(file.FullName))
                                {
                                    result.ErrorList.Add((int)EpsResponseCode.FAILURE,
                                        "Has Revisions: " + file.FileName);
                                }


                                if (FERC.MSOffice.XMLDocument.WordDocHasComments(file.FullName))
                                {
                                    result.ErrorList.Add((int)EpsResponseCode.FAILURE, "Has Comments: " + file.FileName);
                                }

                                break;

                            case "DOC": //never happens, we are not allowed to upload doc file
                                // use word automation to clean up old Word DOC format document.

                                // timeout for hung MS Word automation.
                                int MSWordAutomationOpenTimeout =
                                    Convert.ToInt32(ConfigurationManager.AppSettings.Get("MSWordAutomationOpenTimeout"));
                                if (MSWordAutomationOpenTimeout < 30) MSWordAutomationOpenTimeout = 30;

                                int MSWordAutomationHasFieldTimeout =
                                    Convert.ToInt32(
                                        ConfigurationManager.AppSettings.Get("MSWordAutomationHasFieldTimeout"));
                                if (MSWordAutomationHasFieldTimeout < 30) MSWordAutomationHasFieldTimeout = 30;

                                int MSWordAutomationHasRevisionsTimeout =
                                    Convert.ToInt32(
                                        ConfigurationManager.AppSettings.Get("MSWordAutomationHasRevisionsTimeout"));
                                if (MSWordAutomationHasRevisionsTimeout < 30) MSWordAutomationHasRevisionsTimeout = 30;

                                int MSWordAutomationHasCommentsTimeout =
                                    Convert.ToInt32(
                                        ConfigurationManager.AppSettings.Get("MSWordAutomationHasCommentsTimeout"));
                                if (MSWordAutomationHasCommentsTimeout < 30) MSWordAutomationHasCommentsTimeout = 30;

                                int MSWordAutomationCloseTimeout =
                                    Convert.ToInt32(ConfigurationManager.AppSettings.Get("MSWordAutomationCloseTimeout"));
                                if (MSWordAutomationCloseTimeout < 30) MSWordAutomationCloseTimeout = 30;

                                using (MSWord msw = new MSWord())
                                {
                                    msw.Open(file.FullName, MSWordAutomationOpenTimeout);

                                    if (msw.HasComments(MSWordAutomationHasCommentsTimeout))
                                    {
                                        result.ErrorList.Add((int)EpsResponseCode.FAILURE,
                                            "Has Comments: " + file.FileName);
                                    }

                                    if (msw.HasRevisions(MSWordAutomationHasRevisionsTimeout))
                                    {
                                        result.ErrorList.Add((int)EpsResponseCode.FAILURE,
                                            "Has Revisions: " + file.FileName);
                                    }

                                    if (msw.HasFieldType(WdFieldType.wdFieldDate, MSWordAutomationHasFieldTimeout))
                                    {
                                        result.ErrorList.Add((int)EpsResponseCode.FAILURE,
                                            "Has Macros: " + file.FileName);
                                    }

                                    msw.Close(MSWordAutomationCloseTimeout);
                                }

                                break;

                            // others to be done.
                            default:
                                break;
                        }
                    }
                }
            }

            return result;
        }

        public int getPublishedIssuanceNumberOfPages(string fileURN)
        {
            int numberOfPages = 0;
            var fileInfo = new FileInfo(fileURN);
            string extension = fileInfo.Extension;
            if (extension.ToLower() == ".pdf")
            {
                PdfReader pdfReader = new PdfReader(fileURN);
                numberOfPages = pdfReader.NumberOfPages;
            }
            else if (extension.ToLower() == ".docx")
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(fileURN, false))
                {
                    numberOfPages = int.Parse(doc.ExtendedFilePropertiesPart.Properties.Pages.InnerText);
                }

            }

            return numberOfPages;
        }
        

        public int getNumberOfRowsFromSupplementalMailingListExcelFile(ClientContext clientContext, string listItemID, string fileName)
        {
            var documentServerRelativeURL = helper.getDocumentServerRelativeURL(clientContext, listItemID, fileName);
            FileInformation fileInformation = File.OpenBinaryDirect(clientContext, documentServerRelativeURL);

            using (MemoryStream fileStream = new MemoryStream())
            {
                fileInformation.Stream.CopyTo(fileStream);

                int numberOfRows = 0;
                using (SpreadsheetDocument myDoc = SpreadsheetDocument.Open(fileStream, false))
                {
                    var worksheetParts = myDoc.WorkbookPart.WorksheetParts;
                    foreach (var worksheetPart in worksheetParts)
                    {
                        SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
                        if (sheetData != null)
                        {
                            int AddressRows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().Count() - 1;
                            //subtract the header

                            if (AddressRows > 0)
                            {
                                numberOfRows = numberOfRows + AddressRows;
                            }
                        }
                    }
                }
                return numberOfRows;
            }
        }
    }
}