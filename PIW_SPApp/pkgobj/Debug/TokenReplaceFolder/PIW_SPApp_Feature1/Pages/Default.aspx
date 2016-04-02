<%@ Page Language="C#" MasterPageFile="~masterurl/default.master" Inherits="Microsoft.SharePoint.WebPartPages.WebPartPage, Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<%@ Register TagPrefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<asp:Content ContentPlaceHolderID="PlaceHolderAdditionalPageHead" runat="server">
    <SharePoint:ScriptLink Name="sp.js" runat="server" OnDemand="true" LoadAfterUI="true" Localizable="false" />
</asp:Content>

<asp:Content ContentPlaceHolderID="PlaceHolderMain" runat="server">
    <WebPartPages:WebPartZone runat="server" FrameType="TitleBarOnly" ID="full" Title="loc:full" />
    <script src="utils.js" type="text/javascript"></script>

    <h1>People Picker Samples</h1>
    <h2>Office Web Widgets –Experimental</h2>
    <p>The following code samples show you how to use the People Picker control:</p>
    <ul>
        <li><a id="mus" href="#">MarkupSimple</a> - Learn how to declare the People Picker control in HTML markup.</li>
    </ul>
    <input id="btnCreate" type="button" value="Create new Item"/> 
    <script
        src="//ajax.aspnetcdn.com/ajax/4.0/1/MicrosoftAjax.js"
        type="text/javascript">
    </script>
    <script
        type="text/javascript"
        src="//ajax.aspnetcdn.com/ajax/jQuery/jquery-1.7.2.min.js">
    </script>
    <script type="text/javascript">
        //Build the URLs for the links above
        var remoteAppUrl;
        var hostWebURL;
        var link;
        var qs;
        $(document).ready(function () {
            qs = "?" + document.URL.split("?")[1];
            remoteAppUrl =
                    decodeURIComponent(
                        getQueryStringParameter("RemoteAppUrl")
                    );

            hostWebURL =
                    decodeURIComponent(
                        getQueryStringParameter("SPHostUrl")
                    );

            link = document.getElementById("mus");
            link.href = remoteAppUrl + "/pages/peoplepicker.aspx" + qs;

            //register click
            $("#btnCreate").click(function () {
                CreateNewItem();
            });

        });

        function getQueryStringParameter(paramToRetrieve) {
            var params =
                document.URL.split("?")[1].split("&");
            var strParams = "";
            for (var i = 0; i < params.length; i = i + 1) {
                var singleParam = params[i].split("=");
                if (singleParam[0] == paramToRetrieve)
                    return singleParam[1];
            }
        }

        function CreateNewItem() {
            var scriptbase = hostWebURL + "/_layouts/15/";

            // Load the js files and continue to
            // the execOperation function.
            $.getScript(scriptbase + "SP.Runtime.js",
                function () {
                    $.getScript(scriptbase + "SP.js", execOperation);
                }
            );
        }

        // Function to execute basic operations.
        function execOperation() {

            // Continue your program flow here.
            retrieveWebSite();

        }


        function retrieveWebSite() {
            var currentcontext = new SP.ClientContext.get_current();
            var hostcontext = new SP.AppContextSite(currentcontext, hostWebURL);


            //var clientContext = new SP.ClientContext(hostWebURL);
            var oList = hostcontext.get_web().get_lists().getByTitle('PIWList');

            var itemCreateInfo = new SP.ListItemCreationInformation();
            this.oListItem = oList.addItem(itemCreateInfo);
            oListItem.set_item('Title', 'My New Item!');
            //oListItem.set_item('Body', 'Hello World!');
            oListItem.update();

            currentcontext.load(oListItem);
            currentcontext.executeQueryAsync(
                Function.createDelegate(this, this.onQuerySucceeded),
                Function.createDelegate(this, this.onQueryFailed)
            );
        }

        function onQuerySucceeded(sender, args) {
            //alert('Item created: ' + oListItem.get_id());
            var URL = hostWebURL + "/Lists/PIWList/EditForm.aspx?ID=" + oListItem.get_id();
            window.location.replace(URL);
        }

        function onQueryFailed(sender, args) {
            alert('Request failed. ' + args.get_message() +
                '\n' + args.get_stackTrace());
        }
    </script>
</asp:Content>
