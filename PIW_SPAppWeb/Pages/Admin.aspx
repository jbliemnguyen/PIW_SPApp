﻿<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="PIW_SPAppWeb.Admin" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#AdminURL").addClass("active");
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
        <br/>
        <br />
        <br />
        Docket (short):
        <asp:TextBox ID="tbDocket" runat="server">QF90-203,P-14425</asp:TextBox>
        <asp:Button ID="btnTestExcelGeneration" runat="server" Text="Test Excel Generation" OnClick="btnTestExcelGeneration_Click" />
        <br />
        <br />
        <asp:Button ID="btnTestGetNumberOfPages" runat="server" Text="Test get number of pages" OnClick="btnTestGetNumberOfPages_Click"/>
    &nbsp;&nbsp;
        <asp:Label ID="lbNumberOfPages" runat="server"></asp:Label>
        <br/>
        <asp:Button runat="server" ID="btnTestPermissionSetting" Text="Permission Setting" OnClick="btnTestPermissionSetting_Click"/>
        <br/>
        <asp:Button runat="server" ID="btnEmail" Text="Test Email" OnClick="btnEmail_Click" />
    </form>
</asp:Content>
