<%@ Page Title="" Language="C#" MasterPageFile="~/Pages/Main.Master" AutoEventWireup="true" CodeBehind="EditStandardForm.aspx.cs" Inherits="PIW_SPAppWeb.Pages.EditStandardForm" %>


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
            $("#EditStandardFormURL").addClass("active");

            registerPeoplePicker(spHostUrl, spAppWebUrl, SPLanguage);

            //register date picker
            $("#tbDueDate").datepicker({ minDate: 0 });
            //prevent user edit duedate and set value to past date
            $("#tbDueDate").keydown(function (event) { event.preventDefault(); });

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
            });


            //the option is populated from ajax call (update panel)
            //--> we need to use event deletegate to register the event
            //$("#fieldsetPrePublication").on("change", "#ddAvailableCitationNumbers",function() {
            //    var str = jQuery.trim($(this).val());
            //    if (str.indexOf("FERC") > 0) {
            //        $("#tbCitationNumber").attr("value", str);
            //    }
            //});

            //validate docket number when blur event
            $("#tbDocketNumber").blur(function () {
                var docketNumber = $.trim($("#tbDocketNumber").val());
                if (!(!docketNumber)) {//check if docket is not empty, javascript style, cannot eliminiate the !(!)

                    var isCNF = $("#cbIsCNF").is(':checked');
                    var docketValidationByPass = $("#cbDocketValidationByPass").is(':checked');
                    var postdata = '{docketNumber: "' + docketNumber + '",isCNF:' + isCNF + ',docketValidationByPass:' + docketValidationByPass + ' }';

                    $.ajax({
                        type: "POST",
                        url: "EditStandardForm.aspx/ValidateDocketNumber",
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

                                workflowInitiator = getPeoplePickerInstance(context, $('#spanWorkflowInitiator'), $('#inputWorkflowInitiator'), $('#divWorkflowInitiatorSearch'), $('#hdnWorkflowInitiator'), 'EditStandardForm.aspx/GetPeoplePickerData', 'workflowInitiator', spLanguage);
                                documentOwner = getPeoplePickerInstance(context, $('#spanDocumentOwner'), $('#inputDocumentOwner'), $('#divDocumentOwnerSearch'), $('#hdnDocumentOwner'), 'EditStandardForm.aspx/GetPeoplePickerData', 'documentOwner', spLanguage);
                                notificationRecipient = getPeoplePickerInstance(context, $('#spanNotificationRecipient'), $('#inputNotificationRecipient'), $('#divNotificationRecipientSearch'), $('#hdnNotificationRecipient'), 'EditStandardForm.aspx/GetPeoplePickerData', 'notificationRecipient', spLanguage);


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

        <fieldset>
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
                            <asp:ListItem>CEII</asp:ListItem>
                            <asp:ListItem>Priviledged</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <asp:Button ID="btnUpload" runat="server" Text="Upload" CssClass="btn-sm btn-primary cancel" OnClick="btnUpload_Click" />
                        <%--Note: "cancel" in CssClass is to bypass the jquery validation when user upload file--%>
                    </div>
                </div>
            </fieldset>
            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                <ContentTemplate>
                    <asp:Timer ID="Timer1" runat="server" Interval="15000" OnTick="Timer1_Tick" Enabled="false">
                    </asp:Timer>
                    <div class="form-group">
                        <asp:Label ID="lbUploadedDocuments" runat="server" Text="Uploaded Documents" AssociatedControlID="rpDocumentList" ClientIDMode="Static" CssClass="col-md-2 control-label"></asp:Label>
                        <div class="col-md-9">
                            <asp:Repeater ID="rpDocumentList" runat="server" OnItemCommand="rpDocumentList_ItemCommand">
                                <HeaderTemplate>
                                    <ol class="list-group">
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <li class="list-group-item">
                                        <asp:HyperLink ID="HyperLink1" runat="server" Text='<%#DataBinder.Eval(Container.DataItem,"Name")%>'
                                            NavigateUrl='<%#DataBinder.Eval(Container.DataItem,"URL")%>'>
                                        </asp:HyperLink>
                                        &nbsp;&nbsp;|&nbsp;&nbsp;
                            <asp:LinkButton ID="btnRemoveDocument" runat="server" Text="Remove" CommandName="RemoveDocument"
                                CommandArgument='<%#DataBinder.Eval(Container.DataItem,"ID")%>' />
                                        &nbsp;&nbsp;|&nbsp;&nbsp;
                                        <asp:Label runat="server" ID="lbSecurityLevel" Text='<%#DataBinder.Eval(Container.DataItem,"Security Level")%>'></asp:Label>
                                        &nbsp;&nbsp;|&nbsp;&nbsp;
                            <%#DataBinder.Eval(Container.DataItem,"EPS Passed")%>
                            
                            
                            &nbsp;&nbsp;|&nbsp;&nbsp;
                            <asp:Label runat="server" ID="Label2" Text='<%#DataBinder.Eval(Container.DataItem,"EPS Error")%>'></asp:Label>
                                    </li>
                                </ItemTemplate>
                                <FooterTemplate>
                                    </ol>
                                </FooterTemplate>
                            </asp:Repeater>
                            <br />
                            <asp:Label ID="lbRequiredUploadedDocumentError" runat="server" ForeColor="Red" Visible="false">Please upload at least 1 public document</asp:Label>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:UpdatePanel>

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
                    <asp:CheckBox ID="cbIsCNF" runat="server" Text="CNF" ToolTip="Alternate Identifier Required" ClientIDMode="Static" CssClass="checkbox" />
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
                <asp:Label ID="lbInstructionForOSEC" runat="server" Text="Instructions for OSEC" AssociatedControlID="tbInstruction" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbInstruction" TextMode="MultiLine" Rows="2" CssClass="form-control" runat="server" MaxLength="255"></asp:TextBox>
                </div>
                <div class="col-md-2">
                    <asp:CheckBox ID="cbFederalRegister" runat="server" CssClass="checkbox" Text="Federal Register" />
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
                <asp:Label ID="lbDueDate" runat="server" Text="Due Date" AssociatedControlID="tbDueDate" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-3">
                    <asp:TextBox ID="tbDueDate" ClientIDMode="Static" runat="server" CssClass="form-control"></asp:TextBox>
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
            <fieldset runat="server" id="fieldsetOSECRejectComment" visible="true">
                <div class="form-group">
                    <asp:Label ID="lbOSECRejectComment" runat="server" Text="OSEC Reject Comment" AssociatedControlID="lbOSECRejectCommentValue" CssClass="col-md-2 control-label"></asp:Label>
                    <div class="col-md-6">
                        <asp:Label runat="server" ID="lbOSECRejectCommentValue"></asp:Label>
                    </div>
                </div>
            </fieldset>
            <fieldset runat="server" id="fieldsetRecall" visible="true">
                <div class="form-group">
                    <asp:Label ID="lbRecallComment" runat="server" Text="Recall Comment" AssociatedControlID="tbRecallComment" CssClass="col-md-2 control-label"></asp:Label>
                    <div class="col-md-6">
                        <asp:TextBox ID="tbRecallComment" TextMode="MultiLine" Rows="2" CssClass="form-control" runat="server"></asp:TextBox>
                        <asp:Label ID="lbRecallCommentValidation" runat="server" ForeColor="Red" Visible="false" Text="Recall Comment is required" ClientIDMode="Static"></asp:Label>
                    </div>
                </div>
                <div class="form-group">
                    <div class="col-md-2"></div>
                    <div class="col-md-2">
                        <asp:Button ID="btnRecall" runat="server" Text="Recall" CssClass="btn-sm btn-primary" OnClick="btnRecall_Click" />
                    </div>
                </div>
            </fieldset>

            <%--End of Main Panel--%>

            <%--Button pannel--%>

            <%--</form>--%>
            <%--End of Button pannel--%>
        </fieldset>

        <fieldset runat="server" id="fieldsetOSECVerification" visible="false">
            <legend>OSEC Verification</legend>
            <div class="form-group">
                <div class="col-md-2"></div>
                <div class="col-md-2">
                    <asp:Label ID="lbOSECVerificationAction" runat="server"></asp:Label>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbOSECVerificationComment" runat="server" Text="Comment" AssociatedControlID="tbOSECVerificationComment" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbOSECVerificationComment" TextMode="MultiLine" Rows="2" CssClass="form-control" runat="server"></asp:TextBox>
                </div>
            </div>
        </fieldset>



        <fieldset runat="server" id="fieldsetPrePublication" visible="false" clientidmode="Static">
            <legend>Pre-Publication Review</legend>
            <asp:UpdatePanel runat="server">
                <ContentTemplate>
                    <div class="form-group">
                        <div class="col-md-2"></div>
                        <div class="col-md-2">
                            <asp:Button ID="btnGenerateCitationNumber" runat="server" Text="Generate Citation Number" CssClass="btn-sm btn-primary active" OnClick="btnGenerateCitationNumber_Click" />
                        </div>
                        <div class="col-md-6">
                            <asp:TextBox ID="tbCitationNumber" ClientIDMode="Static" runat="server" CssClass="form-control" Width="200px"></asp:TextBox>

                            <asp:DropDownList ID="ddAvailableCitationNumbers" CssClass="form-control" runat="server" Visible="false" Width="200px" ClientIDMode="Static" AutoPostBack="True" OnSelectedIndexChanged="ddAvailableCitationNumbers_SelectedIndexChanged">
                                <asp:ListItem>-- Available Citation # --</asp:ListItem>
                            </asp:DropDownList>

                            <asp:CheckBox ID="cbOverrideCitationNumber" runat="server" Text="Override" CssClass="checkbox" ClientIDMode="Static" />

                            <asp:Label runat="server" ID="lbCitationNumberError" Visible="false" ForeColor="Red"></asp:Label>
                        </div>
                    </div>
                    <div class="form-group">
                        <div class="col-md-2"></div>
                        <div class="col-md-4">
                            <asp:Button ID="btnAcceptCitationNumber" runat="server" Text="Accept Citation Number" CssClass="btn-sm btn-primary active" OnClick="btnAcceptCitationNumber_Click" />
                            <asp:Button ID="btnRemoveCitationNumber" runat="server" Text="Remove Citation Number" CssClass="btn-sm btn-primary active" OnClick="btnRemoveCitationNumber_Click" />
                        </div>
                    </div>
                </ContentTemplate>
                <Triggers>
                    <asp:AsyncPostBackTrigger ControlID="ddAvailableCitationNumbers" EventName="SelectedIndexChanged" />
                </Triggers>
            </asp:UpdatePanel>

            <div class="form-group">
                <div class="col-md-2"></div>
                <div class="col-md-2">
                    <asp:Label ID="lbPrePublicationReviewAction" runat="server" CssClass="control-label"></asp:Label>
                </div>
            </div>
            <div class="form-group">
                <asp:Label ID="lbPrePublicationComment" runat="server" Text="Comment" AssociatedControlID="tbPrePublicationComment" CssClass="col-md-2 control-label"></asp:Label>
                <div class="col-md-6">
                    <asp:TextBox ID="tbPrePublicationComment" TextMode="MultiLine" Rows="2" CssClass="form-control" runat="server"></asp:TextBox>
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
                <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn-sm btn-primary active" OnClick="btnSubmit_Click" />
                <asp:Button ID="btnOSECTakeOwnership" runat="server" Text="OSEC Take Ownership" CssClass="btn-sm btn-primary active" OnClick="btnOSECTakeOwnership_Click" />
                <asp:Button ID="btnEdit" runat="server" Text="Edit" CssClass="btn-sm btn-primary active" OnClick="btnEdit_Click" />
                <asp:Button ID="btnAccept" runat="server" Text="Accept" CssClass="btn-sm btn-primary active" OnClick="btnAccept_Click" />
                <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn-sm btn-primary active" OnClick="btnReject_Click" />
                <asp:Button ID="btnPublish" runat="server" Text="Initiate Publication" ToolTip="Workflow item routed to eLibrary Data Entry Group" CssClass="btn-sm btn-primary active" ClientIDMode="Static" />
                <asp:Button ID="btnPublishConfirm" runat="server" Text="Publish" Style="visibility: hidden; display: none;" ClientIDMode="Static" OnClick="btnInitiatePublication_Click" />
                <asp:Button ID="btnDelete" runat="server" Text="Delete" CssClass="btn-sm btn-primary active" ClientIDMode="Static" />
                <asp:Button ID="btnDeleteConfirm" Text="DeleteConfirm" runat="server" Style="visibility: hidden; display: none;" ClientIDMode="Static" OnClick="btnDelete_Click" />

            </div>
        </div>
        <div id="deleteDialogConfirmation" title="Are you sure you wish to delete this workflow item?"></div>
        <div id="publishDialogConfirmation" title="Are you sure you wish to publish this issuance?"></div>

    </form>
</asp:Content>
