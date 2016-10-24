using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
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
    }
}