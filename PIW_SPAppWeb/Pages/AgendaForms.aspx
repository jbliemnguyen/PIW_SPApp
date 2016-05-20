<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="AgendaForms.aspx.cs" Inherits="PIW_SPAppWeb.Pages.AgendaForms" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#AgendaFormsURL").addClass("active");
        }
    </script>
    Agenda Form List
    <form runat="server">
        <asp:GridView runat="server" ID="agendaFormsGridView" AutoGenerateColumns="false" CssClass="table-striped table-hover table-condensed piw-borderless" OnRowCreated="agendaFormsGridView_RowCreated">
        </asp:GridView>
    </form>
</asp:Content>
