using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;

namespace PIW_SPAppWeb.Helper
{
    public class CitationNumber
    {
        //Formula
        //Citation_Number = 120 + Current_Quarter + (Current_Years - 2007)*4 - 3                

        #region variable and property
        private int _documentCategoryNumber;
        private int _quarterNumber;
        private int _sequenceNumber;
        private SharePointHelper helper = new SharePointHelper();
        public int DocumentCategoryNumber
        {
            get
            {
                return _documentCategoryNumber;
            }
        }

        public int QuarterNumber
        {
            get
            {
                return _quarterNumber;
            }
        }

        public int SequenceNumber
        {
            get
            {
                return _sequenceNumber;
            }
        }
        #endregion

        public CitationNumber(int documentCategoryNumber, DateTime date)
        {
            _documentCategoryNumber = documentCategoryNumber;
            _quarterNumber = getQuarterNumber(date);
            _sequenceNumber = 0;
        }

        public CitationNumber(int quarterNumber, int documentCategoryNumber, int sequenceNumber)
        {
            _quarterNumber = quarterNumber;
            _documentCategoryNumber = documentCategoryNumber;
            _sequenceNumber = sequenceNumber;
        }

        /// <summary>
        /// This method must be called to set _sequenceNumber value
        /// </summary>
        public void GetNextCitationNumber(ClientContext clientContext)
        {
            var listItemCol = getListItemByQuarterNumberAndDocumentCategory(clientContext,_quarterNumber,_documentCategoryNumber);
            var CitationNumberInternalColumnNames = helper.getInternalColumnNames(clientContext,Constants.CitationNumberListName);
            if (listItemCol.Count == 0)
            {
                _sequenceNumber = 1;
            }
            else
            {
                var listItem = listItemCol[0];
                if (listItem[CitationNumberInternalColumnNames[Constants.CitationNumberList_colName_SequenceNumber]] != null)
                {
                    int currentSequence = int.Parse(listItem[CitationNumberInternalColumnNames[Constants.CitationNumberList_colName_SequenceNumber]].ToString());
                    _sequenceNumber = ++currentSequence;
                }
            }
        }

        /// <summary>
        /// get all existing citation number not assigned (because of delete or reject action)
        /// get all citation has been skipped for example: 1,2,3,8 ---> return 4,5,6,7
        /// get highest citation (recommended)
        /// </summary>
        /// <returns></returns>
        public List<string> getAllAvailableCitationNumber()
        {
            List<string> result = new List<string>();
            SPListItemCollection listItemCol = getListItemByQuarterNumberAndDocumentCategory();

            if (listItemCol.Count > 0)
            {
                //Top sequence number (not in list)
                var listItem = listItemCol[0];
                if (listItem[SPListSetting.col_CitationNumberList_SequenceNumber] != null)
                {
                    int currentSequence = int.Parse(listItem[SPListSetting.col_CitationNumberList_SequenceNumber].ToString());
                    _sequenceNumber = ++currentSequence;
                    result.Add(this.ToString());
                }

                //scan through all citation number, add deleted and skip number 
                for (int i = 0; i < listItemCol.Count; i++)
                {
                    var item = listItemCol[i];
                    //Add all existing but not assiged citation number (deleted)
                    if (item[SPListSetting.col_CitationNumberList_PIWList] == null)
                    {
                        _sequenceNumber = int.Parse(item[SPListSetting.col_CitationNumberList_SequenceNumber].ToString());
                        result.Add(this.ToString());
                    }


                    int currentSequenceNumber = int.Parse(listItemCol[i][SPListSetting.col_CitationNumberList_SequenceNumber].ToString());
                    int nextSequenceNumber = -1;
                    if (i < listItemCol.Count - 1)//check all numbers prior current citation #
                    {
                        nextSequenceNumber = int.Parse(listItemCol[i + 1][SPListSetting.col_CitationNumberList_SequenceNumber].ToString());
                    }
                    else//last number in collection
                    {
                        nextSequenceNumber = 0;//to make sure we have 1 if it is not in list
                    }

                    if (currentSequenceNumber > (nextSequenceNumber + 1))
                    {
                        //Get all cit numbers in the "gap"
                        //for : 6,3,2,1 --> add 4,5,0 in the available cit # list
                        for (int j = currentSequenceNumber - 1; j > nextSequenceNumber; j--)
                        {
                            _sequenceNumber = j;
                            result.Add(this.ToString());
                        }
                    }
                    //Add all skip citation
                }
            }

            return result;


        }

