using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SqlServer.Server;

namespace PIW_SPAppWeb.Helper
{
    public class StandardFormWorkflow
    {
        public string Execute(string previousStatus, string currentStatus, enumAction action, bool isRequireOSECVerification,
            string initiatorOffice, string documentCategory)
        {
            string errorMessage = "Standard WF Error - Unknown combination of Action:{0} and Form Status:{1}";
            string nextStatus = currentStatus;
            switch (currentStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Recalled:
                case Constants.PIWList_FormStatus_Rejected:
                    //in Pending, Recall, Reject
                    //user can only perform Submit action
                    if (action == enumAction.Submit)
                    {
                        //bypass OSEC Take OwnerShip
                        if (initiatorOffice.Equals(Constants.ddProgramOfficeWorkflowInitiator_Option_OSEC))
                        {
                            if (isRequireOSECVerification)
                            {
                                nextStatus = Constants.PIWList_FormStatus_OSECVerification;
                            }
                            else
                            {
                                nextStatus = Constants.PIWList_FormStatus_PrePublication;
                            }
                        }
                        else
                        {
                            nextStatus = Constants.PIWList_FormStatus_Submitted;
                        }

                    }
                    else if (action == enumAction.Save)
                    {
                        nextStatus = currentStatus;
                    }
                    else if (action == enumAction.Delete)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Deleted;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Edited:
                    if (action == enumAction.Save)
                    {
                        nextStatus = previousStatus;
                    }
                    else if (action == enumAction.Delete)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Deleted;
                    }
                    else if (action == enumAction.Accept)
                    {
                        if (previousStatus == Constants.PIWList_FormStatus_OSECVerification)
                        {
                            nextStatus = Constants.PIWList_FormStatus_PrePublication;
                        }
                        else if (previousStatus == Constants.PIWList_FormStatus_PrePublication)
                        {
                            nextStatus = Constants.PIWList_FormStatus_ReadyForPublishing;
                        }
                    }
                    else if (action == enumAction.Reject)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Rejected;
                    }
                    else if (action == enumAction.Publish)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PublishInitiated;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    //In Submitted status
                    //user can only perform Recall and OSECTakeOwnerShip action
                    if (action == enumAction.Recall)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Recalled;
                    }
                    else if (action == enumAction.OSECTakeOwnerShip)
                    {
                        if (isRequireOSECVerification)
                        {
                            nextStatus = Constants.PIWList_FormStatus_OSECVerification;
                        }
                        else
                        {
                            nextStatus = Constants.PIWList_FormStatus_PrePublication;
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_OSECVerification:
                    //In OSECVerification status
                    //user can only perform Reject, Accept and Edit action
                    if (action == enumAction.Reject)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Rejected;
                    }
                    else if (action == enumAction.Accept)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PrePublication;
                    }
                    else if (action == enumAction.Edit)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Edited;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PrePublication:
                    //In PrePublication status
                    //user can only perform Reject, Accept and Edit action
                    if (action == enumAction.Reject)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Rejected;
                    }
                    else if (action == enumAction.Accept)
                    {
                        nextStatus = Constants.PIWList_FormStatus_ReadyForPublishing;
                    }
                    else if (action == enumAction.Edit)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Edited;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    //In ReadyForPublishing status
                    //user can only perform Publish and Edit action
                    if (action == enumAction.Publish)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PublishInitiated;
                    }
                    else if (action == enumAction.Edit)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Edited;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    if (action == enumAction.ReOpen)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PrePublication;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    if (action == enumAction.Save)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PublishedToeLibrary;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }

                    break;
                default:
                    throw new Exception(string.Format(errorMessage, action, currentStatus));
            }
            return nextStatus;
        }
    }

    public class AgendaFormWorkflow
    {
        public string Execute(string previousStatus, string currentStatus, enumAction action)
        {
            string errorMessage = "Agenda Form WF Error - Unknown combination of Action:{0} and Form Status:{1}";
            string nextStatus = currentStatus;
            switch (currentStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_ReOpen:
                case Constants.PIWList_FormStatus_Recalled:
                case Constants.PIWList_FormStatus_Rejected:
                    //in Pending, Recall, Reject
                    //user can submit, save and delete
                    if (action == enumAction.Submit)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Submitted;
                    }
                    else if (action == enumAction.Save)
                    {
                        nextStatus = currentStatus;
                    }
                    else if (action == enumAction.Delete)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Deleted;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Edited:
                    //in edited, user can save and delete
                    if (action == enumAction.Save)
                    {
                        nextStatus = previousStatus;
                    }
                    else if (action == enumAction.Delete)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Deleted;
                    }
                    else if (action == enumAction.Accept)
                    {
                        nextStatus = Constants.PIWList_FormStatus_ReadyForPublishing;
                    }
                    else if (action == enumAction.Reject)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Rejected;
                    }
                    else if (action == enumAction.Publish)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PublishInitiated;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    //In Submitted status
                    //user can only perform Recall and Sec Review take ownership action
                    if (action == enumAction.Recall)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Recalled;
                    }
                    else if (action == enumAction.SecReviewTakeOwnerShip)
                    {
                        nextStatus = Constants.PIWList_FormStatus_SecretaryReview;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_SecretaryReview:
                    //In SecReview status
                    //user can reject, Accept and Edit action
                    if (action == enumAction.Reject)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Rejected;
                    }
                    else if (action == enumAction.Accept)
                    {
                        nextStatus = Constants.PIWList_FormStatus_ReadyForPublishing;
                    }
                    else if (action == enumAction.Edit)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Edited;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_ReadyForPublishing:
                    //In ReadyForPublishing status
                    //user can publish and Edit action
                    if (action == enumAction.Publish)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PublishInitiated;
                    }
                    else if (action == enumAction.Edit)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Edited;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    if (action == enumAction.ReOpen)
                    {
                        nextStatus = Constants.PIWList_FormStatus_ReOpen;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    if (action == enumAction.Save)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PublishedToeLibrary;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }

                    break;
                default:
                    throw new Exception(string.Format(errorMessage, action, currentStatus));

            }
            return nextStatus;
        }
    }

    public class DirectPublicationFormWorkflow
    {
        public string Execute(string currentStatus, enumAction action)
        {
            string errorMessage = "DirectPublication Form WF Error - Unknown combination of Action:{0} and Form Status:{1}";
            string nextStatus = currentStatus;
            switch (currentStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_ReOpen:
                    //in Pending, ReOpen
                    //user can only perform publish action
                    if (action == enumAction.Publish)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PublishInitiated;
                    }
                    else if (action == enumAction.Save)
                    {
                        nextStatus = currentStatus;
                    }
                    else if (action == enumAction.Delete)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Deleted;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;

                case Constants.PIWList_FormStatus_PublishInitiated:
                    if (action == enumAction.ReOpen)
                    {
                        nextStatus = Constants.PIWList_FormStatus_ReOpen;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    if (action == enumAction.Save)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PublishedToeLibrary;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }

                    break;
                default:
                    throw new Exception(string.Format(errorMessage, action, currentStatus));

            }
            return nextStatus;
        }
    }

    public class PrintReqFormWorkflow
    {
        public string Execute(string currentStatus, enumAction action)
        {
            string errorMessage = "Print Req Form WF Error - Unknown combination of Action:{0} and Form Status:{1}";
            string nextStatus = currentStatus;
            switch (currentStatus)
            {
                case Constants.PIWList_FormStatus_Pending:
                case Constants.PIWList_FormStatus_Rejected:
                    //in Pending, ReJected
                    //user can only perform submit action
                    if (action == enumAction.Submit)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Submitted;
                    }
                    else if (action == enumAction.Save)
                    {
                        //no change
                    }
                    else if (action == enumAction.Delete)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Deleted;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Submitted:
                    if (action == enumAction.Accept)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PrintReqAccepted;
                    }
                    else if (action == enumAction.Reject)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Rejected;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PrintReqAccepted:
                case Constants.PIWList_FormStatus_PrintJobCompleted:
                case Constants.PIWList_FormStatus_MailJobCompleted:
                    if (action == enumAction.Save)
                    {
                        //no change
                    }
                    else if (action == enumAction.PrintJobComplete)
                    {
                        nextStatus = Constants.PIWList_FormStatus_PrintJobCompleted;
                    }
                    else if (action == enumAction.MailJobComplete)
                    {
                        nextStatus = Constants.PIWList_FormStatus_MailJobCompleted;
                    }
                    else
                    {
                        throw new Exception(string.Format(errorMessage, action, currentStatus));
                    }
                    break;
                default:
                    throw new Exception(string.Format(errorMessage, action, currentStatus));
                    

            }
            return nextStatus;
        }
    }
}