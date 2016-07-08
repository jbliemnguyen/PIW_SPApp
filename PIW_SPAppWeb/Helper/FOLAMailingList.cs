using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.ApplicationServices;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.SharePoint.Client;

namespace PIW_SPAppWeb.Helper
{
    public class FOLAMailingList
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="WorksetShortLabel">root dockets seperated by comma - ex: P-1234,PQ-789</param>
        /// <returns></returns>
        private FOLAMailingListData GetFOLAMailingList(string WorksetShortLabel)
        {
            FOLAMailingListData data = new FOLAMailingListData();
            data.Headers.Add("Contact Name");
            data.Headers.Add("FERC ID");
            data.Headers.Add("Contact Title");
            data.Headers.Add("Contact Organization");
            data.Headers.Add("PO Box");
            data.Headers.Add("Address Line 1");
            data.Headers.Add("Address Line 2");
            data.Headers.Add("City");
            data.Headers.Add("Zip");
            data.Headers.Add("Zip 2");
            data.Headers.Add("State");
            data.Headers.Add("Docket");
            
            using (SqlConnection con = new SqlConnection(ConfigurationManager.AppSettings["FOLAConnectionString"]))
            {
                using (SqlCommand cmd = new SqlCommand("p_fola_rpt_getmailinglist4", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();


                    cmd.Parameters.Add("@Work_Set_Short_Label", SqlDbType.VarChar).Value = WorksetShortLabel;
                    cmd.Parameters.Add("@Include_Senators", SqlDbType.Bit).Value = 0;
                    cmd.Parameters.Add("@Include_eReg", SqlDbType.Bit).Value = 0;
                    cmd.Parameters.Add("@ReturnBlankAddress", SqlDbType.Bit).Value = 0;


                    SqlDataReader reader = cmd.ExecuteReader();
                    List<string> row;
                    while (reader.Read())
                    {
                        row = new List<string>();

                        //Contact FUll Name
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Full_Name] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_Full_Name].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Full_Name].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }


                        //row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Full_Name].ToString());


                        //FERC ID
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_FERC_id] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_FERC_id].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_FERC_id].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Contact Title
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Title] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_Title].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Title].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Contact Organization
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Organization] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_Organization].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Organization].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //PO Box value = "PO Box:" + value of Contact_PO_Box if it is not null
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Po_Box] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_Po_Box].ToString()))
                        {
                            row.Add("PO Box: " +
                                    reader[Constants.FOLA_MailingListColumnName_Contact_Po_Box].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Address Line 1
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line1] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line1].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line1].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Address Line 2
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line2] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line2].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line2].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Contact City
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_City] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_City].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_City].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Zip and Zip 2
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Zip_2] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Contact_Zip_2].ToString()))
                        {
                            var zips =
                                reader[Constants.FOLA_MailingListColumnName_Contact_Zip_2].ToString()
                                    .Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                            if (zips.Count() == 1)
                            {
                                row.Add(zips[0]);
                                row.Add(string.Empty);
                            }
                            else if (zips.Count() > 1)
                            {
                                row.Add(zips[0]);
                                row.Add(zips[1]);
                            }
                        }
                        else
                        {
                            row.Add(string.Empty);
                            row.Add(string.Empty);
                        }

                        //State - last 2 character of Contact_CS
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_CS] != null) &&
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_CS].ToString()))
                        {
                            string contact_cs = reader[Constants.FOLA_MailingListColumnName_Contact_CS].ToString();
                            string state = contact_cs.Substring(contact_cs.Length - 2);
                            row.Add(state);
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Docket
                        if ((reader[Constants.FOLA_MailingListColumnName_Work_Set_Short_Label] != null) &&
                            !string.IsNullOrEmpty(
                                reader[Constants.FOLA_MailingListColumnName_Work_Set_Short_Label].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Work_Set_Short_Label].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        data.DataRows.Add(row);
                    }
                }
            }


            return data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="docketNumber">root dockets seperated by comma - ex: P-1234,PQ-789</param>
        /// <param name="listItemID"></param>
        public int GenerateFOLAMailingExcelFile(ClientContext clientContext,string docketNumber,string listItemID)
        {
            SharePointHelper helper = new SharePointHelper();
            int numberOfAddress = 0;
            string rootDocketNumbers = string.Empty;
            if (!docketNumber.Equals("non-docket", StringComparison.OrdinalIgnoreCase))
            {
                //docket list                                                                     
                string[] dockets = docketNumber.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                Dictionary<string, int> docketDictionary = new Dictionary<string, int>();//use string to avoid duplicate
                foreach (string docket in dockets)
                {
                    //remove sub-docket- P-12345-000 ---> P-12345
                    var rootDocket = docket.Substring(0, docket.LastIndexOf("-"));
                    if (!docketDictionary.ContainsKey(rootDocket))
                    {
                        docketDictionary.Add(rootDocket, 1);
                        //append to rootDocket string for FOLA use
                        if (string.IsNullOrEmpty(rootDocketNumbers))
                        {
                            rootDocketNumbers = rootDocket;
                        }
                        else
                        {
                            rootDocketNumbers = rootDocketNumbers + "," + rootDocket;
                        }
                    }
                }

                if (!String.IsNullOrEmpty(rootDocketNumbers))
                {
                    var folaMailingList = GetFOLAMailingList(rootDocketNumbers);
                    if (folaMailingList.DataRows.Count > 0)
                    {
                        var file = GenerateExcel(folaMailingList);
                        if (file != null)
                        {
                            string uploadedFileURL = helper.UploadDocumentContentStream(clientContext, file,
                                Constants.PIWDocuments_DocumentLibraryName,
                                listItemID, Constants.FOLA_MailingList_FileName,
                                Constants.ddlSecurityControl_Option_Public,
                                Constants.PIWDocuments_DocTypeOption_FOLAServiceMailingList, true);
                            //save number of fola mailing list address
                            //ListItem listItem = helper.GetPiwListItemById(clientContext, listItemID, false);
                            //helper.InitiatePrintReqForm(clientContext, listItem, folaMailingList.DataRows.Count);
                            numberOfAddress = folaMailingList.DataRows.Count;

                        }
                    }
                }

            }

            return numberOfAddress;
        }

        #region Excel file writer
        private string ColumnLetter(int intCol)
        {
            var intFirstLetter = ((intCol) / 676) + 64;
            var intSecondLetter = ((intCol % 676) / 26) + 64;
            var intThirdLetter = (intCol % 26) + 65;

            var firstLetter = (intFirstLetter > 64)
                ? (char)intFirstLetter : ' ';
            var secondLetter = (intSecondLetter > 64)
                ? (char)intSecondLetter : ' ';
            var thirdLetter = (char)intThirdLetter;

            return string.Concat(firstLetter, secondLetter,
                thirdLetter).Trim();
        }

        private Cell CreateTextCell(string header, UInt32 index,
            string text)
        {
            var cell = new Cell
            {
                DataType = CellValues.InlineString,
                CellReference = header + index
            };

            var istring = new InlineString();
            var t = new Text { Text = text };
            istring.AppendChild(t);
            cell.AppendChild(istring);
            return cell;
        }

        public Stream GenerateExcel(FOLAMailingListData data)
        {
            if (data.DataRows.Count == 0)
            {
                return null;
            }

            var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookpart = document.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();
                var worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();

                worksheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = document.WorkbookPart.Workbook.
                    AppendChild<Sheets>(new Sheets());

                var sheet = new Sheet()
                {
                    Id = document.WorkbookPart
                        .GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Sheet 1"
                };
                sheets.AppendChild(sheet);

                // Add header
                UInt32 rowIdex = 0;
                var row = new Row { RowIndex = ++rowIdex };
                sheetData.AppendChild(row);
                var cellIdex = 0;

                foreach (var header in data.Headers)
                {
                    row.AppendChild(CreateTextCell(ColumnLetter(cellIdex++),
                        rowIdex, header ?? string.Empty));
                }

                // Add sheet data
                foreach (var rowData in data.DataRows)
                {
                    cellIdex = 0;
                    row = new Row { RowIndex = ++rowIdex };
                    sheetData.AppendChild(row);
                    foreach (var callData in rowData)
                    {
                        var cell = CreateTextCell(ColumnLetter(cellIdex++),
                            rowIdex, callData ?? string.Empty);
                        row.AppendChild(cell);
                    }
                }

                workbookpart.Workbook.Save();
            }

            return stream;
        }
        #endregion
    }
}