        public bool Save(SPListItem piwListItem, string FullCitationNumber, ref string returnedError, bool isOverride)
        {
            if (ValidateFormatCitationNumber(FullCitationNumber))
            {
                int previousQuarterNumber = _quarterNumber;//current quarterNumber number (today)
                int previousDocumentTypeNumber = _documentCategoryNumber;//current document type saved in piw list item
                ParseCitationNumber(FullCitationNumber);//update quarterNumber,documenttype and sequence number with new (user input) full citation number

                if (!isOverride)
                {
                    //Not check Document Type and Quarter if override is selected
                    if (!(previousQuarterNumber.Equals(_quarterNumber) && previousDocumentTypeNumber.Equals(_documentCategoryNumber)))
                    {
                        returnedError = String.Format("Invalid Document Type and/or Quarter Number");
                        return false;
                    }
                }

                SPListItemCollection citationNumberListItemCollection = getCitationNumberListItemByQuarterNumberAndDocumentTypeAndSequenceNumber();
                if (citationNumberListItemCollection.Count > 0)//citation number has been created
                {
                    //and assigned to a Piw list item
                    //and the assigned PIW list item is different than current piw list item
                    //----> citation number has been taken
                    //(we assume only 1 citation number exist in the system (no duplication) )
                    var citationNumberListItem = citationNumberListItemCollection[0];
                    if ((citationNumberListItem[SPListSetting.col_CitationNumberList_PIWList] != null) &&
                        (!string.IsNullOrEmpty(citationNumberListItem[SPListSetting.col_CitationNumberList_PIWList].ToString())) &&
                        (!citationNumberListItem[SPListSetting.col_CitationNumberList_PIWList].ToString().Equals(piwListItem["ID"].ToString())))
                    {
                        returnedError = "Citation Number has been taken.";
                        return false;
                    }
                    else//assign the (exist) citation number to current PIW List Item
                    {
                        AssignExistCitationNumberToListItem(piwListItem, citationNumberListItem);
                        return true;
                    }
                }

                //This is brand new citation number
                //we dont have to check if citation number exist before inserting
                //if it exist, we never come here (above scenario)
                InsertCitationNumberListItem(piwListItem);//create new
                return true;
            }
            else
            {
                returnedError = "Invalid Number Format and/or Document Type Number";
                return false;
            }

            //Note: We don't have to check if the specific PIWList item has been assigned
            //a citation number before. Becuase piwlist item only has one opportunity 
            //to get citation number, and the citation number is validated against same
            //document type and quarterNumber, also its format get checked            
        }

        /// <summary>
        /// Get citation number in string format for display
        /// return format: 134 FERC ¶ 61,005
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(this._quarterNumber, this._documentCategoryNumber, this._sequenceNumber);
        }

        public static string ToString(int quarter, int documentType, int sequence)
        {
            string sequenceNumber = string.Empty;
            if (sequence > 99)//more than 3 digit
            {
                sequenceNumber = sequence.ToString();
            }
            else if (sequence > 9)//2 digit
            {
                sequenceNumber = string.Format("0{0}", sequence.ToString());
            }
            else//1 digit
            {
                sequenceNumber = string.Format("00{0}", sequence.ToString());
            }

            return string.Format("{0} FERC ¶ {1},{2}", quarter, documentType, sequenceNumber);
        }


