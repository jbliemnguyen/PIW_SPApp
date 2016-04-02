<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="EditAgendaForm.aspx.cs" Inherits="PIW_SPAppWeb.EditAgendaForm" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {
            //set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#EditAgendaFormURL").addClass("active");
        }
    </script>
    Edit Agenda Form
    

</asp:Content>
