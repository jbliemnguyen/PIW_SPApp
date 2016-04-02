using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using PIW_SPAppWeb.Helper;

namespace PIW_SPAppWeb.Services
{
    public delegate bool AsyncMethodCaller(int callDuration, SPRemoteEventProperties properties, string WebUrl, string ListTitle, string ListItemId, out int threadId);
    public class PIWDocumentsRER : IRemoteEventService
    {
        /// <summary>
        /// Handles events that occur before an action occurs, such as when a user adds or deletes a list item.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        /// <returns>Holds information returned from the remote event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            SPRemoteEventResult result = new SPRemoteEventResult();

            if (properties.EventType == SPRemoteEventType.ItemUpdating)
            {
                //if (ShouldEPSPassedBeUpdated(properties.ItemEventProperties.BeforeProperties,
                //    properties.ItemEventProperties.AfterProperties))
                //{
                using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
                {
                    if (clientContext != null)
                    {
                        if (properties.ItemEventProperties.ListTitle.Equals(
                                Constants.PIWDocuments_DocumentLibraryName))
                        {
                            //User update the document, set the flag for EPSPassed to Pending
                            result.ChangedItemProperties[Constants.PIWDocuments_colName_EPSPassed] =
                                Constants.PIWDocuments_EPSPassed_Option_Pending;
                            result.Status = SPRemoteEventServiceStatus.Continue;
                        }
                    }
                    //}
                }
            }

            return result;
        }

        /// <summary>
        /// Handles events that occur after an action occurs, such as after a user adds an item to a list or deletes an item from a list.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            if ((properties.EventType == SPRemoteEventType.ItemAdded) || (properties.EventType == SPRemoteEventType.ItemUpdated))
            {


                if (properties.ItemEventProperties.ListTitle.Equals(Constants.PIWDocuments_DocumentLibraryName))
                {
                    //if (ShouldEPSPassedBeUpdated(properties.ItemEventProperties.BeforeProperties,
                    //    properties.ItemEventProperties.AfterProperties))
                    //{
                    //    //call long method
                    //    //create delegate 
                    //    int threadId;
                    //    AsyncMethodCaller caller = new AsyncMethodCaller(ValidateDocument);
                    //    IAsyncResult result = caller.BeginInvoke(5000, properties, properties.ItemEventProperties.WebUrl, properties.ItemEventProperties.ListTitle, properties.ItemEventProperties.ListItemId.ToString(), out threadId, null, null);
                        
                    //}
                }
            }
        }

        public bool ValidateDocument(int duration, SPRemoteEventProperties properties, string WebUrl, string ListTitle, string ListItemId, out int threadId)
        {
            //Long time call
            Thread.Sleep(duration); //sleep for 10 second
            threadId = Thread.CurrentThread.ManagedThreadId;

            using (ClientContext clientContext =
                TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            //using (ClientContext clientContext = new ClientContext(WebUrl))
            {
                if (clientContext != null)
                {
                    List oList =
                        clientContext.Web.Lists.GetByTitle(ListTitle);

                    SharePointHelper helper = new SharePointHelper();
                    var internalColumnName = helper.getInternalColumnNames(clientContext,
                        ListTitle);

                    //Update epspassed status to true
                    ListItem listItem =
                        oList.GetItemById(ListItemId);
                    clientContext.Load(listItem);


                    listItem[internalColumnName[Constants.PIWDocuments_colName_EPSPassed]] =
                        Constants.PIWDocuments_EPSPassed_Option_True;
                    listItem.Update();
                    clientContext.ExecuteQuery();
                }
            }

            return true;
        }

        /// <summary>
        /// Check if any property changed, if it changed, go with the update becuaes the changes is comming from user
        /// If beforeproperties and afterProperties are the same, it mean the change is comming from item.update, don't do another update
        /// http://tech.bool.se/how-to-stop-the-itemupdated-event-from-refiring-itself-in-an-remote-event-receiver/
        /// </summary>
        /// <param name="beforeProperties"></param>
        /// <param name="afterProperties"></param>
        /// <returns></returns>
        private static bool ShouldEPSPassedBeUpdated(
            IReadOnlyDictionary<string, object> beforeProperties,
            IReadOnlyDictionary<string, object> afterProperties)
        {
            // If the property doesn't exist, then the field should be updated
            if (!beforeProperties.ContainsKey(Constants.PIWDocuments_colName_EPSPassed) || !afterProperties.ContainsKey(Constants.PIWDocuments_colName_EPSPassed))
            {
                return true;
            }
            //// If the value of differ, then field should be updated
            return (!afterProperties[Constants.PIWDocuments_colName_EPSPassed].ToString().Equals(beforeProperties[Constants.PIWDocuments_colName_EPSPassed].ToString()));
        }
    }

}
