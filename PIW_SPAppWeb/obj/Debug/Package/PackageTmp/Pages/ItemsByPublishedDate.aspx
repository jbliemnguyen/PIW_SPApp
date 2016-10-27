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
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnableCdn="True"></asp:ScriptManager>
        <div class="form-group">
            <div class="col-md-8">
                <span style="font-size: large">Items By Published Date</span>
            </div>
            <div class="col-md-3">
                <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
            </div>
        </div>


        <div class="col-md-4">
            <div class="form-group">
                <asp:Label ID="lbProgramOfficeWorkflowInitiator" runat="server" Text="Program Office (Workflow Initiator)" AssociatedControlID="ddProgramOfficeWorkflowInitiator" CssClass="col-md-6 control-label"></asp:Label>
                <div class="col-md-6">
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
                <asp:Label ID="Label1" runat="server" Text="From Publication Date" AssociatedControlID="tbFromDate" CssClass="col-md-6 control-label" ClientIDMode="Static"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbFromDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                </div>
            </div>

            <div class="form-group">
                <asp:Label ID="Label2" runat="server" Text="To Publication Date" AssociatedControlID="tbToDate" CssClass="col-md-6 control-label" ClientIDMode="Static"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbToDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                </div>
            </div>

        </div>


        <div class="col-md-3">
            <%--Form Type filter--%>
            <div class="form-group">
                <asp:Label ID="Label4" runat="server" Text="Form Type" CssClass="col-md-5 control-label"></asp:Label>
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
                <div class="col-md-3"></div>
                <div class="col-md-9">
                    <asp:Button runat="server" ID="btnRun" Text="Run Report" CssClass="btn-sm btn-primary active" OnClick="btnRun_OnClick" />
                </div>
            </div>

        </div>

        <div class="col-md-5">
            <div id="divDocumentCategory" >
                <asp:Label ID="Label3" runat="server" Text="Document Category" CssClass="col-md-3 control-label"></asp:Label>
                <div class="checkbox checkboxlist col-sm-9">
                    <asp:CheckBoxList ID="cblDocumentCategory" runat="server" RepeatDirection="Horizontal" RepeatColumns="2">
                    </asp:CheckBoxList>
                </div>
            </div>
            
        </div>

        <asp:UpdatePanel runat="server">
            <ContentTemplate>
                <asp:Timer ID="tmrRefresh" runat="server" Interval="300000" Enabled="false" OnTick="tmrRefresh_Tick">
                    <%--5 minutes delays--%>
                </asp:Timer>


                <div class="form-group">
                    <div id="gridDiv" class="col-md-12" style="overflow-x: scroll; overflow-y: hidden">
                        <asp:GridView runat="server" ID="gridView" AutoGenerateColumns="false" CssClass="table table-hover table-condensed piw-borderless" ClientIDMode="Static">
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

