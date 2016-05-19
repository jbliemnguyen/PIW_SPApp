using Microsoft.SqlServer.Server;

namespace PIW_SPAppWeb
{
    internal static class Constants
    {



        //public const string LISTEVTRCVR_NAME = "ListEventReceiver";
        public const string LIBEVTRCVR_NAME = "PIWDocumentsRER";
        //Services
        public const string PIWDocumentsRERServiceName = "PIWDocumentsRER.svc";

        public const string Timer_JOB_Title = "Check EPS Response Timer";

        

        public const string PIWListName = "PIWList";

        public const string PIWList_colName_AccessionNumber = "Accession Number";
        public const string PIWList_colName_AlternateIdentifier = "Alternate Identifier";
        public const string PIWList_colName_WorkflowInitiator = "Workflow Initiator";
        public const string PIWList_colName_ProgramOfficeWFInitator = "Program Office (Workflow Initiator)";
        public const string PIWList_colName_ProgramOfficeDocumentOwner = "Program Office (Document Owner)";
        public const string PIWList_colName_ByPassDocketValidation = "ByPass Docket Validation";
        public const string PIWList_colName_Comment = "Comment";
        public const string PIWList_colName_Description = "Description";
        public const string PIWList_colName_DocketNumber = "Docket Number";
        public const string PIWList_colName_DocumentURLs = "Document URLs";
        public const string PIWList_colName_DocumentOwner = "Document Owner";
        public const string PIWList_colName_DueDate = "Due Date";
        public const string PIWList_colName_FederalRegister = "Federal Register";

        public const string PIWList_colName_LegalResourcesAndReviewGroupCompleteDate =
            "Legal Resources And Review Group Complete Date";

        public const string PIWList_colName_LegalResourcesAndReviewGroupNote = "Legal Resources And Review Group Note";
        public const string PIWList_colName_FormStatus = "Form Status";
        public const string PIWList_colName_FormType = "Form Type";
        
        public const string PIWList_colName_InstructionForOSEC = "Instruction For OSEC";
        public const string PIWList_colName_DocumentCategory = "Document Category";
        public const string PIWList_colName_IsActive = "Is Active";
        public const string PIWList_colName_IsCNF = "Is CNF";
        public const string PIWList_colName_IsNonDocket = "Is NonDocket";
        public const string PIWList_colName_IsRequireOSECVerification = "Is Require OSEC Verification";
        public const string PIWList_colName_NotificationRecipient = "Notification Recipient";
        public const string PIWList_colName_OSECVerificationAction = "OSEC Verification Action";
        public const string PIWList_colName_OSECVerificationComment = "OSEC Verification Comment";
        public const string PIWList_colName_PrePublicationReviewAction = "PrePublication Review Action";
        public const string PIWList_colName_PrePublicationReviewComment = "PrePublication Review Comment";
        public const string PIWList_colName_PreviousFormStatus = "Previous Form Status";
        public const string PIWList_colName_PublishedDate = "Published Date";
        public const string PIWList_colName_PublishedError = "Published Error";
        public const string PIWList_colName_PublishedBy = "Published By";
        public const string PIWList_colName_SecReviewAction = "Sec Review Action";
        public const string PIWList_colName_SecReviewComment = "Sec Review Comment";
        public const string PIWList_colName_CitationNumber = "Citation Number";
        public const string PIWList_colName_RecallComment = "Recall Comment";
        public const string PIWList_colName_OSECRejectedComment = "OSEC Reject Comment";
        public const string PIWList_colName_Modified = "Modified";
        public const string PIWList_colName_Section206Notice = "Section 206 Notice";
        public const string PIWList_colName_HearingOrder = "Hearing Order";





