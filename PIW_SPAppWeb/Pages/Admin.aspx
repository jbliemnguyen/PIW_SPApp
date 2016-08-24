<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="PIW_SPAppWeb.Admin" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#AdminURL").addClass("active");

            $("#hypedit").click(function (event) {
                event.preventDefault();
                editDocumentWithProgID2("https://fdc1s-sp23wfed2.ferc.gov/piw/PIWDocuments/4/ER15-1451-002.docx", "",
                    "SharePoint.OpenDocuments", "0", "https://fdc1s-sp23wfed2.ferc.gov/piw", "0");
            });
        }
    </script>
    <form runat="server">
        <asp:TextBox runat="server" ID="txtTitle"></asp:TextBox>

        <br />
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Add Title to Announcement" />
        <asp:Button ID="btnRegisterRER" runat="server" OnClick="btnRegisterRER_Click" Text="Register Remote Event Receiver for PIW Documents" BorderStyle="Ridge" />

        <br />
        <asp:Button ID="btnRemoveRER" runat="server" OnClick="btnRemoveRER_Click" Text="Remove Remote Event Receiver for PIW Documents" BorderStyle="Ridge" />

        <br />
        <asp:Button ID="EPSValidation" runat="server" Text="Test Doc Validation" OnClick="EPSValidation_Click" />
        </br>
        <asp:Button ID="btnTestCitationAppended" runat="server" Text="Test Citation Number Append" OnClick="btnTestCitationAppended_Click" />
        <br />
        <br />
        <br />
        Docket (short):
        <asp:TextBox ID="tbDocket" runat="server">QF90-203,P-14425</asp:TextBox>
        <asp:Button ID="btnTestExcelGeneration" runat="server" Text="Test Excel Generation" OnClick="btnTestExcelGeneration_Click" />
        <br />
        <br />
        <asp:Button ID="btnTestGetNumberOfPages" runat="server" Text="Test get number of pages" OnClick="btnTestGetNumberOfPages_Click" />
        &nbsp;&nbsp;
        <asp:Label ID="lbNumberOfPages" runat="server"></asp:Label>
        <br />
        <asp:Button runat="server" ID="btnTestPermissionSetting" Text="Permission Setting" OnClick="btnTestPermissionSetting_Click" />
        <br />
        <asp:Button runat="server" ID="btnEmail" Text="Test Email" OnClick="btnEmail_Click" />
        <br />
        <asp:Button runat="server" ID="btnTestDvvo" Text="Test DVVO" />

        <%--<a href="" id="hypedit">This will open the file in edit mode</a>--%>
        <a href="https://fdc1s-sp23wfed2.ferc.gov/piw/PIWDocuments/4/ER15-1451-002.docx?web=0" id="hypedit1">Read Only</a>
        <a href="https://fdc1s-sp23wfed2.ferc.gov/piw/PIWDocuments/4/ER15-1451-002.docx?web=1" id="hypedit2">Edit mode</a>
    </form>
</asp:Content>
