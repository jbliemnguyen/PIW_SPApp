<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="CitationNumberListing.aspx.cs.aspx.cs" Inherits="PIW_SPAppWeb.Pages.CitationNumberListing" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {
            ////set active left navigation tab 
            $("#sidebar a").removeClass("active");
            $("#CitationNumberListingURL").addClass("active");

            //date picker
            $("#tbActionDate").datepicker();

            //jquery validation
            //citation category is required
            $.validator.addMethod("requiredWhenAllDateNotChecked", function (value, element) {
                if ($("#cbAllDate").prop("checked")) {
                    return true;
                } else {
                    if (value) {
                        return true;
                    } else {
                        return false;
                    }
                }
            }, 'This field is required');

            $("#mainForm").validate({
                rules: {
                    ctl00$MainContentPlaceHolder$ddlCitationNumberCategory: "required",
                    ctl00$MainContentPlaceHolder$tbActionDate: "requiredWhenAllDateNotChecked"
                }
            });

            //action date textbox enable
            setActionDateEnabled($("#cbAllDate"));

            //all date checkbox click event
            $("#cbAllDate").click(function (event) {
                setActionDateEnabled($(this));
            });
        }

        function setActionDateEnabled(jqueryAllDateCheckBox) {
            if (jqueryAllDateCheckBox.prop("checked")) {
                $("#tbActionDate").prop("value", "");
                $("#tbActionDate").prop("disabled", "disabled");
            }
            else {
                //enable the tbActionDate
                $("#tbActionDate").removeProp("disabled");
            }
        }
    </script>
    <form id="mainForm" runat="server" class="form-horizontal">
        <div class="form-group">
            <div class="col-xs-8">
                <span style="font-size: large">Citation Number Listing</span>
            </div>
            <div class="col-xs-3">
                <asp:Label runat="server" ID="lbLastUpdated" Font-Italic="True" Font-Bold="True"></asp:Label>
            </div>
        </div>
        <div class="col-xs-6">
            <div class="form-group">
                <asp:Label ID="Label1" runat="server" Text="Document Category" AssociatedControlID="ddlCitationNumberCategory" CssClass="col-xs-5 control-label"></asp:Label>
                <div class="col-xs-7">
                    <asp:DropDownList ID="ddlCitationNumberCategory" CssClass="form-control" runat="server" ClientIDMode="Static">
                        <asp:ListItem Value="">-- Please Select --</asp:ListItem>
                        <asp:ListItem Value="61">Agenda / Notice (61)</asp:ListItem>
                        <asp:ListItem Value="62">Delegated (62)</asp:ListItem>
                        <asp:ListItem Value="63">OALJ (63)</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="Label2" runat="server" Text="Quarter" AssociatedControlID="ddlQuarter" CssClass="col-xs-5 control-label"></asp:Label>
                <div class="col-xs-7">
                    <asp:DropDownList ID="ddlQuarter" CssClass="form-control" runat="server" ClientIDMode="Static">
                    </asp:DropDownList>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="Label3" runat="server" Text="Action Date" AssociatedControlID="tbActionDate" CssClass="col-xs-5 control-label"></asp:Label>
                <div class="col-xs-3">
                    <asp:TextBox ID="tbActionDate" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                </div>
                <div class="col-xs-3">
                    <asp:CheckBox runat="server" ID="cbAllDate" Text="All Dates" CssClass="checkbox" ClientIDMode="Static" Checked="True" />
                </div>
            </div>
        </div>
        <div class="col-xs-6">
            <div class="form-group">
                <asp:Button runat="server" ID="btnRun" Text="Run Report" CssClass="btn-sm btn-primary active" OnClick="btnRun_OnClick" />
            </div>
        </div>
        <div class="form-group">
            <%--<div id="gridDiv" class="col-xs-12" style="overflow-x: scroll; overflow-y: hidden">--%>
            <asp:Label runat="server" ID="lbTest"></asp:Label>
            <div id="gridDiv" class="col-xs-12" style="overflow-x: hidden; overflow-y: hidden">
                <asp:GridView runat="server" ID="gridView" AutoGenerateColumns="false" CssClass="table table-hover table-condensed piw-borderless"
                    OnRowDataBound="sPGridView_RowDataBound" ClientIDMode="Static">
                    <PagerStyle CssClass="pagination-piw" />
                </asp:GridView>
            </div>
        </div>
    </form>
</asp:Content>
