<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="EditDirectPublicationForm.aspx.cs" Inherits="PIW_SPAppWeb.Pages.EditDirectPublicationForm" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript">
        function PageClient(spHostUrl, spAppWebUrl, SPLanguage) {
            //declare peoplepicker control
            var workflowInitiator;
            var documentOwner;
            var notificationRecipient;

            //set active left navigation tab 
            $("#sidebar a").removeClass("active");

            registerPeoplePicker(spHostUrl, spAppWebUrl, SPLanguage);

            $("#tbLegalResourcesReviewCompletionDate").datepicker();

            //disabled Docket Number textbox is IsNonDocket ischecked
            if ($("#cbIsNonDocket").is(':checked')) {
                $("#tbDocketNumber").prop("readonly", "readonly");
            }

            //event for Non-Docketed checkbox
            $("#cbIsNonDocket").change(function () {
                if (this.checked) {
                    //disable docket number
                    $("#tbDocketNumber").prop("readonly", "readonly");
                    $("#tbDocketNumber").prop("value", "Non-Docket"); //can combined with above, but this way is clearer
                } else {
                    $("#tbDocketNumber").removeProp("readonly");
                    $("#tbDocketNumber").prop("value", ""); //can combined with above, but this way is clearer
                }

                $("#tbDocketNumber").blur();//call blur event to validate the docket number and display message
            });

            //validate docket number when blur event
            $("#tbDocketNumber").blur(function () {
                var docketNumber = $.trim($("#tbDocketNumber").val());
                if (!(!docketNumber)) {//check if docket is not empty, javascript style, cannot eliminiate the !(!)

                    var isCNF = $("#cbIsCNF").is(':checked');
                    var docketValidationByPass = $("#cbDocketValidationByPass").is(':checked');
                    var postdata = '{docketNumber: "' + docketNumber + '",isCNF:' + isCNF + ',docketValidationByPass:' + docketValidationByPass + ' }';

                    $.ajax({
                        type: "POST",
                        url: "EditDirectPublicationForm.aspx/ValidateDocketNumber",
                        data: postdata,
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function (response) {
                            if (response.d) { //Not valid docket-display error and hide icon
                                $("#spanDocketValidationClientSideError").text(response.d);
                                $("#spanDocketValidationClientSideError").removeClass("invisible");
                                $("#plyiconDocketValid").addClass("invisible");

                            } else { //return empty string --> docket is good --> clear and hide error message and display good icon
                                $("#spanDocketValidationClientSideError").text("");
                                $("#spanDocketValidationClientSideError").addClass("invisible");
                                $("#plyiconDocketValid").removeClass("invisible");
                                //need to hide the error from client side too-scenerio: user have server-side error, he change it and tab out--> display good icon 
                                //but the server side error is not hide until click again.
                                $("#lbDocketValidationServerSideError").addClass("invisible");
                            }
                        },
                        failure: function (response) {
                            alert(response.d);
                        }
                    });
                }
            });

            //Confirm of deletion
            $("#btnDelete").click(function (event) {
                event.preventDefault();
                $("#deleteDialogConfirmation").dialog({
                    buttons: {
                        "No": function (e) {
                            $(this).dialog("close");

                        },
                        "Yes": function (e) {
                            $("#btnDeleteConfirm").click();
                        }
                    }
                }, { width: 500 });
            });

            //Confirm of Publish
            $("#btnInitiatePublication").click(function (event) {
                event.preventDefault();

                //display warning if due date is future date
                var modalHeight = 100;
                var dueDate = new Date($("#tbDueDate").attr('value'));
                var today = new Date();
                if (dueDate > today) {
                    modalHeight = 250;
                    $("#publishDialogConfirmation").html("<span style='color:green'>Warning: Due Date is a future date</span>");
                }
                else {
                    $("#publishDialogConfirmation").html("");
                }

                //dialog
                $("#publishDialogConfirmation").dialog({
                    buttons: {
                        "No": function (e) {
                            $(this).dialog("close");

                        },
                        "Yes": function (e) {
                            $("#btnPublishConfirm").click();
                            $(this).dialog("close");
                        }
                    }
                }, { width: 600, height: modalHeight });
            });

            //spinner
            $("#btnPublishConfirm").click(function (event) {
                var btnInitiatePublication = $("#btnInitiatePublication");
                var opt = {
                    img: '../Scripts/spinner/spinner-large.gif',
                    position: 'center',
                    height: 48,
                    width: 48
                };

                btnInitiatePublication.spinner(opt);
                //disable Publish and Edit button - avoid use clicking when long-process printing
                $("#btnInitiatePublication").attr("disabled", "disabled");
            });

            $(".historyhead").click(function () {
                $(".historylist").slideToggle(100);
            });
        }

        function registerPeoplePicker(spHostUrl, appWebUrl, spLanguage) {
            //Build absolute path to the layouts root with the spHostUrl
            var layoutsRoot = spHostUrl + '/_layouts/15/';

            //load all appropriate scripts for the page to function
            $.getScript(layoutsRoot + 'SP.Runtime.js',
                function () {
                    $.getScript(layoutsRoot + 'SP.js',
                        function () {
                            //load scripts for cross site calls (needed to use the people picker control in an IFrame)
                            $.getScript(layoutsRoot + 'SP.RequestExecutor.js', function () {
                                context = new SP.ClientContext(appWebUrl);
                                var factory = new SP.ProxyWebRequestExecutorFactory(appWebUrl);
                                context.set_webRequestExecutorFactory(factory);

                                workflowInitiator = getPeoplePickerInstance(context, $('#spanWorkflowInitiator'), $('#inputWorkflowInitiator'), $('#divWorkflowInitiatorSearch'), $('#hdnWorkflowInitiator'), 'EditDirectPublicationForm.aspx/GetPeoplePickerData', 'workflowInitiator', spLanguage);
                                documentOwner = getPeoplePickerInstance(context, $('#spanDocumentOwner'), $('#inputDocumentOwner'), $('#divDocumentOwnerSearch'), $('#hdnDocumentOwner'), 'EditDirectPublicationForm.aspx/GetPeoplePickerData', 'documentOwner', spLanguage);
                                notificationRecipient = getPeoplePickerInstance(context, $('#spanNotificationRecipient'), $('#inputNotificationRecipient'), $('#divNotificationRecipientSearch'), $('#hdnNotificationRecipient'), 'EditDirectPublicationForm.aspx/GetPeoplePickerData', 'notificationRecipient', spLanguage);


                                //we need to disable people picker here becuase this call is slow and asynchronous
                                //we can only disable people picker after it is loaded
                                disablePeoplePickers();
                            });

                        });
                });
        }

        function disablePeoplePickers() {
            //people picker textbox (where user type the name) is disabled from server side, but the href link to remove user 
            //must be disable from client side
            //check if textbox is disabled, if yes, then disable the link
            if ($("#inputWorkflowInitiator").prop("disabled")) {
                $("#inputWorkflowInitiator").parent().find("a").remove();
            }

            if ($("#inputDocumentOwner").prop("disabled")) {
                $("#inputDocumentOwner").parent().find("a").remove();
            }

            if ($("#inputNotificationRecipient").prop("disabled")) {
                $("#inputNotificationRecipient").parent().find("a").remove();
            }
        }
    </script>
    <form id="mainForm" runat="server" class="form-horizontal">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnableCdn="True"></asp:ScriptManager>

        <fieldset id="mainFieldSet">
            <legend>Standard Form</legend>

            <asp:Label ID="lbMainMessage" runat="server" CssClass="error" Visible="false"></asp:Label>

            <fieldset runat="server" id="fieldsetUpload">
                <div class="form-group">

                    <asp:Label ID="lbFileName" runat="server" Text="File Name" AssociatedControlID="fileUpload" CssClass="col-md-2 control-label"></asp:Label>

                    <div class="col-md-7">
                        <asp:FileUpload ID="fileUpload" runat="server" Width="100%" placeholder="Click here to browse the file" />
                    </div>
                    <div class="col-md-3">
                        <asp:Label ID="lbUploadedDocumentError" runat="server" ForeColor="Red" Visible="false"></asp:Label>
                    </div>
                </div>
                <div class="form-group">
                    <asp:Label ID="lbSecurityLevel" runat="server" Text="Security Level" AssociatedControlID="ddlSecurityControl" CssClass="col-md-2 control-label"></asp:Label>
                    <div class="col-md-2">
                        <asp:DropDownList ID="ddlSecurityControl" CssClass="form-control" runat="server">
                            <asp:ListItem>Public</asp:ListItem>
                            <%--Note:Update the Constants.cs if change this value--%>
                            <asp:ListItem>CEII</asp:ListItem>
                            <%--Note:Update the Constants.cs if change this value--%>
                            <asp:ListItem>Priviledged</asp:ListItem>
                            <%--Note:Update the Constants.cs if change this value--%>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <asp:Button ID="btnUpload" runat="server" Text="Upload" CssClass="btn-sm btn-primary cancel" OnClick="btnUpload_Click" />
                        <%--Note: "cancel" in CssClass is to bypass the jquery validation when user upload file--%>
                    </div>
                </div>
            </fieldset>
            <div class="form-group">
                <asp:Label ID="lbUploadedDocuments" runat="server" Text="Uploaded Documents" AssociatedControlID="rpDocumentList" ClientIDMode="Static" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-9">
                    <asp:Repeater ID="rpDocumentList" runat="server" OnItemCommand="rpDocumentList_ItemCommand">
                        <HeaderTemplate>
                            <ol class="list-group">
                        </HeaderTemplate>
                        <ItemTemplate>
                            <li class="list-group-item">
                                <asp:HyperLink ID="hyperlinkFileURL" runat="server" Text='<%#DataBinder.Eval(Container.DataItem,"Name")%>'
                                    NavigateUrl='<%#DataBinder.Eval(Container.DataItem,"URL")%>'>
                                </asp:HyperLink>
                                &nbsp;&nbsp;|&nbsp;&nbsp;
                            <asp:LinkButton ID="btnRemoveDocument" runat="server" Text="Remove" CommandName="RemoveDocument"
                                CommandArgument='<%#DataBinder.Eval(Container.DataItem,"ID")%>' />
                                &nbsp;&nbsp;|&nbsp;&nbsp;
                                        <asp:Label runat="server" ID="lbSecurityLevel" Text='<%#DataBinder.Eval(Container.DataItem,"Security Level")%>'></asp:Label>
                            </li>
                        </ItemTemplate>
                        <FooterTemplate>
                            </ol>
                        </FooterTemplate>
                    </asp:Repeater>
                    <br />
                    <asp:Label ID="lbRequiredUploadedDocumentError" runat="server" ForeColor="Red" Visible="false">Please upload at least one document</asp:Label>
                </div>
            </div>


            <%--<form id="mainForm1">--%>
            <%--Main panel--%>
            <div class="form-group">
                <asp:Label ID="lbDocketNumber" runat="server" Text="Docket Number<span class='accentText'> *</span>" CssClass="col-md-2 control-label" AssociatedControlID="tbDocketNumber"></asp:Label>

                <div class="col-md-6">
                    <asp:TextBox ID="tbDocketNumber" runat="server" CssClass="form-control" TextMode="MultiLine" ClientIDMode="Static"></asp:TextBox>
                    <span id="plyiconDocketValid" class='glyphicon glyphicon-ok invisible' style='color: green'></span>
                    <asp:Label ID="lbDocketValidationServerSideError" runat="server" ForeColor="Red" Visible="false" ClientIDMode="Static"></asp:Label>
                    <span id="spanDocketValidationClientSideError" style="color: red;" class="invisible"></span>
                </div>
                <div class="col-md-2">
                    <asp:CheckBox ID="cbIsNonDocket" runat="server" Text="Non-Docketed"
                        ToolTip="Alternate Identifier Required" CssClass="checkbox" ClientIDMode="Static" />
                </div>
                <div class="col-md-2">
                    <asp:CheckBox ID="cbDocketValidationByPass" runat="server" Text="ByPass Docket Validation" ClientIDMode="Static" AutoPostBack="false" ToolTip="Check here to bypass docket validation" Visible="false" CssClass="checkbox" />
                </div>

            </div>

            <div class="form-group">
                <asp:Label ID="lbAlternateIdentifier" runat="server" Text="Alternate Identifier" AssociatedControlID="tbAlternateIdentifier" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbAlternateIdentifier" runat="server" CssClass="form-control" MaxLength="255" TextMode="MultiLine" placeholder="Additional Information To Further Identify A Workflow Item"></asp:TextBox>
                </div>
            </div>

            <div class="form-group">
                <asp:Label ID="lbDescription" runat="server" Text="Description<span class='accentText'> *</span>" AssociatedControlID="tbDescription" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbDescription" runat="server" TextMode="MultiLine" Rows="4" CssClass="form-control"></asp:TextBox>
                </div>
            </div>

            <div class="form-group">
                <asp:Label ID="lbDocumentCategory" runat="server" Text="Document Category<span class='accentText'> *</span>" AssociatedControlID="ddDocumentCategory" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-3">
                    <asp:DropDownList ID="ddDocumentCategory" CssClass="form-control" runat="server">
                        <asp:ListItem Value="">Please Select</asp:ListItem>
                        <asp:ListItem>Delegated Errata</asp:ListItem>
                        <asp:ListItem>Delegated Letter</asp:ListItem>
                        <asp:ListItem>Delegated Notice</asp:ListItem>
                        <asp:ListItem>Delegated Order</asp:ListItem>
                        <asp:ListItem>OALJ</asp:ListItem>
                        <asp:ListItem>OALJ Errata</asp:ListItem>
                        <asp:ListItem>Notice Errata</asp:ListItem>
                        <asp:ListItem>Notice</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>

            <div class="form-group">
                <asp:Label ID="lbProgramOfficeWorkflowInitiator" runat="server" Text="Program Office (Workflow Initiator)<span class='accentText'> *</span>" AssociatedControlID="ddProgramOfficeWorkflowInitiator" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-3">
                    <asp:DropDownList ID="ddProgramOfficeWorkflowInitiator" CssClass="form-control" runat="server">
                        <asp:ListItem Value="">Please Select</asp:ListItem>
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
                <asp:Label ID="lbWorkflowInitiator" runat="server" Text="Workflow Initiator" AssociatedControlID="inputWorkflowInitiator" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-4">
                    <div>
                        <div id="divWorkflowInitiator" class="cam-peoplepicker-userlookup">
                            <span id="spanWorkflowInitiator"></span>
                            <asp:TextBox ID="inputWorkflowInitiator" ClientIDMode="Static" runat="server" CssClass="cam-peoplepicker-edit" Width="100%"></asp:TextBox>
                        </div>
                        <div id="divWorkflowInitiatorSearch" class="cam-peoplepicker-usersearch"></div>
                        <asp:HiddenField ID="hdnWorkflowInitiator" ClientIDMode="Static" runat="server" />
                    </div>

                </div>
            </div>


            <div class="form-group">
                <asp:Label ID="lbProgramOfficeDocumentOwner" runat="server" Text="Program Office (Document Owner)" AssociatedControlID="ddProgramOfficeDocumentOwner" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-3">
                    <asp:DropDownList ID="ddProgramOfficeDocumentOwner" CssClass="form-control" runat="server">
                        <asp:ListItem>Please Select</asp:ListItem>
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
                <asp:Label ID="lbDocumentOwner" runat="server" Text="Document Owner" CssClass="col-md-2 control-label" AssociatedControlID="inputDocumentOwner"></asp:Label>
                <div class="col-md-4">
                    <div>
                        <div id="divDocumentOwner" class="cam-peoplepicker-userlookup">
                            <span id="spanDocumentOwner"></span>
                            <asp:TextBox ID="inputDocumentOwner" ClientIDMode="Static" runat="server" CssClass="cam-peoplepicker-edit" Width="100%"></asp:TextBox>
                        </div>
                        <div id="divDocumentOwnerSearch" class="cam-peoplepicker-usersearch"></div>
                        <asp:HiddenField ID="hdnDocumentOwner" ClientIDMode="Static" runat="server" />
                    </div>
                </div>
            </div>

            <div class="form-group">
                <asp:Label ID="lbNotificationRecipient" runat="server" Text="Notification Recipient" AssociatedControlID="inputNotificationRecipient" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-3">
                    <div>
                        <div id="divNotificationRecipient" class="cam-peoplepicker-userlookup">
                            <span id="spanNotificationRecipient"></span>
                            <asp:TextBox ID="inputNotificationRecipient" ClientIDMode="Static" runat="server" CssClass="cam-peoplepicker-edit" Width="100%"></asp:TextBox>
                        </div>
                        <div id="divNotificationRecipientSearch" class="cam-peoplepicker-usersearch"></div>
                        <asp:HiddenField ID="hdnNotificationRecipient" ClientIDMode="Static" runat="server" />
                    </div>
                </div>
            </div>

            <div class="form-group">
                <asp:Label ID="lbComment" runat="server" Text="Comment" AssociatedControlID="tbComment" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-3">
                    <asp:TextBox ID="tbComment" TextMode="MultiLine" Rows="2" CssClass="form-control" runat="server"></asp:TextBox>
                </div>
                <div class="col-md-1"></div>
                <div class="col-md-5">
                    <asp:Label runat="server" ID="lbCommentValue"></asp:Label>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbFOLAServiceRequired" runat="server" Text="FOLA Service Required<span class='accentText'> *</span>" AssociatedControlID="ddFolaServiceRequired" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-3">
                    <asp:DropDownList ID="ddFolaServiceRequired" CssClass="form-control" runat="server">
                        <asp:ListItem Value="">Please Select</asp:ListItem>
                        <asp:ListItem>Yes</asp:ListItem>
                        <asp:ListItem>No</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>

            <fieldset runat="server" id="fieldSetSupplementalMailingList">
                <div class="form-group">
                    <asp:Label ID="lbSupplementalMailingListFileName" runat="server" Text="Supplemental Mailing List" AssociatedControlID="supplementalMailingListFileUpload" CssClass="col-md-2 control-label"></asp:Label>

                    <div class="col-md-7">
                        <asp:FileUpload ID="supplementalMailingListFileUpload" runat="server" Width="100%" placeholder="Click here to browse the file" />
                    </div>

                    <div class="col-md-3">
                        <asp:Label ID="lbSupplementalMailingListUploadError" runat="server" ForeColor="Red" Visible="false"></asp:Label>
                    </div>
                </div>
                <div class="form-group">
                    <div class="col-md-2"></div>
                    <div class="col-md-2">
                        <asp:Button ID="btnSupplementalMailingListUpload" runat="server" Text="Upload" CssClass="btn-sm btn-primary cancel" OnClick="btnSupplementalMailingListUpload_Click" />
                        <%--Note: "cancel" in CssClass is to bypass the jquery validation when user upload file--%>
                    </div>
                </div>
            </fieldset>
            <div class="form-group">
                <asp:Label ID="lbUploadedSupplementalMailingList" runat="server" Text="Uploaded Supplemental Mailing List" AssociatedControlID="rpSupplementalMailingListDocumentList" ClientIDMode="Static" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-9">
                    <asp:Repeater ID="rpSupplementalMailingListDocumentList" runat="server" OnItemCommand="rpSupplementalMailingListDocumentList_ItemCommand">
                        <HeaderTemplate>
                            <ol class="list-group">
                        </HeaderTemplate>
                        <ItemTemplate>
                            <li class="list-group-item">
                                <asp:HyperLink ID="hyperlinkFileURL" runat="server" Text='<%#DataBinder.Eval(Container.DataItem,"Name")%>'
                                    NavigateUrl='<%#DataBinder.Eval(Container.DataItem,"URL")%>'>
                                </asp:HyperLink>
                                &nbsp;&nbsp;|&nbsp;&nbsp;
                            <asp:LinkButton ID="btnRemoveDocument" runat="server" Text="Remove" CommandName="RemoveDocument"
                                CommandArgument='<%#DataBinder.Eval(Container.DataItem,"ID")%>' />
                            </li>
                        </ItemTemplate>
                        <FooterTemplate>
                            </ol>
                        </FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>

            <%--End of Main Panel--%>

            <%--Button pannel--%>

            <%--</form>--%>
            <%--End of Button pannel--%>
        </fieldset>
        

        <fieldset runat="server" id="fieldsetMailedRoom" visible="false">
            <legend>Mail Room</legend>
            <div class="form-group">
                <asp:Label ID="lbMailedDate" runat="server" Text="Mailed Date" AssociatedControlID="lbMailedDateValue" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-2">
                    <asp:Label ID="lbMailedDateValue" runat="server"></asp:Label>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbMailedRoomNote" runat="server" Text="Note" AssociatedControlID="lbMailedRoomNoteValue" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-2">
                    <asp:Label ID="lbMailedRoomNoteValue" runat="server"></asp:Label>
                </div>
            </div>
        </fieldset>

        <fieldset runat="server" id="fieldsetLegalResourcesReview" visible="false">
            <legend>Legal Resources And Review</legend>
            <div class="form-group">
                <asp:Label ID="lbLegalResourcesReviewCompletionDate" runat="server" Text="Completion Date" AssociatedControlID="tbLegalResourcesReviewCompletionDate" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbLegalResourcesReviewCompletionDate" ClientIDMode="Static" runat="server" CssClass="form-control"></asp:TextBox>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbLegalResourcesNote" runat="server" Text="Note" AssociatedControlID="tbLegalResourcesReviewNote" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbLegalResourcesReviewNote" TextMode="MultiLine" Rows="2" CssClass="form-control" runat="server"></asp:TextBox>
                </div>
            </div>
        </fieldset>

        <div class="form-group"></div>
        <%--empty line--%>
        <div class="form-group"></div>
        <div class="form-group">
            <div class="col-md-2"></div>
            <div class="col-md-6">
                <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn-sm btn-primary active" OnClick="btnSave_Click" />
                <asp:Button ID="btnInitiatePublication" runat="server" Text="Initiate Publication" ToolTip="Workflow item routed to eLibrary Data Entry Group" CssClass="btn-sm btn-primary active" ClientIDMode="Static" />
                <asp:Button ID="btnPublishConfirm" runat="server" Text="Publish" Style="visibility: hidden; display: none;" ClientIDMode="Static" OnClick="btnInitiatePublication_Click" />
                <asp:Button ID="btnDelete" runat="server" Text="Delete" CssClass="btn-sm btn-primary active" ClientIDMode="Static" />
                <asp:Button ID="btnDeleteConfirm" Text="DeleteConfirm" runat="server" Style="visibility: hidden; display: none;" ClientIDMode="Static" OnClick="btnDeleteConfirm_Click" />
                <asp:Button ID="btnReopen" runat="server" Text="Re-Open" CssClass="btn-sm btn-primary active" OnClick="btnReopen_Click" />
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
        <div id="deleteDialogConfirmation" title="Are you sure you wish to delete this workflow item?"></div>
        <div id="publishDialogConfirmation" title="Are you sure you wish to publish this issuance?"></div>

    </form>
</asp:Content>
