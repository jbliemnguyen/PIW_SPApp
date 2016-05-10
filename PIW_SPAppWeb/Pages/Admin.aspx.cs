using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Web.UI;
using Microsoft.SharePoint.Client;
using PIW_SPAppWeb.Helper;
using ListItem = System.Web.UI.WebControls.ListItem;


namespace PIW_SPAppWeb
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_PreInit(object sender, EventArgs e)
        {
            Uri redirectUrl;
            switch (SharePointContextProvider.CheckRedirectionStatus(Context, out redirectUrl))
            {
                case RedirectionStatus.Ok:
                    return;
                case RedirectionStatus.ShouldRedirect:
                    Response.Redirect(redirectUrl.AbsoluteUri, endResponse: true);
                    break;
                case RedirectionStatus.CanNotRedirect:
                    Response.Write("An error occurred while processing your request.");
                    Response.End();
                    break;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // The following code gets the client context and Title property by using TokenHelper.
            // To access other properties, the app may need to request permissions on the host web.
            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                //clientContext.Load(clientContext.Web, web => web.Title);
                //clientContext.ExecuteQuery();
                //Response.Write(clientContext.Web.Title);
                //Response.Write(DateTime.Now.ToShortTimeString());

            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                List oList = clientContext.Web.Lists.GetByTitle("Announcements");
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                Microsoft.SharePoint.Client.ListItem oListItem = oList.AddItem(itemCreateInfo);

                oListItem["Title"] = txtTitle.Text;
                oListItem["Body"] = "Hello World!";

                oListItem.Update();

                clientContext.ExecuteQuery();
            }
        }

        protected void btnRegisterRER_Click(object sender, EventArgs e)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                var listName = Constants.PIWDocuments_DocumentLibraryName;
                if (String.IsNullOrEmpty(listName)) return;
                List srcList = clientContext.Web.Lists.GetByTitle(listName);
                clientContext.Load(clientContext.Web);
                clientContext.ExecuteQuery();
                // Get the operation context and remote event service URL.
                string remoteUrl;
                if (null != OperationContext.Current)
                {
                    string url = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri;
                    string opContext = url.Substring(0, url.LastIndexOf("/", StringComparison.Ordinal));
                    remoteUrl = String.Format("{0}/{1}", opContext, Constants.PIWDocumentsRERServiceName);
                }
                else if (null != HttpContext.Current)
                {
                    string url = GetSiteRoot();
                    string opContext = url.Substring(0, url.LastIndexOf("/", StringComparison.Ordinal));
                    remoteUrl = String.Format("{0}/Services/{1}", opContext, Constants.PIWDocumentsRERServiceName);
                }
                else
                {
                    return;
                }

                //Register remote event receiver ItemUpdated for the PIW Documents
                if (!IsRemoteEventRegistered(clientContext, EventReceiverType.ItemUpdated,
                            Constants.PIWDocuments_DocumentLibraryName, Constants.LIBEVTRCVR_NAME))
                {
                    srcList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation
                    {
                        EventType = EventReceiverType.ItemUpdated,
                        ReceiverName = Constants.LIBEVTRCVR_NAME,
                        ReceiverUrl = remoteUrl,
                        SequenceNumber = 10
                    });
                    clientContext.ExecuteQuery();
                }

                //register remote event receiver ItemUpdating for the PIW Documents
                if (!IsRemoteEventRegistered(clientContext, EventReceiverType.ItemUpdating,
                            Constants.PIWDocuments_DocumentLibraryName, Constants.LIBEVTRCVR_NAME))
                {
                    srcList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation
                    {
                        EventType = EventReceiverType.ItemUpdating,
                        ReceiverName = Constants.LIBEVTRCVR_NAME,
                        ReceiverUrl = remoteUrl,
                        SequenceNumber = 10
                    });
                    clientContext.ExecuteQuery();
                }

                //register remote event receiver ItemAdded
                if (!IsRemoteEventRegistered(clientContext, EventReceiverType.ItemAdded,
                            Constants.PIWDocuments_DocumentLibraryName, Constants.LIBEVTRCVR_NAME))
                {
                    srcList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation
                    {
                        EventType = EventReceiverType.ItemAdded,
                        ReceiverName = Constants.LIBEVTRCVR_NAME,
                        ReceiverUrl = remoteUrl,
                        SequenceNumber = 10
                    });
                    clientContext.ExecuteQuery();
                }

            }
        }

        private static string GetSiteRoot()
        {
            if (HttpContext.Current == null) return null;
            HttpRequest request = HttpContext.Current.Request;
            string siteRoot = request.Url.AbsoluteUri
                .Replace(request.Url.AbsolutePath, String.Empty) // trim the current page off
                .Replace(request.Url.Query, string.Empty); // trim the query string off
            if (request.Url.Segments.Length == 4)
                // If hosted in a virtual directory, restore that segment
                siteRoot += "/" + request.Url.Segments[1];
            if (!siteRoot.EndsWith("/"))
                siteRoot += "/";
            return siteRoot;
        }

        public static bool IsRemoteEventRegistered(ClientContext clientContext, EventReceiverType type,
            string listName, string ListEvtRcvr)
        {
            bool result = false;
            if (null == clientContext) throw new ArgumentNullException("clientContext");
            try
            {
                // Get the list
                //Logger.Logger.LogInfo("Checking if remote events registered", () =>
                //var listName = Constants.PIWDocumentListName;

                List srcList = clientContext.Web.Lists.GetByTitle(listName);
                clientContext.Load(clientContext.Web);
                clientContext.ExecuteQuery();
                // Iterate all event receivers.
                clientContext.Load(srcList.EventReceivers);
                clientContext.ExecuteQuery();
                foreach (EventReceiverDefinition er in srcList.EventReceivers)
                    if (er.ReceiverName == ListEvtRcvr && er.EventType == type)
                    {
                        result = true;
                        break;
                    }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //Logger.Logger.LogError(ex.ToString());
            }
            return false;
        }

        protected void btnRemoveRER_Click(object sender, EventArgs e)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                UnregisterRemoteEvents(clientContext);
            }
        }

        public static void UnregisterRemoteEvents(ClientContext clientContext)
        {
            if (null == clientContext) throw new ArgumentNullException("clientContext");
            try
            {
                
                    // Get the list
                    var listName = Constants.PIWDocuments_DocumentLibraryName;
                    
                    List srcList = clientContext.Web.Lists.GetByTitle(listName);
                    clientContext.Load(clientContext.Web);
                    clientContext.ExecuteQuery();
                    // Remove all event receivers.
                    clientContext.Load(srcList.EventReceivers);
                    clientContext.ExecuteQuery();
                    var toDelete = new List<EventReceiverDefinition>();
                    
                    foreach (EventReceiverDefinition er in srcList.EventReceivers)
                        if (er.ReceiverName == Constants.LIBEVTRCVR_NAME)
                            toDelete.Add(er);
                    foreach (EventReceiverDefinition er in toDelete)
                    {
                        er.DeleteObject();
                        clientContext.ExecuteQuery();
                    }
                
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
                //Logger.Logger.LogError(ex.ToString());
            }
        }

        protected void Unnamed1_Click(object sender, EventArgs e)
        {
            //EPSPublicationHelper pubHelper = new EPSPublicationHelper();
            //pubHelper.ValidateDocument();
        }
    }
}