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
            return result;
        }

        /// <summary>
        /// Handles events that occur after an action occurs, such as after a user adds an item to a list or deletes an item from a list.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            if (properties.EventType == SPRemoteEventType.ItemAdded)
            {
                if (properties.ItemEventProperties.ListTitle.Equals(Constants.PIWDocuments_DocumentLibraryName))
                {
                    var ListTitle = properties.ItemEventProperties.ListTitle;
                    var ListItemId = properties.ItemEventProperties.ListItemId.ToString();
                    var valid = ValidateDocument(10000, properties, properties.ItemEventProperties.WebUrl,
                    properties.ItemEventProperties.ListTitle,
                    properties.ItemEventProperties.ListItemId.ToString());
                    if (valid)
                    {
                        using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
                        {
                            if (clientContext != null)
                            {
                                List oList =
                                    clientContext.Web.Lists.GetByTitle(ListTitle);

                                SharePointHelper helper = new SharePointHelper();
                                var internalColumnName = helper.getInternalColumnNamesFromCache(clientContext,
                                    ListTitle);

                                //Update epspassed status to true
                                ListItem listItem = oList.GetItemById(ListItemId);
                                clientContext.Load(listItem);

                                listItem[internalColumnName[Constants.PIWDocuments_colName_EPSPassed]] =
                                    Constants.PIWDocuments_EPSPassed_Option_True;
                                listItem.Update();
                                clientContext.ExecuteQuery();
                            }
                        }
                    }

                }
            }
        }

        public bool ValidateDocument(int duration, SPRemoteEventProperties properties, string WebUrl, string ListTitle, string ListItemId)
        {
            //Long time call
            Thread.Sleep(duration); //sleep for duration millisecond
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

            if (afterProperties[Constants.PIWDocuments_colName_EPSPassed].ToString().Equals(beforeProperties[Constants.PIWDocuments_colName_EPSPassed].ToString()))
            {
                //If beforeproperties and afterProperties are the same, it mean the change is comming from item.update, don't do another update
                return false;
            }
            else
            {
                return true;
            }
        }
    }

}
