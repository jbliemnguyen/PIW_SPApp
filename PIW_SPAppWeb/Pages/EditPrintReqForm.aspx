<%@ Page Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="EditPrintReqForm.aspx.cs" Inherits="PIW_SPAppWeb.Pages.EditPrintReqForm" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient() {

            $(".historyhead").click(function () {
                $(".historylist").slideToggle(100);
            });
        }
    </script>
    <form id="mainForm" runat="server" class="form-horizontal">
        <fieldset id="mainFieldSet">
            <asp:Label ID="lbMainMessage" runat="server" CssClass="error" Visible="false"></asp:Label>
            <legend>Print Requisition Form</legend>
            <div class="form-group">
                <asp:Label ID="lbDocketNumber" runat="server" Text="Docket Number" CssClass="col-md-2 control-label" AssociatedControlID="tbDocketNumber"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbDocketNumber" runat="server" CssClass="form-control" TextMode="MultiLine" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbAuthorizingOffice" runat="server" Text="Authorizing Office" CssClass="col-md-2 control-label" AssociatedControlID="tbAuthorizingOffice"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbAuthorizingOffice" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
                <asp:Label ID="lbPriority" runat="server" Text="Print Priority" CssClass="col-md-2 control-label" AssociatedControlID="tbPriority"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbPriority" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="false"></asp:TextBox>
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

                <asp:Label ID="lbNumberofPages" runat="server" Text="Number of Pages" CssClass="col-md-2 control-label" AssociatedControlID="tbNumberofPages"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbNumberofPages" runat="server" CssClass="form-control" ClientIDMode="Static"></asp:TextBox>
                </div>
                <asp:Label ID="lbNumberofCopies" runat="server" Text="Number of Copies to be Mailed" CssClass="col-md-2 control-label" AssociatedControlID="tbNumberofCopies"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbNumberofCopies" runat="server" CssClass="form-control" ClientIDMode="Static"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <div class="col-md-4"></div>
                <asp:Label ID="lbTotalPrint" runat="server" Text="Total Print Pages" CssClass="col-md-2 control-label" AssociatedControlID="tbTotalPrintPages"></asp:Label>
                <div class="col-md-2">
                    <asp:TextBox ID="tbTotalPrintPages" runat="server" CssClass="form-control" ClientIDMode="Static" Enabled="false"></asp:TextBox>
                </div>
            </div>

            <div class="form-group">
                <div class="col-md-2"></div>
                <asp:HyperLink runat="server" ID="hplPIWFormLink" Target="_blank" CssClass="col-md-2">Link to PIW Form</asp:HyperLink>
                <asp:HyperLink runat="server" ID="hplFOLAMailingList" CssClass="col-md-2">FOLA Mailing List</asp:HyperLink>
                <asp:HyperLink runat="server" ID="hplSupplementalMailingList" CssClass="col-md-2">Supplemental Mailing List</asp:HyperLink>
            </div>
            <div class="form-group">
                <asp:Label ID="lbDocumenttobePrinted" runat="server" Text="Printing Document(s)" CssClass="col-md-2 control-label" AssociatedControlID="rpDocumentList"></asp:Label>
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
            <div class="form-group">
                <asp:Label ID="lbComment" runat="server" Text="Comment" AssociatedControlID="tbComment" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-4">
                    <asp:TextBox ID="tbComment" TextMode="MultiLine" Rows="3" CssClass="form-control" runat="server" ClientIDMode="Static"></asp:TextBox>
                </div>
                <div class="col-md-5">
                    <asp:Label runat="server" ID="lbCommentValue"></asp:Label>
                </div>
            </div>
        </fieldset>
        <div class="form-group">
            <div class="col-md-2"></div>
            <div class="col-md-6">
                <asp:Button ID="btnAccept" runat="server" Text="Accept" CssClass="btn-sm btn-primary active" OnClick="btnAccept_Click" />
                <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn-sm btn-primary active" OnClick="btnReject_Click" />
                <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn-sm btn-primary active" OnClick="btnSubmit_Click" />
                <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn-sm btn-primary active" OnClick="btnSave_Click" />
                <asp:Button ID="btnPrintJobComplete" runat="server" Text="Print Job Complete" CssClass="btn-sm btn-primary active" OnClick="btnPrintJobComplete_Click" />
                <asp:Button ID="btnMailJobComplete" runat="server" Text="Mail Job Complete" CssClass="btn-sm btn-primary active" OnClick="btnMailJobComplete_Click" />
                <asp:Button ID="btnDelete" runat="server" Text="Delete" CssClass="btn-sm btn-primary active" ClientIDMode="Static" />
                <asp:Button ID="btnDeleteConfirm" Text="DeleteConfirm" runat="server" Style="visibility: hidden; display: none;" OnClick="btnDeleteConfirm_Click" ClientIDMode="Static" />
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
                                <td>Status</td>
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
        <div id="deleteDialogConfirmation" title="Are you sure you wish to delete this print requisition form?"></div>
    </form>
</asp:Content>