        private int getCurrentQuarter(DateTime now)
        {
            return ((now.Month - 1) / 3) + 1;
        }

        private int getQuarterNumber(DateTime now)
        {
            return (120 + getCurrentQuarter(now) + (now.Year - 2007) * 4 - 3);
        }

        /// <summary>
        /// parse and update all components of citation number
        /// </summary>
        /// <param name="FullCitationNumber">input: 134 FERC ¶ 61,005</param>
        /// output: _sequenceNumber = 5, _quarterNumber = 134, _documentCategoryNumber = 61
        private void ParseCitationNumber(string FullCitationNumber)
        {
            string searchKey;
            //parse quarterNumber
            searchKey = " ";
            string quarter = FullCitationNumber.Substring(0, FullCitationNumber.IndexOf(searchKey) + 1);
            _quarterNumber = int.Parse(quarter);

            //parse document type
            searchKey = "FERC ¶ ";
            int startIndex = FullCitationNumber.IndexOf(searchKey) + searchKey.Length;
            string documentNumber = FullCitationNumber.Substring(startIndex, 2);
            _documentCategoryNumber = int.Parse(documentNumber);

            //parse sequence number
            searchKey = ",";
            string sequenceNumber = FullCitationNumber.Substring(FullCitationNumber.IndexOf(searchKey) + 1);
            char[] array = sequenceNumber.ToCharArray();

            //remove prefix '0'
            //set sequence number
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != '0')
                {
                    string truncatedSequenceNumber = sequenceNumber.Substring(i);
                    _sequenceNumber = int.Parse(truncatedSequenceNumber);
                    break;
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullCitationNumber">134 FERC ¶ 61,005</param>
        /// <returns></returns>
        private bool ValidateFormatCitationNumber(string fullCitationNumber)
        {
            string pattern = @"^[1-9]\d+ FERC ¶ (61|62|63),\d+$";
            return System.Text.RegularExpressions.Regex.IsMatch(fullCitationNumber, pattern);
        }

        #region SP Connection

        public ListItemCollection getListItemByQuarterNumberAndDocumentCategory(ClientContext clientContext, int quarterNumber, int documentCategoryNumber)
        {
            List citationNumberList = clientContext.Web.Lists.GetByTitle(Constants.CitationNumberListName);
            var citationNumberInternalNameList = helper.getInternalColumnNames(clientContext, Constants.CitationNumberListName);
            CamlQuery query = new CamlQuery();
            var args = new string[]
            {
                citationNumberInternalNameList[Constants.CitationNumberList_colName_QuarterNumber],
                quarterNumber.ToString(),
                citationNumberInternalNameList[Constants.CitationNumberList_colName_DocumentCategoryNumber],
                documentCategoryNumber.ToString(),
                citationNumberInternalNameList[Constants.CitationNumberList_colName_SequenceNumber],

            };
            query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>
			                                            <And>
				                                            <Eq>
					                                            <FieldRef Name='{0}'/>
					                                            <Value Type='Number'>{1}</Value>
				                                            </Eq>
				                                            <Eq>
					                                            <FieldRef Name='{2}'/>
					                                            <Value Type='Number'>{3}</Value>
				                                            </Eq>
			                                            </And>
		                                            </Where>
		                                            <OrderBy>
			                                            <FieldRef Name='{4}' Ascending='False'/>
		                                            </OrderBy>
	                                            <Query>
                                            <View>", args);

            var citationListItems = citationNumberList.GetItems(query);

            clientContext.Load(citationListItems);
            clientContext.ExecuteQuery();

            return citationListItems;
            //query.ViewXml = string.Format("<Where><And><Eq><FieldRef Name='{0}'/><Value Type='Number'>{1}</Value></Eq><Eq><FieldRef Name='{2}'/><Value Type='Number'>{3}</Value></Eq></And></Where><OrderBy><FieldRef Name='{4}' Ascending='False'/></OrderBy>", SPListSetting.col_CitationNumberList_QuarterNumber, quarterNumber, SPListSetting.col_CitationNumberList_DocumentTypeNumber, documentCategoryNumber, SPListSetting.col_CitationNumberList_SequenceNumber);

        }

        public ListItemCollection getListItemByQuarterNumberAndDocumentCategoryAndSequenceNumber(ClientContext clientContext, int quarterNumber, int documentCategoryNumber, int sequenceNumber)
        {
            List citationNumberList = clientContext.Web.Lists.GetByTitle(Constants.CitationNumberListName);
            var citationNumberInternalNameList = helper.getInternalColumnNames(clientContext, Constants.CitationNumberListName);
            CamlQuery query = new CamlQuery();
            var args = new string[]
            {
                citationNumberInternalNameList[Constants.CitationNumberList_colName_QuarterNumber],
                quarterNumber.ToString(),
                citationNumberInternalNameList[Constants.CitationNumberList_colName_DocumentCategoryNumber],
                documentCategoryNumber.ToString(),
                citationNumberInternalNameList[Constants.CitationNumberList_colName_SequenceNumber],
                sequenceNumber.ToString()
            };
            query.ViewXml = string.Format(@"<View>
	                                            <Query>
		                                            <Where>
			                                            <And>
				                                            <And>
					                                            <Eq>
						                                            <FieldRef Name='{0}'/>
						                                            <Value Type='Number'>{1}</Value>
					                                            </Eq>
					                                            <Eq>
						                                            <FieldRef Name='{2}'/>
						                                            <Value Type='Number'>{3}</Value>
					                                            </Eq>
				                                            </And>
				                                            <Eq>
					                                            <FieldRef Name='{4}'/>
					                                            <Value Type='Number'>{5}</Value>
				                                            </Eq>
			                                            </And>
		                                            </Where>		
	                                            <Query>
                                            <View>", args);

            var citationListItems = citationNumberList.GetItems(query);

            clientContext.Load(citationListItems);
            clientContext.ExecuteQuery();

            return citationListItems;

        }

        private void AssignExistCitationNumberToListItem(ClientContext clientContext, string piwListItemID, ListItem citationNumberListItem)
        {
            var citationNumberInternalNameList = helper.getInternalColumnNames(clientContext, Constants.CitationNumberListName);

            //piwlist
            FieldLookupValue lv = new FieldLookupValue { LookupId = int.Parse(piwListItemID) };
            citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_PIWList]] = lv;

            citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_Status]] = Constants.CitationNumber_REASSIGNED_Status;

            citationNumberListItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_ReAssignedDate]] = DateTime.Now.ToString();

            citationNumberListItem.Update();
            clientContext.ExecuteQuery();
        }

        private void InsertCitationNumberListItem(ClientContext clientContext,string  piwListItemID)
        {
            List citationNumberList = clientContext.Web.Lists.GetByTitle(Constants.CitationNumberListName);
            var citationNumberInternalNameList = helper.getInternalColumnNames(clientContext, Constants.CitationNumberListName);

            ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
            
            ListItem newItem = citationNumberList.AddItem(itemCreateInfo);
            newItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_QuarterNumber]] = _quarterNumber;
            newItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_DocumentCategoryNumber]] = _documentCategoryNumber;
            newItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_SequenceNumber]] = _sequenceNumber;
            newItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_Title]] = this.ToString();
            newItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_Status]] = Constants.CitationNumber_ASSIGNED_Status;
            newItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_AssignedDate]] = DateTime.Now.ToString();
            newItem.Update();
            clientContext.ExecuteQuery();

            //set ref to piwlist
            FieldLookupValue lv = new FieldLookupValue { LookupId = int.Parse(piwListItemID) };
            newItem[citationNumberInternalNameList[Constants.CitationNumberList_colName_PIWList]] = lv;
            newItem.Update();
            clientContext.ExecuteQuery();

        }


        #endregion
    }
}