using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PIW_SPAppWeb
{
    internal static class Constants
    {



        //public static string LISTEVTRCVR_NAME = "ListEventReceiver";
        public static string LIBEVTRCVR_NAME = "PIWDocumentsRER";
        //Services
        public static string PIWDocumentsRERServiceName = "PIWDocumentsRER.svc";

        public static string Timer_JOB_Title = "Check EPS Response Timer";

        #region List Setting

        public static string PIWListName = "PIWList";

        public static string PIWList_colName_AccessionNumber = "Accession Number";
        public static string PIWList_colName_AlternativeID = "Alternative ID";
        public static string PIWList_colName_OwnerOffice = "Owner Office";
        public static string PIWList_colName_ByPassDocketValidation = "ByPass Docket Validation";
        public static string PIWList_colName_Comment = "Comment";
        public static string PIWList_colName_Description = "Description";
        public static string PIWList_colName_DocketNumber = "Docket Number";
        public static string PIWList_colName_DocumentFileName = "Document File Name";
        public static string PIWList_colName_DocumentOwner = "Document Owner";
        public static string PIWList_colName_DueDate = "Due Date";
        public static string PIWList_colName_FederalRegister = "Federal Register";

        public static string PIWList_colName_LegalResourcesAndReviewGroupCompleteDate =
            "Legal Resources And Review Group Complete Date";

        public static string PIWList_colName_LegalResourcesAndReviewGroupNote = "Legal Resources And Review Group Note";
        public static string PIWList_colName_FormStatus = "Form Status";
        public static string PIWList_colName_FormType = "Form Type";
        public static string PIWList_colName_WorkflowInitiator = "Workflow Initiator";
        public static string PIWList_colName_InitiatorOffice = "Initiator Office";
        public static string PIWList_colName_InstructionForOSEC = "Instruction For OSEC";
        public static string PIWList_colName_IsActive = "Is Active";
        public static string PIWList_colName_IsCNF = "Is CNF";
        public static string PIWList_colName_IsNonDocket = "Is NonDocket";
        public static string PIWList_colName_IsRequireOSECVerification = "Is Require OSEC Verification";
        public static string PIWList_colName_NotificationReceiver = "Notification Receiver";
        public static string PIWList_colName_OSECVerification_Action = "OSEC Verification Action";
        public static string PIWList_colName_OSECVerificationComment = "OSEC Verification Comment";
        public static string PIWList_colName_PrePublicationReviewAction = "PrePublication Review Action";
        public static string PIWList_colName_PrePublicationReviewComment = "PrePublication Review Comment";
        public static string PIWList_colName_PreviousFormStatus = "Previous Form Status";
        public static string PIWList_colName_PublishedDate = "Published Date";
        public static string PIWList_colName_PublishedError = "Published Error";
        public static string PIWList_colName_PublishedBy = "Published By";
        public static string PIWList_colName_RecallComment = "Recall Comment";
        public static string PIWList_colName_SecReviewAction = "Sec Review Action";
        public static string PIWList_colName_SecReviewComment = "Sec Review Comment";
        public static string PIWList_colName_CitationNumber = "Citation Number";

        



        public static string PIWList_FormStatus_Pending = "Pending";
        public static string PIWList_FormStatus_Rejected = "Rejected";
        public static string PIWList_FormStatus_Recalled = "Recalled";
        public static string PIWList_FormStatus_Submitted = "Submitted";
        public static string PIWList_FormStatus_OSECVerification = "OSEC Verification";
        public static string PIWList_FormStatus_SecretaryReview = "Secretary Review";
        public static string PIWList_FormStatus_PrePublication = "PrePublication";
        public static string PIWList_FormStatus_ReadyForPublishing = "Ready For Publishing";
        public static string PIWList_FormStatus_PublishInitiated = "Publish Initiated";
        public static string PIWList_FormStatus_Edited = "Edited";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending";
        //public static string PIWList_FormStatus_Pending = "Pending"; 


        public static string PIWList_FormType_StandardForm = "Standard Form";
        public static string PIWList_FormType_AgendaForm = "Agenda Form";
        public static string PIWList_FormType_DirectPublicationForm = "Direct Publication Form";

        //PIWDocument list
        public static string PIWDocuments_DocumentLibraryName = "PIW Documents";
        public static string PIWDocuments_colName_SecurityLevel = "Security Level";
        public static string PIWDocuments_colName_NumberOfPages = "Number Of Pages";
        public static string PIWDocuments_colName_EPSPassed = "EPS Passed";
        public static string PIWDocuments_colName_EPSError = "EPS Error";

        public static string PIWDocuments_EPSPassed_Option_True = "True";
        public static string PIWDocuments_EPSPassed_Option_False = "False";
        public static string PIWDocuments_EPSPassed_Option_Pending = "Pending";



        //FormStatus
        public static string formStatusViewStateKey = "FormStatusKey";

        //Previous Form Status
        public static string previousFormStatusViewStateKey = "PreviousFormStatusKey";

        //Document Title key
        public static string DocumentTitleKey = "DocumentTitleKey";

        //Document File name key
        public static string DocumentFileNameKey = "DocumentFileNameKey";

        //ViewModifiedDateTime key
        public static string ViewModifiedDateTimeKey = "ViewModifiedDateTimeKey";

        public static string CacheKey_PIWListInternalColumnName = "CacheKey_PIWListInternalColumnName";
        public static string CacheKey_PIWDocumentsInternalColumnName = "CacheKey_PIWDocumentsInternalColumnName";
    }

    public enum enumAction
    {
        Submit = 1,
        Recall,
        Reject,
        Accept,
        OSECTakeOwnerShip,
        Publish,
        Save,
        Edit,
        Complete
    }
}


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------















        //public static string col_PIWList_FederalRegisterComment = "FederalRegisterComment";
        //public static string col_PIWList_FederalRegister = "FederalRegister";
        //public static string col_PIWList_Description = "Description";
        //public static string col_PIWList_Docket = "Docket";
        //public static string col_PIWList_DocumentType = "DocumentType";
        //public static string col_PIWList_DocumentTitle = "DocumentTitle";
        //public static string col_PIWList_DocumentFileName = "DocumentFileName";
        //public static string col_PIWList_DocumentOwnerID = "DocumentOwnerID";
        //public static string col_PIWList_NotificationReceiverID = "NotificationReceiverID";
        //public static string col_PIWList_DueDate = "DueDate";
        //public static string col_PIWList_FormStatus = "FormStatus";
        //public static string col_PIWList_PreviousFormStatus = "PreviousFormStatus";
        //public static string col_PIWList_Instruction = "Instruction";
        //public static string col_PIWList_IsActive = "IsActive";
        //public static string col_PIWList_IsFinished = "IsFinished";
        //public static string col_PIWList_IsRequireOSECVerification = "IsRequireOSECVerification";
        //public static string col_PIWList_IsRequireRequestCitation = "IsRequireRequestCitation";
        //public static string col_PIWList_IsRequireSecReview = "IsRequireSecReview";
        //public static string col_PIWList_MailDate = "MailDate";
        //public static string col_PIWList_MailNote = "MailNote";
        //public static string col_PIWList_FinishedDate = "FinishedDate";
        //public static string col_PIWList_FinishedNote = "FinishedNote";
        //public static string col_PIWList_InitiatorOffice = "InitiatorOffice";
        //public static string col_PIWList_AuthorOffice = "AuthorOffice";
        //public static string col_PIWList_OSECVerificationAction = "OSECVerificationAction";
        //public static string col_PIWList_OSECVerificationComment = "OSECVerificationComment";

        //public static string col_PIWList_PublishDate = "PublishDate";
        //public static string col_PIWList_SecReviewAction = "SecReviewAction";
        //public static string col_PIWList_SecReviewComment = "SecReviewComment";
        //public static string col_PIWList_PrePublicationAction = "PrePublicationAction";
        //public static string col_PIWList_PrePublicationComment = "PrePublicationComment";
        //public static string col_PIWList_SOCCitationNumber = "SOCCitationNumber";

        //public static string col_PIWList_OSECComment = "OSECComment";
        //public static string col_PIWList_Title = "Title";
        //public static string col_PIWList_WFInitiatorID = "WFInitiatorID";
        //public static string col_PIWList_CreatedUser = "CreatedUser";
        //public static string col_PIWList_PublisherID = "PublisherID";
        //public static string col_PIWList_FormType = "FormType";
        //public static string col_PIWList_Modified = "Modified";
        //public static string col_PIWList_Created = "Created";
        //public static string col_PIWList_IsNonDocket = "IsNonDocket";
        //public static string col_PIWList_IsCNF = "IsCNF";
        //public static string col_PIWList_AlternativeID = "AlternativeID";
        //public static string col_PIWList_RecallComment = "RecallComment";
        //public static string col_PIWList_Print = "Print";
        //public static string col_PIWList_AccessionNumber = "AccessionNumber";
        //public static string col_PIWList_PublishedError = "PublishedError";
        //public static string col_PIWList_RequirePrintReq = "RequirePrintReq";
        //public static string col_PIWList_ByPassDocketValidation = "ByPassDocketValidation";

        //PIWList Document Category Options
        //public static string PIWList_DocumentCategory_Option_ProgramOffice = "Program Office";
        //public static string PIWList_DocumentCategory_Option_OALJ = "OALJ";
        //public static string PIWList_DocumentCategory_Option_Notice = "Notice";
        //public static string PIWList_DocumentCategory_Option_Commission61 = "Commission (61)";
        //public static string PIWList_DocumentCategory_Option_Other = "Other (61)";




        //LUFormStatus list
        //public static string LUFormStatusListName = "LUFormStatus";
        //public static string col_LUFormStatus_Title = "Title";
        //public static string col_LUFormStatus_ID = "ID";
        //public static string col_LUFormStatus_Key = "Key";

        //PIWListHistory list
        //public static string PIWListHistoryListName = "PIWListHistory";
        //public static string col_PIWListHistory_Title = "Title";
        //public static string col_PIWListHistory_PIWList = "PIWList";
        //public static string col_PIWListHistory_User = "User";
        //public static string col_PIWListHistory_Action = "Action";
        //public static string col_PIWListHistory_FormStatus = "FormStatus";
        //public static string col_PIWListHistory_Created = "Created";
        //public static string col_PIWListHistory_CreatedBy = "Created By";

        

        //CitationNumber list
        //public static string CitationNumberListName = "CitationNumberList";
        //public static string col_CitationNumberList_PIWList = "PIWList";
        //public static string col_CitationNumberList_QuarterNumber = "QuarterNumber";
        //public static string col_CitationNumberList_DocumentTypeNumber = "DocumentTypeNumber";
        //public static string col_CitationNumberList_SequenceNumber = "SequenceNumber";
        //public static string col_CitationNumberList_Title = "Title";
        //public static string col_CitationNumberList_Status = "Status";
        //public static string col_CitationNumberList_AssignedDate = "AssignedDate";
        //public static string col_CitationNumberList_DeletedDate = "DeletedDate";
        //public static string col_CitationNumberList_ReAssignedDate = "ReAssignedDate";

        //LUFormType list
        //public static string LUFormTypeListName = "LUFormType";

        //ErrorLog list
        //public static string ErrorLogListName = "ErrorLog";
        //public static string col_ErrorLog_ListItem = "ListItem";
        //public static string col_ErrorLog_Docket = "Docket";
        //public static string col_ErrorLog_FormType = "FormType";
        //public static string col_ErrorLog_FormStatus = "FormStatus";
        //public static string col_ErrorLog_DocumentType = "DocumentType";
        //public static string col_ErrorLog_Office = "Office";
        //public static string col_ErrorLog_DocumentTitle = "DocumentTitle";
        //public static string col_ErrorLog_UserName = "UserName";
        //public static string col_ErrorLog_ErrorMessage = "ErrorMessage";
        //public static string col_ErrorLog_ErrorPageName = "ErrorPageName";

        #endregion
        ////FormStatus
        //public static string formStatusViewStateKey = "FormStatusKey";

        ////Previous Form Status
        //public static string previousFormStatusViewStateKey = "PreviousFormStatusKey";

        ////Document Title key
        //public static string DocumentTitleKey = "DocumentTitleKey";

        ////Document File name key
        //public static string DocumentFileNameKey = "DocumentFileNameKey";

        ////ViewModifiedDateTime key
        //public static string ViewModifiedDateTimeKey = "ViewModifiedDateTimeKey";

        ////group name
        //public static string Grp_PIWUsers = "PIWUsers";
        //public static string Grp_OSECGroupName = "PIWOSEC";
        //public static string Grp_SecretaryReviewGroupName = "PIWSecReview";
        //public static string Grp_PIWDirectPublication = "PIWDirectPublication";
        //public static string Grp_PIWDirectPublicationSubmitOnly = "PIWDirectPublicationSubmitOnly";
        //public static string Grp_PIWAdmin = "PIWAdmin";
        //public static string Grp_PIWAgendaNonManagement = "PIWAgendaNonManagement";
        //public static string Grp_PrintRequisitionSubmitter = "PrintRequisitionSubmitter";
        //public static string Grp_PrintRequisitionApproval = "PrintRequisitionApproval";
        //public static string Grp_PIWOSECFERCReports = "PIWOSECFERCReports";

        ////role
        //public static string Role_Contribute = "Contribute";
        //public static string Role_Read = "Read";

        ////page name
        ////this setting store the Page Name, used mainly in code to create URL
        ////So when we need to change Page (which is rarely happens), we just come and change this setting.
        ////This is relative location in combine with site root url
        //public static string Page_PIWForm = "PIWForm.aspx";
        //public static string Page_EditPIWForm = "EditPIWForm.aspx";
        //public static string Page_AgendaForm = "AgendaForm.aspx";
        //public static string Page_EditAgendaForm = "EditAgendaForm.aspx";
        //public static string Page_DirectPublicationForm = "DirectPublicationForm.aspx";
        //public static string Page_EditDirectPublicationForm = "EditDirectPublicationForm.aspx";
        //public static string Page_DocketSearch = "DocketSearch.aspx";
        //public static string Page_PublishItemsByDate = "PublishItemsByDate.aspx";
        //public static string Page_SiteAdmin = "SiteAdmin.aspx";
        //public static string Page_CitationNumberReport = "CitationNumberReport.aspx";
        //public static string Page_UnCompletedItemsReport = "UnCompletedItems.aspx";
        //public static string Page_OEPMailingRequired = "MailingRequired.aspx";
        //public static string Page_OEPMailingPending = "MailingPending.aspx";

        ////Citation Status 
        //public static string CitationNumber_ASSIGNED_Status = "assigned";
        //public static string CitationNumber_DELETED_Status = "deleted";
        //public static string CitationNumber_REASSIGNED_Status = "reassigned";

        ////ATMS Validation error
        //public static string ATMSRemotingServiceConnectionError = "Cannot connect to ATMS to validate docket.";

        //#region Print Requision
        ////Print Requisition
        //public static string PrintRequisitionListName = "PrintRequisition";
        //public static string col_PrintRequisition_FileName = "FileName";
        //public static string col_PrintRequisition_PrintStatus = "PrintStatus";
        //public static string col_PrintRequisition_CompletedDate = "CompletedDate";
        //public static string col_PrintRequisition_RefID = "RefID";
        //public static string col_PrintRequisition_IsActive = "IsActive";
        //public static string col_PrintRequisition_SubmittedDate = "SubmittedDate";
        //public static string col_PrintRequisition_DateRequired = "DateRequired";
        //public static string col_PrintRequisition_Office = "Office";
        //public static string col_PrintRequisition_Docket = "Docket";
        //public static string col_PrintRequisition_NumberOfPage = "NumberOfPage";
        //public static string col_PrintRequisition_NumberOfCopies = "NumberOfCopies";

        //public static string NewPrintPrequisitionURL = @"{0}/_layouts/FormServer.aspx?XsnLocation={1}/FormServerTemplates/PrintRequisition.xsn&DefaultItemOpen=1&RefID={2}";
        //#endregion

        //#region Publishing
        //public static string Affiliation_FirstName = "K";
        //public static string Affiliation_LastName = "Bose";
        //public static string Affiliation_MiddleInitial = "D";
        //public static string Affiliation_Organization = "SECRETARY OF THE COMMISSION, FERC";
        //public static string document_Availability = "P";
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

    