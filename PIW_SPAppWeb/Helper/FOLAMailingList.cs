using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.ApplicationServices;

namespace PIW_SPAppWeb.Helper
{
    public class FOLAMailingList
    {
        public FOLAMailingListData GetFOLAMailingList(string WorksetShortLabel)
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

                    cmd.Parameters.Add("@Work_Set_Short_Label", SqlDbType.VarChar).Value = WorksetShortLabel;
                    cmd.Parameters.Add("@Include_Senators", SqlDbType.Bit).Value = 1;
                    cmd.Parameters.Add("@Include_eReg", SqlDbType.Bit).Value = 1;
                    cmd.Parameters.Add("@ReturnBlankAddress", SqlDbType.Bit).Value = 1;

                    con.Open();
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
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_FERC_id].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_FERC_id].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }
                        
                        //Contact Title
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Title] != null) &&
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_Title].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Title].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Contact Organization
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Organization] != null) &&
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_Organization].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Organization].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //PO Box value = "PO Box:" + value of Contact_PO_Box if it is not null
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Po_Box] != null) &&
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_Po_Box].ToString()))
                        {
                            row.Add("PO Box: " + reader[Constants.FOLA_MailingListColumnName_Contact_Po_Box].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Address Line 1
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line1] != null) &&
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line1].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line1].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Address Line 2
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line2] != null) &&
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line2].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_Address_Line2].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }

                        //Contact City
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_City] != null) &&
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_City].ToString()))
                        {
                            row.Add(reader[Constants.FOLA_MailingListColumnName_Contact_City].ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }
                        
                        //Zip and Zip 2
                        if ((reader[Constants.FOLA_MailingListColumnName_Contact_Zip_2] != null) &&
                            !string.IsNullOrEmpty(reader[Constants.FOLA_MailingListColumnName_Contact_Zip_2].ToString()))
                        {
                            var zips = reader[Constants.FOLA_MailingListColumnName_Contact_Zip_2].ToString().Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
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

        public void GetFOLAMailingList(string[] dockets)
        {
            
        }
    }
}