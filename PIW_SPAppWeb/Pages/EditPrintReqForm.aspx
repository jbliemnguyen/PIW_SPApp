<%@ Page Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="EditPrintReqForm.aspx.cs" Inherits="PIW_SPAppWeb.Pages.EditPrintReqForm" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {

            //register date picker
            $("#tbPrintJobCompletedDate").datepicker();
            $("#tbMailJobCompletedDate").datepicker();

            //prevent user from clear the date
            $("#tbPrintJobCompletedDate").keydown(function (event) { event.preventDefault(); });
            $("#tbMailJobCompletedDate").keydown(function (event) { event.preventDefault(); });

            
        }
    </script>
    <form id="mainForm" runat="server" class="form-horizontal">
        <fieldset id="mainFieldSet">
            <div class="form-group">
                <asp:Label ID="lbDocketNumber" runat="server" Text="Docket Number" CssClass="col-md-2 control-label" AssociatedControlID="tbDocketNumber"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbDocketNumber" runat="server" CssClass="form-control" TextMode="MultiLine" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbDateRequested" runat="server" Text="Date Requested" CssClass="col-md-2 control-label" AssociatedControlID="tbDateRequested"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbDateRequested" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
                <asp:Label ID="lbDateRequired" runat="server" Text="Date Required" CssClass="col-md-2 control-label" AssociatedControlID="tbDateRequired"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbDateRequired" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbAuthorizingOffice" runat="server" Text="Authorizing Office" CssClass="col-md-2 control-label" AssociatedControlID="tbAuthorizingOffice"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbAuthorizingOffice" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
                <asp:Label ID="lbNumberofPages" runat="server" Text="Number of Pages" CssClass="col-md-2 control-label" AssociatedControlID="tbNumberofPages"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbNumberofPages" runat="server" CssClass="form-control" ClientIDMode="Static"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbNumberofCopies" runat="server" Text="Number of Copies to be Mailed" CssClass="col-md-2 control-label" AssociatedControlID="tbNumberofCopies"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbNumberofCopies" runat="server" CssClass="form-control" ClientIDMode="Static"></asp:TextBox>
                </div>
                <asp:Label ID="lbTotalPrint" runat="server" Text="Total Print Pages" CssClass="col-md-2 control-label" AssociatedControlID="tbTotalPrintPages"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbTotalPrintPages" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbPriority" runat="server" Text="Print Priority" CssClass="col-md-2 control-label" AssociatedControlID="tbPriority"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbPriority" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
            </div>
        </fieldset>
        <fieldset id="documents">
            <legend>Documents</legend>
            <div class="form-group">
                <div class="col-md-2"></div>
                <div class="col-md-2">
                    <asp:HyperLink runat="server" ID="hplPIWFormLink" Target="_blank">Link to PIW Form</asp:HyperLink>
                </div>
                <div class="col-md-2">
                    <asp:HyperLink runat="server" ID="hplFOLAMailingList">FOLA Mailing List</asp:HyperLink>
                </div>
                <div class="col-md-2">
                    <asp:HyperLink runat="server" ID="hplSupplementalMailingList">Supplemental Mailing List</asp:HyperLink>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbDocumenttobePrinted" runat="server" Text="Document(s) to be Printed" CssClass="col-md-2 control-label" AssociatedControlID="rpDocumentList"></asp:Label>
                <div class="col-md-6">
                    <asp:Repeater ID="rpDocumentList" runat="server">
                        <HeaderTemplate>
                            <ol class="list-group">
                        </HeaderTemplate>
                        <ItemTemplate>
                            <li class="list-group-item">
                                <asp:HyperLink ID="hyperlinkFileURL" runat="server" Text='<%#DataBinder.Eval(Container.DataItem,"Name")%>'
                                    NavigateUrl='<%#DataBinder.Eval(Container.DataItem,"URL")%>'>
                                </asp:HyperLink>
                            </li>
                        </ItemTemplate>
                        <FooterTemplate>
                            </ol>
                        </FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </fieldset>
        <fieldset id="documents">
            <legend>Tasks</legend>
            <div class="form-group">
                <div class="col-md-2"></div>
                <div class="col-md-2">
                    <asp:CheckBox ID="cbPrintJobCompleted" runat="server" Text="Print Job Completed" CssClass="checkbox" ClientIDMode="Static" OnCheckedChanged="cbPrintJobCompleted_CheckedChanged" AutoPostBack="True" />
                </div>
                <asp:Label ID="lbPrintJobCompletedDate" runat="server" Text="Print Date" CssClass="col-md-2 control-label" AssociatedControlID="tbPrintJobCompletedDate"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbPrintJobCompletedDate" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="False"></asp:TextBox>
                </div>

            </div>
            <div class="form-group">
                <div class="col-md-2"></div>
                <div class="col-md-2">
                    <asp:CheckBox ID="cbMailJobCompleted" runat="server" Text="Mail Job Completed" CssClass="checkbox" ClientIDMode="Static" OnCheckedChanged="cbMailJobCompleted_CheckedChanged" AutoPostBack="True" />
                </div>
                <asp:Label ID="lbMailJobCompletedDate" runat="server" Text="Mail Date" CssClass="col-md-2 control-label" AssociatedControlID="tbMailJobCompletedDate"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbMailJobCompletedDate" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="False"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbNote" runat="server" Text="Note" CssClass="col-md-2 control-label" AssociatedControlID="tbNote"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbNote" TextMode="MultiLine" Rows="4" CssClass="form-control" runat="server"></asp:TextBox>
                </div>
            </div>
        </fieldset>
        <div class="form-group"></div>
        <div class="form-group">
            <div class="col-md-2"></div>
            <div class="col-md-1">
                <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn-lg btn-primary active" OnClick="btnSave_Click" />

            </div>
            <div class="col-md-1">
                <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn-lg btn-primary active" />
            </div>
        </div>
                <div class="form-group">
            <div class="col-md-2"></div>
            <div class="col-md-8 historyhead">
                History (Click here to collapse/expand)
            </div>
            <br />
            <div class="col-md-2"></div>
            <div id="historylist" class="col-md-8 historylist">
                <asp:Repeater ID="rpHistoryList" runat="server">
                    <HeaderTemplate>
                        <table class="table table-bordered table-striped">
                            <tr style='font-weight: bold'>
                                <td>Date and Time</td>
                                <td>User</td>
                                <td>Action</td>
                                <td>Post-Action PIW Status</td>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td>
                                <%#DataBinder.Eval(Container.DataItem,"Created")%> 
                            </td>
                            <td>
                                <%#DataBinder.Eval(Container.DataItem,"User")%> 
                            </td>
                            <td>
                                <%#DataBinder.Eval(Container.DataItem,"Action")%> 
                            </td>
                            <td>
                                <%#DataBinder.Eval(Container.DataItem,"FormStatus")%> 
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </form>
</asp:Content>
