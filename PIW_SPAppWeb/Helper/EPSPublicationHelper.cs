using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using FERC.Common.Queues;
using FERC.eLibrary.Eps.Common;
//using FERC.eLibrary.Eps.Data;
using FERC.MSOffice;
using FERC.MSOffice;
using FERC.MSOfficeAutomation;
using Microsoft.Office.Interop.Word;
using Microsoft.SharePoint.Client;
using Document = FERC.eLibrary.Eps.Common.Document;
using File = Microsoft.SharePoint.Client.File;

namespace PIW_SPAppWeb.Helper
{
    public class EPSPublicationHelper
    {
        SharePointHelper helper = new SharePointHelper();
        public bool Publish(ClientContext clientContext, Dictionary<string, string> documentWithFullURLs, ListItem listItem)
        {
            bool result = false;
            string submissionQueue = ConfigurationManager.AppSettings["submissionqueue"];
            string responseQueue = ConfigurationManager.AppSettings["responsequeue"];
            string fileStoragePath = ConfigurationManager.AppSettings["PIWDocuments"];


            var internalColumnNameList = helper.getInternalColumnNamesFromCache(clientContext, Constants.PIWListName);

            string listItemId = listItem["ID"].ToString();
            string docketNumber = listItem[internalColumnNameList[Constants.PIWList_colName_DocketNumber]] != null ?
                listItem[internalColumnNameList[Constants.PIWList_colName_DocketNumber]].ToString() : string.Empty;

            string description = listItem[internalColumnNameList[Constants.PIWList_colName_Description]] != null ?
                listItem[internalColumnNameList[Constants.PIWList_colName_Description]].ToString() : string.Empty;

            string fercCitation = listItem[internalColumnNameList[Constants.PIWList_colName_CitationNumber]] != null ?
                listItem[internalColumnNameList[Constants.PIWList_colName_CitationNumber]].ToString() : string.Empty;

            string destinationUrnFolder = string.Format("{0}\\{1}", fileStoragePath, listItemId);

            DateTime dueDate;
            if (listItem[internalColumnNameList[Constants.PIWList_colName_DueDate]] != null)
            {
                if (!string.IsNullOrEmpty(listItem[internalColumnNameList[Constants.PIWList_colName_DueDate]].ToString().Trim()))
                {
                    dueDate = DateTime.Parse(listItem[internalColumnNameList[Constants.PIWList_colName_DueDate]].ToString().Trim()).Date;
                }
            }



            //start publishing
            Publication publication = new Publication(EpsCallingApplication.PIW, EpsCatCode.ISSUANCE);
            publication.HasFamily = (documentWithFullURLs.Count > 1);//if more than 1 document, set the HasFamily to true so parent/child relationship canbe set in EPS
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
            AffiliationInfo affiliationInfo = new AffiliationInfo(Constants.Affiliation_FirstName, Constants.Affiliation_LastName,
                Constants.Affiliation_MiddleInitial, Constants.Affiliation_Organization, AuthRecipRole.AUTHOR);
            publication.AffiliationsList.Add(affiliationInfo);

            var documentsWithServerRelativeURL = helper.getDocumentServerRelativeURL(clientContext, listItemId, documentWithFullURLs);

            //Copy all documentWithFullURLs
            foreach (KeyValuePair<string, string> file in documentsWithServerRelativeURL)
            {
                string fileURN = helper.CopyFile(clientContext, file.Key, destinationUrnFolder);

                //Document
                Document document = new Document();
                document.AvailabilityCode = helper.getEPSAvailabilityCode(file.Value);
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

            result = true;

            return result;
        }
        public EpsResult ValidateDocument(string fullPathFileName, int? documentOfficialFlag, string documentAvailability)
        {
            if (documentOfficialFlag == null)
            {
                documentOfficialFlag = 1;
            }

            if (string.IsNullOrEmpty(documentAvailability))
            {
                documentAvailability = "P";
            }

            var publication = PopulatePublication(documentOfficialFlag.Value, documentAvailability, string.Empty, string.Empty, fullPathFileName);

            return HasMSWordModifications(publication);
        }

        Publication PopulatePublication(int documentOfficialFlag, string documentAvailability, string description, string fercCitation, string fileURN)
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
                                    result.ErrorList.Add((int)EpsResponseCode.FAILURE, "Has Revisions: " + file.FileName);
                                }


