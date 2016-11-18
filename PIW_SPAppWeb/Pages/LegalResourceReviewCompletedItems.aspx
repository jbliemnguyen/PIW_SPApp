<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="LegalResourceReviewCompletedItems.aspx.cs" Inherits="PIW_SPAppWeb.Pages.LegalResourceReviewCompletedItems" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#LegalResourceReviewCompletedItemsURL").addClass("active");

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
                <span style="font-size: large">Legal Resources and Review Group Completed</span>
            </div>
            <div class="col-xs-3">
                <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
            </div>
        </div>
        <fieldset class="fieldsetreport-border">
            <legend class="legendreport-border">Report Filter</legend>
            <div class="col-xs-4">
                <div class="form-group">
                    <asp:Label ID="Label1" runat="server" Text="From Completion Date" AssociatedControlID="tbFromDate" CssClass="col-xs-6 control-label" ClientIDMode="Static"></asp:Label>
                    <div class="col-xs-6">
                        <asp:TextBox ID="tbFromDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                    </div>
                </div>

                <div class="form-group">
                    <asp:Label ID="Label2" runat="server" Text="To Completion Date" AssociatedControlID="tbToDate" CssClass="col-xs-6 control-label" ClientIDMode="Static"></asp:Label>
                    <div class="col-xs-6">
                        <asp:TextBox ID="tbToDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                    </div>
                </div>
            </div>
            <div class="col-xs-4">
                <asp:Button runat="server" ID="btnRunReport" Text="Run Report" CssClass="btn-sm btn-primary active" OnClick="btnRunReport_OnClick" ClientIDMode="Static" />
            </div>
        </fieldset>

        <div class="form-group">
            <%--<div id="gridDiv" class="col-xs-12" style="overflow-x: scroll; overflow-y: hidden">--%>
            <div id="gridDiv" class="col-xs-12" style="overflow-x: hidden; overflow-y: hidden">
                <asp:GridView runat="server" ID="gridView" AutoGenerateColumns="false" CssClass="table table-hover table-condensed piw-borderless"
                    ClientIDMode="Static" OnRowCreated="gridView_OnRowCreated">
                    <PagerStyle CssClass="pagination-piw" />
                </asp:GridView>
            </div>
        </div>
        <div id="skm_LockBackground" class="LockOff"></div>
        <div id="skm_LockPane" class="LockOff">
            <div id="skm_LockPaneText">&nbsp;</div>
        </div>
    </form>
</asp:Content>
