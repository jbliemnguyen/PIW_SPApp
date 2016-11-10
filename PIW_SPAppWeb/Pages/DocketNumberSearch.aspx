<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="DocketNumberSearch.aspx.cs" Inherits="PIW_SPAppWeb.Pages.DocketNumberSearch" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">

    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#DocketNumberSearchURL").addClass("active");

            
            //event when click on any Document Category Checkbox --> Uncheck "All" checkbox
            $("span.jqueryselector_FormTypeCheckBox input:checkbox").click(function (event) {
                if ($(this).prop("checked")) {
                    $("span.jqueryselector_FormTypeCheckBoxAll input:checkbox").prop("checked", false);
                }
            });

            //event when click on All checkbox --> uncheck all other checkboxes
            $("span.jqueryselector_FormTypeCheckBoxAll input:checkbox").click(function (event) {
                if ($(this).prop("checked")) {
                    $("span.jqueryselector_FormTypeCheckBox input:checkbox").removeAttr('checked');

                }
            });
        }
    </script>
    <form id="mainForm" runat="server" class="form-horizontal">
        <div class="form-group">
            <div class="col-xs-8">
                <asp:Label ID="lbReportName" style="font-size: large" runat="server" Text="Docket Number Search"></asp:Label>
            </div>
            <div class="col-xs-3">
                <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
            </div>
        </div>


        <div class="col-xs-6">
            <div class="form-group">
                <asp:Label ID="Label1" runat="server" Text="Docket Number" AssociatedControlID="tbDocketNumber" CssClass="col-xs-5 control-label"></asp:Label>
                <div class="col-xs-7">
                    <asp:TextBox ID="tbDocketNumber" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" ClientIDMode="Static"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbProgramOfficeWorkflowInitiator" runat="server" Text="Program Office (Workflow Initiator)" AssociatedControlID="ddProgramOfficeWorkflowInitiator" CssClass="col-xs-5 control-label"></asp:Label>
                <div class="col-xs-7">
                    <div class="form-inline">

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
                        <asp:Button runat="server" ID="btnSearch" Text="Search" CssClass="btn-sm btn-primary active" OnClick="btnSearch_OnClick" />
                    </div>
                </div>
            </div>
        </div>


        <div class="col-xs-6">
            <%--Form Type filter--%>
            <div class="form-group">
                <asp:Label ID="Label4" runat="server" Text="Form Type" CssClass="col-xs-2 control-label"></asp:Label>
                <div class="col-xs-5">
                    <div class="checkbox">
                        <asp:CheckBox ID="cbAll" runat="server" Checked="true" ClientIDMode="Static" CssClass="jqueryselector_FormTypeCheckBoxAll" Text="All Forms"></asp:CheckBox>
                    </div>
                    <div class="checkbox">
                        <asp:CheckBox ID="cbStandardForm" runat="server" ClientIDMode="Static" CssClass="jqueryselector_FormTypeCheckBox" Text="Standard Form"/>
                    </div>
                    <div class="checkbox">
                        <asp:CheckBox ID="cbAgendaForm" runat="server" ClientIDMode="Static" CssClass="jqueryselector_FormTypeCheckBox" Text="Agenda Form"/>
                    </div>
                    <div class="checkbox">
                        <asp:CheckBox ID="cbDirecPubForm" runat="server" ClientIDMode="Static" CssClass="jqueryselector_FormTypeCheckBox" Text="Direct Publication Form" />
                    </div>
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
