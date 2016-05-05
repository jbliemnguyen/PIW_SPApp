using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using FERC.eLibrary.Eps.Common;
//using FERC.eLibrary.Eps.Data;
using FERC.MSOffice;
using FERC.MSOffice;
using FERC.MSOfficeAutomation;
using Microsoft.Office.Interop.Word;
using Document = FERC.eLibrary.Eps.Common.Document;

namespace PIW_SPAppWeb.Helper
{
    public class EPSPublicationHelper
    {
        public void ValidateDocument()
        {
            //testing
            int documentOfficialFlag = 1;
            string documentAvailability = "P";
            //string fileURN1 = @"C:\Users\lnguyen\Desktop\Temp\45\GP04-1-000-PIWTest.docx";
            //string fileURN1 = @"http://fdc1s-sp23wfed2.ferc.gov/piw/PIW Documents/42/GP04-1-000-PIWTest - Copy (3).docx";
            //string fileURN1 = @"http://fdc1s-sp23wfed2.ferc.gov/piw/PIW Documents/42/GP04-1-000-PIWTest -  Copy.docx";
            //string fileURN1 = @"http://fdc1s-sp23wfed2.ferc.gov/piw/PIW Documents/42/GP04-1-000-PIWTest.docx";
            string fileURN1 = @"\\fdc1s-sp23wfed2.ferc.gov\piw\PIW Documents\42\GP04-1-000-PIWTest.docx";

            
            string fileURN2 = @"C:\Users\lnguyen\Desktop\Temp\45\FileContainsRevisionMarks.docx";
            string fileURN3 = @"C:\Users\lnguyen\Desktop\Temp\45\FileContainsRevisionMarks.doc";
            

            
            var publication1 = PopulatePublication(documentOfficialFlag, documentAvailability, string.Empty, string.Empty,fileURN1);
            var publication2 = PopulatePublication(documentOfficialFlag, documentAvailability, string.Empty, string.Empty, fileURN2);
            var publication3 = PopulatePublication(documentOfficialFlag, documentAvailability, string.Empty, string.Empty, fileURN3);
            
            //testing

            
            var result1 = HasMSWordModifications(publication1);
            var result2 = HasMSWordModifications(publication2);
            var result3 = HasMSWordModifications(publication3);





        }

        Publication PopulatePublication(int documentOfficialFlag, string documentAvailability, string description, string fercCitation, string fileURN)
        {
            Publication publication = new Publication(EpsCallingApplication.PIW, EpsCatCode.ISSUANCE);

            //Document
            Document document = new Document();
            document.AvailabilityCode = documentAvailability;
            document.OfficialFlag = documentOfficialFlag;
            document.FileDate = DateTime.Now;
            document.ReceivedDate = DateTime.Now;
            document.IssueDate = DateTime.Now;
            document.Description = description;
            //142 FERC ¶ 62,014 is passed as 142FERC62,014
            document.FERCCitation = fercCitation.Replace("¶", string.Empty).Replace(" ", string.Empty);

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

                            case "DOC":
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


                                // timeout for hung MS Word automation.
                                int WordAutomationTimeout = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MSWordAutomationTimeout"));

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
    }
}