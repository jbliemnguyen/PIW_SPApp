using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PIW_SPAppWeb.Helper;

namespace PIW_SPAppTest
{
    [TestClass]
    public class FOLAMailingListTest
    {
        [TestMethod]
        public void getIncludeSenatorsParameterTest()
        {
            /**
            If "RM" and "P" docket --> True
            else If "EL"
	            if same fiscal year --> True ("but a docket: ER04-7 EL09-3 would get a ‘True’ value")
	            else (if different fiscal year)
		            if there is another docket --> true ("A lead EL docket stated with a different fiscal 							year would get a ‘True’ value also")

            other than above, false
             * */
            //this code dertermine if includesenator is true, default is false

            FOLAMailingList folaMailingList = new FOLAMailingList();
            
            //case 0: docket not {RM,P and EL} --> return false
            string docket = "ER15-1234,CP16-1234";
            Assert.IsFalse(folaMailingList.getIncludeSenatorsParameter(docket), docket + " should return false in IncludeSenator");

            
            //case 1: docket RM and P in any --> return true
            docket = "RM15-1234,EL16-1234";
            Assert.IsTrue(folaMailingList.getIncludeSenatorsParameter(docket),docket + " should return true in IncludeSenator");
            docket = "EL16-1234,RM15-1234";
            Assert.IsTrue(folaMailingList.getIncludeSenatorsParameter(docket), docket + " should return true in IncludeSenator");

            //P docket as start docket --> true
            docket = "P15-1234,EL16-1234";
            Assert.IsTrue(folaMailingList.getIncludeSenatorsParameter(docket), docket + " should return true in IncludeSenator");

            //P docket as sub-docket --> true
            docket = "EL16-1234,P15-1234";
            Assert.IsTrue(folaMailingList.getIncludeSenatorsParameter(docket), docket + " should return true in IncludeSenator");

            //P appear in other docket --> not P docket --> false
            docket = "CP16-1234,CP15-1234";
            Assert.IsFalse(folaMailingList.getIncludeSenatorsParameter(docket), docket + " should return false in IncludeSenator");

            //case 2: EL docket
            //EL same fiscal year --> true
            docket = "ER04-7,EL17-3";//this is example in document from Mellissa + Ken
            Assert.IsTrue(folaMailingList.getIncludeSenatorsParameter(docket), docket + " should return true in IncludeSenator");
            
            //EL different fiscal year,single docket --> false
            docket = "EL16-3";
            Assert.IsFalse(folaMailingList.getIncludeSenatorsParameter(docket), docket + " should return false in IncludeSenator");

            //EL different fiscal year,multiple docket --> true
            docket = "CP11-1234,EL16-3";
            Assert.IsTrue(folaMailingList.getIncludeSenatorsParameter(docket), docket + " should return true in IncludeSenator");
        }
    }
}