                                if (FERC.MSOffice.XMLDocument.WordDocHasComments(file.FullName))
                                {
                                    result.ErrorList.Add((int)EpsResponseCode.FAILURE, "Has Comments: " + file.FileName);
                                }

                                break;

                            case "DOC"://never happens, we are not allowed to upload doc file
                                // use word automation to clean up old Word DOC format document.

                                // timeout for hung MS Word automation.
                                int MSWordAutomationOpenTimeout = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MSWordAutomationOpenTimeout"));
                                if (MSWordAutomationOpenTimeout < 30) MSWordAutomationOpenTimeout = 30;

                                int MSWordAutomationHasFieldTimeout = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MSWordAutomationHasFieldTimeout"));
                                if (MSWordAutomationHasFieldTimeout < 30) MSWordAutomationHasFieldTimeout = 30;

                                int MSWordAutomationHasRevisionsTimeout = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MSWordAutomationHasRevisionsTimeout"));
                                if (MSWordAutomationHasRevisionsTimeout < 30) MSWordAutomationHasRevisionsTimeout = 30;

                                int MSWordAutomationHasCommentsTimeout = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MSWordAutomationHasCommentsTimeout"));
                                if (MSWordAutomationHasCommentsTimeout < 30) MSWordAutomationHasCommentsTimeout = 30;

                                int MSWordAutomationCloseTimeout = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MSWordAutomationCloseTimeout"));
                                if (MSWordAutomationCloseTimeout < 30) MSWordAutomationCloseTimeout = 30;

                                using (MSWord msw = new MSWord())
                                {
                                    msw.Open(file.FullName, MSWordAutomationOpenTimeout);

                                    if (msw.HasComments(MSWordAutomationHasCommentsTimeout))
                                    {
                                        result.ErrorList.Add((int)EpsResponseCode.FAILURE, "Has Comments: " + file.FileName);
                                    }

                                    if (msw.HasRevisions(MSWordAutomationHasRevisionsTimeout))
                                    {
                                        result.ErrorList.Add((int)EpsResponseCode.FAILURE, "Has Revisions: " + file.FileName);
                                    }

                                    if (msw.HasFieldType(WdFieldType.wdFieldDate, MSWordAutomationHasFieldTimeout))
                                    {
                                        result.ErrorList.Add((int)EpsResponseCode.FAILURE, "Has Macros: " + file.FileName);
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

        public int getNumberOfPages(string fileURN)
        {
            int numberOfPages = 0;
            var fileInfo = new FileInfo(fileURN);
            string extension = fileInfo.Extension;
            if (extension.ToLower() == ".pdf")
            {
                numberOfPages = getPDFNumberOfPages(fileURN);
            }
            else if (extension.ToLower() == ".docx")
            {
                numberOfPages = getDOCXNumberOfPages(fileURN);
            }

            return numberOfPages;
        }
        public int getPDFNumberOfPages(string fileURL)
        {
            //http://www.dotnetspider.com/resources/21866-Count-pages-PDF-file.aspx
            //Function for finding the number of pages in a given PDF file
            FileStream fs = new FileStream(fileURL, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string pdf = sr.ReadToEnd();
            Regex rx = new Regex(@"/Type\s/Page[^s]");
            //Regex rx = new Regex(@"/Type/Page");

            int pages = rx.Matches(pdf).Count;


            if (pages == 0)
            {
                rx = new Regex(@"/Type/Page");
                pages = rx.Matches(pdf).Count;
            }

            return pages;
        }

        public int getDOCXNumberOfPages(string fileURN)
        {
            throw new NotImplementedException();
        }
    }
}