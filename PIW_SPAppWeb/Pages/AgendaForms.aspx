﻿<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="AgendaForms.aspx.cs" Inherits="PIW_SPAppWeb.Pages.AgendaForms" %>

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
    <form id="mainForm" runat="server" class="form-horizontal">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnableCdn="True"></asp:ScriptManager>
        <asp:UpdatePanel runat="server">
            <ContentTemplate>
                <asp:Timer ID="tmrRefresh" runat="server" Interval="300000" Enabled="false" OnTick="tmrRefresh_Tick">
                    <%--5 minutes delays--%>
                </asp:Timer>
                <div class="form-group">
                    <div class="col-md-8">
                        <span style="font-size: large">Agenda Forms</span>
                    </div>
                    <div class="col-md-3">
                        <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
                    </div>
                </div>
                <div class="form-group">
                    <div id="gridDiv" class="col-md-12" style="overflow-x: scroll;overflow-y: hidden">
                        <asp:GridView runat="server" ID="gridView" AutoGenerateColumns="false" CssClass="table-striped table-hover table-condensed piw-borderless" OnRowCreated="standardFormsGridView_RowCreated">
                        </asp:GridView>
                    </div>
                </div>
            </ContentTemplate>
            <Triggers>
                <asp:AsyncPostBackTrigger ControlID="tmrRefresh" EventName="Tick" />
            </Triggers>
        </asp:UpdatePanel>
    </form>
</asp:Content>
