﻿<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="StandardForms.aspx.cs" Inherits="PIW_SPAppWeb.Pages.StandardForms" %>

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
    <form id="mainForm" runat="server" class="form-horizontal">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnableCdn="True"></asp:ScriptManager>
        <asp:UpdatePanel runat="server">
            <ContentTemplate>
                <asp:Timer ID="tmrRefresh" runat="server" Interval="30000" Enabled="true" OnTick="tmrRefresh_Tick">
                </asp:Timer>
                <div class="form-group">
                    <div class="col-md-8">
                        <span style="font-size:large">Standard Form Queue</span>
                    </div>
                    <div class="col-md-3">
                        <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
                    </div>
                </div>
                <asp:GridView runat="server" ID="standardFormsGridView" AutoGenerateColumns="false" CssClass="table-striped table-hover table-condensed piw-borderless" OnRowCreated="standardFormsGridView_RowCreated">
                </asp:GridView>
            </ContentTemplate>
            <Triggers>
                <asp:AsyncPostBackTrigger ControlID="tmrRefresh" EventName="Tick" />
            </Triggers>
        </asp:UpdatePanel>
    </form>
</asp:Content>
