using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = System.Web.UI.WebControls.ListItem;
using ListItemCollection = Microsoft.SharePoint.Client.ListItemCollection;

namespace PIW_SPAppWeb.Pages
{
    public partial class ItemsByPublishedDate : System.Web.UI.Page
    {
        private SharePointHelper helper;
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                displayData();
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, string.Empty, Page.Request.Url.OriginalString);
                //helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
                if (exc is ServerUnauthorizedAccessException)
                {
                    helper.RedirectToAPage(Page.Request, Page.Response, Constants.Page_AccessDenied);
                }
                else
                {
                    helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
                }
            }
        }

        protected void tmrRefresh_Tick(object sender, EventArgs e)
        {
            try
            {
                displayData();
            }
            catch (Exception exc)
            {
                helper.LogError(Context, Request, exc, string.Empty, Page.Request.Url.OriginalString);
                helper.RedirectToAPage(Page.Request, Page.Response, "Error.aspx");
            }

        }

        private void displayData()
        {
        }

        protected void formTypeRadioButtonList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListItem allCheckBox = new ListItem() { Selected = true, Text = "All", Value = "All"};
            ListItem checkBox;
            allCheckBox.Attributes.Add("class", "jqueryselector_CategoryAllCheckBox");
            if (formTypeRadioButtonList.SelectedValue.Equals(Constants.PIWList_FormType_StandardForm))
            {
                cblDocumentCategory.Items.Clear();

                cblDocumentCategory.Items.Add(allCheckBox);

                //Delegated Letter                
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_DelegatedLetter, "jqueryselector_CategoryCheckBox"));

                //Delegated Notice
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_DelegatedNotice, "jqueryselector_CategoryCheckBox"));

                //Delegated Order
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_DelegatedOrder, "jqueryselector_CategoryCheckBox"));

                //Delegated Errata
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_DelegatedErrata, "jqueryselector_CategoryCheckBox"));

                //OALJ
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_OALJ, "jqueryselector_CategoryCheckBox"));

                //OALJ Errata
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_OALJErrata, "jqueryselector_CategoryCheckBox"));

                //Notice
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_Notice, "jqueryselector_CategoryCheckBox"));

                //Notice Errata
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_NoticeErrata, "jqueryselector_CategoryCheckBox"));
            }
            else if (formTypeRadioButtonList.SelectedValue.Equals(Constants.PIWList_FormType_AgendaForm))
            {
                cblDocumentCategory.Items.Clear();

                cblDocumentCategory.Items.Add(allCheckBox);

                //Notational Order
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_NotationalOrder, 
                    "jqueryselector_CategoryCheckBox"));

                //Notational Notice
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_NotationalNotice,
                    "jqueryselector_CategoryCheckBox"));

                //Commission Order
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_CommissionOrder,
                    "jqueryselector_CategoryCheckBox"));

                //Consent
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_Consent,
                    "jqueryselector_CategoryCheckBox"));

                //Errata
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_Errata,
                    "jqueryselector_CategoryCheckBox"));

                //Tolling Order
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_TollingOrder,
                    "jqueryselector_CategoryCheckBox"));

                //Sunshine Notice
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_SunshineNotice,
                    "jqueryselector_CategoryCheckBox"));

                //Notice of Action Taken
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_NoticeofActionTaken,
                    "jqueryselector_CategoryCheckBox"));

            }
            else if (formTypeRadioButtonList.SelectedValue.Equals(Constants.PIWList_FormType_DirectPublicationForm))
            {
                cblDocumentCategory.Items.Clear();

                cblDocumentCategory.Items.Add(allCheckBox);

                //Chairman Statement
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_ChairmanStatement,
                    "jqueryselector_CategoryCheckBox"));

                //Commissioner Statement
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_CommissionerStatement,
                    "jqueryselector_CategoryCheckBox"));

                //Delegated Letter - existing in Standard Form
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_DelegatedLetter,
                    "jqueryselector_CategoryCheckBox"));

                //EA
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_EA,
                    "jqueryselector_CategoryCheckBox"));

                //EIS
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_EIS,
                    "jqueryselector_CategoryCheckBox"));

                //Errata - existing in Agenda
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_Errata,
                    "jqueryselector_CategoryCheckBox"));

                //Inspection Report
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_InspectionReport,
                    "jqueryselector_CategoryCheckBox"));

                //Memo
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_Memo,
                    "jqueryselector_CategoryCheckBox"));

                //News Release
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_NewsRelease,
                    "jqueryselector_CategoryCheckBox"));

                //Notice of Action Taken
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_NoticeofActionTaken,
                    "jqueryselector_CategoryCheckBox"));

                //Project Update
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_ProjectUpdate,
                    "jqueryselector_CategoryCheckBox"));

                //Sunshine Act Meeting Notice
                cblDocumentCategory.Items.Add(getCheckBox(Constants.ddDocumentCategory_Option_SunshineActMeetingNotice,
                    "jqueryselector_CategoryCheckBox"));
            }
            else
            {
                cblDocumentCategory.Items.Clear();
            }
        }
        public ListItem getCheckBox(string value,string jqueryClass)
        {
            ListItem checkBox = new ListItem()
            {
                Text = value,
                Value = value,
            };
            checkBox.Attributes.Add("class", jqueryClass);

            return checkBox;
        }
    }

    
}