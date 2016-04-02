using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Web;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;

namespace PIW_SPAppWeb.Services
{
    public class AppEventReceiver : IRemoteEventService
    {
        //private const string ReceiverName = "ItemAddedEvent";
        //private const string ListName = "PIW Documents";
        /// <summary>
        /// Handles app events that occur after the app is installed or upgraded, or when app is being uninstalled.
        /// </summary>
        /// <param name="properties">Holds information about the app event.</param>
        /// <returns>Holds information returned from the app event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {

            SPRemoteEventResult result = new SPRemoteEventResult();

            using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
            {
                if (clientContext != null)
                {
                    //clientContext.Load(clientContext.Web);
                    //clientContext.ExecuteQuery();
                    if (properties.EventType == SPRemoteEventType.AppInstalled)
                    {
                        InstalledApp(clientContext);
                    }
                    else if (properties.EventType == SPRemoteEventType.AppUninstalling)
                    {
                        
                    }

                }
            }

            //var result = new SPRemoteEventResult();
            //// Deal with application installed event.
            //switch (properties.EventType)
            //{
            //    case SPRemoteEventType.AppInstalled:
            //    case SPRemoteEventType.AppUninstalling:
            //        using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, false))
            //            //AppHelper.UnregisterRemoteEvents(clientContext, Constants.LISTNAME_PROPERTY,
            //            //    Constants.LISTEVTRCVR_NAME);
            //        break;
            //}
            return result;
        }

        private void InstalledApp(ClientContext clientContext)
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

                //Register remote event receiver for the PIW Documents
                if (
                        !IsRemoteEventRegistered(clientContext, EventReceiverType.ItemUpdated,
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
        }

        private void UnInstallingApp(ClientContext clientContext)
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
                // ReSharper disable once LoopCanBeConvertedToQuery
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
                //Debug.WriteLine(ex.ToString());
                //Logger.Logger.LogError(ex.ToString());
            }
            return false;
        }

        private void HandleItemAdded(SPRemoteEventProperties properties)
        {
            
        }

        //private void HandleAppUninstalling(SPRemoteEventProperties properties)
        //{
            
        //}

        //private void HandleAppInstalled(SPRemoteEventProperties properties)
        //{
            //using (ClientContext clientContext =
            // TokenHelper.CreateAppEventClientContext(properties, false))
            //{
            //    if (clientContext != null)
            //    {
            //        List myList = clientContext.Web.Lists.GetByTitle(ListName);
            //        clientContext.Load(myList, p => p.EventReceivers);
            //        clientContext.ExecuteQuery();

            //        bool rerExists = false;

            //        foreach (var rer in myList.EventReceivers)
            //        {
            //            if (rer.ReceiverName == ReceiverName)
            //            {
            //                rerExists = true;
            //                System.Diagnostics.Trace.WriteLine("Found existing ItemAdded receiver at "
            //                    + rer.ReceiverUrl);
            //            }
            //        }

            //        if (!rerExists)
            //        {
            //            EventReceiverDefinitionCreationInformation receiver =
            //                new EventReceiverDefinitionCreationInformation();
            //            receiver.EventType = EventReceiverType.ItemAdded;

            //            //Get WCF URL where this message was handled
            //            OperationContext op = OperationContext.Current;
            //            Message msg = op.RequestContext.RequestMessage;

            //            receiver.ReceiverUrl = msg.Headers.To.ToString();

            //            receiver.ReceiverName = ReceiverName;
            //            receiver.Synchronization = EventReceiverSynchronization.Synchronous;
            //            myList.EventReceivers.Add(receiver);

            //            clientContext.ExecuteQuery();

            //            System.Diagnostics.Trace.WriteLine("Added ItemAdded receiver at "
            //                    + msg.Headers.To.ToString());
            //        }
            //    }
            //}
        //}

        /// <summary>
        /// This method is a required placeholder, but is not used by app events.
        /// </summary>
        /// <param name="properties">Unused.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {

        }

    }
}