        public const string PIWList_FormStatus_Pending = "Pending";
        public const string PIWList_FormStatus_Rejected = "Rejected";
        public const string PIWList_FormStatus_Recalled = "Recalled";
        public const string PIWList_FormStatus_Submitted = "Submitted";
        public const string PIWList_FormStatus_Edited = "Edited";
        public const string PIWList_FormStatus_Deleted = "Deleted";
        public const string PIWList_FormStatus_OSECVerification = "OSEC Verification";
        public const string PIWList_FormStatus_SecretaryReview = "Secretary Review";
        public const string PIWList_FormStatus_PrePublication = "PrePublication";
        public const string PIWList_FormStatus_ReadyForPublishing = "Ready For Publishing";
        public const string PIWList_FormStatus_PublishInitiated = "Publish Initiated";
        public const string PIWList_FormStatus_PublishedToeLibrary = "Published To eLibrary";
        public const string PIWList_FormStatus_ReOpen = "ReOpen";


        public const string PIWList_DocCat_DelegatedErrata = "Delegated Errata";
        public const string PIWList_DocCat_DelegatedLetter = "Delegated Letter";
        public const string PIWList_DocCat_DelegatedNotice = "Delegated Notice";
        public const string PIWList_DocCat_DelegatedOrder = "Delegated Order";
        public const string PIWList_DocCat_OALJErrata = "OALJ Errata";
        public const string PIWList_DocCat_OALJ = "OALJ";
        public const string PIWList_DocCat_NoticeErrata = "Notice Errata";
        public const string PIWList_DocCat_Notice = "Notice";
        //Agenda
        public const string PIWList_DocCat_NotationalOrder = "Notational Order";
        public const string PIWList_DocCat_NotationalNotice = "Notational Notice";
        public const string PIWList_DocCat_CommissionOrder = "Commission Order";
        public const string PIWList_DocCat_Consent = "Consent";
        public const string PIWList_DocCat_Errata = "Errata";
        public const string PIWList_DocCat_TollingOrder = "Tolling Order";
        public const string PIWList_DocCat_SunshineNotice = "Sunshine Notice";
        public const string PIWList_DocCat_NoticeofActionTaken = "Notice of Action Taken";





        public const string PIWList_FormType_StandardForm = "Standard Form";
        public const string PIWList_FormType_AgendaForm = "Agenda Form";
        public const string PIWList_FormType_DirectPublicationForm = "Direct Publication Form";

        //PIWDocument list
        public const string PIWDocuments_DocumentLibraryName = "PIW Documents";
        public const string PIWDocuments_colName_SecurityLevel = "Security Level";
        public const string PIWDocuments_colName_NumberOfPages = "Number Of Pages";
        public const string PIWDocuments_colName_EPSPassed = "EPS Passed";
        public const string PIWDocuments_colName_EPSError = "EPS Error";

        public const string PIWDocuments_EPSPassed_Option_True = "True";
        public const string PIWDocuments_EPSPassed_Option_False = "False";
        public const string PIWDocuments_EPSPassed_Option_Pending = "Pending";

        public const string PIWDocuments_EPSSecurityLevel_Option_Public = "P";
        public const string PIWDocuments_EPSSecurityLevel_Option_CEII = "C";
        public const string PIWDocuments_EPSSecurityLevel_Option_NonPublic = "N";

        public const string ddlSecurityControl_Option_Public = "Public";
        public const string ddlSecurityControl_Option_CEII = "CEII";
        public const string ddlSecurityControl_Option_Priviledged = "Priviledged";

        //ErrorLog list
        public const string ErrorLogListName = "ErrorLog";
        public const string ErrorLog_colName_PIWListItem = "PIWListItem";
        public const string ErrorLog_colName_User = "User";
        public const string ErrorLog_colName_ErrorMessage = "ErrorMessage";
        public const string ErrorLog_colName_ErrorPageName = "ErrorPageName";

        //PIWListHistory listreje
        public const string PIWListHistory_ListName = "PIWListHistory";
        public const string PIWListHistory_colName_Title = "Title";
        public const string PIWListHistory_colName_PIWList = "PIW List Item";
        public const string PIWListHistory_colName_User = "User";
        public const string PIWListHistory_colName_Action = "Action";
        public const string PIWListHistory_colName_FormStatus = "Form Status";
        public const string PIWListHistory_colName_Created = "Created";
        public const string PIWListHistory_colName_CreatedBy = "Created By";

