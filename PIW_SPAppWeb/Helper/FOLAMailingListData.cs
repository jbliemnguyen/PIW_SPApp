﻿using System.Collections.Generic;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PIW_SPAppWeb.Helper
{
    public class FOLAMailingListData
    {
        public List<string> Headers { get; set; }
        public List<List<string>> DataRows { get; set; }

        public FOLAMailingListData(string[] docket)
        {
            Headers = new List<string>();
            DataRows = getFOLAMailingList(docket);
        }

        public FOLAMailingListData()
        {
            Headers = new List<string>();
            DataRows = new List<List<string>>();
        }

        public List<List<string>> getFOLAMailingList(string[] docket)
        {
            return null;
        }

    }
}