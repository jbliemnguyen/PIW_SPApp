using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private FOLAMailingListData GetFOLAMailingList(string WorksetShortLabel, ref int numberOfAddress)
        {
            FOLAMailingListData data = new FOLAMailingListData();
            int groupCount = 0;
            int grandCount = 0;

            using (SqlConnection con = new SqlConnection(ConfigurationManager.AppSettings["FOLAConnectionString"]))
            {
                using (SqlCommand cmd = new SqlCommand("p_fola_rpt_getmailinglist4", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();


                    cmd.Parameters.Add("@Work_Set_Short_Label", SqlDbType.VarChar).Value = WorksetShortLabel;
                    cmd.Parameters.Add("@Include_Senators", SqlDbType.Bit).Value = getIncludeSenatorsParameter(WorksetShortLabel);
                    cmd.Parameters.Add("@Include_eReg", SqlDbType.Bit).Value = 0;
                    cmd.Parameters.Add("@ReturnBlankAddress", SqlDbType.Bit).Value = 0;
                    //SqlDataReader dataRow = cmd.ExecuteReader();

                    DataTable dt = new DataTable();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }

                    DataView dv = dt.DefaultView;
                    dv.Sort = Constants.FOLA_MailingListColumnName_Contact_Country_Name + " DESC";
                    DataTable sortedDT = dv.ToTable();





                    for (int index = 0; index < sortedDT.Rows.Count; index++)
                    {
                        DataRow dataRow = sortedDT.Rows[index];
                        string Country = dataRow[Constants.FOLA_MailingListColumnName_Contact_Country_Name].ToString().Trim();

                        //add first header for each country
                        if (groupCount.Equals(0))
                        {
                            data.DataRows.Add(getHeaderRow());
                        }

                        groupCount++;
                        grandCount++;

                        data.DataRows.Add(getDataRow(dataRow));
                        if (index < sortedDT.Rows.Count - 1)
                        {
                            string nextCountry =
                                sortedDT.Rows[index + 1][Constants.FOLA_MailingListColumnName_Contact_Country_Name].ToString().Trim();

                            if (!Country.Equals(nextCountry, StringComparison.OrdinalIgnoreCase))
                            {
                                //new country row, add an summary (group total)
                                var groupRow = new List<string>();
                                groupRow.Add(String.Format("Group Total for {0}: {1}", Country, groupCount));
                                data.DataRows.Add(groupRow);
                                groupCount = 0;
                            }
                        }
                        else if (index.Equals(sortedDT.Rows.Count - 1))//last row, add final group count and total group count
                        {
                            var groupRow = new List<string>();
                            groupRow.Add(String.Format("Group Total for {0}: {1}", Country, groupCount));
                            data.DataRows.Add(groupRow);

                            groupRow = new List<string>();
                            groupRow.Add(String.Format("Grand Report Total: {0}", grandCount));
                            data.DataRows.Add(groupRow);
                        }

                    }
                }
            }

            numberOfAddress = grandCount;
            return data;
        }

        private List<String> getDataRow(DataRow dataRow)
        {
            var row = new List<string>();
            //Contact FUll Name
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_Full_Name] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_Full_Name].ToString()))
            {
                row.Add(dataRow[Constants.FOLA_MailingListColumnName_Contact_Full_Name].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }


            //FERC ID
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_FERC_id] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_FERC_id].ToString()))
            {
                row.Add(dataRow[Constants.FOLA_MailingListColumnName_Contact_FERC_id].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }

            //Contact Title
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_Title] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_Title].ToString()))
            {
                row.Add(dataRow[Constants.FOLA_MailingListColumnName_Contact_Title].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }

            //Contact Organization
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_Organization] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_Organization].ToString()))
            {
                row.Add(dataRow[Constants.FOLA_MailingListColumnName_Contact_Organization].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }

            //PO Box value = "PO Box:" + value of Contact_PO_Box if it is not null
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_Po_Box] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_Po_Box].ToString()))
            {
                row.Add("PO Box: " +
                        dataRow[Constants.FOLA_MailingListColumnName_Contact_Po_Box].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }

            //Address Line 1
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_Address_Line1] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_Address_Line1].ToString()))
            {
                row.Add(dataRow[Constants.FOLA_MailingListColumnName_Contact_Address_Line1].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }

            //Address Line 2
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_Address_Line2] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_Address_Line2].ToString()))
            {
                row.Add(dataRow[Constants.FOLA_MailingListColumnName_Contact_Address_Line2].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }

            //Contact City
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_City] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_City].ToString()))
            {
                row.Add(dataRow[Constants.FOLA_MailingListColumnName_Contact_City].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }

            //Zip and Zip 2
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_Zip_2] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_Zip_2].ToString()))
            {
                var zips =
                    dataRow[Constants.FOLA_MailingListColumnName_Contact_Zip_2].ToString()
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
            if ((dataRow[Constants.FOLA_MailingListColumnName_Contact_CS] != null) &&
                !string.IsNullOrEmpty(dataRow[Constants.FOLA_MailingListColumnName_Contact_CS].ToString()))
            {
                string contact_cs = dataRow[Constants.FOLA_MailingListColumnName_Contact_CS].ToString();
                string state = contact_cs.Substring(contact_cs.Length - 2);
                row.Add(state);
            }
            else
            {
                row.Add(string.Empty);
            }

            //Docket
            if ((dataRow[Constants.FOLA_MailingListColumnName_Work_Set_Short_Label] != null) &&
                !string.IsNullOrEmpty(
                    dataRow[Constants.FOLA_MailingListColumnName_Work_Set_Short_Label].ToString()))
            {
                row.Add(dataRow[Constants.FOLA_MailingListColumnName_Work_Set_Short_Label].ToString());
            }
            else
            {
                row.Add(string.Empty);
            }

            return row;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="docketNumber">root dockets seperated by comma - ex: P-1234,PQ-789</param>
        /// <param name="listItemID"></param>
        public int GenerateFOLAMailingExcelFile(ClientContext clientContext, string docketNumber, string listItemID)
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
                    if (docket.IndexOf("-") > 0)
                    {
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
                }

                if (!String.IsNullOrEmpty(rootDocketNumbers))
                {
                    var folaMailingList = GetFOLAMailingList(rootDocketNumbers,ref numberOfAddress);
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
                        }
                    }
                }

            }

            return numberOfAddress;
        }

        public bool getIncludeSenatorsParameter(string shortLabel)
        {
            /*
            If "RM" and "P" docket --> True
            else If "EL"
	            if same fiscal year --> True ("but a docket: ER04-7 EL09-3 would get a ‘True’ value")
	            else (if different fiscal year)
		            if there is another docket --> true ("A lead EL docket stated with a different fiscal 							year would get a ‘True’ value also")

            other than above, false
             * */
            //this code dertermine if includesenator is true, default is false
            bool includeSenator = false;
            const string Ppattern_start = "^P[0-9]";
            const string Ppattern_middle = ",P[0-9]";

            const string RMpattern = "RM[0-9]";
            const string ELpattern = "EL[0-9]";
            



            //if ((shortLabel.StartsWith("RM", StringComparison.OrdinalIgnoreCase)) ||
            //    (shortLabel.StartsWith("P", StringComparison.OrdinalIgnoreCase)))
            if (Regex.IsMatch(shortLabel, RMpattern, RegexOptions.IgnoreCase))
            {
                includeSenator = true;
            }
            else if ((Regex.IsMatch(shortLabel, Ppattern_start, RegexOptions.IgnoreCase)) || 
                (Regex.IsMatch(shortLabel, Ppattern_middle, RegexOptions.IgnoreCase)))
            {
                includeSenator = true;
            }
            else if (Regex.IsMatch(shortLabel, ELpattern, RegexOptions.IgnoreCase))
            {
                int fiscalYear = DateTime.Now.Year;
                if (DateTime.Now.Month >= 10)
                {
                    fiscalYear++;
                }

                //turn fiscal year to short: 2016 --> 16
                var strfiscalYear = fiscalYear.ToString().Substring(2, 2);
                var searchText = "EL" + strfiscalYear;

                if (shortLabel.IndexOf(searchText) > -1) //it means same fiscal year for EL
                {
                    includeSenator = true;
                }
                else
                {
                    //if there is another docket, check comma ','
                    if (shortLabel.IndexOf(",") > -1)
                    {
                        includeSenator = true;
                    }
                }
            }

            return includeSenator;
        }

        private List<String> getHeaderRow()
        {
            var result = new List<String>
            {
                "Contact Name",
                "FERC ID",
                "Contact Title",
                "Contact Organization",
                "PO Box",
                "Address Line 1",
                "Address Line 2",
                "City",
                "Zip",
                "Zip 2",
                "State",
                "Docket"
            };

            return result;
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

                UInt32 rowIdex = 0;
                Row row = null;

                // Add sheet data
                foreach (var rowData in data.DataRows)
                {
                    var cellIdex = 0;
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