        //CitationNumber list
        public const string CitationNumberListName = "Citation Number";
        public const string CitationNumberList_colName_PIWList = "PIWList";
        public const string CitationNumberList_colName_QuarterNumber = "Quarter Number";
        public const string CitationNumberList_colName_DocumentCategoryNumber = "Document Category Number";
        public const string CitationNumberList_colName_SequenceNumber = "Sequence Number";
        public const string CitationNumberList_colName_Title = "Title";
        public const string CitationNumberList_colName_Status = "Status";
        public const string CitationNumberList_colName_AssignedDate = "Assigned Date";
        public const string CitationNumberList_colName_DeletedDate = "Deleted Date";
        public const string CitationNumberList_colName_ReAssignedDate = "ReAssigned Date";

        ////Citation Status 
        public const string CitationNumber_ASSIGNED_Status = "assigned";
        public const string CitationNumber_DELETED_Status = "deleted";
        public const string CitationNumber_REASSIGNED_Status = "reassigned";


        //group name
        public const string Grp_PIWUsers = "PIWUsers";
        public const string Grp_OSECGroupName = "PIWOSEC";
        public const string Grp_SecretaryReviewGroupName = "PIWSecReview";
        public const string Grp_PIWDirectPublication = "PIWDirectPublication";
        public const string Grp_PIWDirectPublicationSubmitOnly = "PIWDirectPublicationSubmitOnly";
        public const string Grp_PIWAdmin = "PIWAdmin";
        public const string Grp_PIWSystemAdmin = "PIWSystemAdmin";
        public const string Grp_PIWAgendaNonManagement = "PIWAgendaNonManagement";
        //public const string Grp_PrintRequisitionSubmitter = "PrintRequisitionSubmitter";
        public const string Grp_CopyCenter = "CopyCenter";
        public const string Grp_PIWOSECFERCReports = "PIWOSECFERCReports";
        
        //FormStatus Key
        public const string formStatusViewStateKey = "FormStatusKey";

        //Previous Form Status
        public const string previousFormStatusViewStateKey = "PreviousFormStatusKey";

        //ModifiedDateTime key
        public const string ModifiedDateTimeKey = "ModifiedDateTimeKey";

        //DocumentURLsFromViewState key
        public const string DocumentURLsKey = "DocumentURLsKey";

        //document urls seperator
        public const string DocumentURLsSeparator = "_##_";

        //ATMS Validation error
        public const string ATMSRemotingServiceConnectionError = "Cannot connect to ATMS to validate docket.";

        //page name
        //this setting store the Page Name, used mainly in code to create URL
        public const string Page_EditStandardForm = "EditStandardForm.aspx";
        public const string Page_EditAgendaForm = "EditAgendaForm.aspx";
        public const string Page_EditDirectPublicationForm = "EditDirectPublicationForm.aspx";
        public const string Page_StandardForms = "StandardForms.aspx";
        public const string Page_AgendaForms = "AgendaForms.aspx";
        public const string Page_DirectPublicationForms = "DirectPublicationForms.aspx";
        public const string Page_AccessDenied = "AccessDenied.aspx";
        public const string Page_ItemNotFound = "ItemNotFound.aspx";

        #region Publishing
        public const string Affiliation_FirstName = "K";
        public const string Affiliation_LastName = "Bose";
        public const string Affiliation_MiddleInitial = "D";
        public const string Affiliation_Organization = "SECRETARY OF THE COMMISSION, FERC";
        public const int document_OfficialFlag = 1;


        //misc
        public const string ValidationFolder = "Validation";


    }

