using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.SharePoint.Client;

namespace PIW_SPAppJob
{
    class Program
    {
        private static string SharePointPrincipal = "00000003-0000-0ff1-ce00-000000000000";
        static void Main(string[] args)
        {
            //Get the realm for the URL
            Uri siteUri = new Uri(ConfigurationManager.AppSettings["SiteUrl"]);

            //Get the realm for the URL
            //string realm = TokenHelper.GetRealmFromTargetUrl(siteUri);


            //Test
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();

            //ClientContext clientContext = TokenHelper.GetS2SClientContextWithWindowsIdentity(siteUri, windowsIdentity);
            using (var clientContext = TokenHelper.GetS2SClientContextWithWindowsIdentity(siteUri, windowsIdentity))
            {
                List piwList = clientContext.Web.Lists.GetByTitle("Announcements");


                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = piwList.AddItem(itemCreateInfo);
                newItem["Title"] = DateTime.Now.ToLongTimeString();

                newItem.Update();

                clientContext.ExecuteQuery();

            }

            
        }
    }
}
