<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="ItemsByPublishedDate.aspx.cs" Inherits="PIW_SPAppWeb.Pages.ItemsByPublishedDate" %>

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
        <div class="form-group">
            <div class="col-md-8">
                <span style="font-size: large">Items By Published Date</span>
            </div>
            <div class="col-md-3">
                <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
            </div>
        </div>
        <fieldset>
            <legend></legend>
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
                    <asp:Label ID="Label1" runat="server" Text="From Publication Date" AssociatedControlID="tbFromDate" CssClass="col-md-6 control-label"></asp:Label>
                    <div class="col-md-6">
                        <asp:TextBox ID="tbFromDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                    </div>
                </div>

                <div class="form-group">
                    <asp:Label ID="Label2" runat="server" Text="To Publication Date" AssociatedControlID="tbToDate" CssClass="col-md-6 control-label"></asp:Label>
                    <div class="col-md-6">
                        <asp:TextBox ID="tbToDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                    </div>
                </div>
            </div>

            <div class="col-md-4">
                <%--Form Type filter--%>
                <div class="form-group">
                    <asp:Label ID="Label3" runat="server" Text="Form Type" CssClass="col-md-4 control-label"></asp:Label>
                    <div class="col-md-8">
                        <asp:CheckBox ID="cbStandardForm" runat="server" Text="Standard Form"
                            Checked="true" CssClass="checkbox" ClientIDMode="Static" />
                    </div>
                    <%--</div>--%>

                    <%--<div class="form-group">--%>
                    <div class="col-md-4"></div>
                    <div class="col-md-8">
                        <asp:CheckBox ID="cbAgendaForm" runat="server" Text="Agenda Form"
                            Checked="true" CssClass="checkbox" ClientIDMode="Static" />
                    </div>
                    <%--</div>--%>

                    <%--<div class="form-group">--%>
                    <div class="col-md-4"></div>
                    <div class="col-md-8">
                        <asp:CheckBox ID="cbDirectPublicationForm" runat="server" Text="Direct Publication Form"
                            Checked="true" CssClass="checkbox" ClientIDMode="Static" />
                    </div>
                </div>


                <div class="form-group">
                    <div class="col-md-4"></div>
                    <div class="col-md-8">
                        <asp:Button runat="server" ID="btnRun" Text="Run Report" CssClass="btn-sm btn-primary active" />
                    </div>
                </div>
                <%--</div>--%>


                <%--<asp:Label ID="lbFormType" runat="server" Text="Form Type" CssClass="col-md-1 control-label"></asp:Label>--%>
            </div>
        </fieldset>
        <asp:UpdatePanel runat="server">
            <ContentTemplate>
                <asp:Timer ID="tmrRefresh" runat="server" Interval="300000" Enabled="false" OnTick="tmrRefresh_Tick">
                    <%--5 minutes delays--%>
                </asp:Timer>

                <%--<div class="form-group">
                    <div id="gridDiv" class="col-md-12" style="overflow-x: scroll;overflow-y: hidden">
                        <asp:GridView runat="server" ID="standardFormsGridView" AutoGenerateColumns="false" CssClass="table table-fixed table-striped table-hover table-condensed piw-borderless" OnRowCreated="standardFormsGridView_RowCreated">
                        </asp:GridView>
                    </div>
                </div>--%>
                <div class="form-group">
                    <div id="gridDiv" class="col-md-12" style="overflow-x: scroll; overflow-y: hidden">
                        <asp:GridView runat="server" ID="standardFormsGridView" AutoGenerateColumns="false" CssClass="table table-hover table-condensed piw-borderless" ClientIDMode="Static">
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