    public enum enumAction
    {
        Submit = 1,
        Recall,
        Reject,
        Accept,
        OSECTakeOwnerShip,
        SubmitToSecReview,
        Publish,
        Save,
        Edit,
        Complete,
        ReOpen,
        Delete
    }
}


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------















        //public const string col_PIWList_FederalRegisterComment = "FederalRegisterComment";
        //public const string col_PIWList_FederalRegister = "FederalRegister";
        //public const string col_PIWList_Description = "Description";
        //public const string col_PIWList_Docket = "Docket";
        //public const string col_PIWList_DocumentType = "DocumentType";
        //public const string col_PIWList_DocumentTitle = "DocumentTitle";
        //public const string col_PIWList_DocumentFileName = "DocumentFileName";
        //public const string col_PIWList_DocumentOwnerID = "DocumentOwnerID";
        //public const string col_PIWList_NotificationReceiverID = "NotificationReceiverID";
        //public const string col_PIWList_DueDate = "DueDate";
        //public const string col_PIWList_FormStatus = "FormStatus";
        //public const string col_PIWList_PreviousFormStatus = "PreviousFormStatus";
        //public const string col_PIWList_Instruction = "Instruction";
        //public const string col_PIWList_IsActive = "IsActive";
        //public const string col_PIWList_IsFinished = "IsFinished";
        //public const string col_PIWList_IsRequireOSECVerification = "IsRequireOSECVerification";
        //public const string col_PIWList_IsRequireRequestCitation = "IsRequireRequestCitation";
        //public const string col_PIWList_IsRequireSecReview = "IsRequireSecReview";
        //public const string col_PIWList_MailDate = "MailDate";
        //public const string col_PIWList_MailNote = "MailNote";
        //public const string col_PIWList_FinishedDate = "FinishedDate";
        //public const string col_PIWList_FinishedNote = "FinishedNote";
        //public const string col_PIWList_InitiatorOffice = "InitiatorOffice";
        //public const string col_PIWList_AuthorOffice = "AuthorOffice";
        //public const string col_PIWList_OSECVerificationAction = "OSECVerificationAction";
        //public const string col_PIWList_OSECVerificationComment = "OSECVerificationComment";

        //public const string col_PIWList_PublishDate = "PublishDate";
        //public const string col_PIWList_SecReviewAction = "SecReviewAction";
        //public const string col_PIWList_SecReviewComment = "SecReviewComment";
        //public const string col_PIWList_PrePublicationAction = "PrePublicationAction";
        //public const string col_PIWList_PrePublicationComment = "PrePublicationComment";
        //public const string col_PIWList_SOCCitationNumber = "SOCCitationNumber";

        //public const string col_PIWList_OSECComment = "OSECComment";
        //public const string col_PIWList_Title = "Title";
        //public const string col_PIWList_WFInitiatorID = "WFInitiatorID";
        //public const string col_PIWList_CreatedUser = "CreatedUser";
        //public const string col_PIWList_PublisherID = "PublisherID";
        //public const string col_PIWList_FormType = "FormType";
        //public const string col_PIWList_Modified = "Modified";
        //public const string col_PIWList_Created = "Created";
        //public const string col_PIWList_IsNonDocket = "IsNonDocket";
        //public const string col_PIWList_IsCNF = "IsCNF";
        //public const string col_PIWList_AlternativeID = "AlternativeID";
        //public const string col_PIWList_RecallComment = "RecallComment";
        //public const string col_PIWList_Print = "Print";
        //public const string col_PIWList_AccessionNumber = "AccessionNumber";
        //public const string col_PIWList_PublishedError = "PublishedError";
        //public const string col_PIWList_RequirePrintReq = "RequirePrintReq";
        //public const string col_PIWList_ByPassDocketValidation = "ByPassDocketValidation";

        //PIWList Document Category Options
        //public const string PIWList_DocumentCategory_Option_ProgramOffice = "Program Office";
        //public const string PIWList_DocumentCategory_Option_OALJ = "OALJ";
        //public const string PIWList_DocumentCategory_Option_Notice = "Notice";
        //public const string PIWList_DocumentCategory_Option_Commission61 = "Commission (61)";
        //public const string PIWList_DocumentCategory_Option_Other = "Other (61)";




        //LUFormStatus list
        //public const string LUFormStatusListName = "LUFormStatus";
        //public const string col_LUFormStatus_Title = "Title";
        //public const string col_LUFormStatus_ID = "ID";
        //public const string col_LUFormStatus_Key = "Key";

        //PIWListHistory list
        //public const string PIWListHistoryListName = "PIWListHistory";
        //public const string PIWListHistory_colName_Title = "Title";
        //public const string PIWListHistory_colName_PIWList = "PIWList";
        //public const string PIWListHistory_colName_User = "User";
        //public const string PIWListHistory_colName_Action = "Action";
        //public const string PIWListHistory_colName_FormStatus = "FormStatus";
        //public const string PIWListHistory_colName_Created = "Created";
        //public const string PIWListHistory_colName_CreatedBy = "Created By";

        

        //CitationNumber list
        //public const string CitationNumberListName = "CitationNumberList";
        //public const string col_CitationNumberList_PIWList = "PIWList";
        //public const string col_CitationNumberList_QuarterNumber = "QuarterNumber";
        //public const string col_CitationNumberList_DocumentTypeNumber = "DocumentCategoryNumber";
        //public const string col_CitationNumberList_SequenceNumber = "SequenceNumber";
        //public const string col_CitationNumberList_Title = "Title";
        //public const string col_CitationNumberList_Status = "Status";
        //public const string col_CitationNumberList_AssignedDate = "AssignedDate";
        //public const string col_CitationNumberList_DeletedDate = "DeletedDate";
        //public const string col_CitationNumberList_ReAssignedDate = "ReAssignedDate";

        //LUFormType list
        //public const string LUFormTypeListName = "LUFormType";

        //ErrorLog list
        //public const string ErrorLogListName = "ErrorLog";
        //public const string col_ErrorLog_ListItem = "ListItem";
        //public const string col_ErrorLog_Docket = "Docket";
        //public const string col_ErrorLog_FormType = "FormType";
        //public const string col_ErrorLog_FormStatus = "FormStatus";
        //public const string col_ErrorLog_DocumentType = "DocumentType";
        //public const string col_ErrorLog_Office = "Office";
        //public const string col_ErrorLog_DocumentTitle = "DocumentTitle";
        //public const string ErrorLog_colName_User = "UserName";
        //public const string ErrorLog_colName_ErrorMessage = "ErrorMessage";
        //public const string ErrorLog_colName_ErrorPageName = "ErrorPageName";

        #endregion
        ////FormStatus
        //public const string formStatusViewStateKey = "FormStatusKey";

        ////Previous Form Status
        //public const string previousFormStatusViewStateKey = "PreviousFormStatusKey";

        ////Document Title key
        //public const string DocumentTitleKey = "DocumentTitleKey";

        ////Document File name key
        //public const string DocumentFileNameKey = "DocumentFileNameKey";

        ////ModifiedDateTime key
        //public const string ModifiedDateTimeKey = "ModifiedDateTimeKey";

        ////group name
        //public const string Grp_PIWUsers = "PIWUsers";
        //public const string Grp_OSECGroupName = "PIWOSEC";
        //public const string Grp_SecretaryReviewGroupName = "PIWSecReview";
        //public const string Grp_PIWDirectPublication = "PIWDirectPublication";
        //public const string Grp_PIWDirectPublicationSubmitOnly = "PIWDirectPublicationSubmitOnly";
        //public const string Grp_PIWAdmin = "PIWAdmin";
        //public const string Grp_PIWAgendaNonManagement = "PIWAgendaNonManagement";
        //public const string Grp_PrintRequisitionSubmitter = "PrintRequisitionSubmitter";
        //public const string Grp_CopyCenter = "PrintRequisitionApproval";
        //public const string Grp_PIWOSECFERCReports = "PIWOSECFERCReports";

        ////role
        //public const string Role_Contribute = "Contribute";
        //public const string Role_Read = "Read";

        ////page name
        ////this setting store the Page Name, used mainly in code to create URL
        ////So when we need to change Page (which is rarely happens), we just come and change this setting.
        ////This is relative location in combine with site root url
        //public const string Page_PIWForm = "PIWForm.aspx";
        //public const string Page_EditPIWForm = "EditPIWForm.aspx";
        //public const string Page_AgendaForm = "AgendaForm.aspx";
        //public const string Page_EditAgendaForm = "EditAgendaForm.aspx";
        //public const string Page_DirectPublicationForm = "DirectPublicationForm.aspx";
        //public const string Page_EditDirectPublicationForm = "EditDirectPublicationForm.aspx";
        //public const string Page_DocketSearch = "DocketSearch.aspx";
        //public const string Page_PublishItemsByDate = "PublishItemsByDate.aspx";
        //public const string Page_SiteAdmin = "SiteAdmin.aspx";
        //public const string Page_CitationNumberReport = "CitationNumberReport.aspx";
        //public const string Page_UnCompletedItemsReport = "UnCompletedItems.aspx";
        //public const string Page_OEPMailingRequired = "MailingRequired.aspx";
        //public const string Page_OEPMailingPending = "MailingPending.aspx";

        ////Citation Status 
        //public const string CitationNumber_ASSIGNED_Status = "assigned";
        //public const string CitationNumber_DELETED_Status = "deleted";
        //public const string CitationNumber_REASSIGNED_Status = "reassigned";

        ////ATMS Validation error
        //public const string ATMSRemotingServiceConnectionError = "Cannot connect to ATMS to validate docket.";

        //#region Print Requision
        ////Print Requisition
        //public const string PrintRequisitionListName = "PrintRequisition";
        //public const string col_PrintRequisition_FileName = "FileName";
        //public const string col_PrintRequisition_PrintStatus = "PrintStatus";
        //public const string col_PrintRequisition_CompletedDate = "CompletedDate";
        //public const string col_PrintRequisition_RefID = "RefID";
        //public const string col_PrintRequisition_IsActive = "IsActive";
        //public const string col_PrintRequisition_SubmittedDate = "SubmittedDate";
        //public const string col_PrintRequisition_DateRequired = "DateRequired";
        //public const string col_PrintRequisition_Office = "Office";
        //public const string col_PrintRequisition_Docket = "Docket";
        //public const string col_PrintRequisition_NumberOfPage = "NumberOfPage";
        //public const string col_PrintRequisition_NumberOfCopies = "NumberOfCopies";

        //public const string NewPrintPrequisitionURL = @"{0}/_layouts/FormServer.aspx?XsnLocation={1}/FormServerTemplates/PrintRequisition.xsn&DefaultItemOpen=1&RefID={2}";
        //#endregion

        //#region Publishing
        //public const string Affiliation_FirstName = "K";
        //public const string Affiliation_LastName = "Bose";
        //public const string Affiliation_MiddleInitial = "D";
        //public const string Affiliation_Organization = "SECRETARY OF THE COMMISSION, FERC";
        //public const string document_Availability = "P";
        //public static int document_OfficialFlag = 1;
        //#endregion

    //}

    //public enum enumFormStatus
    //{
    //    Pending = 1,
    //    Reject = 2,
    //    Recall = 3,
    //    Submitted = 4,
    //    OSECVerification = 5,
    //    SecretaryReview = 6,
    //    PrePublication = 7,
    //    ReadyForPublishing = 8,
    //    Publishing = 9,
    //    PublishInitiated = 10,
    //    Edited = 11,
    //    Complete = 12
    //}

    //public enum enumFormType
    //{
    //    PIWForm = 1,
    //    AgendaForm,
    //    DirectPublicationForm
    //}

    