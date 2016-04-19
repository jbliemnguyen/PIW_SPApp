using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PIW_SPAppWeb.Helper
{
    public class StandardFormWorkflow
    {
        public string Execute(string previousStatus, string currentStatus, enumAction action, bool isRequireOSECVerification, bool isRequiredPrePublication)
        {
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
                        nextStatus = Constants.PIWList_FormStatus_Submitted;
                    }
                    else if (action == enumAction.Save)
                    {
                        nextStatus = currentStatus;
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown combination of Action:{0} and Form Status:{1}",action,currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_Edited:
                    if (action == enumAction.Save)
                    {
                        nextStatus = previousStatus;
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown combination of Action:{0} and Form Status:{1}", action, currentStatus));
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
                            if (isRequiredPrePublication)
                            {
                                nextStatus = Constants.PIWList_FormStatus_PrePublication;
                            }
                            else
                            {
                                nextStatus = Constants.PIWList_FormStatus_ReadyForPublishing;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown combination of Action:{0} and Form Status:{1}", action, currentStatus));
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
                        if (isRequiredPrePublication)
                        {
                            nextStatus = Constants.PIWList_FormStatus_PrePublication;
                        }
                        else
                        {
                            nextStatus = Constants.PIWList_FormStatus_ReadyForPublishing;
                        }
                    }
                    else if (action == enumAction.Edit)
                    {
                        nextStatus = Constants.PIWList_FormStatus_Edited;
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown combination of Action:{0} and Form Status:{1}", action, currentStatus));
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
                        throw new Exception(string.Format("Unknown combination of Action:{0} and Form Status:{1}", action, currentStatus));
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
                        throw new Exception(string.Format("Unknown combination of Action:{0} and Form Status:{1}", action, currentStatus));
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishedToeLibrary:
                    if (action == enumAction.ReOpen)
                    {
                        nextStatus = Constants.PIWList_FormStatus_ReOpen;
                    }
                    break;
                case Constants.PIWList_FormStatus_PublishInitiated:
                    break;
                case Constants.PIWList_FormStatus_ReOpen:
                    //after ReOpen, automatic transfer to PrePublication
                    nextStatus = Constants.PIWList_FormStatus_PrePublication;
                    break;
                default: throw new Exception("Form Status unknown:" + currentStatus);
            }
            return nextStatus;
        }
    }

    public class AgendaFormWorkflow
    {
        
    }
}