<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="MailingRequired.aspx.cs" Inherits="PIW_SPAppWeb.Pages.MailingRequired" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">

    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#OEPMailingRequiredURL").addClass("active");

            //date picker
            $("#tbFromDate").datepicker();
            //prevent user edit manually
            $("#tbFromDate").keydown(function (event) { event.preventDefault(); });

            $("#tbToDate").datepicker();
            //prevent user edit manually
            $("#tbToDate").keydown(function (event) { event.preventDefault(); });
            
        }
    </script>
    <form id="mainForm" runat="server" class="form-horizontal">
        <div class="form-group">
            <div class="col-xs-8">
                <span style="font-size: large">OEP Mailing Required</span>
            </div>
            <div class="col-xs-3">
                <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
            </div>
        </div>


        <div class="col-xs-4">
            <%--<div class="form-group">
                <asp:Label ID="lbProgramOfficeWorkflowInitiator" runat="server" Text="Program Office (Workflow Initiator)" AssociatedControlID="ddProgramOfficeWorkflowInitiator" CssClass="col-xs-6 control-label"></asp:Label>
                <div class="col-xs-6">
                    <asp:DropDownList ID="ddProgramOfficeWorkflowInitiator" CssClass="form-control" runat="server" ClientIDMode="Static">
                        <asp:ListItem Selected="True">-- All Offices --</asp:ListItem>
                        <asp:ListItem>OAL</asp:ListItem>
                        <asp:ListItem>OALJ</asp:ListItem>
                        <asp:ListItem>OE</asp:ListItem>
                        <asp:ListItem>OEA</asp:ListItem>
                        <asp:ListItem>OED</asp:ListItem>
                        <asp:ListItem>OEMR</asp:ListItem>
                        <asp:ListItem>OEP</asp:ListItem>
                        <asp:ListItem>OEPI</asp:ListItem>
                        <asp:ListItem>OER</asp:ListItem>
                        <asp:ListItem>OGC</asp:ListItem>
                        <asp:ListItem>OSEC</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>--%>

            <div class="form-group">
                <asp:Label ID="Label1" runat="server" Text="From Publication Date" AssociatedControlID="tbFromDate" CssClass="col-xs-6 control-label" ClientIDMode="Static"></asp:Label>
                <div class="col-xs-6">
                    <asp:TextBox ID="tbFromDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                </div>
            </div>

            <div class="form-group">
                <asp:Label ID="Label2" runat="server" Text="To Publication Date" AssociatedControlID="tbToDate" CssClass="col-xs-6 control-label" ClientIDMode="Static"></asp:Label>
                <div class="col-xs-6">
                    <asp:TextBox ID="tbToDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <div class="col-xs-6"></div>
                <div class="col-xs-6">
                    <asp:Button runat="server" ID="btnRun" Text="Run Report" CssClass="btn-sm btn-primary active" OnClick="btnRun_OnClick" />
                </div>
            </div>

        </div>

        <div class="form-group">
            <div id="gridDiv" class="col-xs-12" style="overflow-x: hidden; overflow-y: hidden">
                <asp:GridView runat="server" ID="gridView" AutoGenerateColumns="false" CssClass="table table-hover table-condensed piw-borderless"
                    OnPageIndexChanging="gridView_OnPageIndexChanging" ClientIDMode="Static">
                    <PagerStyle CssClass="pagination-piw" />
                </asp:GridView>
            </div>
        </div>

    </form>
</asp:Content>
