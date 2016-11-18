<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="ItemsByPublishedDate.aspx.cs" Inherits="PIW_SPAppWeb.Pages.ItemsByPublishedDate" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">

    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#ItemsByPublishedDateURL").addClass("active");

            //date picker
            $("#tbFromDate").datepicker();
            //prevent user edit manually
            $("#tbFromDate").keydown(function (event) { event.preventDefault(); });

            $("#tbToDate").datepicker();
            //prevent user edit manually
            $("#tbToDate").keydown(function (event) { event.preventDefault(); });

            //event when click on any Document Category Checkbox --> Uncheck "All" checkbox
            $("span.jqueryselector_CategoryCheckBox input:checkbox").click(function (event) {
                if ($(this).prop("checked")) {
                    $("span.jqueryselector_CategoryAllCheckBox input:checkbox").prop("checked", false);
                }
            });

            //event when click on All checkbox --> uncheck all other checkboxes
            $("span.jqueryselector_CategoryAllCheckBox input:checkbox").click(function (event) {
                if ($(this).prop("checked")) {
                    $("span.jqueryselector_CategoryCheckBox input:checkbox").removeAttr('checked');

                }
            });
        }
    </script>
    <form id="mainForm" runat="server" class="form-horizontal">
        <div class="form-group">
            <div class="col-xs-8">
                <span style="font-size: large">Items By Published Date</span>
            </div>
            <div class="col-xs-3">
                <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
            </div>
        </div>

        <fieldset class="fieldsetreport-border">
            <legend class="legendreport-border">Report Filter</legend>
            <div class="col-xs-4">
                <div class="form-group">
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
                </div>

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

            </div>


            <div class="col-xs-3">
                <%--Form Type filter--%>
                <div class="form-group">
                    <asp:Label ID="Label4" runat="server" Text="Form Type" CssClass="col-xs-5 control-label"></asp:Label>
                    <div class="radio radiobuttonlist col-sm-7">
                        <asp:RadioButtonList ID="formTypeRadioButtonList" runat="server" RepeatDirection="Vertical" AutoPostBack="True" OnSelectedIndexChanged="formTypeRadioButtonList_SelectedIndexChanged">
                            <asp:ListItem Selected="True">All Forms</asp:ListItem>
                            <asp:ListItem>Standard Form</asp:ListItem>
                            <asp:ListItem>Agenda Form</asp:ListItem>
                            <asp:ListItem>Direct Publication Form</asp:ListItem>
                        </asp:RadioButtonList>
                    </div>
                </div>

                <div class="form-group">
                    <div class="col-xs-3"></div>
                    <div class="col-xs-9">
                        <asp:Button runat="server" ID="btnRunReport" Text="Run Report" CssClass="btn-sm btn-primary active" OnClick="btnRunReport_OnClick" ClientIDMode="Static" />
                    </div>
                </div>

            </div>

            <div class="col-xs-5">
                <div id="divDocumentCategory">
                    <asp:Label ID="Label3" runat="server" Text="Document Category" CssClass="col-xs-3 control-label"></asp:Label>
                    <div class="checkbox checkboxlist col-sm-9">
                        <asp:CheckBoxList ID="cblDocumentCategory" runat="server" RepeatDirection="Horizontal" RepeatColumns="2">
                        </asp:CheckBoxList>
                    </div>
                </div>
            </div>
        </fieldset>

        <div class="form-group">
            <%--<div id="gridDiv" class="col-xs-12" style="overflow-x: scroll; overflow-y: hidden">--%>
            <div id="gridDiv" class="col-xs-12" style="overflow-x: hidden; overflow-y: hidden">
                <asp:GridView runat="server" ID="gridView" AutoGenerateColumns="false" CssClass="table table-hover table-condensed piw-borderless"
                    OnPageIndexChanging="gridView_OnPageIndexChanging" ClientIDMode="Static">
                    <PagerStyle CssClass="pagination-piw" />
                </asp:GridView>
            </div>
        </div>

        <div class="form-group col-xs-3">
            <hr style="width: 100%; color: #204d74; height: 2px; background-color: #204d74;" class="col-xs-12"></hr>

            <asp:Label ID="lbSucessfull" runat="server" Text="Successful" CssClass="col-xs-6 control-label" AssociatedControlID="lbSucessfullValue"></asp:Label>
            <asp:Label ID="lbSucessfullValue" runat="server" CssClass="control-label col-xs-6"></asp:Label>

            <asp:Label ID="lbFail" runat="server" Text="Failed" CssClass="col-xs-6 control-label" AssociatedControlID="lbFailValue"></asp:Label>
            <asp:Label ID="lbFailValue" runat="server" CssClass="col-xs-6 control-label"></asp:Label>

            <asp:Label ID="lbPending" runat="server" Text="Pending" CssClass="col-xs-6 control-label" AssociatedControlID="lbPendingValue"></asp:Label>
            <asp:Label ID="lbPendingValue" runat="server" CssClass="col-xs-6 control-label"></asp:Label>
            <hr style="width: 100%; color: #204d74; height: 2px; background-color: #204d74;" class="col-xs-12"></hr>
            <asp:Label ID="lbTotal" runat="server" Text="Total Issuances" CssClass="col-xs-6 control-label" AssociatedControlID="lbTotalValue"></asp:Label>
            <asp:Label ID="lbTotalValue" runat="server" CssClass="col-xs-6 control-label"></asp:Label>


        </div>
        <div id="skm_LockBackground" class="LockOff"></div>
        <div id="skm_LockPane" class="LockOff">
            <div id="skm_LockPaneText">&nbsp;</div>
        </div>

    </form>
</asp:Content>

