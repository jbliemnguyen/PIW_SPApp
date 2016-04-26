<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="StandardForms.aspx.cs" Inherits="PIW_SPAppWeb.Pages.StandardForms" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#StandardFormsURL").addClass("active");
        }
    </script>
    Standard Form List
    <form runat="server">
        <asp:GridView runat="server" ID="standardFormsGridView" AutoGenerateColumns="false" RowStyle-BackColor="#DDDDDD">
        </asp:GridView>
    </form>
</asp:Content>
