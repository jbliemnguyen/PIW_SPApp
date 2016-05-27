<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="EditDirectPublicationForm.aspx.cs" Inherits="PIW_SPAppWeb.Pages.EditDirectPublicationForm" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {
            //set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#EditDirectPublicationFormURL").addClass("active");
        }
    </script>
    Direct pub
</asp:Content>
