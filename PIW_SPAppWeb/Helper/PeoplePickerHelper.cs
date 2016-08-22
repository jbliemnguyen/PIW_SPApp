using Microsoft.SharePoint.ApplicationPages.ClientPickerQuery;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
namespace PIW_SPAppWeb
{
    public class PeoplePickerHelper
    {
        private static int GroupID = -1;

        public static string GetPeoplePickerSearchData()
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            using (var context = spContext.CreateUserClientContextForSPHost())
            {
                return GetPeoplePickerSearchData(context);
            }
        }

        public static string GetPeoplePickerSearchData(ClientContext context)
        {
            //get searchstring and other variables
            var searchString = (string)HttpContext.Current.Request["SearchString"];
            int principalType = Convert.ToInt32(HttpContext.Current.Request["PrincipalType"]);
            string spGroupName = (string)HttpContext.Current.Request["SPGroupName"];

            ClientPeoplePickerQueryParameters querryParams = new ClientPeoplePickerQueryParameters();
            querryParams.AllowMultipleEntities = false;
            querryParams.MaximumEntitySuggestions = 2000;
            querryParams.PrincipalSource = PrincipalSource.All;
            querryParams.PrincipalType = (PrincipalType)principalType;
            querryParams.QueryString = searchString;

            if (!string.IsNullOrEmpty(spGroupName))
            {
                if (PeoplePickerHelper.GroupID == -1)
                {
                    var group = context.Web.SiteGroups.GetByName(spGroupName);
                    if (group != null)
                    {
                        context.Load(group, p => p.Id);
                        context.ExecuteQuery();

                        PeoplePickerHelper.GroupID = group.Id;

                        querryParams.SharePointGroupID = group.Id;
                    }
                }
                else
                {
                    querryParams.SharePointGroupID = PeoplePickerHelper.GroupID;
                }
            }

            //execute query to Sharepoint
            ClientResult<string> clientResult = Microsoft.SharePoint.ApplicationPages.ClientPickerQuery.ClientPeoplePickerWebServiceInterface.ClientPeoplePickerSearchUser(context, querryParams);
            context.ExecuteQuery();
            return clientResult.Value;
        }

        public static void FillPeoplePickerValue(HiddenField peoplePickerHiddenField, Microsoft.SharePoint.Client.User user)
        {
            List<PeoplePickerUser> peoplePickerUsers = new List<PeoplePickerUser>(1);
            peoplePickerUsers.Add(new PeoplePickerUser() { Name = user.Title, Email = user.Email, Login = user.LoginName });
            peoplePickerHiddenField.Value = JsonHelper.Serialize<List<PeoplePickerUser>>(peoplePickerUsers);
        }

        public static void FillPeoplePickerValue(HiddenField peoplePickerHiddenField, Microsoft.SharePoint.Client.User[] users)
        {
            List<PeoplePickerUser> peoplePickerUsers = new List<PeoplePickerUser>();
            foreach (var user in users)
            {
                peoplePickerUsers.Add(new PeoplePickerUser() { Name = user.Title, Email = user.Email, Login = user.LoginName });
            }
            peoplePickerHiddenField.Value = JsonHelper.Serialize<List<PeoplePickerUser>>(peoplePickerUsers);
        }

        public static List<PeoplePickerUser> GetValuesFromPeoplePicker(HiddenField peoplePickerHiddenField)
        {
            return JsonHelper.Deserialize<List<PeoplePickerUser>>(peoplePickerHiddenField.Value);
        }

        /// <summary>
        /// Written by Liem Nguyen to extend the function of People Picker Helper
        /// Add users to existing People Picker which has values in it
        /// </summary>
        /// <param name="peoplePickerHiddenField"></param>
        /// <param name="users"></param>
        public static void AddPeoplePickerValues(HiddenField peoplePickerHiddenField,Microsoft.SharePoint.Client.User[] users)
        {
            List<PeoplePickerUser> existingPeoples = null;
            if (!string.IsNullOrEmpty(peoplePickerHiddenField.Value))
            {
                existingPeoples =
                    PeoplePickerHelper.GetValuesFromPeoplePicker(peoplePickerHiddenField);
            }
            else
            {
                existingPeoples = new List<PeoplePickerUser>();
            }

            foreach (var user in users)
            {
                if (existingPeoples.Any(g => g.Login == user.LoginName))
                {
                    //duplicate - dont add
                }
                else
                {
                    existingPeoples.Add(new PeoplePickerUser()
                    {
                        Name = user.Title,
                        Email = user.Email,
                        Login = user.LoginName
                    });
                }
            }
            peoplePickerHiddenField.Value = JsonHelper.Serialize<List<PeoplePickerUser>>(existingPeoples);
        }
    }
}