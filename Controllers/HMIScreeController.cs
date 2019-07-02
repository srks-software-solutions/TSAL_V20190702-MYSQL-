using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Tata;
using Tata.Models;
using TataMySqlConnection;
using MySql.Data.MySqlClient;

namespace TATA.Controllers
{
    public class HMIScreeController : Controller
    {
        private mazakdaqEntities db = new mazakdaqEntities();

        public ActionResult SelectMachine()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int opid = Convert.ToInt32(Session["UserId"]);
            ViewBag.RHID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineInvNo");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectMachine(tblreportholder tbl)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.roleid = Session["RoleID"];

            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan End = StartTime.EndTime; // this is Shift End Time Specified
            TimeSpan EndTimeSpan = new TimeSpan(0, 0, 0); // 00:00:00 Normal day end time.
            TimeSpan TimeSpanNow = DateTime.Now.TimeOfDay;
            if (TimeSpanNow >= EndTimeSpan && TimeSpanNow <= End)
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }

            int MachineID = Convert.ToInt32(tbl.RHID);
            //Gatting UderID
            var User = db.tblusers.Where(m => m.MachineID == MachineID && m.IsDeleted == 0).FirstOrDefault();
            int opid = 0;
            if (User != null)
            {
                opid = User.UserID;
            }
            tbllivehmiscreen HMI = db.tbllivehmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).Where(m => m.MachineID == MachineID).FirstOrDefault();
            if (HMI == null)
            {
                
                tbllivehmiscreen tblhmiscreen = new tbllivehmiscreen();
                tblhmiscreen.MachineID = MachineID;
                tblhmiscreen.CorrectedDate = CorrectedDate;
                //tblhmiscreen.PEStartTime = DateTime.Now;
                //tblhmiscreen.Date = DateTime.Now;
                //tblhmiscreen.Time = DateTime.Now;
                tblhmiscreen.Shift = Convert.ToString(Session["shift"]);
                tblhmiscreen.Status = 0;
                tblhmiscreen.OperatiorID = opid;
                tblhmiscreen.isWorkInProgress = 2;
                //tblhmiscreen.HMIID = (HMMID.HMIID + 1); // by Ashok
                db.tbllivehmiscreens.Add(tblhmiscreen);
                db.SaveChanges();

                //tblhmiscreen tblhmiscreenSecondRow = new tblhmiscreen();
                //tblhmiscreenSecondRow.MachineID = MachineID;
                //tblhmiscreenSecondRow.CorrectedDate = CorrectedDate;
                //tblhmiscreenSecondRow.Date = DateTime.Now.Date;
                //tblhmiscreenSecondRow.Shift = Convert.ToString(Session["shift"]);
                //tblhmiscreenSecondRow.Status = 1;
                //tblhmiscreenSecondRow.OperatiorID = opid;
                //tblhmiscreenSecondRow.isWorkInProgress = 2;
                //tblhmiscreenSecondRow.Time = DateTime.Now.TimeOfDay;
                //db.tblhmiscreens.Add(tblhmiscreenSecondRow);
                //db.SaveChanges();
            }
            //HMIScreenForAdmin(MachineID, opid, CorrectedDate);
            Session["opid"] = opid;
            if (opid == 0)
            {
                Session["Error"] = "This machine is not Allocated to any Operator";
                return RedirectToAction("SelectMachine");
            }
            return RedirectToAction("HMIScreenForAdmin", "HMIScree", new { MachineID, opid, CorrectedDate });
            //return View(db.tblhmiscreens.Where(m => m.MachineID == MachineID && m.OperatiorID == opid).Where(m => m.Status != 2).Where(m => m.CorrectedDate == CorrectedDate).ToList());
        }

        public ActionResult HMIScreenForAdmin(int MachineID, int opid, string CorrectedDate)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            TempData["saveORUpdate"] = null;
            //Getting Shift Value

            DateTime Time = DateTime.Now;
            TimeSpan Tm = new TimeSpan(Time.Hour, Time.Minute, Time.Second);
            var ShiftDetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm && m.EndTime >= Tm);
            string Shift = null;
            foreach (var a in ShiftDetails)
            {
                Shift = a.ShiftName;
            }
            ViewBag.date = System.DateTime.Now;
            if (Shift != null)
                ViewBag.shift = Shift;
            else
                ViewBag.shift = "C";
            Shift = "C";

            //bool tick = checkingIdle();
            bool tick = true;
            //if (tick == true)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //}

            int handleidleReturnValue = HandleIdle();
            if (handleidleReturnValue == 0)
            {
                return RedirectToAction("DownCodeEntry");
            }

            tbluser tbl = db.tblusers.Find(opid);
            ViewBag.operatordisplay = tbl.DisplayName;
            ViewBag.machineID = Convert.ToInt32(MachineID);
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            Session["MchnID"] = MachineID;
            Session["Opid"] = opid;
            Session["realshift"] = Shift;
            string gshift = null;
            if (Session["gshift" + opid] != null)
                gshift = Session["gshift" + opid].ToString();

            #region old code
            //var data = db.tblhmiscreens.Where(m => m.MachineID == MachineID && m.OperatiorID == opid).Where(m => m.CorrectedDate == CorrectedDate).Where(m => m.Shift == gshift).ToList();
            //if (data.Count() != 0)
            //{
            //    ViewBag.shift = gshift;
            //    Session["realshift"] = gshift;
            //    if (Session["Show"] == null)
            //    {
            //        ViewBag.hide = 1;
            //    }
            //    else
            //    {
            //        ViewBag.hide = null;
            //    }
            //    tick = false;
            //    var data1 = db.tblhmiscreens.Where(m => m.MachineID == MachineID && m.OperatiorID == opid).Where(m => m.CorrectedDate == CorrectedDate).Where(m => m.Shift == gshift).OrderByDescending(u => u.HMIID).Take(1).ToList();
            //    foreach (var a in data1)
            //    {
            //        //ViewBag.shift = a.Shift;
            //        if (a.isUpdate == 1)
            //            tick = true;
            //    }
            //    if (tick)
            //    {
            //        TempData["saveORUpdate"] = 1;
            //    }
            //    return View(data1);
            //}
            //else
            //{
            //    tick = false;
            //    var data2 = db.tblhmiscreens.Where(m => m.MachineID == MachineID && m.OperatiorID == opid).Where(m => m.Status != 2).Where(m => m.CorrectedDate == CorrectedDate).OrderByDescending(u => u.HMIID).Take(2).ToList();
            //    foreach (var a in data2)
            //    {
            //        //ViewBag.shift = a.Shift;
            //        if (a.isUpdate == 1)
            //            tick = true;
            //    }
            //    if (tick)
            //    {
            //        TempData["saveORUpdate"] = 1;
            //    }
            //    return View(data2);
            //}

            #endregion

            var resumeWorkOrder = db.tbllivehmiscreens.Where(m => m.MachineID == MachineID && m.OperatiorID == opid).Where(m => m.CorrectedDate == CorrectedDate).OrderByDescending(m => m.HMIID).Take(1).ToList();
            if (resumeWorkOrder != null)
            {
                ViewBag.hide = 1;
            }
            ViewBag.ProdFAI = resumeWorkOrder[0].Prod_FAI;
            return View(resumeWorkOrder);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult UpdateData(IEnumerable<tbllivehmiscreen> tbldaily_plan, int Line1 = 0)
        public ActionResult HMIScreenForAdmin(IEnumerable<tbllivehmiscreen> tbldaily_plan, int Line1 = 0, String Shift = null, string hiddentextbox = null)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            DateTime presentdate = System.DateTime.Now.Date;
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"].ToString());
            ViewBag.DPIsMenu = 0;
            int opid = 0;
            int MachineID = 0;
            if (tbldaily_plan != null)
            {
                //if (ModelState.IsValid)
                {
                    int count = 0;
                    foreach (var plan in tbldaily_plan)
                    {



                        if (plan.Project != null || plan.Prod_FAI != null || plan.PartNo != null || plan.Work_Order_No != null || plan.OperationNo != null || plan.Work_Order_No != null || plan.Target_Qty.HasValue || plan.Rej_Qty.HasValue || plan.Delivered_Qty.HasValue)
                            plan.isUpdate = 1;
                        else
                            plan.isUpdate = 0;

                        opid = plan.OperatiorID;
                        MachineID = plan.MachineID;


                        if (count == 1)
                        {
                            //when Record WIP is clicked. => work is in progress.
                            plan.isWorkInProgress = 1;

                        }
                        plan.Time = DateTime.Now;
                        db.Entry(plan).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        count++;
                    }
                    return RedirectToAction("HMIScreenForAdmin", "HMIScree", new { MachineID, opid, CorrectedDate });
                }
            }
            return View();
        }

        // host Name: 10.30.10.57
        //port = 14

        public ActionResult Index(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            //Session["FromDDL"] = 0;
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int opid = Convert.ToInt32(Session["UserId"]);
            int ShiftID = 0;

            TempData["saveORUpdate"] = null;

            //Getting Shift Value
            #region
            DateTime Time = DateTime.Now;
            TimeSpan Tm = new TimeSpan(Time.Hour, Time.Minute, Time.Second);
            var ShiftDetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm && m.EndTime >= Tm);
            string Shift = null;
            foreach (var a in ShiftDetails)
            {
                Shift = a.ShiftName;
            }
            ViewBag.date = System.DateTime.Now;
            if (Shift != null)
            {
                ViewBag.shift = Shift;
                Session["shift"] = Shift;
            }
            else
            {
                ViewBag.shift = "C";
                Session["shift"] = "C";
                Shift = "C";
            }
            Session["realshift"] = Shift;
            if (Shift == "A")
                ShiftID = 1;
            else if (Shift == "B")
                ShiftID = 2;
            else
                ShiftID = 3;
            #endregion

            //Code For Admin And Super Admin
            int RoleID = Convert.ToInt32(Session["RoleID"]);
            if (RoleID == 1 || RoleID == 2)
            {
                return RedirectToAction("SelectMachine", "HMIScree", null);
            }
            ViewBag.roleid = Session["RoleID"];

            //code to get CorrectedDate
            #region
            string CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            if (DateTime.Now.Hour < 6 && DateTime.Now.Hour >= 0)
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            #endregion

            //extracting all machineID's 
            var MachineID = db.tblmachinedetails.Where(m => m.IsDeleted == 0);

            int Count = MachineID.Count();
            int[] machineID = new int[Count];
            int i = 0;
            foreach (var machine in MachineID)
            {
                machineID[i] = machine.MachineID;
                i++;
            }

            //int Machinid = machineID[0];
            int Machinid = Convert.ToInt32(Session["MachineID"]);

            //Checking operator machine is allocated or not
            var machineallocation = db.tblmachineallocations.Where(m => m.IsDeleted == 0 && m.CorrectedDate == CorrectedDate && m.UserID == opid && m.ShiftID == ShiftID);
            if (machineallocation.Count() != 0)
            {
                foreach (var a in machineallocation)
                {
                    Machinid = Convert.ToInt32(a.MachineID);
                    Session["MachineID"] = Machinid;
                }
            }

            #region OLD
            ////session["isWorkOrder"] value is set in ChooseWorkOrder action.
            //if (Convert.ToInt32(Session["isWorkOrder"]) == 1)
            //{
            //    Session["isWorkOrder"] = null;
            //    return RedirectToAction("WorkInProgressList", new { id = Machinid });
            //}

            //Based on the MachineAllocated to User insert 2 rows .When user enters the HMIScreen itself.
            //1 for Current Job
            //2 for Next Job

            //for (int j = 0; j < Count; j++)
            //{
            //    tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).Where(m => m.MachineID == Machinid).FirstOrDefault();
            //    if (HMI == null)
            //    {
            //        tblhmiscreen tblhmiscreen = new tblhmiscreen();
            //        tblhmiscreen.MachineID = Machinid;
            //        tblhmiscreen.CorrectedDate = CorrectedDate;
            //        tblhmiscreen.Date = DateTime.Now.Date;
            //        tblhmiscreen.Shift = Convert.ToString(ViewBag.shift);
            //        tblhmiscreen.Status = 0;
            //        tblhmiscreen.isWorkInProgress = 2;
            //        tblhmiscreen.isWorkOrder = Convert.ToInt32(Session["isWorkOrder"]);
            //        tblhmiscreen.OperatiorID = Convert.ToInt32(Session["UserId"]);
            //        tblhmiscreen.Time = DateTime.Now.TimeOfDay;
            //        db.tblhmiscreens.Add(tblhmiscreen);
            //        db.SaveChanges();

            //    }
            //}
            #endregion

            //insert a new row if there is no row for this machine for this date.
            // tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).Where(m => m.MachineID == Machinid && m.Status == 0).FirstOrDefault();
            //tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.Status == 0).OrderByDescending(m => m.HMIID).FirstOrDefault();

            //there was a problem where : After login in MWC and open another tab and you will get NWC for the same machine.
            //this used to give new row. So Below Validation.

            var MacDetails = db.tblmachinedetails.Where(m => m.MachineID == Machinid).FirstOrDefault();
            if (MacDetails.IsNormalWC == 1)
            {
                //Response.Redirect("~/ManualHMIScreen/Index",true);
                return RedirectToAction("Index", "ManualHMIScreen");
            }

            tbllivehmiscreen HMI = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid).OrderByDescending(m => m.HMIID).FirstOrDefault();
            if (HMI == null)
            {
                
                tbllivehmiscreen tblhmiscreen = new tbllivehmiscreen();
                tblhmiscreen.MachineID = Machinid;
                tblhmiscreen.CorrectedDate = CorrectedDate;
                tblhmiscreen.PEStartTime = DateTime.Now;
                tblhmiscreen.Shift = Convert.ToString(ViewBag.shift);
                tblhmiscreen.Status = 0;
                tblhmiscreen.isWorkInProgress = 2;
                tblhmiscreen.isWorkOrder = Convert.ToInt32(Session["isWorkOrder"]);
                tblhmiscreen.OperatiorID = Convert.ToInt32(Session["UserId"]);
               // tblhmiscreen.HMIID = (HMMID.HMIID + 1); // by Ashok
                db.tbllivehmiscreens.Add(tblhmiscreen);
                db.SaveChanges();

                Session["FromDDL"] = 0;
            }
            else
            {
                if (HMI.isWorkInProgress == 0 || HMI.isWorkInProgress == 1)
                {
                    
                    tbllivehmiscreen tblhmiscreen = new tbllivehmiscreen();
                    tblhmiscreen.MachineID = Machinid;
                    tblhmiscreen.CorrectedDate = CorrectedDate;
                    tblhmiscreen.PEStartTime = DateTime.Now;
                    //tblhmiscreen.Date = DateTime.Now;
              
                    //tblhmiscreen.Time = DateTime.Now;
                    tblhmiscreen.Shift = Convert.ToString(ViewBag.shift);
                    tblhmiscreen.Status = 0;
                    tblhmiscreen.isWorkInProgress = 2;
                    tblhmiscreen.isWorkOrder = Convert.ToInt32(Session["isWorkOrder"]);
                    tblhmiscreen.OperatiorID = Convert.ToInt32(Session["UserId"]);
                    //tblhmiscreen.HMIID = (HMMID.HMIID + 1); // by Ashok
                    db.tbllivehmiscreens.Add(tblhmiscreen);
                    db.SaveChanges();

                    Session["FromDDL"] = 0;
                }
                else if (HMI.CorrectedDate != CorrectedDate && HMI.Date == null)
                {
                    HMI.Shift = Convert.ToString(ViewBag.shift);
                    HMI.CorrectedDate = CorrectedDate;
                    HMI.PEStartTime = DateTime.Now;
                    db.Entry(HMI).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }

            ViewBag.DATE = DateTime.Now.Date;

            #region Old Code

            //tbllossofentry DowncodeEntryTime = db.tbllossofentries.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == Machinid && m.Shift == Shift && m.IsUpdate == 1).OrderByDescending(m => m.EntryTime).FirstOrDefault();
            //DateTime EnteryTime = DateTime.Now;
            ////check if any idle code has been entered for this particular Date, MachineID and Shift.
            ////if DowncodeEntryTime == null that means Idle Code has "NOT been Entered".
            ////Then we will check if dailyprodstatus, if yellow, go to DownCodeEntry View

            //if (DowncodeEntryTime != null)
            //{
            //    //Check if idle state is continuing
            //    EnteryTime = Convert.ToDateTime(DowncodeEntryTime.EntryTime);
            //    int TotalMinute = 0;
            //    TotalMinute = System.DateTime.Now.Subtract(EnteryTime).Minutes;
            //    #region DownColor

            //    if (TotalMinute >= 2)
            //    {
            //        var check1 = db.tbldailyprodstatus.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == Machinid && m.StartTime >= DowncodeEntryTime.EntryTime && m.ColorCode == "green").OrderBy(m => m.StartTime);
            //        //check1 picks latest green(production) that has been inserted after IdleCode "has been Entered"
            //        //It means Machine is in Production again.

            //        // Current shift != to shift when Idle Code was entered (or)
            //        // if non-green comes (no-Production) then go to downcodeEntry itself
            //        // if NOT update the tbllossofentry.EndDateTime

            //        if (Shift != DowncodeEntryTime.Shift || check1 != null)
            //        {
            //            #region DownColor
            //            int count = 0;
            //            int ContinuesChecking = 0;
            //            var productionstatus = db.tbldailyprodstatus.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == Machinid && m.StartTime >= DowncodeEntryTime.EntryTime).OrderByDescending(m => m.StartTime);
            //            foreach (var check in productionstatus)
            //            {
            //                if (check.ColorCode == "yellow")
            //                {
            //                    count++;
            //                    if (count == 1)
            //                    {
            //                        break;
            //                    }
            //                }
            //                else
            //                {
            //                    count = 0;
            //                }
            //                ContinuesChecking++;
            //            }
            //            if (count >= 1 && ContinuesChecking < 2)
            //            {
            //                return RedirectToAction("DownCodeEntry");
            //            }
            //            else //this has been added to handle Idle POPUP to be shown ONly once
            //            {
            //                Session["showIdlePopUp"] = 0;
            //            }
            //            #endregion
            //        }
            //        else
            //        {
            //            DowncodeEntryTime.EndDateTime = DateTime.Now;
            //            db.Entry(DowncodeEntryTime).State = System.Data.Entity.EntityState.Modified;
            //            db.SaveChanges();
            //        }
            //    }
            //    #endregion 
            //}
            //// if code not entered then this checks for yellow.
            //else
            //{
            //    #region DownColor
            //    int count = 0;
            //    int ContinuesChecking = 0;
            //    int n = 0;

            //    var productionstatus = db.tbldailyprodstatus.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == Machinid).OrderByDescending(m => m.StartTime);
            //    foreach (var check in productionstatus)
            //    {
            //        if (n == 0)
            //        {
            //            if (check.ColorCode == "green")
            //            {
            //                ContinuesChecking = 90;
            //            }
            //        }
            //        else if (check.ColorCode == "yellow")
            //        {
            //            count++;
            //            if (count == 1)
            //            {
            //                break;
            //            }
            //        }
            //        else
            //        {
            //            count = 0;
            //        }
            //        ContinuesChecking++;
            //        n++;
            //    }
            //    if (count >= 1 && ContinuesChecking < 2)
            //    {
            //        return RedirectToAction("DownCodeEntry");
            //    }
            //    else //this has been added to handle Idle POPUP to be shown ONly once
            //    {
            //        Session["showIdlePopUp"] = 0;
            //    }
            //    #endregion
            //}
            #endregion

            //To Get Error Message of PF.
            if (TempData["VError"] != null)
            {
                Session["Error"] = TempData["VError"];
                TempData["VError"] = null;
            }

            string ja = Convert.ToString(Session["showIdlePopUp"]);

            int handleidleReturnValue = HandleIdle();
            if (handleidleReturnValue == 0)
            {
                return RedirectToAction("DownCodeEntry");
            }

            ViewBag.machineID = Convert.ToInt32(Session["MachineID"]);

            tbluser tbl = db.tblusers.Find(opid);
            ViewBag.operatordisplay = tbl.DisplayName;

            //for setup mode
            var brkdown = db.tbllivelossofentries.Where(m => m.MachineID == Machinid).Where(m => m.CorrectedDate == CorrectedDate && m.EndDateTime == null && m.MessageCodeID == 81);
            if (brkdown.Count() != 0)
            {
                TempData["Enable"] = "Enable";
            }
            Session["MchnID"] = Machinid;
            Session["Opid"] = opid;
            Session["realshift"] = Convert.ToString(ViewBag.shift);
            string gshift = null;
            if (Session["gshift" + opid] != null)
                gshift = Session["gshift" + opid].ToString();


            //Code to resume particular workOrder 
            if (id != 0)
            {
                var resumeWorkOrder = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid && (m.HMIID == id)).Where(m => m.Status != 2).Where(m => m.CorrectedDate == CorrectedDate).Take(1).ToList();
                //ViewBag.shiftshivu = new SelectList(db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid && (m.HMIID == id)).Where(m => m.Status != 2).Where(m => m.CorrectedDate == CorrectedDate), "Shift", "Shift");
                #region commmented
                //if (resumeWorkOrder.Count == 1)
                //{
                //    int extrarowid = 0;
                //    var resumeworkExtraRow = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).OrderByDescending(m => m.HMIID).FirstOrDefault();
                //    if (resumeworkExtraRow != null)
                //    {
                //        extrarowid = resumeworkExtraRow.HMIID;
                //    }
                //    resumeWorkOrder = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid && (m.HMIID == id || m.HMIID == extrarowid)).Where(m => m.CorrectedDate == CorrectedDate).ToList();
                //}
                //else
                //{
                //    int extrarowid = 0;
                //    var resumeworkExtraRow = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).OrderByDescending(m => m.HMIID).FirstOrDefault();
                //    if (resumeworkExtraRow != null)
                //    {
                //        extrarowid = resumeworkExtraRow.HMIID;
                //    }
                //    resumeWorkOrder = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid && (m.HMIID == id || m.HMIID == extrarowid)).Where(m => m.CorrectedDate == CorrectedDate).ToList();
                //}
                //if (resumeWorkOrder[0].Status == resumeWorkOrder[1].Status)
                //{
                //    resumeWorkOrder[0].Status = 0;

                //}
                #endregion

                if (resumeWorkOrder.Count == 0)
                {
                    resumeWorkOrder = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid && (m.HMIID == id)).Where(m => m.CorrectedDate == CorrectedDate).ToList();
                    //ViewBag.shiftshivu = new SelectList(db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid && (m.HMIID == id)).Where(m => m.CorrectedDate == CorrectedDate), "Shift", "Shift");
                }
                ViewBag.ProdFAI = resumeWorkOrder[0].Prod_FAI;
                var j = ViewBag.shiftshivu;
                return View(resumeWorkOrder);
            }
            // used to work if shift setting was not allowed
            //else
            //{
            //    var resumeWorkOrder = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status != 2).Where(m => m.CorrectedDate == CorrectedDate).OrderByDescending(m => m.HMIID).Take(1).ToList();
            //    if (resumeWorkOrder != null)
            //    {
            //        ViewBag.hide = 1;
            //    }
            //    return View(resumeWorkOrder);
            //}

            #region . if data not selected.

            //var data = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status != 2).Where(m => m.CorrectedDate == CorrectedDate).Where(m => m.Shift == gshift).ToList();
            var data = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status == 0).Where(m => m.CorrectedDate == CorrectedDate).OrderByDescending(m => m.HMIID).FirstOrDefault();
            if (data != null)
            {
                //Default to Handle Manual/ScanEntry
                if (Convert.ToInt32(TempData["ForDDL2"]) == 2)
                {
                    Session["FromDDL"] = 2;
                    Session["SubmitClicked"] = 0;
                }

                int fromDDLInt = 6; //i am fake initializing to 6 , because i haven't used 6 anywhere.
                string blah = Convert.ToString(Session["FromDDL"]);
                int.TryParse(Convert.ToString(Session["FromDDL"]), out fromDDLInt);

                if (!string.IsNullOrEmpty(blah.Trim())) // Implies Session is Alive. Continous
                {
                    #region
                    if (fromDDLInt == 4) //implies that its a MultiWO & From DDL
                    {
                        if (data.Date == null) //Before Submit
                        {
                            Session["FromDDL"] = 4;
                            Session["SubmitClicked"] = 0;
                        }
                        else //After Submit
                        {
                            Session["FromDDL"] = 4;
                            Session["SubmitClicked"] = 1;
                        }
                    }
                    else if (fromDDLInt == 1)
                    {
                        if (data.Date == null) //Before Submit
                        {
                            Session["FromDDL"] = 1;
                            Session["SubmitClicked"] = 0;
                        }

                        else //After Submit
                        {
                            Session["FromDDL"] = 1;
                            Session["SubmitClicked"] = 1;
                        }
                    }
                    else if (fromDDLInt == 2) // Manual/ScanEntry
                    {
                        if (data.Date == null) //Before Submit
                        {
                            Session["FromDDL"] = 2;
                            Session["SubmitClicked"] = 0;
                        }
                        else //After Submit
                        {
                            Session["FromDDL"] = 0;
                            Session["SubmitClicked"] = 1;
                        }
                    }
                    else if (fromDDLInt == 0)
                    {
                        //Session["FromDDL"] = 0;
                        //Session["SubmitClicked"] = 0;
                        if (data.Date == null) //Before Submit
                        {
                            Session["FromDDL"] = 0;
                            Session["SubmitClicked"] = 0;
                        }
                        else //After Submit
                        {
                            Session["FromDDL"] = 0;
                            Session["SubmitClicked"] = 1;
                        }
                    }

                    #region Usefull Not Using
                    //if (data.Date == null) //Before Submit
                    //{
                    //    if(data.IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                    //    {
                    //        if (fromDDLInt == 4)
                    //        {
                    //            Session["FromDDL"] = 4;
                    //        }
                    //    }
                    //    else //Its a single WO
                    //    {
                    //        if (data.DDLWokrCentre != null) //Its a single WO from DDL
                    //        {

                    //        }
                    //        else //Its Manual Entry.
                    //        {

                    //        }

                    //    }

                    //    if (fromDDLInt == 1)
                    //    {
                    //        Session["FromDDL"] = 1;
                    //    }
                    //}
                    //else//After Submit
                    //{
                    //    if(data.IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                    //    {
                    //        if (fromDDLInt == 4)
                    //        {
                    //            Session["FromDDL"] = 4;
                    //            Session["SubmitClicked"] = 1;
                    //        }
                    //    }
                    //}
                    #endregion
                    #endregion

                }
                else //After Auto Logout or Session out.
                {
                    #region
                    if (data.Date == null) //Before Submit
                    {
                        if (data.IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                        {
                            Session["FromDDL"] = 4;
                        }
                        else //Its a single WO
                        {
                            string P = data.Project;
                            string wo = data.Work_Order_No;
                            string pno = data.PartNo;
                            string opno = data.OperationNo;

                            if (data.DDLWokrCentre != null) //Its a single WO from DDL
                            {
                                Session["FromDDL"] = 1;
                                Session["SubmitClicked"] = 0;
                            }
                            else if ((!string.IsNullOrEmpty(data.Project)) && (!string.IsNullOrEmpty(data.PartNo)) && (!string.IsNullOrEmpty(data.OperationNo)) && (!string.IsNullOrEmpty(data.Work_Order_No)) && (!string.IsNullOrEmpty(Convert.ToString(data.Target_Qty))))
                            {
                                Session["FromDDL"] = 1;
                            }
                            else if ((!string.IsNullOrEmpty(data.Project)) || (!string.IsNullOrEmpty(data.PartNo)) || (!string.IsNullOrEmpty(data.OperationNo)) || (!string.IsNullOrEmpty(data.Work_Order_No)))
                            {
                                Session["FromDDL"] = 2;
                            }
                            else
                            {
                                Session["FromDDL"] = 0;
                            }
                            //else //Its Manual Entry.
                            //{
                            //    Session["FromDDL"] = 2;
                            //}

                        }
                    }
                    else//After Submit
                    {
                        if (data.IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                        {
                            Session["FromDDL"] = 4;
                            Session["SubmitClicked"] = 1;
                        }
                        else //Its a single WO
                        {
                            Session["FromDDL"] = 1;
                            Session["SubmitClicked"] = 1;

                            //if (data.DDLWokrCentre != null) //Its a single WO from DDL
                            //{
                            //    Session["FromDDL"] = 1;
                            //    Session["SubmitClicked"] = 1;
                            //}
                            //else //Its Manual Entry.
                            //{
                            //    Session["FromDDL"] = 2;
                            //    Session["SubmitClicked"] = 1;
                            //}

                        }
                    }
                    #endregion

                }

                #region OLD Useful
                //if (data.Date != null && data.DDLWokrCentre != null && data.IsMultiWO == 1)
                //{
                //    Session["FromDDL"] = 4;
                //    Session["SubmitClicked"] = 1;
                //}
                //else if (data.Date != null && data.DDLWokrCentre != null && data.IsMultiWO == 0)
                //{
                //    Session["FromDDL"] = 4;
                //    Session["SubmitClicked"] = 1;
                //}
                //else if (data.Date != null && data.DDLWokrCentre == null)
                //{
                //    if (Convert.ToInt32(TempData["ForDDL2"]) == 2)
                //    {
                //        Session["FromDDL"] = 2;
                //        Session["SubmitClicked"] = null;
                //    }
                //    else
                //    {
                //        Session["FromDDL"] = 1;
                //        Session["SubmitClicked"] = 1;
                //    }
                //}

                //else if (data.Date == null && data.DDLWokrCentre != null && data.IsMultiWO == 0)
                //{
                //    Session["FromDDL"] = 1;
                //}
                //else if (data.Date == null && data.DDLWokrCentre != null && data.IsMultiWO == 1)
                //{
                //    Session["FromDDL"] = 4;
                //}
                //else if (data.Date == null && !(Convert.ToInt32(Session["FromDDL"]) == 2) && (data.PartNo != null || data.Project != null || data.OperationNo != null || data.Work_Order_No != null))
                //{
                //    Session["FromDDL"] = 2;
                //}
                //else if (data.Date == null && ((Convert.ToInt32(Session["FromDDL"]) == 2) || data.DDLWokrCentre != null))
                //{
                //    Session["FromDDL"] = 2;
                //}
                //else if (Convert.ToInt32(Session["FromDDL"]) == 1)
                //{
                //    Session["FromDDL"] = 1;
                //}
                ////else{
                ////    Session["SubmitClicked"] = 0;
                ////}
                #endregion

                ViewBag.shift = data.Shift;
                Session["realshift"] = data.Shift;
                if (data.isUpdate == 0)
                {
                    ViewBag.hide = null;
                }
                else
                {
                    ViewBag.hide = 1;
                }
                var data2 = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status == 0).Where(m => m.CorrectedDate == CorrectedDate).Where(m => m.Shift == data.Shift).OrderByDescending(u => u.HMIID).ToList();
                ViewBag.ProdFAI = data2[0].Prod_FAI;
                if (data2[0].Date != null)
                {
                    if (string.IsNullOrEmpty(data2[0].Project) || string.IsNullOrEmpty(data2[0].Prod_FAI) || string.IsNullOrEmpty(data2[0].PartNo) || string.IsNullOrEmpty(data2[0].Work_Order_No) || string.IsNullOrEmpty(data2[0].OperationNo) || string.IsNullOrEmpty(data2[0].Work_Order_No))
                    {
                        data2[0].Date = null;
                        db.Entry(data2[0]).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        Session["Error"] = "Please enter All Details Before Submit";
                        Session["FromDDL"] = 2;
                        Session["SubmitClicked"] = 0;
                    }
                    if (string.IsNullOrWhiteSpace(data2[0].Project) || string.IsNullOrWhiteSpace(data2[0].Prod_FAI) || string.IsNullOrWhiteSpace(data2[0].PartNo) || string.IsNullOrWhiteSpace(data2[0].Work_Order_No) || string.IsNullOrWhiteSpace(data2[0].OperationNo) || string.IsNullOrWhiteSpace(data2[0].Work_Order_No))
                    {
                        data2[0].Date = null;
                        db.Entry(data2[0]).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        Session["FromDDL"] = 2;
                        Session["SubmitClicked"] = 0;
                        Session["Error"] = "Please enter All Details Before Submit";
                    }
                    if ((!data2[0].Target_Qty.HasValue))
                    {
                        data2[0].Date = null;
                        db.Entry(data2[0]).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        Session["FromDDL"] = 2;
                        Session["SubmitClicked"] = 0;
                        Session["Error"] = "Please enter All Details Before Submit";
                    }
                }
                //if (data2[0].Date != null)
                //{
                //    if (string.IsNullOrEmpty(data2[0].Project) || string.IsNullOrEmpty(data2[0].Prod_FAI) || string.IsNullOrEmpty(data2[0].PartNo) || string.IsNullOrEmpty(data2[0].Work_Order_No) || string.IsNullOrEmpty(data2[0].OperationNo) || string.IsNullOrEmpty(data2[0].Work_Order_No))
                //    {
                //        Session["Error"] = "Please enter All Details Before Submit";
                //        Session["FromDDL"] = 2;
                //    }

                //    if (string.IsNullOrWhiteSpace(data2[0].Project) || string.IsNullOrWhiteSpace(data2[0].Prod_FAI) || string.IsNullOrWhiteSpace(data2[0].PartNo) || string.IsNullOrWhiteSpace(data2[0].Work_Order_No) || string.IsNullOrWhiteSpace(data2[0].OperationNo) || string.IsNullOrWhiteSpace(data2[0].Work_Order_No))
                //    {
                //        Session["FromDDL"] = 2;
                //        Session["Error"] = "Please enter All Details Before Submit";
                //    }

                //    //if ( (!plan.Target_Qty.HasValue) || (!plan.Rej_Qty.HasValue) || (!plan.Delivered_Qty.HasValue))
                //    if ((!data2[0].Target_Qty.HasValue) || (!data2[0].Rej_Qty.HasValue))
                //    {
                //        Session["FromDDL"] = 2;
                //        Session["Error"] = "Please enter All Details Before Submit";
                //    }
                //}
                return View(data2);
            }
            else
            {
                bool tick = false;
                //var data1 = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status != 2).Where(m => m.CorrectedDate == CorrectedDate).OrderByDescending(u => u.HMIID).ToList();
                //Working as of 2016-12-03 
                //var data1 = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status == 0).Where(m => m.CorrectedDate == CorrectedDate).OrderByDescending(u => u.HMIID).ToList();
                var data1 = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid).Where(m => m.Status == 0).OrderByDescending(u => u.HMIID).Take(1).ToList();
                foreach (var a in data1)
                {
                    //ViewBag.shift = a.Shift;
                    if (a.isUpdate == 1)
                        tick = true;
                }
                if (tick)
                {
                    TempData["saveORUpdate"] = 1;
                }

                //Default to Handle Manual/ScanEntry
                if (Convert.ToInt32(TempData["ForDDL2"]) == 2)
                {
                    Session["FromDDL"] = 2;
                    Session["SubmitClicked"] = 0;
                }

                int fromDDLInt = 6; //i am fake initializing to 6 , because i haven't used 6 anywhere.
                string blah = Convert.ToString(Session["FromDDL"]);
                int.TryParse(Convert.ToString(Session["FromDDL"]), out fromDDLInt);

                if (!string.IsNullOrEmpty(blah.Trim())) // Implies Session is Alive. Continous
                {
                    #region
                    if (fromDDLInt == 4) //implies that its a MultiWO & From DDL
                    {
                        if (data1[0].Date == null) //Before Submit
                        {
                            Session["FromDDL"] = 4;
                            Session["SubmitClicked"] = 0;
                        }
                        else //After Submit
                        {
                            Session["FromDDL"] = 4;
                            Session["SubmitClicked"] = 1;
                        }
                    }
                    else if (fromDDLInt == 1)
                    {
                        if (data1[0].Date == null) //Before Submit
                        {
                            Session["FromDDL"] = 1;
                            Session["SubmitClicked"] = 0;
                        }
                        else //After Submit
                        {
                            Session["FromDDL"] = 1;
                            Session["SubmitClicked"] = 1;
                        }
                    }
                    else if (fromDDLInt == 2) // Manual/ScanEntry
                    {
                        if (data1[0].Date == null) //Before Submit
                        {
                            Session["FromDDL"] = 2;
                            Session["SubmitClicked"] = 0;
                        }
                        else //After Submit
                        {
                            Session["FromDDL"] = 0;
                            Session["SubmitClicked"] = 1;
                        }
                    }
                    else if (fromDDLInt == 0)
                    {
                        Session["FromDDL"] = 0;
                        Session["SubmitClicked"] = 0;
                    }

                    #region Usefull Not Using
                    //if (data.Date == null) //Before Submit
                    //{
                    //    if(data.IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                    //    {
                    //        if (fromDDLInt == 4)
                    //        {
                    //            Session["FromDDL"] = 4;
                    //        }
                    //    }
                    //    else //Its a single WO
                    //    {
                    //        if (data.DDLWokrCentre != null) //Its a single WO from DDL
                    //        {

                    //        }
                    //        else //Its Manual Entry.
                    //        {

                    //        }

                    //    }

                    //    if (fromDDLInt == 1)
                    //    {
                    //        Session["FromDDL"] = 1;
                    //    }
                    //}
                    //else//After Submit
                    //{
                    //    if(data.IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                    //    {
                    //        if (fromDDLInt == 4)
                    //        {
                    //            Session["FromDDL"] = 4;
                    //            Session["SubmitClicked"] = 1;
                    //        }
                    //    }
                    //}
                    #endregion
                    #endregion

                }
                else //After Auto Logout or Session out.
                {
                    #region
                    if (data1[0].Date == null) //Before Submit
                    {
                        if (data1[0].IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                        {
                            Session["FromDDL"] = 4;
                        }
                        else //Its a single WO
                        {
                            if (data1[0].DDLWokrCentre != null) //Its a single WO from DDL
                            {
                                Session["FromDDL"] = 1;
                                Session["SubmitClicked"] = 0;
                            }
                            else if ((!string.IsNullOrEmpty(data1[0].Project)) && (!string.IsNullOrEmpty(data1[0].Work_Order_No)) && (!string.IsNullOrEmpty(data1[0].OperationNo)) && (!string.IsNullOrEmpty(data1[0].PartNo)) && (!string.IsNullOrEmpty(Convert.ToString(data1[0].Target_Qty))))
                            {
                                Session["FromDDL"] = 1;
                            }
                            else if ((!string.IsNullOrEmpty(data1[0].Project)) || (!string.IsNullOrEmpty(data1[0].Work_Order_No)) || (!string.IsNullOrEmpty(data1[0].OperationNo)) || (!string.IsNullOrEmpty(data1[0].PartNo)))//Its Manual Entry.
                            {
                                Session["FromDDL"] = 2;
                            }
                            else
                            {
                                Session["FromDDL"] = 0;
                            }

                        }
                    }
                    else//After Submit
                    {
                        if (data1[0].IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                        {
                            Session["FromDDL"] = 4;
                            Session["SubmitClicked"] = 1;
                        }
                        else //Its a single WO
                        {
                            //Its a single WO from DDL or Manual Entry(AfterSubmit) . They are same.
                            Session["FromDDL"] = 1;
                            Session["SubmitClicked"] = 1;


                            //if (data1[0].DDLWokrCentre != null)
                            //{
                            //    Session["FromDDL"] = 1;
                            //    Session["SubmitClicked"] = 1;
                            //}
                            //else //Its Manual Entry.
                            //{
                            //    Session["FromDDL"] = 2;
                            //    Session["SubmitClicked"] = 1;
                            //}
                        }
                    }
                    #endregion

                }


                #region OLD Usefull
                //if (data1[0].Date != null && data1[0].IsMultiWO == 1)
                //{
                //    Session["FromDDL"] = 4;
                //    Session["SubmitClicked"] = 1;
                //}
                //else if (data1[0].Date != null && data1[0].IsMultiWO == 0)
                //{
                //    if (Convert.ToInt32(TempData["ForDDL2"]) == 2)
                //    {
                //        Session["FromDDL"] = 2;
                //        Session["SubmitClicked"] = null;
                //    }
                //    else
                //    {
                //        Session["FromDDL"] = 1;
                //        Session["SubmitClicked"] = 1;
                //    }
                //}
                //else if (data1[0].Date == null && data1[0].IsMultiWO == 0)
                //{
                //    Session["FromDDL"] = 1;
                //}
                #endregion

                if (data1[0].Date != null)
                {
                    if (string.IsNullOrEmpty(data1[0].Project) || string.IsNullOrEmpty(data1[0].Prod_FAI) || string.IsNullOrEmpty(data1[0].PartNo) || string.IsNullOrEmpty(data1[0].Work_Order_No) || string.IsNullOrEmpty(data1[0].OperationNo) || string.IsNullOrEmpty(data1[0].Work_Order_No))
                    {
                        data1[0].Date = null;
                        db.Entry(data1[0]).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                    if (string.IsNullOrWhiteSpace(data1[0].Project) || string.IsNullOrWhiteSpace(data1[0].Prod_FAI) || string.IsNullOrWhiteSpace(data1[0].PartNo) || string.IsNullOrWhiteSpace(data1[0].Work_Order_No) || string.IsNullOrWhiteSpace(data1[0].OperationNo) || string.IsNullOrWhiteSpace(data1[0].Work_Order_No))
                    {
                        data1[0].Date = null;
                        db.Entry(data1[0]).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }

                if (data1[0].Date != null)
                {
                    if (string.IsNullOrEmpty(data1[0].Project) || string.IsNullOrEmpty(data1[0].Prod_FAI) || string.IsNullOrEmpty(data1[0].PartNo) || string.IsNullOrEmpty(data1[0].Work_Order_No) || string.IsNullOrEmpty(data1[0].OperationNo) || string.IsNullOrEmpty(data1[0].Work_Order_No))
                    {
                        data1[0].Date = null;
                        db.Entry(data1[0]).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        Session["Error"] = "Please enter All Details Before Submit";
                        Session["FromDDL"] = 2;
                        Session["SubmitClicked"] = 0;
                    }
                    if (string.IsNullOrWhiteSpace(data1[0].Project) || string.IsNullOrWhiteSpace(data1[0].Prod_FAI) || string.IsNullOrWhiteSpace(data1[0].PartNo) || string.IsNullOrWhiteSpace(data1[0].Work_Order_No) || string.IsNullOrWhiteSpace(data1[0].OperationNo) || string.IsNullOrWhiteSpace(data1[0].Work_Order_No))
                    {
                        data1[0].Date = null;
                        db.Entry(data1[0]).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        Session["FromDDL"] = 2;
                        Session["SubmitClicked"] = 0;
                        Session["Error"] = "Please enter All Details Before Submit";
                    }
                    //if ( (!plan.Target_Qty.HasValue) || (!plan.Rej_Qty.HasValue) || (!plan.Delivered_Qty.HasValue))
                    if ((!data1[0].Target_Qty.HasValue) || (!data1[0].Rej_Qty.HasValue))
                    {
                        data1[0].Date = null;
                        db.Entry(data1[0]).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        Session["FromDDL"] = 2;
                        Session["SubmitClicked"] = 0;
                        Session["Error"] = "Please enter All Details Before Submit";
                    }
                }


                ViewBag.ProdFAI = data1[0].Prod_FAI;
                return View(data1);
            }

            #endregion
        }

        //Control comes here when submit button is clicked.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult UpdateData(IEnumerable<tblHMIScreen> tbldaily_plan, int Line1 = 0)
        public ActionResult Index(IList<tblhmiscreen> tbldaily_plan, int Line1 = 0, string hiddentextbox = null)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            #region OLD NOT USING
            //if (hiddentextbox != null)
            //{
            //    string newval = null;
            //    var getNumbers = (from t in hiddentextbox
            //                      where char.IsDigit(t)
            //                      select t).ToArray();

            //    int n = getNumbers.Count();
            //    for (int p = 0; p < n; p++)
            //    {
            //        newval = newval + getNumbers[p];
            //    }
            //    int a = Convert.ToInt32(newval) + 1;
            //    string b = "cjtextbox" + a;
            //    Session["valOfHidden"] = b;
            //}
            //else {
            //    Session["valOfHidden"] = null;
            //}
            #endregion

            DateTime presentdate = System.DateTime.Now.Date;
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"].ToString());
            int machineID = Convert.ToInt32(Session["MachineID"]);
            ViewBag.DPIsMenu = 0;
            if (tbldaily_plan != null)
            {
                //if (ModelState.IsValid)
                {

                    //Check if this WONo, PartNo, OpNo was Previously JF'ed
                    bool isNoDuplicate = false;
                    foreach (var DDLrow in tbldaily_plan)
                    {
                        int isMultiWO = DDLrow.IsMultiWO;
                        if (isMultiWO == 1)
                        {
                            int HMIID = DDLrow.HMIID;
                            var multiWOData = db.tbllivemultiwoselections.Where(m => m.HMIID == HMIID).ToList();
                            foreach (var multiworow in multiWOData)
                            {
                                int PrvProcessQty = 0, PrvDeliveredQty = 0;
                                String WONo = multiworow.WorkOrder;
                                String Part = multiworow.PartNo;
                                String Operation = multiworow.OperationNo;

                                //check both in tblhmiscreen and tbl_multiwoselection tables.
                                var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 1).FirstOrDefault();
                                if (hmiData != null)
                                {
                                    Session["Error"] = "It's already JobFinished for WorkOrder:" + WONo + " OpNo: " + Operation;
                                    DDLrow.Prod_FAI = null;
                                    DDLrow.Target_Qty = null;
                                    DDLrow.OperationNo = null;
                                    DDLrow.PartNo = null;
                                    DDLrow.Work_Order_No = null;
                                    DDLrow.Project = null;
                                    DDLrow.Date = null;
                                    DDLrow.DDLWokrCentre = null;
                                    DDLrow.ProcessQty = 0;
                                    Session["FromDDL"] = 2;
                                    TempData["ForDDL2"] = 2;
                                    db.Entry(DDLrow).State = System.Data.Entity.EntityState.Modified;
                                    db.SaveChanges();
                                    isNoDuplicate = true;
                                    break;
                                }

                                var multiwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation && m.IsCompleted == 1).FirstOrDefault();
                                if (multiwoData != null)
                                {
                                    Session["Error"] = "It's already JobFinished for WorkOrder:" + WONo + " OpNo: " + Operation;
                                    DDLrow.Prod_FAI = null;
                                    DDLrow.Target_Qty = null;
                                    DDLrow.OperationNo = null;
                                    DDLrow.PartNo = null;
                                    DDLrow.Work_Order_No = null;
                                    DDLrow.Project = null;
                                    DDLrow.Date = null;
                                    DDLrow.DDLWokrCentre = null;
                                    DDLrow.ProcessQty = 0;
                                    Session["FromDDL"] = 2;
                                    TempData["ForDDL2"] = 2;
                                    db.Entry(DDLrow).State = System.Data.Entity.EntityState.Modified;
                                    db.SaveChanges();
                                    isNoDuplicate = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (isNoDuplicate)
                    {
                        return RedirectToAction("Index");
                    }

                    List<int> data = new List<int>();
                    bool isSingleWo = true;
                    if (tbldaily_plan[0].IsMultiWO == 1)
                    {
                        isSingleWo = false;
                    }

                    if (isSingleWo)
                    {
                        string WONo = tbldaily_plan[0].Work_Order_No;
                        string Part = tbldaily_plan[0].PartNo;
                        string Operation = tbldaily_plan[0].OperationNo;

                        //OpNo sequence
                        #region 2017-02-07
                        bool IsInHMI = false;
                        var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONo && m.MaterialDesc == Part && m.OperationNo != Operation && m.IsCompleted == 0).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                        foreach (var row in Siblingddldata)
                        {
                            IsInHMI = true; //reinitialize
                            int localOPNo = Convert.ToInt32(row.OperationNo);
                            string localOPNoString = Convert.ToString(row.OperationNo);
                            if (localOPNo < Convert.ToInt32(Operation))
                            {
                                #region //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                                //which case is valid.
                                var SiblingHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == localOPNoString).FirstOrDefault();
                                var SiblingHMIdatahistorian = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == localOPNoString).FirstOrDefault(); //added by Ashok
                                if (SiblingHMIdata == null)// || SiblingHMIdatahistorian==null)
                                {

                                    Session["VError"] = "Please Select Below WorkOrder, WONo: " + WONo + " PartNo: " + Part + " OperationNo: " + localOPNo;
                                    IsInHMI = false;
                                    //break;
                                }
                                else
                                {
                                    if (SiblingHMIdata.Date == null)// || SiblingHMIdatahistorian.Date==null) //=> lower OpNo is not submitted.
                                    {

                                        Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " && OperationNo: " + localOPNoString;
                                        //return RedirectToAction("Index");
                                        IsInHMI = false;
                                        break;
                                    }
                                  
                                    else
                                    {
                                        IsInHMI = true;
                                        Session["VError"] = null;
                                    }
                                }
                                #region ForHistorian
                                if (SiblingHMIdatahistorian == null)// || SiblingHMIdatahistorian==null)
                                {

                                    Session["VError"] = "Please Select Below WorkOrder, WONo: " + WONo + " PartNo: " + Part + " OperationNo: " + localOPNo;
                                    IsInHMI = false;
                                    //break;
                                }
                                else
                                {
                                    if (SiblingHMIdatahistorian.Date == null)// || SiblingHMIdatahistorian.Date==null) //=> lower OpNo is not submitted.
                                    {

                                        Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " && OperationNo: " + localOPNoString;
                                        //return RedirectToAction("Index");
                                        IsInHMI = false;
                                        break;
                                    }

                                    else
                                    {
                                        IsInHMI = true;
                                        Session["VError"] = null;
                                    }
                                }
                                #endregion
                                #endregion

                                if (!IsInHMI)
                                {
                                    #region //also check in MultiWO table
                                    string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                    var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();
                                    string WIPQueryMultiWOHistorian = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                    var WIPMWOHistorian = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWOHistorian).ToList();
                                    if (WIPMWO.Count == 0 || WIPMWOHistorian.Count==0)
                                    {
                                        Session["VError"] = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                        return RedirectToAction("Index");
                                        //IsInHMI = false;
                                        //break;
                                    }

                                    foreach (var rowHMI in WIPMWO)
                                    {
                                        int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                        var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                        if (MWOHMIData != null) //obviously != 0
                                        {
                                            if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                return RedirectToAction("Index");
                                                //break;
                                            }
                                            else
                                            {
                                                Session["VError"] = null;
                                            }
                                        }
                                    }
                                    // For Historian 
                                    foreach (var rowHMI in WIPMWOHistorian)
                                    {
                                        int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                        var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                        if (MWOHMIData != null) //obviously != 0
                                        {
                                            if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                return RedirectToAction("Index");
                                                //break;
                                            }
                                            else
                                            {
                                                Session["VError"] = null;
                                            }
                                        }
                                    }
                                    #endregion

                                    
                                }
                                else
                                {
                                    Session["VError"] = null;
                                    //continue with other execution
                                    //return RedirectToAction("Index");
                                }
                            }
                        }

                        //Commented On 2017-05-29
                        /////to Catch those Manual WorkOrders 
                        //string WIPQuery1 = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo != '" + Operation + "' and  IsMultiWO = 0 and DDLWokrCentre is null order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo ) order by OperationNo ;";
                        //var WIPDDL1 = db.tblhmiscreens.SqlQuery(WIPQuery1).ToList();
                        //foreach (var row in WIPDDL1)
                        //{
                        //    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                        //    if (InnerOpNo < Convert.ToInt32(Operation))
                        //    {
                        //        string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
                        //        var WIP1 = db.tblhmiscreens.SqlQuery(WIPQueryHMI).ToList();
                        //        if (WIP1.Count == 0)
                        //        {
                        //            Session["VError"] = " Select & Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                        //            return RedirectToAction("Index");
                        //        }
                        //        foreach (var rowHMI in WIP1)
                        //        {
                        //            if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                        //            {
                        //                Session["VError"] = " Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                        //                return RedirectToAction("Index");
                        //            }
                        //        }
                        //    }
                        //}
                        #endregion
                    }
                    else
                    {
                        //Collect the DDLIDs
                        int hmiid = tbldaily_plan[0].HMIID;
                        var multiWOData = db.tbllivemultiwoselections.Where(m => m.HMIID == hmiid).ToList();
                        foreach (var DDLrow in multiWOData)
                        {
                            string partno = DDLrow.PartNo;
                            string wono = DDLrow.WorkOrder;
                            string opno = DDLrow.OperationNo;

                            var ddldata = db.tblddls.Where(m => m.MaterialDesc == partno && m.WorkOrder == wono && m.OperationNo == opno && m.IsCompleted == 0).FirstOrDefault();
                            if (ddldata != null)
                            {
                                data.Add(ddldata.DDLID);
                            }

                        }
                        string DDLIDString = string.Join(",", data.Select(x => x.ToString()).ToArray());
                        #region 2017-02-07
                        foreach (var DDLID in data)
                        {
                            //int DDLID = DDLRow;
                            var ddldataInner = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                            String SplitWOInner = ddldataInner.SplitWO;
                            String WONoInner = ddldataInner.WorkOrder;
                            String PartInner = ddldataInner.MaterialDesc;
                            String OperationInner = ddldataInner.OperationNo;

                            bool IsInHMI = true;
                            var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONoInner && m.MaterialDesc == PartInner && m.OperationNo != OperationInner && m.IsCompleted == 0).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                            foreach (var row in Siblingddldata)
                            {
                                string localddlid = Convert.ToString(row.DDLID);
                                int localOPNo = Convert.ToInt32(row.OperationNo);
                                string localOPNoString = Convert.ToString(row.OperationNo);
                                if (localOPNo < Convert.ToInt32(OperationInner))
                                {
                                    if (DDLIDString.Contains(localddlid))
                                    { }
                                    else
                                    {
                                        //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                                        //which case is valid.
                                        var SiblingHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == localOPNoString).FirstOrDefault();
                                        var SiblingHMIdatahistorian = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == localOPNoString).FirstOrDefault(); //added by Ashok
                                        if (SiblingHMIdata == null || SiblingHMIdatahistorian==null)
                                        {
                                            Session["VError"] = "Please Select Below WorkOrder , WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + localOPNo;
                                            //isValid = false;
                                            //break;
                                            IsInHMI = false;
                                        }
                                        else
                                        {
                                            if (SiblingHMIdata.Date == null)// || SiblingHMIdatahistorian.Date==null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                //return RedirectToAction("Index");
                                                IsInHMI = false;
                                                //break;
                                            }
                                            if(SiblingHMIdatahistorian!=null)
                                            {
                                                if(SiblingHMIdatahistorian.Date == null)
                                                {
                                                    Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                    //return RedirectToAction("Index");
                                                    IsInHMI = false;
                                                }
                                            }
                                        }

                                        if (!IsInHMI)
                                        {
                                            //also check in MultiWO table
                                            string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + WONoInner + "' and PartNo = '" + PartInner + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                            var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();
                                            string WIPQueryMultiWOHistorian = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + WONoInner + "' and PartNo = '" + PartInner + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                            var WIPMWOHistorian = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWOHistorian).ToList();
                                            if (WIPMWO.Count == 0 || WIPMWOHistorian.Count==0)
                                            {
                                                Session["VError"] = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                return RedirectToAction("Index");
                                                //IsInHMI = false;
                                                //break;
                                            }
                                            foreach (var rowHMI in WIPMWO)
                                            {
                                                int hmiidInner = Convert.ToInt32(rowHMI.HMIID);
                                                var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiidInner).FirstOrDefault();
                                                if (MWOHMIData != null)
                                                {
                                                    if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                                    {
                                                        Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                        return RedirectToAction("Index");
                                                        //break;
                                                    }
                                                    else
                                                    {
                                                        IsInHMI = true;
                                                        Session["VError"] = null;
                                                    }
                                                }
                                            }
                                            //For Historian
                                            foreach (var rowHMI in WIPMWOHistorian)
                                            {
                                                int hmiidInner = Convert.ToInt32(rowHMI.HMIID);
                                                var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiidInner).FirstOrDefault();
                                                if (MWOHMIData != null)
                                                {
                                                    if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                                    {
                                                        Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                        return RedirectToAction("Index");
                                                        //break;
                                                    }
                                                    else
                                                    {
                                                        IsInHMI = true;
                                                        Session["VError"] = null;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Session["VError"] = null;
                                            //continue with other execution
                                        }

                                    }
                                }
                            }
                        }
                        #endregion

                    }

                    int count = 0;
                    foreach (var plan in tbldaily_plan)
                    {
                        #region Not Using
                        //if (string.IsNullOrEmpty(plan.Project) || string.IsNullOrEmpty(plan.Prod_FAI) || string.IsNullOrEmpty(plan.PartNo) || string.IsNullOrEmpty(plan.Work_Order_No) || string.IsNullOrEmpty(plan.OperationNo) || string.IsNullOrEmpty(plan.Work_Order_No))
                        //{
                        //    Session["Error"] = "Please enter All Details Before Submit";
                        //    Session["FromDDL"] = 2;
                        //    TempData["ForDDL2"] = 2;
                        //    return RedirectToAction("Index");
                        //}

                        //if (string.IsNullOrWhiteSpace(plan.Project) || string.IsNullOrWhiteSpace(plan.Prod_FAI) || string.IsNullOrWhiteSpace(plan.PartNo) || string.IsNullOrWhiteSpace(plan.Work_Order_No) || string.IsNullOrWhiteSpace(plan.OperationNo) || string.IsNullOrWhiteSpace(plan.Work_Order_No))
                        //{
                        //    Session["FromDDL"] = 2;
                        //    TempData["ForDDL2"] = 2;
                        //    Session["Error"] = "Please enter All Details Before Submit";
                        //    return RedirectToAction("Index");
                        //}

                        ////if ( (!plan.Target_Qty.HasValue) || (!plan.Rej_Qty.HasValue) || (!plan.Delivered_Qty.HasValue))
                        //if ((!plan.Target_Qty.HasValue) || (!plan.Rej_Qty.HasValue))
                        //{
                        //    Session["FromDDL"] = 2;
                        //    TempData["ForDDL2"] = 2;
                        //    Session["Error"] = "Please enter All Details Before Submit";
                        //    return RedirectToAction("Index");
                        //}
                        #endregion

                        if (plan.Project != null || plan.Prod_FAI != null || plan.PartNo != null || plan.Work_Order_No != null || plan.OperationNo != null || plan.Work_Order_No != null || plan.Target_Qty.HasValue || plan.Rej_Qty.HasValue || plan.Delivered_Qty.HasValue)
                            plan.isUpdate = 1;
                        else
                            plan.isUpdate = 0;

                        if (count == 1 && hiddentextbox != "autosave")
                        {
                            //when Record WIP is clicked. => work is in progress.
                            plan.isWorkInProgress = 0;
                        }
                        int hmiid = plan.HMIID;
                        tbllivehmiscreen hmiidData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();

                        hmiidData.Date = DateTime.Now;
                        hmiidData.OperatorDet = plan.OperatorDet;
                        hmiidData.Shift = plan.Shift;
                        hmiidData.Prod_FAI = plan.Prod_FAI;

                        string woNo = Convert.ToString(hmiidData.Work_Order_No);
                        string opNo = Convert.ToString(hmiidData.OperationNo);
                        string partNo = Convert.ToString(hmiidData.PartNo);
                        int deliveredQty = 0;
                        int TargetQtyNew = Convert.ToInt32(hmiidData.Target_Qty);
                        int isMultiWO = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).Select(m => m.IsMultiWO).FirstOrDefault();
                        int PrvProcessQty = 0, PrvDeliveredQty = 0;
                        if (isMultiWO == 0)
                        {
                            #region
                            //var getProcessQty = db.tblhmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo).OrderByDescending(m => m.HMIID).Take(2).ToList();
                            ////var getProcessQty = db.tblhmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo).OrderByDescending(m => m.Date).Take(2).ToList(); //2017-05-11
                            //if (getProcessQty.Count == 2)
                            //{
                            //    #region new code

                            //    //here  get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                            //    int isHMIFirst = 2; //default NO History for that wo,pn,on

                            //    var mulitwoData = db.tbl_multiwoselection.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo).OrderByDescending(m => m.MultiWOID).Take(1).ToList();
                            //    //var hmiData = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 0).OrderByDescending(m => m.HMIID).Take(1).ToList();

                            //    if (getProcessQty.Count == 2 && mulitwoData.Count > 0) // now check for greatest amongst
                            //    {
                            //        DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn);
                            //        DateTime hmiDateTime = Convert.ToDateTime(getProcessQty[1].Time);

                            //        if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                            //        {
                            //            isHMIFirst = 1;
                            //        }
                            //        else
                            //        {
                            //            isHMIFirst = 0;
                            //        }

                            //    }
                            //    else if (mulitwoData.Count > 0)
                            //    {
                            //        isHMIFirst = 1;
                            //    }
                            //    else if (getProcessQty.Count == 2)
                            //    {
                            //        isHMIFirst = 0;
                            //    }

                            //    if (isHMIFirst == 1)
                            //    {
                            //        string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                            //        int delivInt = 0;
                            //        int.TryParse(delivString, out delivInt);

                            //        string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                            //        int procInt = 0;
                            //        int.TryParse(processString, out procInt);

                            //        PrvProcessQty += procInt;
                            //        PrvDeliveredQty += delivInt;
                            //    }
                            //    else if (isHMIFirst == 0)
                            //    {
                            //        string delivString = Convert.ToString(getProcessQty[1].Delivered_Qty);
                            //        int delivInt = 0;
                            //        int.TryParse(delivString, out delivInt);

                            //        string processString = Convert.ToString(getProcessQty[1].ProcessQty);
                            //        int procInt = 0;
                            //        int.TryParse(processString, out procInt);

                            //        PrvProcessQty += procInt;
                            //        PrvDeliveredQty += delivInt;
                            //    }
                            //    else
                            //    {
                            //        //no previous delivered or processed qty so Do Nothing.
                            //    }
                            //    #endregion

                            //    int newProcessedQty = PrvProcessQty + PrvDeliveredQty;
                            //    if (Convert.ToInt32(getProcessQty[1].isWorkInProgress) == 1 || TargetQtyNew == newProcessedQty)
                            //    {
                            //        Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                            //        hmiidData.Prod_FAI = null;
                            //        hmiidData.Target_Qty = null;
                            //        hmiidData.OperationNo = null;
                            //        hmiidData.PartNo = null;
                            //        hmiidData.Work_Order_No = null;
                            //        hmiidData.Project = null;
                            //        hmiidData.Date = null;
                            //        hmiidData.DDLWokrCentre = null;
                            //        hmiidData.ProcessQty = 0;
                            //        Session["FromDDL"] = 2;
                            //        TempData["ForDDL2"] = 2;
                            //        db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                            //        db.SaveChanges();
                            //        return RedirectToAction("Index");
                            //    }
                            //    if (TargetQtyNew < newProcessedQty)
                            //    {
                            //        Session["Error"] = "Previous ProcessedQty :" + newProcessedQty + ". TargetQty Cannot be Less than Processed";
                            //        hmiidData.ProcessQty = 0;
                            //        hmiidData.Date = null;
                            //        Session["FromDDL"] = 2;
                            //        TempData["ForDDL2"] = 2;
                            //        db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                            //        db.SaveChanges();
                            //        return RedirectToAction("Index");
                            //    }
                            //}
                            //else //Its not in HMIScreen , then it may be in multiwoselection table.
                            //{
                            //    #region new code

                            //    //here  get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                            //    int isHMIFirst = 2; //default NO History for that wo,pn,on

                            //    var mulitwoData = db.tbl_multiwoselection.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo).OrderByDescending(m => m.MultiWOID).Take(1).ToList();
                            //    //var hmiData = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 0).OrderByDescending(m => m.HMIID).Take(1).ToList();

                            //    if (getProcessQty.Count == 2 && mulitwoData.Count > 0) // now check for greatest amongst
                            //    {
                            //        DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn);
                            //        DateTime hmiDateTime = Convert.ToDateTime(getProcessQty[1].Time);

                            //        if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                            //        {
                            //            isHMIFirst = 1;
                            //        }
                            //        else
                            //        {
                            //            isHMIFirst = 0;
                            //        }
                            //    }
                            //    else if (mulitwoData.Count > 0)
                            //    {
                            //        isHMIFirst = 1;
                            //    }
                            //    if (isHMIFirst == 1)
                            //    {
                            //        string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                            //        int delivInt = 0;
                            //        int.TryParse(delivString, out delivInt);

                            //        string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                            //        int procInt = 0;
                            //        int.TryParse(processString, out procInt);

                            //        PrvProcessQty += procInt;
                            //        PrvDeliveredQty += delivInt;
                            //    }
                            //    #endregion
                            //}
                            //hmiidData.ProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);
                            #endregion

                            //2017-06-01
                            var ddlDataForIsCompleted = db.tblddls.Where(m => m.WorkOrder == woNo && m.MaterialDesc == partNo && m.OperationNo == opNo && m.IsCompleted == 1).FirstOrDefault();
                            if (ddlDataForIsCompleted == null)
                            {
                            }
                            else
                            {
                                Session["Error"] = "Job is Finished for WorkOrder: " + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                                hmiidData.Prod_FAI = null;
                                hmiidData.Target_Qty = null;
                                hmiidData.OperationNo = null;
                                hmiidData.PartNo = null;
                                hmiidData.Work_Order_No = null;
                                hmiidData.Project = null;
                                hmiidData.Date = null;
                                hmiidData.DDLWokrCentre = null;
                                hmiidData.ProcessQty = 0;
                                Session["FromDDL"] = 2;
                                TempData["ForDDL2"] = 2;
                                db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                                db.SaveChanges();
                                return RedirectToAction("Index");
                            }

                            //2017-06-22
                            var HMICompletedData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.isWorkInProgress == 1).FirstOrDefault();
                         
                            if (HMICompletedData != null )
                            {
                                Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                                var hmirow = db.tbllivehmiscreens.Find(hmiid);
                                db.tbllivehmiscreens.Remove(hmirow);
                                db.SaveChanges();
                                return RedirectToAction("Index");
                            }
                           
                            //New Code To Update ProcessedQty 02017-05-11
                            var getProcessQty1 = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.HMIID != hmiid && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).FirstOrDefault();
                           
                              var getProcessQty1historian = db.tblhmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.HMIID != hmiid && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).FirstOrDefault(); // added by Ashok
                            if (getProcessQty1 != null || getProcessQty1historian!=null)
                            {
                                #region new code
                                //here  get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                                int isHMIFirst = 2; //default NO History for that wo,pn,on

                                var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                                //var hmiData = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 0).OrderByDescending(m => m.HMIID).Take(1).ToList();
                                var mulitwoDataHistorian = db.tbl_multiwoselection.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.tblhmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tblhmiscreen.Time).Take(1).ToList();

                                if (getProcessQty1 != null && mulitwoData.Count > 0) // now check for greatest amongst
                                {

                                    //DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn); //2017-06-02
                                    //Based on hmiid of  multiwotable get  Time Column of tblhmiscreen 
                                    //int localhmiid = Convert.ToInt32(mulitwoData[0].HMIID);
                                    //var hmiiData = db.tblhmiscreens.Find(localhmiid);
                                    DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].tbllivehmiscreen.Time);
                                    DateTime hmiDateTime = Convert.ToDateTime(getProcessQty1.Time);

                                    if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                                    {
                                        isHMIFirst = 1;
                                    }
                                    else
                                    {
                                        isHMIFirst = 0;
                                    }

                                }
                                else if (getProcessQty1historian != null && mulitwoDataHistorian.Count > 0) {
                                    DateTime multiwoDateTime = Convert.ToDateTime(mulitwoDataHistorian[0].tblhmiscreen.Time);
                                    DateTime hmiDateTime = Convert.ToDateTime(getProcessQty1historian.Time);

                                    if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                                    {
                                        isHMIFirst = 1;
                                    }
                                    else
                                    {
                                        isHMIFirst = 0;
                                    }
                                }
                                else if (mulitwoData.Count > 0)
                                {
                                    isHMIFirst = 1;
                                }
                                else if(mulitwoDataHistorian.Count>0)
                                {
                                    isHMIFirst = 1;
                                }
                                else if (getProcessQty1 != null)
                                {
                                    isHMIFirst = 0;
                                }
                                else if(getProcessQty1historian!=null)
                                    isHMIFirst = 0;

                                if (isHMIFirst == 1)
                                {
                                    string delivString = "";
                                    if (mulitwoData.Count>0)
                                        delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                                    if(mulitwoData.Count == 0 && mulitwoDataHistorian != null)
                                        delivString= Convert.ToString(mulitwoDataHistorian[0].DeliveredQty);
                                    int delivInt = 0;
                                    int.TryParse(delivString, out delivInt);
                                    string processString = "";
                                    if (mulitwoData.Count > 0)
                                         processString = Convert.ToString(mulitwoData[0].ProcessQty);
                                    if (mulitwoData.Count == 0 && mulitwoDataHistorian != null)
                                        processString = Convert.ToString(mulitwoDataHistorian[0].ProcessQty);
                                    int procInt = 0;
                                    int.TryParse(processString, out procInt);

                                    PrvProcessQty += procInt;
                                    PrvDeliveredQty += delivInt;
                                }
                                else if (isHMIFirst == 0)
                                {
                                    string delivString = "";
                                    if (getProcessQty1!=null)
                                         delivString = Convert.ToString(getProcessQty1.Delivered_Qty);
                                    if (getProcessQty1==null && getProcessQty1historian != null)
                                        delivString = Convert.ToString(getProcessQty1historian.Delivered_Qty);
                                    int delivInt = 0;
                                    int.TryParse(delivString, out delivInt);

                                    string processString = "";
                                    if (getProcessQty1 != null)
                                        processString = Convert.ToString(getProcessQty1.ProcessQty);
                                    if (getProcessQty1 == null && getProcessQty1historian != null)
                                        processString = Convert.ToString(getProcessQty1historian.ProcessQty);
                                    int procInt = 0;
                                    int.TryParse(processString, out procInt);

                                    PrvProcessQty += procInt;
                                    PrvDeliveredQty += delivInt;
                                }
                                else
                                {
                                    //no previous delivered or processed qty so Do Nothing.
                                }
                                #endregion

                                int newProcessedQty = PrvProcessQty + PrvDeliveredQty;
                                if (getProcessQty1 != null)
                                if (Convert.ToInt32(getProcessQty1.isWorkInProgress) == 1)
                                {
                                    Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                                    hmiidData.Prod_FAI = null;
                                    hmiidData.Target_Qty = null;
                                    hmiidData.OperationNo = null;
                                    hmiidData.PartNo = null;
                                    hmiidData.Work_Order_No = null;
                                    hmiidData.Project = null;
                                    hmiidData.Date = null;
                                    hmiidData.DDLWokrCentre = null;
                                    hmiidData.ProcessQty = 0;
                                    Session["FromDDL"] = 2;
                                    TempData["ForDDL2"] = 2;
                                    db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                                    db.SaveChanges();
                                    return RedirectToAction("Index");
                                }

                                if (TargetQtyNew == newProcessedQty)
                                {
                                    hmiidData.Target_Qty = newProcessedQty;
                                    hmiidData.ProcessQty = newProcessedQty;
                                    hmiidData.SplitWO = "No";
                                    hmiidData.isWorkInProgress = 1;
                                    hmiidData.Status = 2;
                                    hmiidData.Time = hmiidData.Date;
                                    hmiidData.Delivered_Qty = 0;

                                    db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                                    db.SaveChanges();

                                    //if it existing in DDLList Update 
                                    var DDLList = db.tblddls.Where(m => m.WorkOrder == hmiidData.Work_Order_No && m.MaterialDesc == hmiidData.PartNo && m.OperationNo == hmiidData.OperationNo && m.IsCompleted == 0).ToList();
                                    foreach (var row in DDLList)
                                    {
                                        row.IsCompleted = 1;
                                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                                        db.SaveChanges();
                                    }

                                    Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                                    return RedirectToAction("Index");

                                }

                                if (TargetQtyNew < newProcessedQty)
                                {
                                    Session["Error"] = "Previous ProcessedQty :" + newProcessedQty + ". TargetQty Cannot be Less than Processed";
                                    hmiidData.ProcessQty = 0;
                                    hmiidData.Date = null;
                                    Session["FromDDL"] = 2;
                                    TempData["ForDDL2"] = 2;
                                    db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                                    db.SaveChanges();
                                    return RedirectToAction("Index");
                                }


                            }
                            else //Its not in HMIScreen , then it may be in multiwoselection table.
                            {
                                #region new code

                                //here  get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                                int isHMIFirst = 2; //default NO History for that wo,pn,on

                                var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                                //var hmiData = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 0).OrderByDescending(m => m.HMIID).Take(1).ToList();
                                var mulitwoDataHistorian = db.tbl_multiwoselection.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.tblhmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tblhmiscreen.Time).Take(1).ToList();
                                if (getProcessQty1 != null && mulitwoData.Count > 0) // now check for greatest amongst
                                {
                                    //DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn); //2017-06-02
                                    //Based on hmiid of  multiwotable get  Time Column of tblhmiscreen 
                                    //int localhmiid = Convert.ToInt32(mulitwoData[0].HMIID);
                                    //var hmiiData = db.tblhmiscreens.Find(localhmiid);
                                    DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].tbllivehmiscreen.Time);
                                    DateTime hmiDateTime = Convert.ToDateTime(getProcessQty1.Time);

                                    if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                                    {
                                        isHMIFirst = 1;
                                    }
                                    else
                                    {
                                        isHMIFirst = 0;
                                    }
                                }
                                else if (getProcessQty1historian != null && mulitwoDataHistorian.Count > 0)
                                {
                                    DateTime multiwoDateTime = Convert.ToDateTime(mulitwoDataHistorian[0].tblhmiscreen.Time);
                                    DateTime hmiDateTime = Convert.ToDateTime(getProcessQty1historian.Time);

                                    if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                                    {
                                        isHMIFirst = 1;
                                    }
                                    else
                                    {
                                        isHMIFirst = 0;
                                    }
                                }
                                else if (mulitwoData.Count > 0)
                                {
                                    isHMIFirst = 1;
                                }
                                else if (mulitwoDataHistorian.Count > 0)
                                {
                                    isHMIFirst = 1;
                                }
                                if (isHMIFirst == 1)
                                {
                                    string delivString = "";
                                   
                                    if(mulitwoData.Count>0)
                                        delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                                    if (mulitwoData.Count == 0 && mulitwoDataHistorian != null)
                                        delivString = Convert.ToString(mulitwoDataHistorian[0].DeliveredQty);
                                    int delivInt = 0;
                                    int.TryParse(delivString, out delivInt);

                                    string processString = "";
                                    if (mulitwoData.Count > 0)
                                        processString = Convert.ToString(mulitwoData[0].ProcessQty);
                                    if (mulitwoData.Count == 0 && mulitwoDataHistorian != null)
                                        processString = Convert.ToString(mulitwoDataHistorian[0].ProcessQty);
                                    int procInt = 0;
                                    int.TryParse(processString, out procInt);

                                    PrvProcessQty += procInt;
                                    PrvDeliveredQty += delivInt;
                                }
                                #endregion
                            }

                            hmiidData.ProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);
                        }
                        else if (isMultiWO == 1) // Its a MultiWOSelection so Its already taken care in ddllist post method.
                        {
                            //hmiidData.Date = DateTime.Now;

                            //2017-06-22
                            var HMICompletedData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.isWorkInProgress == 1).FirstOrDefault();
                            if (HMICompletedData != null)
                            {
                                Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                                var hmirow = db.tbllivehmiscreens.Find(hmiid);
                                db.tbllivehmiscreens.Remove(hmirow);
                                db.SaveChanges();
                                return RedirectToAction("Index");
                            }
                            //2017-06-22
                            var MultiWoCompletedData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.IsCompleted == 1).FirstOrDefault();
                            if (MultiWoCompletedData != null)
                            {
                                Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                                var hmirow = db.tbllivehmiscreens.Find(hmiid);
                                db.tbllivehmiscreens.Remove(hmirow);
                                db.SaveChanges();
                                return RedirectToAction("Index");
                            }
                        }

                        db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        Session["SubmitClicked"] = 1;

                        var tmWOs = db.tbllivemultiwoselections.Where(m => m.HMIID == hmiid && m.IsSubmit == 0).ToList();
                        foreach (var row in tmWOs)
                        {
                            row.IsSubmit = 1;
                            db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }

                        //plan.Date = DateTime.Now;
                        //plan.Time = DateTime.Now;
                        //db.Entry(plan).State = System.Data.Entity.EntityState.Modified;
                        //db.SaveChanges();
                        count++;
                    }

                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
        }

        //Developer : 
        public int HandleIdle()
        {
            int status = -1;
            int doneWithRow = -1;//some default value
            int isUpdate = -1;
            int lossid = -1;
            int isStart = 0, isScreen = 0;
            int machineid = Convert.ToInt32(Session["MachineID"]);
            int userid = Convert.ToInt16(Session["UserID"]);
            DateTime endTime = DateTime.Now, startTime = DateTime.Now;
            string shift = null;
            string LCorrectedDate, todaysCorrectedDate;

            todaysCorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            LCorrectedDate = todaysCorrectedDate;//dummy initializaition;
            //correcteddate
            string correcteddate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                correcteddate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                correcteddate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            //shift
            DateTime Time = DateTime.Now;
            TimeSpan Tm = new TimeSpan(Time.Hour, Time.Minute, Time.Second);
            var ShiftDetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm && m.EndTime >= Tm);
            string Shift = null;
            foreach (var a in ShiftDetails)
            {
                Shift = a.ShiftName;
            }

            var lossStatusData = db.tbllivelossofentries.Where(m => m.MachineID == machineid).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
            if (lossStatusData != null)
            {
                lossid = lossStatusData.LossID;
                doneWithRow = lossStatusData.DoneWithRow;
                isUpdate = lossStatusData.IsUpdate;
                endTime = Convert.ToDateTime(lossStatusData.EndDateTime);
                startTime = Convert.ToDateTime(lossStatusData.StartDateTime);
                shift = lossStatusData.Shift;
                isStart = Convert.ToInt32(lossStatusData.IsStart);
                isScreen = Convert.ToInt32(lossStatusData.IsScreen);
                LCorrectedDate = lossStatusData.CorrectedDate;
            }

            if (doneWithRow == 0 && isUpdate == 0 && isStart == 1 && isScreen == 0)
            {
                string x = Convert.ToString(Session["showIdlePopUp"]);
                int value;
                if (int.TryParse(x, out value))
                {
                }
                else
                {
                    Session["showIdlePopUp"] = 0;
                }
                status = 0;
            }
            else if (doneWithRow == 0 && isUpdate == 1 && isStart == 1 && isScreen == 1)
            {
                //don't add code to show popup
                Session["showIdlePopUp"] = 2;
                status = 0;
            }

            else if (doneWithRow == 1 && isUpdate == 1 && isStart == 0 && isScreen == 0)
            {
                //RedirectToAction("Index");
                Session["showIdlePopUp"] = 0;
                status = 1;
            }
            else if (doneWithRow == 0 && isUpdate == 1 && isStart == 1 && isScreen == 0)
            {
                //RedirectToAction("Index");

                Session["showIdlePopUp"] = 0;
                status = 1;
            }

            //checking for shift change.
            //Getting Shift Value
            #region
            //string[] Shift1 = GetShift(machineid);
            //if (LCorrectedDate == todaysCorrectedDate)
            //{
            //    if (shift == Shift1[0])
            //    {
            //        //shift has not changed so do nothing.
            //    }
            //    else
            //    {
            //        if (isUpdate == 0 && doneWithRow == 0)
            //        {
            //            string colorcode = null;
            //            var dailyprodstatusdata1 = db.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > startTime && (m.ColorCode == "green" || m.ColorCode == "blue" || m.ColorCode == "red")).OrderBy(m => m.StartTime).FirstOrDefault();
            //            if (dailyprodstatusdata1 != null)
            //            {
            //                colorcode = dailyprodstatusdata1.ColorCode;
            //                endTime = Convert.ToDateTime(dailyprodstatusdata1.StartTime);
            //            }
            //            DateTime starttimeGetShiftMethod = Convert.ToDateTime(todaysCorrectedDate + " " + Shift1[1]);

            //            if (colorcode == "green" || colorcode == "blue" || colorcode == "red")
            //            {
            //                var lossofentryrow = db.tbllossofentries.Where(m => m.MachineID == machineid && m.CorrectedDate == correcteddate).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
            //                if (lossofentryrow != null)
            //                {
            //                    lossofentryrow.EndDateTime = endTime;
            //                    lossofentryrow.IsUpdate = 1;
            //                    lossofentryrow.DoneWithRow = 1;
            //                    db.Entry(lossofentryrow).State = System.Data.Entity.EntityState.Modified;
            //                    db.SaveChanges();
            //                }
            //            }
            //            else
            //            {
            //                var lossdata = db.tbllossofentries.Find(lossid);
            //                lossdata.EndDateTime = starttimeGetShiftMethod;
            //                lossdata.IsUpdate = 1;
            //                lossdata.DoneWithRow = 1;
            //                db.Entry(lossdata).State = System.Data.Entity.EntityState.Modified;
            //                db.SaveChanges();

            //            }

            //            //insert fresh row
            //            //Session["showIdlePopUp"] = 1;
            //            tbllossofentry lossentry = new tbllossofentry();
            //            lossentry.Shift = Shift1[0];
            //            lossentry.EntryTime = starttimeGetShiftMethod;
            //            lossentry.StartDateTime = starttimeGetShiftMethod;
            //            lossentry.EndDateTime = starttimeGetShiftMethod;
            //            lossentry.CorrectedDate = correcteddate;
            //            lossentry.IsUpdate = 0;
            //            lossentry.DoneWithRow = 0;
            //            lossentry.MessageCodeID = 999;
            //            int abc = Convert.ToInt32(lossentry.MessageCodeID);
            //            var a = db.message_code_master.Find(abc);
            //            lossentry.MessageDesc = a.MessageDescription.ToString();
            //            lossentry.MessageCode = a.MessageCode.ToString();
            //            lossentry.MachineID = machineid;

            //            if (ModelState.IsValid)
            //            {
            //                Session["showIdlePopUp"] = 0;
            //                db.tbllossofentries.Add(lossentry);
            //                db.SaveChanges();
            //            }
            //        }
            //        else if (isUpdate == 1 && doneWithRow == 0)
            //        {
            //            string colorcode = null;
            //            var dailyprodstatusdata1 = db.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > startTime && (m.ColorCode == "green" || m.ColorCode == "blue" || m.ColorCode == "red")).OrderBy(m => m.StartTime).FirstOrDefault();
            //            if (dailyprodstatusdata1 != null)
            //            {
            //                colorcode = dailyprodstatusdata1.ColorCode;
            //                endTime = Convert.ToDateTime(dailyprodstatusdata1.StartTime);
            //            }
            //            DateTime starttimeGetShiftMethod = Convert.ToDateTime(todaysCorrectedDate + " " + Shift1[1]);

            //            if (colorcode == "green" || colorcode == "blue" || colorcode == "red")
            //            {
            //                var lossofentryrow = db.tbllossofentries.Where(m => m.MachineID == machineid && m.CorrectedDate == correcteddate).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
            //                if (lossofentryrow != null)
            //                {
            //                    lossofentryrow.EndDateTime = endTime;
            //                    lossofentryrow.IsUpdate = 1;
            //                    lossofentryrow.DoneWithRow = 1;
            //                    db.Entry(lossofentryrow).State = System.Data.Entity.EntityState.Modified;
            //                    db.SaveChanges();
            //                }
            //            }
            //            else
            //            {
            //                var lossdata = db.tbllossofentries.Find(lossid);
            //                lossdata.EndDateTime = starttimeGetShiftMethod;
            //                lossdata.IsUpdate = 1;
            //                lossdata.DoneWithRow = 1;
            //                db.Entry(lossdata).State = System.Data.Entity.EntityState.Modified;
            //                db.SaveChanges();

            //            }

            //            var lossdata1 = db.tbllossofentries.Find(lossid);
            //            //insert fresh row
            //            //Session["showIdlePopUp"] = 1;
            //            tbllossofentry lossentry = new tbllossofentry();
            //            lossentry.Shift = Shift1[0];
            //            lossentry.EntryTime = starttimeGetShiftMethod;
            //            lossentry.StartDateTime = starttimeGetShiftMethod;
            //            lossentry.EndDateTime = starttimeGetShiftMethod;
            //            lossentry.CorrectedDate = correcteddate;
            //            lossentry.IsUpdate = 0;
            //            lossentry.DoneWithRow = 0;
            //            lossentry.MessageCodeID = lossdata1.MessageCodeID;
            //            int abc = Convert.ToInt32(lossdata1.MessageCodeID);
            //            var a = db.message_code_master.Find(abc);
            //            lossentry.MessageDesc = a.MessageDescription.ToString();
            //            lossentry.MessageCode = a.MessageCode.ToString();
            //            lossentry.MachineID = machineid;

            //            if (ModelState.IsValid)
            //            {
            //                Session["showIdlePopUp"] = 0;
            //                db.tbllossofentries.Add(lossentry);
            //                db.SaveChanges();
            //            }

            //        }
            //    }

            //}
            //else
            //{
            //    #region
            //    if (isUpdate == 0 && doneWithRow == 0)
            //    {
            //        string colorcode = null;
            //        var dailyprodstatusdata1 = db.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > startTime && (m.ColorCode == "green" || m.ColorCode == "blue" || m.ColorCode == "red")).OrderBy(m => m.StartTime).FirstOrDefault();
            //        if (dailyprodstatusdata1 != null)
            //        {
            //            colorcode = dailyprodstatusdata1.ColorCode;
            //            endTime = Convert.ToDateTime(dailyprodstatusdata1.StartTime);
            //        }
            //        DateTime starttimeGetShiftMethod = Convert.ToDateTime(todaysCorrectedDate + " " + Shift1[1]);

            //        if (colorcode == "green" || colorcode == "blue" || colorcode == "red")
            //        {
            //            var lossofentryrow = db.tbllossofentries.Where(m => m.MachineID == machineid && m.CorrectedDate == correcteddate).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
            //            if (lossofentryrow != null)
            //            {
            //                lossofentryrow.EndDateTime = endTime;
            //                lossofentryrow.IsUpdate = 1;
            //                lossofentryrow.DoneWithRow = 1;
            //                db.Entry(lossofentryrow).State = System.Data.Entity.EntityState.Modified;
            //                db.SaveChanges();
            //            }
            //        }
            //        else
            //        {
            //            var lossdata = db.tbllossofentries.Find(lossid);
            //            lossdata.EndDateTime = starttimeGetShiftMethod;
            //            lossdata.IsUpdate = 1;
            //            lossdata.DoneWithRow = 1;
            //            db.Entry(lossdata).State = System.Data.Entity.EntityState.Modified;
            //            db.SaveChanges();

            //        }


            //        //insert fresh row
            //        //Session["showIdlePopUp"] = 1;
            //        tbllossofentry lossentry = new tbllossofentry();
            //        lossentry.Shift = Shift1[0];
            //        lossentry.EntryTime = starttimeGetShiftMethod;
            //        lossentry.StartDateTime = starttimeGetShiftMethod;
            //        lossentry.EndDateTime = starttimeGetShiftMethod;
            //        lossentry.CorrectedDate = correcteddate;
            //        lossentry.IsUpdate = 0;
            //        lossentry.DoneWithRow = 0;
            //        lossentry.MessageCodeID = 999;
            //        int abc = Convert.ToInt32(lossentry.MessageCodeID);
            //        var a = db.message_code_master.Find(abc);
            //        lossentry.MessageDesc = a.MessageDescription.ToString();
            //        lossentry.MessageCode = a.MessageCode.ToString();
            //        lossentry.MachineID = machineid;

            //        if (ModelState.IsValid)
            //        {
            //            Session["showIdlePopUp"] = 0;
            //            db.tbllossofentries.Add(lossentry);
            //            db.SaveChanges();
            //        }
            //    }
            //    else if (isUpdate == 1 && doneWithRow == 0)
            //    {
            //        string colorcode = null;
            //        var dailyprodstatusdata1 = db.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > startTime && (m.ColorCode == "green" || m.ColorCode == "blue" || m.ColorCode == "red")).OrderBy(m => m.StartTime).FirstOrDefault();
            //        if (dailyprodstatusdata1 != null)
            //        {
            //            colorcode = dailyprodstatusdata1.ColorCode;
            //            endTime = Convert.ToDateTime(dailyprodstatusdata1.StartTime);
            //        }
            //        DateTime starttimeGetShiftMethod = Convert.ToDateTime(todaysCorrectedDate + " " + Shift1[1]);

            //        if (colorcode == "green" || colorcode == "blue" || colorcode == "red")
            //        {
            //            var lossofentryrow = db.tbllossofentries.Where(m => m.MachineID == machineid && m.CorrectedDate == correcteddate).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
            //            if (lossofentryrow != null)
            //            {
            //                lossofentryrow.EndDateTime = endTime;
            //                lossofentryrow.IsUpdate = 1;
            //                lossofentryrow.DoneWithRow = 1;
            //                db.Entry(lossofentryrow).State = System.Data.Entity.EntityState.Modified;
            //                db.SaveChanges();
            //            }
            //        }
            //        else
            //        {
            //            var lossdata = db.tbllossofentries.Find(lossid);
            //            lossdata.EndDateTime = starttimeGetShiftMethod;
            //            lossdata.IsUpdate = 1;
            //            lossdata.DoneWithRow = 1;
            //            db.Entry(lossdata).State = System.Data.Entity.EntityState.Modified;
            //            db.SaveChanges();

            //        }

            //        var lossdata1 = db.tbllossofentries.Find(lossid);
            //        //insert fresh row

            //        tbllossofentry lossentry = new tbllossofentry();
            //        lossentry.Shift = Shift1[0];
            //        lossentry.EntryTime = starttimeGetShiftMethod;
            //        lossentry.StartDateTime = starttimeGetShiftMethod;
            //        lossentry.EndDateTime = starttimeGetShiftMethod;
            //        lossentry.CorrectedDate = correcteddate;
            //        lossentry.IsUpdate = 0;
            //        lossentry.DoneWithRow = 0;
            //        lossentry.MessageCodeID = lossdata1.MessageCodeID;
            //        int abc = Convert.ToInt32(lossdata1.MessageCodeID);
            //        var a = db.message_code_master.Find(abc);
            //        lossentry.MessageDesc = a.MessageDescription.ToString();
            //        lossentry.MessageCode = a.MessageCode.ToString();
            //        lossentry.MachineID = machineid;

            //        if (ModelState.IsValid)
            //        {
            //            Session["showIdlePopUp"] = 0;
            //            db.tbllossofentries.Add(lossentry);
            //            db.SaveChanges();
            //        }

            //    }
            //    #endregion

            //}

            #endregion

            #region
            //if ((isUpdate == 1 && doneWithRow == 1) || lossStatusData == null)
            //{

            //    #region
            //    int yellowcount = 0;

            //    //var dailyprodstatusdata = db.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid).OrderByDescending(m => m.StartTime);
            //    //int checkcount = 0;
            //    //foreach (var dailyrow in dailyprodstatusdata)
            //    //{
            //    //    if (dailyrow.ColorCode == "yellow")
            //    //    {
            //    //        yellowcount++;
            //    //    }
            //    //    if (checkcount >= 2)
            //    //    {
            //    //        break;
            //    //    }
            //    //}

            //    bool IdleStatus = false;
            //    int TotalMinute = 0;
            //    TotalMinute = System.DateTime.Now.Subtract(startTime).Minutes;
            //    if (TotalMinute >= 3)
            //    {
            //        #region DownColor
            //        int count = 0;
            //        int ContinuesChecking = 0;
            //        var productionstatus = db.tbldailyprodstatus.Where(m => m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > startTime).OrderByDescending(m => m.StartTime);
            //        foreach (var check in productionstatus)
            //        {
            //            if (ContinuesChecking < 2)
            //            {
            //                if (check.ColorCode == "yellow")
            //                {
            //                    count++;
            //                    if (count == 2)
            //                    {
            //                        break;
            //                    }
            //                }
            //                else
            //                {
            //                    count = 0;
            //                }
            //                ContinuesChecking++;
            //            }
            //            else
            //                break;
            //        }
            //        if (count >= 2 && ContinuesChecking < 5)
            //        {
            //            IdleStatus = true;
            //        }
            //        #endregion
            //    }

            //    if (IdleStatus)
            //    {
            //        DateTime starttime = GetIdleStartTime(0, correcteddate);
            //        //insert fresh row
            //        tbllossofentry lossentry = new tbllossofentry();
            //        lossentry.Shift = Session["realshift"].ToString();
            //        lossentry.EntryTime = starttime;
            //        lossentry.StartDateTime = starttime;
            //        lossentry.EndDateTime = starttime;
            //        lossentry.CorrectedDate = correcteddate;
            //        lossentry.IsUpdate = 0;
            //        lossentry.DoneWithRow = 0;
            //        lossentry.MessageCodeID = 999;
            //        int abc = Convert.ToInt32(lossentry.MessageCodeID);
            //        var a = db.message_code_master.Find(abc);
            //        lossentry.MessageDesc = a.MessageDescription.ToString();
            //        lossentry.MessageCode = a.MessageCode.ToString();
            //        lossentry.MachineID = machineid;

            //        if (ModelState.IsValid)
            //        {
            //            Session["showIdlePopUp"] = 0;
            //            db.tbllossofentries.Add(lossentry);
            //            db.SaveChanges();
            //        }

            //        //RedirectToAction("DownCodeEntry");
            //        status = 0;
            //    }
            //    //else do nothing
            //    #endregion

            //}
            ////its a fresh row so, check color. if yellow update endtime if green end IDLE
            //else if (isUpdate == 0 && doneWithRow == 0)
            //{

            //    #region

            //    string colorcode = null;
            //    var dailyprodstatusdata1 = db.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > startTime && (m.ColorCode == "green" || m.ColorCode == "blue" || m.ColorCode == "red")).OrderBy(m => m.StartTime).FirstOrDefault();
            //    if (dailyprodstatusdata1 != null)
            //    {
            //        colorcode = dailyprodstatusdata1.ColorCode;
            //        endTime = Convert.ToDateTime(dailyprodstatusdata1.StartTime);
            //    }

            //    bool IdleStatus = false;
            //    int TotalMinute = 0;
            //    TotalMinute = System.DateTime.Now.Subtract(startTime).Minutes;
            //    if (TotalMinute >= 2)
            //    {
            //        #region DownColor
            //        int count = 0;
            //        int ContinuesChecking = 0;
            //        var productionstatus = db.tbldailyprodstatus.Where(m => m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > startTime).OrderByDescending(m => m.StartTime);
            //        foreach (var check in productionstatus)
            //        {
            //            if (ContinuesChecking < 2)
            //            {
            //                if (check.ColorCode == "yellow")
            //                {
            //                    count++;
            //                    if (count == 2)
            //                    {
            //                        break;
            //                    }
            //                }
            //                else
            //                {
            //                    count = 0;
            //                }
            //                ContinuesChecking++;
            //            }
            //            else
            //                break;
            //        }
            //        if (count >= 2 && ContinuesChecking < 5)
            //        {
            //            IdleStatus = true;
            //        }
            //        #endregion
            //    }
            //    #region commented
            //    //if (colorcode == "yellow") //update endtime 
            //    //{
            //    //    var lossofentryrow = db.tbllossofentries.Where(m => m.MachineID == machineid && m.CorrectedDate == correcteddate).OrderByDescending(m => m.StartDateTime).Take(1);
            //    //    foreach (var row in lossofentryrow)
            //    //    {
            //    //        row.EndDateTime = DateTime.Now;
            //    //        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
            //    //        db.SaveChanges();
            //    //        break;
            //    //    }
            //    //    //if you have isUpdate & donewithrow then use its lossID . This fail's for the 1st time.
            //    //    //RedirectToAction("DownCodeEntry");
            //    //    //status = 0;
            //    //}
            //    //else 
            //    #endregion

            //    if (IdleStatus)
            //    {
            //        status = 0;
            //    }
            //    else if (colorcode == "green" || colorcode == "blue" || colorcode == "red")
            //    {
            //        var lossofentryrow = db.tbllossofentries.Where(m => m.MachineID == machineid && m.CorrectedDate == correcteddate).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
            //        if(lossofentryrow != null)
            //        {
            //            lossofentryrow.EndDateTime = endTime;
            //            lossofentryrow.IsUpdate = 1;
            //            lossofentryrow.DoneWithRow = 1;
            //            db.Entry(lossofentryrow).State = System.Data.Entity.EntityState.Modified;
            //            db.SaveChanges();
            //        }
            //        //RedirectToAction("Index");
            //        status = 1;
            //    }

            //    #endregion

            //}

            // //its already updated row . update endtime every minute here.
            //// idleCode reentry will be handled in downcodeentry POST Method.
            //else if (isUpdate == 1 && doneWithRow == 0)
            //{

            //    #region
            //    string colorcode = null;
            //    var dailyprodstatusdata2 = db.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > startTime && (m.ColorCode == "green" || m.ColorCode == "blue" || m.ColorCode == "red")).OrderBy(m => m.StartTime).FirstOrDefault();
            //    if (dailyprodstatusdata2 != null)
            //    {
            //        colorcode = dailyprodstatusdata2.ColorCode;
            //        endTime = Convert.ToDateTime(dailyprodstatusdata2.StartTime);
            //    }

            //    bool IdleStatus = false;
            //    int TotalMinute = 0;
            //    TotalMinute = System.DateTime.Now.Subtract(endTime).Minutes;
            //    if (TotalMinute >= 2)
            //    {
            //        #region DownColor
            //        int count = 0;
            //        int ContinuesChecking = 0;
            //        var productionstatus = db.tbldailyprodstatus.Where(m => m.CorrectedDate == correcteddate && m.MachineID == machineid && m.StartTime > endTime).OrderByDescending(m => m.StartTime);
            //        foreach (var check in productionstatus)
            //        {

            //            if (ContinuesChecking < 2)
            //            {
            //                if (check.ColorCode == "yellow")
            //                {
            //                    count++;
            //                    if (count == 2)
            //                    {
            //                        break;
            //                    }
            //                }
            //                else
            //                {
            //                    count = 0;
            //                }
            //                ContinuesChecking++;
            //            }
            //            else
            //                break;
            //        }
            //        if (count >= 2 && ContinuesChecking < 5)
            //        {
            //            IdleStatus = true;
            //        }
            //        #endregion
            //    }
            //    if (IdleStatus)
            //    {
            //        status = 0;
            //    }
            //    else if (colorcode == "green" || colorcode == "blue" || colorcode == "red")
            //    {
            //        var lossofentryrow = db.tbllossofentries.Where(m => m.MachineID == machineid && m.CorrectedDate == correcteddate).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
            //        if (lossofentryrow != null)
            //        {
            //            lossofentryrow.EndDateTime = endTime;
            //            lossofentryrow.IsUpdate = 1;
            //            lossofentryrow.DoneWithRow = 1;
            //            db.Entry(lossofentryrow).State = System.Data.Entity.EntityState.Modified;
            //            db.SaveChanges();
            //        }
            //        //RedirectToAction("Index");
            //        status = 1;
            //    }
            //    #endregion
            //}
            #endregion
            //return RedirectToAction("Index");

            return status;
        }

        public string[] GetShift(int machineID)
        {
            string[] shift = new string[4];
            shift[0] = "C";
            DateTime Time1 = DateTime.Now;
            TimeSpan Tm1 = new TimeSpan(Time1.Hour, Time1.Minute, Time1.Second);
            var Shiftdetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm1 && m.EndTime >= Tm1).FirstOrDefault();
            if (Shiftdetails != null)
            {
                shift[0] = Shiftdetails.ShiftName;
                shift[1] = Shiftdetails.StartTime.ToString();
                shift[2] = Shiftdetails.EndTime.ToString();
            }

            return shift;
        }

        //1st yellow row in tbldailyprodstatus after endtime of donewithrow in tbllossofentry for that machine , or now
        public DateTime GetIdleStartTime(int status, string correcteddate)
        {
            DateTime starttime = DateTime.Now;
            DateTime lastdonewithrowendtime = new DateTime(2012, 12, 12);
            DateTime duplicatedate = lastdonewithrowendtime;
            int machineid = Convert.ToInt32(Session["MachineID"]);

            using (mazakdaqEntities db1 = new mazakdaqEntities())
            {

                var lastdownwithrowdata = db1.tbllivelossofentries.Where(m => m.MachineID == machineid && m.IsUpdate == 1 && m.DoneWithRow == 1).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
                if (lastdownwithrowdata != null)
                {
                    lastdonewithrowendtime = Convert.ToDateTime(lastdownwithrowdata.EndDateTime);
                    if (status == 4)
                    {
                        starttime = lastdonewithrowendtime;
                    }
                    else
                    {

                        //var dailyprodstatusdata = db1.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid).OrderByDescending(m => m.StartTime);
                        //foreach (var dailyrow in dailyprodstatusdata)
                        //{
                        //    if (dailyrow.ColorCode == "yellow")
                        //    {
                        //        starttime = Convert.ToDateTime(dailyrow.StartTime);
                        //    }
                        //    else
                        //    {
                        //        break;
                        //    }
                        //}

                        starttime = (DateTime)db1.tbllivemodedbs.Where(m => m.CorrectedDate == correcteddate && m.MachineID == machineid).OrderByDescending(m => m.StartTime).Select(m => m.StartTime).FirstOrDefault();
                    }
                }
                else
                {
                    //var dailyprodstatusdata = db1.tbldailyprodstatus.Where(m => m.IsDeleted == 0 && m.CorrectedDate == correcteddate && m.MachineID == machineid).OrderByDescending(m => m.StartTime);
                    //foreach (var dailyrow in dailyprodstatusdata)
                    //{
                    //    if (dailyrow.ColorCode == "yellow")
                    //    {
                    //        starttime = Convert.ToDateTime(dailyrow.StartTime);
                    //    }
                    //    else
                    //    {
                    //        break;
                    //    }
                    //}
                    starttime = (DateTime)db1.tbllivemodedbs.Where(m => m.CorrectedDate == correcteddate && m.MachineID == machineid).OrderByDescending(m => m.StartTime).Select(m => m.StartTime).FirstOrDefault();
                }
            }
            return starttime;
        }

        public JsonResult ReworkOrderClicked(int HMIID)
        {
            var nothing = 1;
            //Session["FromDDL"] = 2;
            var thisrow = db.tbllivehmiscreens.Find(HMIID);
            thisrow.isWorkOrder = 1;
            db.Entry(thisrow).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return Json(nothing, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AutoSave(int HMIID, string field, string value)
        {
            var nothing = 1;
            using (mazakdaqEntities dbUpdate = new mazakdaqEntities())
            {
                var thisrow = dbUpdate.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
                if (thisrow != null && thisrow.Date == null)
                {
                    switch (field)
                    {
                        case "cjtextboxshift":
                            {
                                thisrow.Shift = value;
                                break;
                            }
                        case "cjtextboxop":
                            {
                                thisrow.OperatorDet = value;
                                thisrow.PEStartTime = DateTime.Now;
                                break;
                            }
                        case "cjtextbox1":
                            {
                                //thisrow.PEStartTime = DateTime.Now;
                                thisrow.Project = value;
                                break;
                            }
                        case "Prod_FAI":
                            {
                                thisrow.Prod_FAI = value;
                                break;
                            }
                        case "cjtextbox3":
                            {
                                thisrow.PartNo = value;
                                break;
                            }
                        case "cjtextbox4":
                            {
                                thisrow.Work_Order_No = value;
                                break;
                            }
                        case "cjtextbox5":
                            {
                                thisrow.OperationNo = value;
                                break;
                            }
                        case "cjtextbox6":
                            {
                                if (value != "")
                                    thisrow.Target_Qty = Convert.ToInt32(value);
                                break;
                            }
                        //case "cjtextbox7":
                        //    {
                        //        if (value != "")
                        //            thisrow[0].Rej_Qty = Convert.ToInt32(value);
                        //        break;
                        //    }
                        //case "cjtextbox8":
                        //    {
                        //        if (value != "")
                        //            thisrow[0].Delivered_Qty = Convert.ToInt32(value);
                        //        break;
                        //    }
                        default:
                            {
                                break;
                            }
                    }
                    dbUpdate.Entry(thisrow).State = System.Data.Entity.EntityState.Modified;
                    dbUpdate.SaveChanges();
                }

            }
            return Json(nothing, JsonRequestBehavior.AllowGet);
        }

        //To select work order or rework order
        public ActionResult ChooseWorkOrder()
        {

            //Code For Admin And Super Admin
            int RoleID = Convert.ToInt32(Session["RoleID"]);
            if (RoleID == 1 || RoleID == 2)
            {
                return RedirectToAction("SelectMachine", "HMIScree", null);
            }
            return View();
        }
        [HttpPost]
        public ActionResult ChooseWorkOrder(string wo, string reworkwo)
        {

            Session["isWorkOrder"] = reworkwo == null ? 0 : 1;
            int data = Convert.ToInt32(Session["isWorkOrder"]);
            //if()
            return RedirectToAction("Index");
        }

        //IDLE codes
        public ActionResult DownCodeEntry(int Bid = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            Session["starttime"] = DateTime.Now;
            //ViewData["MessageCodeID"] = new SelectList(db.message_code_master.Where(m => m.IsDeleted == 0).Where(m => m.MessageType == "IDLE" || m.MessageType == "SETUP"), "MessageCodeID", "MessageDescription");
            //corrected date
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            int macid = Convert.ToInt32(Session["MachineID"]);
            string shift = Session["realshift"].ToString();



            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == macid).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);

            #region Old code
            //var check = db.tbllossofentries.Where(m => m.MachineID == macid && m.CorrectedDate == CorrectedDate).OrderByDescending(m => m.LossID).ToList();//m.IsUpdate == 0 && m.DoneWithRow == 0   && m.Shift == shift
            //int downcheck = 1;
            //if (check.Count > 0)
            //{
            //    downcheck = check[0].DoneWithRow;
            //}
            //if (downcheck == 1)
            //{

            //    DateTime starttime = DateTime.Now;
            //    //this variable has been declared to get the start time of the idle
            //    var productionstatus1 = db.tbldailyprodstatus.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == macid).OrderByDescending(m => m.StartTime);
            //    foreach (var check1 in productionstatus1)
            //    {
            //        if (check1.ColorCode == "yellow")
            //        {
            //            starttime = Convert.ToDateTime(check1.StartTime);
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }

            //    tbllossofentry lossentry = new tbllossofentry();

            //    lossentry.Shift = Session["realshift"].ToString();
            //    lossentry.EntryTime = starttime;
            //    lossentry.StartDateTime = starttime;
            //    lossentry.EndDateTime = starttime;
            //    lossentry.CorrectedDate = CorrectedDate;
            //    lossentry.DoneWithRow = 0;
            //    //lossentry.MessageCodeID = 4; narendra
            //    lossentry.MessageCodeID = 999;
            //    int abc = Convert.ToInt32(lossentry.MessageCodeID);
            //    string msgcode = null;
            //    var a = db.message_code_master.Find(abc);
            //    lossentry.MessageDesc = a.MessageDescription.ToString();
            //    lossentry.MessageCode = a.MessageCode.ToString();
            //    //lossentry.MessageCodeID = validatingCode.MessageCodeID;
            //    lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
            //    lossentry.IsUpdate = 0;
            //    //if (ModelState.IsValid)
            //    {
            //        db.tbllossofentries.Add(lossentry);
            //        db.SaveChanges();
            //    }
            //    return View(lossentry);
            //}
            //else
            //{
            //    int id = 0;
            //    foreach (var a in check)
            //    {
            //        id = a.LossID;
            //        break;
            //    }
            //    tbllossofentry lossentry = db.tbllossofentries.Find(id);
            //    // situation is: user doesn't enter code and production starts
            //    var check1 = db.tbldailyprodstatus.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == macid && m.ColorCode == "green" && m.EndTime >= lossentry.StartDateTime);
            //    if (check1.Count() == 0)
            //    {
            //        lossentry.EndDateTime = DateTime.Now;

            //        db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
            //        db.SaveChanges();
            //    }
            //    //update the messagecode and message description.
            //    else
            //    {
            //        foreach (var j in check1)
            //        {
            //            lossentry.EndDateTime = j.EndTime;
            //            lossentry.EntryTime = j.EndTime;
            //            lossentry.MessageCodeID = 999;

            //            lossentry.DoneWithRow = 1;

            //            lossentry.MessageDesc = "IDLECODE NOT ENTERED";
            //            db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
            //            db.SaveChanges();
            //            return RedirectToAction("Index");
            //        }
            //    }
            //    return View(lossentry);
            //}
            #endregion

            int handleidleReturnValue = HandleIdle();
            if (handleidleReturnValue == 1)
            {
                Session["showIdlePopUp"] = 0;
                return RedirectToAction("Index");
            }

            //Get Previous Loss to Display.
            var PrevIdleToView = db.tbllivelossofentries.Where(m => m.MachineID == macid && m.DoneWithRow == 0).OrderByDescending(m => m.LossID).FirstOrDefault();
            if (PrevIdleToView != null)
            {
                int losscode = PrevIdleToView.MessageCodeID;
                ViewBag.PrevLossName = GetLossPath(losscode);
                ViewBag.PrevLossStartTime = PrevIdleToView.StartDateTime;
            }

            //stage 2. Idle is running and u need to send data to view regarding that.

            var IdleToView = db.tbllivelossofentries.Where(m => m.MachineID == macid).OrderByDescending(m => m.LossID).FirstOrDefault();
            
            if (IdleToView != null) //implies idle is running
            {
                if (IdleToView.DoneWithRow == 0 && IdleToView.MessageCodeID != 999)
                {
                    int idlecode = Convert.ToInt32(IdleToView.MessageCodeID);
                    var DataToView = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodeID == idlecode).ToList();
                    ViewBag.Level = DataToView[0].LossCodesLevel;
                    ViewBag.LossCode = DataToView[0].LossCode;
                    ViewBag.LossId = DataToView[0].LossCodeID;
                    ViewBag.IdleStartTime = IdleToView.StartDateTime;                   
                }
            }

            //stage 3. Operator is selecting the Idle by traversing down the Hierarchy of LossCodes.
            if (Bid != 0)
            {
                var lossdata = db.tbllossescodes.Find(Bid);
                int level = lossdata.LossCodesLevel;
                string losscode = lossdata.LossCode;
                if (level == 1)
                {
                    var level2Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel1ID == Bid && m.LossCodesLevel == 2 && m.LossCodesLevel2ID == null && m.MessageType != "BREAKDOWN").ToList();
                    if (level2Data.Count == 0)
                    {
                        var level1Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel == level && m.LossCodesLevel1ID == null && m.LossCodesLevel2ID == null && m.MessageType != "NoCode" && m.MessageType != "BREAKDOWN" && m.MessageType != "PM").ToList();
                        ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + losscode + " as reason.";
                        ViewBag.LossID = Bid;
                        ViewBag.Level = level;
                        ViewBag.breadScrum = losscode + "-->  ";
                        return View(level1Data);
                    }
                    ViewBag.Level = level + 1;
                    ViewBag.LossID = Bid;
                    ViewBag.breadScrum = losscode + "-->  ";
                    return View(level2Data);
                }
                else if (level == 2)
                {
                    var level3Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel2ID == Bid && m.LossCodesLevel == 3 && m.MessageType != "BREAKDOWN").ToList();
                    int prevLevelId = Convert.ToInt32(lossdata.LossCodesLevel1ID);
                    var level1data = db.tbllossescodes.Where(m => m.LossCodeID == prevLevelId).Select(m => m.LossCode).FirstOrDefault();
                    if (level3Data.Count == 0)
                    {
                        var level2Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel1ID == prevLevelId && m.LossCodesLevel2ID == null).ToList();
                        ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + losscode + " as reason.";
                        ViewBag.LossID = Bid;
                        ViewBag.Level = level;
                        ViewBag.breadScrum = level1data + " --> " + losscode + " --> ";
                        return View(level2Data);
                    }
                    ViewBag.breadScrum = level1data + " --> " + losscode;
                    ViewBag.Level = level + 1;
                    ViewBag.LossID = Bid;

                    return View(level3Data);
                }
                else if (level == 3)
                {
                    int prevLevelId = Convert.ToInt32(lossdata.LossCodesLevel2ID);
                    int FirstLevelID = Convert.ToInt32(lossdata.LossCodesLevel1ID);
                    var level2scrum = db.tbllossescodes.Where(m => m.LossCodeID == prevLevelId).Select(m => m.LossCode).FirstOrDefault();
                    var level1scrum = db.tbllossescodes.Where(m => m.LossCodeID == FirstLevelID).Select(m => m.LossCode).FirstOrDefault();
                    var level2Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel2ID == prevLevelId && m.LossCodesLevel == 3).ToList();
                    ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + losscode + " as reason.";
                    ViewBag.LossID = Bid;
                    ViewBag.Level = 3;
                    ViewBag.breadScrum = level1scrum + " --> " + level2scrum + " --> ";
                    return View(level2Data);
                }
            }
            else
            {
                var level1Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel == 1 && m.MessageType != "NoCode" && m.MessageType != "BREAKDOWN" && m.MessageType != "PM").ToList();
                ViewBag.Level = 1;
                return View(level1Data);
            }

            //Fail Safe: if everything else fails send level1 codes.
            ViewBag.Level = 1;
            var level10Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel == 1 && m.MessageType != "NoCode" && m.MessageType != "BREAKDOWN" && m.MessageType != "PM").ToList();
            return View(level10Data);

            //var lossentry = db.tbllossofentries.Where(m => m.MachineID == macid).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
            //return View(lossentry);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DownCodeEntry(tbllossescode losscode, int HiddenID = 0)
        {

            ViewData["MessageCodeID"] = new SelectList(db.message_code_master.Where(m => m.IsDeleted == 0).Where(m => m.MessageType == "IDLE" || m.MessageType == "SETUP"), "MessageCodeID", "MessageDescription");
            int RotationCount = Convert.ToInt32(Session["Rotation"]);
            if (RotationCount == 0)
            {
                Session["Rotation"] = 1;
            }
            if (RotationCount >= 3)
            {
                Session["Rotation"] = 1;
                //return RedirectToAction("Index", "MachineStatus", null);
            }

            //corrected date
            string CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            if (DateTime.Now.Hour < 6 && DateTime.Now.Hour >= 0)
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            //tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            //TimeSpan Start = StartTime.StartTime;
            //if (Start <= DateTime.Now.TimeOfDay)
            //{
            //    CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            //}
            //else
            //{
            //    CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            //}



            //string MessageCode = lossentry.MessageCodeID.ToString();
            //var validatingCode = db.message_code_master.Where(m => m.MessageCode == MessageCode).Where(m => m.MessageType == "IDLE").FirstOrDefault();
            try
            {
                int machid = Convert.ToInt32(Session["MachineID"]);
                string shift = Session["realshift"].ToString();
                int isupdate = 0, donewithrow = 0;
                int lossid = 0;

                //here only 2 scenarios
                // 1 => isUpdate = 0 & doneWithRow = 0 :: we shall update the messageCode , isUpdate & stuff.
                // 2 => isUpdate = 1 & doneWithRow = 0 :: we shall update the old row & insert new one.
                using (mazakdaqEntities db1 = new mazakdaqEntities())
                {
                    var lossentryrow = db1.tbllivelossofentries.Where(m => m.MachineID == machid).OrderByDescending(m => m.LossID).Take(1).ToList();
                    foreach (var row in lossentryrow)
                    {
                        isupdate = row.IsUpdate;
                        donewithrow = row.DoneWithRow;
                        lossid = row.LossID;
                        break;
                    }
                }
                //IDLE Popup has come, now we are updating the Losscode will be selected by Operator.
                if (isupdate == 0 && donewithrow == 0)
                {
                    tbllivelossofentry lossentry = db.tbllivelossofentries.Find(lossid);
                    //lossentry.StartDateTime = GetIdleStartTime(0, CorrectedDate);
                    lossentry.EntryTime = DateTime.Now;
                    lossentry.EndDateTime = DateTime.Now;
                    //lossentry.CorrectedDate = CorrectedDate;
                    lossentry.MachineID = machid;
                    lossentry.Shift = shift;
                    if (HiddenID != 0)
                    {
                        using (mazakdaqEntities db2 = new mazakdaqEntities())
                        {
                            var lossdata = db2.tbllossescodes.Where(m => m.LossCodeID == HiddenID).FirstOrDefault();
                            lossentry.MessageCodeID = HiddenID;
                            lossentry.MessageDesc = lossdata.LossCodeDesc.ToString();
                            lossentry.MessageCode = lossdata.LossCode.ToString();
                        }
                    }
                    else
                    {
                        int abc = Convert.ToInt32(lossentry.MessageCodeID);
                        lossentry.MessageCodeID = Convert.ToInt32(lossentry.MessageCodeID);
                        var a = db.message_code_master.Find(abc);
                        lossentry.MessageDesc = a.MessageDescription.ToString();
                        lossentry.MessageCode = a.MessageCode.ToString();
                    }
                    lossentry.IsUpdate = 1;
                    lossentry.DoneWithRow = 0;
                    lossentry.IsStart = 1;
                    lossentry.IsScreen = 0;
                    lossentry.ForRefresh = 1;
                    db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                else if (isupdate == 1 && donewithrow == 0) // operator is entering new code for 2nd time and so on.
                {
                    var previousLoss = db.tbllivelossofentries.Find(lossid);
                    previousLoss.EndDateTime = DateTime.Now;
                    previousLoss.DoneWithRow = 1;
                    previousLoss.IsStart = 0;
                    previousLoss.IsScreen = 0;
                    previousLoss.ForRefresh = 0;
                    db.Entry(previousLoss).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    tbllivelossofentry loss = new tbllivelossofentry();

                    loss.StartDateTime = GetIdleStartTime(4, CorrectedDate);
                    loss.EntryTime = DateTime.Now;
                    loss.EndDateTime = DateTime.Now;
                    loss.CorrectedDate = CorrectedDate;
                    loss.MachineID = machid;
                    loss.Shift = shift;
                    if (HiddenID != 0)
                    {
                        var lossdata = db.tbllossescodes.Where(m => m.LossCodeID == HiddenID).FirstOrDefault();
                        loss.MessageCodeID = HiddenID;
                        loss.MessageDesc = lossdata.LossCodeDesc.ToString();
                        loss.MessageCode = lossdata.LossCode.ToString();
                    }
                    else
                    {
                        int abc = 999;
                        loss.MessageCodeID = Convert.ToInt32(loss.MessageCodeID);
                        var a = db.message_code_master.Find(abc);
                        loss.MessageDesc = a.MessageDescription.ToString();
                        loss.MessageCode = a.MessageCode.ToString();
                    }
                    loss.IsUpdate = 1;
                    loss.DoneWithRow = 0;
                    loss.IsStart = 1;
                    loss.IsScreen = 0;
                    loss.ForRefresh = 1;
                    db.tbllivelossofentries.Add(loss);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }

                #region old code
                ////if machine under setting( that is messageCodeID = 81 )
                //if (MessageCode != "81")
                //{
                //    //lossentry.Shift = Session["realshift"].ToString();
                //    //lossentry.EntryTime = DateTime.Now;
                //    //lossentry.StartDateTime = Convert.ToDateTime(Session["starttime"]);
                //    //lossentry.EndDateTime = DateTime.Now;
                //    //lossentry.CorrectedDate = CorrectedDate;
                //    //int abc = Convert.ToInt32(lossentry.MessageCodeID);
                //    //string msgcode = null;
                //    //var a = db.message_code_master.Find(abc);
                //    //lossentry.MessageDesc = a.MessageDescription.ToString();
                //    //lossentry.MessageCode = a.MessageCode.ToString();
                //    ////lossentry.MessageCodeID = validatingCode.MessageCodeID;
                //    //lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
                //    ////if (ModelState.IsValid)
                //    //{
                //    //    db.tbllossofentries.Add(lossentry);
                //    //    db.SaveChanges();
                //    //    return RedirectToAction("Index");
                //    //}

                //    int machid = Convert.ToInt32(Session["MachineID"]);
                //    string shift = Session["realshift"].ToString();
                //    var check = db.tbllossofentries.Where(m => m.MachineID == machid && m.CorrectedDate == CorrectedDate && m.IsUpdate == 0 && m.Shift == shift && m.DoneWithRow == 0).OrderByDescending(m => m.LossID);
                //    if (check.Count() != 0)
                //    {
                //        lossentry.EntryTime = DateTime.Now;
                //        lossentry.EndDateTime = DateTime.Now;
                //        int abc = Convert.ToInt32(lossentry.MessageCodeID);
                //        string msgcode = null;
                //        var a = db.message_code_master.Find(abc);
                //        lossentry.MessageDesc = a.MessageDescription.ToString();
                //        lossentry.MessageCode = a.MessageCode.ToString();
                //        lossentry.DoneWithRow = 0;
                //        lossentry.IsUpdate = 1;
                //        //lossentry.MessageCodeID = validatingCode.MessageCodeID;
                //        db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
                //        db.SaveChanges();
                //        return RedirectToAction("Index");
                //    }
                //    else
                //    {
                //        var check1 = db.tbllossofentries.Where(m => m.MachineID == machid && m.CorrectedDate == CorrectedDate && m.IsUpdate == 1 && m.Shift == shift && m.DoneWithRow == 0).OrderByDescending(m => m.LossID);
                //        if (check1.Count() > 0)
                //        {
                //            foreach (var c in check1)
                //            {
                //                c.EndDateTime = DateTime.Now;
                //                c.DoneWithRow = 1;
                //                db.Entry(c).State = System.Data.Entity.EntityState.Modified;
                //                db.SaveChanges();
                //                break;
                //            }
                //            tbllossofentry tle = new tbllossofentry();

                //            tle.StartDateTime = DateTime.Now;
                //            tle.EntryTime = DateTime.Now;
                //            tle.EndDateTime = DateTime.Now;
                //            tle.CorrectedDate = CorrectedDate;
                //            tle.MachineID = machid;
                //            tle.Shift = shift;
                //            int abc = Convert.ToInt32(lossentry.MessageCodeID);
                //            string msgcode = null;
                //            tle.MessageCodeID = Convert.ToInt32(lossentry.MessageCodeID);
                //            var a = db.message_code_master.Find(abc);
                //            tle.MessageDesc = a.MessageDescription.ToString();
                //            tle.MessageCode = a.MessageCode.ToString();
                //            tle.IsUpdate = 1;
                //            tle.DoneWithRow = 0;
                //            //lossentry.MessageCodeID = validatingCode.MessageCodeID;
                //            db.tbllossofentries.Add(tle);
                //            //db.Entry(tle).State = System.Data.Entity.EntityState.Modified;
                //            db.SaveChanges();
                //            return RedirectToAction("Index");
                //        }
                //    }

                //}
                //else
                //{
                //    //lossentry.Shift = Session["realshift"].ToString();
                //    //lossentry.EntryTime = DateTime.Now;
                //    //lossentry.StartDateTime = DateTime.Now;
                //    //lossentry.CorrectedDate = CorrectedDate;
                //    //int abc = Convert.ToInt32(lossentry.MessageCodeID);
                //    //string msgcode = null;
                //    //var a = db.message_code_master.Find(abc);
                //    //lossentry.MessageDesc = a.MessageDescription.ToString();
                //    //lossentry.MessageCode = a.MessageCode.ToString();
                //    ////lossentry.MessageCodeID = validatingCode.MessageCodeID;
                //    //lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
                //    //if (ModelState.IsValid)
                //    //{
                //    //    db.tbllossofentries.Add(lossentry);
                //    //    db.SaveChanges();
                //    //    return RedirectToAction("Index");
                //    //}

                //    int machid = Convert.ToInt32(Session["MachineID"]);
                //    string shift = Session["realshift"].ToString();
                //    var check = db.tbllossofentries.Where(m => m.MachineID == machid && m.CorrectedDate == CorrectedDate && m.IsUpdate == 1 && m.Shift == shift && m.DoneWithRow == 0).OrderByDescending(m => m.LossID);
                //    if (check.Count() == 0)
                //    {
                //        lossentry.EntryTime = DateTime.Now;
                //        lossentry.EndDateTime = DateTime.Now;
                //        int abc = Convert.ToInt32(lossentry.MessageCodeID);
                //        string msgcode = null;
                //        var a = db.message_code_master.Find(abc);
                //        lossentry.MessageDesc = a.MessageDescription.ToString();
                //        lossentry.MessageCode = a.MessageCode.ToString();

                //        lossentry.DoneWithRow = 0;

                //        lossentry.IsUpdate = 1;
                //        //lossentry.MessageCodeID = validatingCode.MessageCodeID;
                //        db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
                //        db.SaveChanges();
                //        return RedirectToAction("Index");
                //    }
                //    else
                //    {
                //        foreach (var b in check)
                //        {
                //            b.EndDateTime = DateTime.Now;
                //            //j 2016-06-15
                //            //b.DoneWithRow = 1;
                //            db.Entry(b).State = System.Data.Entity.EntityState.Modified;
                //            db.SaveChanges();
                //            break;
                //        }

                //        lossentry.StartDateTime = DateTime.Now;
                //        lossentry.EntryTime = DateTime.Now;
                //        lossentry.EndDateTime = DateTime.Now;
                //        int abc = Convert.ToInt32(lossentry.MessageCodeID);
                //        string msgcode = null;
                //        var a = db.message_code_master.Find(abc);
                //        lossentry.MessageDesc = a.MessageDescription.ToString();
                //        lossentry.MessageCode = a.MessageCode.ToString();
                //        lossentry.IsUpdate = 1;
                //        //lossentry.MessageCodeID = validatingCode.MessageCodeID;
                //        db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
                //        db.SaveChanges();
                //        return RedirectToAction("Index");
                //    }

                //}
                #endregion

            }
            catch { }
            return RedirectToAction("Index");
        }

        public string GetLossPath(int LossCode)
        {
            string path = null;
            var lossdata = db.tbllossescodes.Find(LossCode);
            int level = lossdata.LossCodesLevel;
            string losscode = lossdata.LossCode;
            if (level == 1)
            {
                path = losscode;
            }
            else if (level == 2)
            {
                int prevLevelId = Convert.ToInt32(lossdata.LossCodesLevel1ID);
                var level1data = db.tbllossescodes.Where(m => m.LossCodeID == prevLevelId).Select(m => m.LossCode).FirstOrDefault();
                path = level1data + " --> " + losscode;
            }
            else if (level == 3)
            {
                int prevLevelId = Convert.ToInt32(lossdata.LossCodesLevel2ID);
                int FirstLevelID = Convert.ToInt32(lossdata.LossCodesLevel1ID);
                var level2scrum = db.tbllossescodes.Where(m => m.LossCodeID == prevLevelId).Select(m => m.LossCode).FirstOrDefault();
                var level1scrum = db.tbllossescodes.Where(m => m.LossCodeID == FirstLevelID).Select(m => m.LossCode).FirstOrDefault();

                path = level1scrum + " --> " + level2scrum + " --> " + losscode;
            }


            return path;
        }

        public ActionResult Details(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            tbllivehmiscreen tblhmiscreen = db.tbllivehmiscreens.Find(id);
            if (tblhmiscreen == null)
            {
                return HttpNotFound();
            }
            return View(tblhmiscreen);
        }

        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tbllivehmiscreen tblhmiscreen)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            if (ModelState.IsValid)
            {
                db.tbllivehmiscreens.Add(tblhmiscreen);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tblhmiscreen);
        }

        //Control comes here when jobfinish is clicked.
        public ActionResult Edit(int id = 0, int reworkorderhidden = 0, int cjtextbox9 = 0, int cjtextbox8 = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            int reworkyes = Convert.ToInt32(reworkorderhidden);
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            //Getting Shift Value
            DateTime Time = DateTime.Now;
            TimeSpan Tm = new TimeSpan(Time.Hour, Time.Minute, Time.Second);
            var ShiftDetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm && m.EndTime >= Tm);
            string Shift = null;
            foreach (var a in ShiftDetails)
            {
                Shift = a.ShiftName;
            }
            ViewBag.date = System.DateTime.Now;
            if (Shift != null)
                ViewBag.shift = Shift;
            else
                ViewBag.shift = "C";

            int machineID = 0;
            tbllivehmiscreen tblhmiscreen = db.tbllivehmiscreens.Find(id);
            machineID = Convert.ToInt32(tblhmiscreen.MachineID);

            int Uid = tblhmiscreen.OperatiorID;

            int ID = id;
            tbllivehmiscreen OldWork = db.tbllivehmiscreens.Find(ID);
            OldWork.Status = 2;
            OldWork.ProcessQty = cjtextbox9;
            OldWork.Delivered_Qty = cjtextbox8;
            OldWork.Time = DateTime.Now;
            //update isWorkInProgress When WorkIs finished is clicked.

            //SplitWO
            OldWork.SplitWO = "No";

            OldWork.isWorkInProgress = 1;//job finished

            if (reworkorderhidden == 1)
            {
                OldWork.isWorkOrder = 1;
            }

            string Shiftgen = OldWork.Shift;
            string operatorName = OldWork.OperatorDet;
            int Opgid = OldWork.OperatiorID;

            List<string> MacHierarchy = GetHierarchyData(machineID);
            int IsWOMultiWO = OldWork.IsMultiWO;
            int HMIId = OldWork.HMIID;
            if (IsWOMultiWO == 0)
            {
                string woNo = Convert.ToString(OldWork.Work_Order_No);
                string opNo = Convert.ToString(OldWork.OperationNo);
                string partNo = Convert.ToString(OldWork.PartNo);
                int OperationNoInt = Convert.ToInt32(opNo);

                #region 2017-07-01

                string WIPQuery6 = @"SELECT * from tblddl where WorkOrder = '" + woNo + "' and MaterialDesc = '" + partNo + "' and OperationNo != '" + opNo + "'  and IsCompleted = 0 order by WorkOrder,MaterialDesc,OperationNo ";
                var WIPDDL6 = db.tblddls.SqlQuery(WIPQuery6).ToList();
                foreach (var row in WIPDDL6)
                {
                    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                    string ddlopno = row.OperationNo;
                    if (InnerOpNo < OperationNoInt)
                    {
                        int PrvProcessQty = 0, PrvDeliveredQty = 0, TotalProcessQty = 0, ishold = 0;
                        #region new code
                        //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                        int isHMIFirst = 2; //default NO History for that wo,pn,on

                        // Modified by Ashok
                        var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == ddlopno && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                        var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == ddlopno && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();

                        if (hmiData.Count > 0 && mulitwoData.Count > 0) // now check for greatest amongst
                        {
                            DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].tbllivehmiscreen.Time);
                            DateTime hmiDateTime = Convert.ToDateTime(hmiData[0].Time);

                            if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                            {
                                isHMIFirst = 1;
                            }
                            else
                            {
                                isHMIFirst = 0;
                            }

                        }
                        else if (mulitwoData.Count > 0)
                        {
                            isHMIFirst = 1;
                        }
                        else if (hmiData.Count > 0)
                        {
                            isHMIFirst = 0;
                        }

                        if (isHMIFirst == 1)
                        {
                            string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                            int delivInt = 0;
                            int.TryParse(delivString, out delivInt);

                            string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                            int procInt = 0;
                            int.TryParse(processString, out procInt);

                            PrvProcessQty += procInt;
                            PrvDeliveredQty += delivInt;

                            ishold = mulitwoData[0].tbllivehmiscreen.IsHold;
                            ishold = ishold == 2 ? 0 : ishold;

                        }
                        else if (isHMIFirst == 0)
                        {
                            string delivString = Convert.ToString(hmiData[0].Delivered_Qty);
                            int delivInt = 0;
                            int.TryParse(delivString, out delivInt);

                            string processString = Convert.ToString(hmiData[0].ProcessQty);
                            int procInt = 0;
                            int.TryParse(processString, out procInt);

                            PrvProcessQty += procInt;
                            PrvDeliveredQty += delivInt;

                            ishold = hmiData[0].IsHold;
                            ishold = ishold == 2 ? 0 : ishold;
                        }
                        else
                        {
                            //no previous delivered or processed qty so Do Nothing.
                        }
                        #endregion
                        TotalProcessQty = PrvProcessQty + PrvDeliveredQty;
                        //var hmiPFed = db.tblhmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo).OrderByDescending(m => m.Time).FirstOrDefault();

                        if (Convert.ToInt32(row.TargetQty) == TotalProcessQty)
                        {
                            #region
                            if (isHMIFirst == 1 && Convert.ToInt32(row.TargetQty) < Convert.ToInt32(mulitwoData[0].TargetQty))
                            {
                                int hmiidmultitbl = Convert.ToInt32(mulitwoData[0].HMIID);
                                var hmiTomulittblData = db.tbllivehmiscreens.Find(hmiidmultitbl);
                                if (hmiTomulittblData != null)
                                {
                                    tbllivehmiscreen tblh = new tbllivehmiscreen();
                                    tblh.CorrectedDate = row.CorrectedDate;
                                    tblh.Date = DateTime.Now;
                                    tblh.Time = DateTime.Now;
                                    tblh.PEStartTime = DateTime.Now;
                                    tblh.DDLWokrCentre = hmiTomulittblData.DDLWokrCentre;
                                    tblh.Delivered_Qty = 0;
                                    tblh.DoneWithRow = 1;
                                    tblh.IsHold = 0;
                                    tblh.IsMultiWO = 0;
                                    tblh.isUpdate = 1;
                                    tblh.isWorkInProgress = 1;
                                    tblh.isWorkOrder = hmiTomulittblData.isWorkOrder;
                                    tblh.MachineID = hmiTomulittblData.MachineID;
                                    tblh.OperationNo = mulitwoData[0].OperationNo;
                                    tblh.OperatiorID = hmiTomulittblData.OperatiorID;
                                    tblh.OperatorDet = hmiTomulittblData.OperatorDet;
                                    tblh.PartNo = mulitwoData[0].PartNo;
                                    tblh.ProcessQty = TotalProcessQty;
                                    tblh.Prod_FAI = hmiTomulittblData.Prod_FAI;
                                    tblh.Project = hmiTomulittblData.Project;
                                    tblh.Rej_Qty = hmiTomulittblData.Rej_Qty;
                                    tblh.Shift = hmiTomulittblData.Shift;
                                    tblh.SplitWO = "No";
                                    tblh.Status = hmiTomulittblData.Status;
                                    tblh.Target_Qty = TotalProcessQty;
                                    tblh.Work_Order_No = mulitwoData[0].WorkOrder;

                                    db.tbllivehmiscreens.Add(tblh);
                                    db.SaveChanges();
                                }

                            }
                            else if (isHMIFirst == 0 && Convert.ToInt32(row.TargetQty) < Convert.ToInt32(hmiData[0].Target_Qty))
                            {
                                tbllivehmiscreen tblh = new tbllivehmiscreen();
                                tblh.CorrectedDate = row.CorrectedDate;
                                tblh.Date = DateTime.Now;
                                tblh.Time = DateTime.Now;
                                tblh.PEStartTime = DateTime.Now;
                                tblh.DDLWokrCentre = hmiData[0].DDLWokrCentre;
                                tblh.Delivered_Qty = 0;
                                tblh.DoneWithRow = 1;
                                tblh.IsHold = 0;
                                tblh.IsMultiWO = hmiData[0].IsMultiWO;
                                tblh.isUpdate = 1;
                                tblh.isWorkInProgress = 1;
                                tblh.isWorkOrder = hmiData[0].isWorkOrder;
                                tblh.MachineID = hmiData[0].MachineID;
                                tblh.OperationNo = hmiData[0].OperationNo;
                                tblh.OperatiorID = hmiData[0].OperatiorID;
                                tblh.OperatorDet = hmiData[0].OperatorDet;
                                tblh.PartNo = hmiData[0].PartNo;
                                tblh.ProcessQty = TotalProcessQty;
                                tblh.Prod_FAI = hmiData[0].Prod_FAI;
                                tblh.Project = hmiData[0].Project;
                                tblh.Rej_Qty = hmiData[0].Rej_Qty;
                                tblh.Shift = hmiData[0].Shift;
                                tblh.SplitWO = hmiData[0].SplitWO;
                                tblh.Status = hmiData[0].Status;
                                tblh.Target_Qty = TotalProcessQty;
                                tblh.Work_Order_No = hmiData[0].Work_Order_No;

                                db.tbllivehmiscreens.Add(tblh);
                                db.SaveChanges();
                            }
                            #endregion

                            row.IsCompleted = 1;
                            db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }

                #endregion

                //OpNo sequence
                #region 2017-02-07
                //New Logic to Overcome WorkOrder Sequence Scenario 2017-02-03
                string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + woNo + "' and MaterialDesc = '" + partNo + "' and OperationNo != '" + opNo + "'  and IsCompleted = 0 order by WorkOrder,MaterialDesc,OperationNo ";
                var WIPDDL1 = db.tblddls.SqlQuery(WIPQuery1).ToList();
                foreach (var row in WIPDDL1)
                {
                    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                    if (InnerOpNo < OperationNoInt)
                    {
                        if (row.IsCompleted == 0)
                        {
                            Session["Error"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            Session["Error"] = null;
                        }

                        //bool IsItWrong = false;
                        //string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID desc limit 1 ";
                        //var WIP = db.tblhmiscreens.SqlQuery(WIPQueryHMI).ToList();

                        //if (WIP.Count == 0)
                        //{
                        //    Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                        //    IsItWrong = true;
                        //}
                        //else
                        //{
                        //    foreach (var rowHMI in WIP)
                        //    {
                        //        if (rowHMI.isWorkInProgress != 1) //=> lower OpNo is in HMIScreen & not Finished.
                        //        {
                        //            Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                        //            //return RedirectToAction("Index");
                        //            IsItWrong = true;
                        //        }
                        //    }
                        //}
                        //if (IsItWrong)
                        //{
                        //    //Strange , it might have been started in Normal WorkCenter as MultiWorkOrder.
                        //    string WIPQueryMultiWO = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by MultiWOID desc limit 1 ";
                        //    var WIPMWO = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWO).ToList();

                        //    if (WIPMWO.Count == 0)
                        //    {
                        //        Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                        //        return RedirectToAction("Index");
                        //    }

                        //    foreach (var rowHMI in WIPMWO)
                        //    {
                        //        int hmiid = Convert.ToInt32(rowHMI.HMIID);
                        //        var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                        //        if (MWOHMIData != null)
                        //        {
                        //            if (MWOHMIData.isWorkInProgress != 1) //=> lower OpNo is not Finished.
                        //            {
                        //                Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                        //                return RedirectToAction("Index");
                        //            }
                        //        }
                        //    }
                        //}

                    }
                }

                //2017-06-01 No need check manual entries
                ////string WIPQuery = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where Work_Order_No = '" + wono + "' and PartNo = '" + partno + "' and OperationNo != '" + opno + "' order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo ";
                //// 2017-01-21
                //string WIPQuery = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' and IsMultiWO = 0 and DDLWokrCentre is null order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo  ) order by OperationNo ;";
                //var WIPOuter = db.tblhmiscreens.SqlQuery(WIPQuery).ToList();
                //if (WIPOuter.Count == 0)
                //{
                //}
                //else
                //{
                //    foreach (var row in WIPOuter)
                //    {
                //        int InnerOpNo = Convert.ToInt32(row.OperationNo);
                //        if (InnerOpNo < OperationNoInt)
                //        {
                //            if (row.isWorkInProgress != 1) //=> lower OpNo is not JF 'ed.
                //            {
                //                Session["VError"] = " JobFinish WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                //                return RedirectToAction("Index");
                //                //break;
                //            }
                //        }
                //    }
                //}
                #endregion

                using (mazakdaqEntities dbsimilar = new mazakdaqEntities())
                {
                    #region If its as SingleWO
                    var SimilarWOData = dbsimilar.tbllivehmiscreens.Where(m => m.HMIID != OldWork.HMIID && m.Work_Order_No == OldWork.Work_Order_No && m.OperationNo == OldWork.OperationNo && m.PartNo == OldWork.PartNo && m.MachineID != machineID && m.isWorkInProgress == 2).FirstOrDefault();
                    if (SimilarWOData != null)
                    {
                        int InnerMacID = Convert.ToInt32(dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == SimilarWOData.HMIID).Select(m => m.MachineID).FirstOrDefault());
                        var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());

                        Session["Error"] = " Same WorkOrder in Machine: " + MacDispName + " , So you cannot JobFinish ";
                        return RedirectToAction("Index");
                    }
                    #endregion

                    #region If its as MultiWO
                    var SimilarWODataMulti = dbsimilar.tbllivemultiwoselections.Where(m => m.WorkOrder == OldWork.Work_Order_No && m.OperationNo == OldWork.OperationNo && m.PartNo == OldWork.PartNo && m.HMIID != HMIId && m.IsCompleted == 0).FirstOrDefault();
                    if (SimilarWODataMulti != null)
                    {
                        int InnerHMIID = (int)SimilarWODataMulti.HMIID;
                        var InnerHMIDupData = dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == InnerHMIID).FirstOrDefault();
                        if (InnerHMIDupData != null)
                        {
                            if (InnerHMIDupData.isWorkInProgress == 2)
                            {
                                int InnerMacID = Convert.ToInt32(InnerHMIDupData.MachineID);
                                var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());
                                Session["Error"] = " Same WorkOrder in Machine: " + MacDispName + " , So you cannot JobFinish ";
                                return RedirectToAction("Index");
                            }
                        }
                    }
                    #endregion
                }

                var DDLData = db.tblddls.Where(m => m.MaterialDesc == partNo && m.OperationNo == opNo && m.WorkOrder == woNo).FirstOrDefault();
                if (DDLData != null)
                {
                    DDLData.IsCompleted = 1;
                    db.Entry(DDLData).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }
            else
            {



                using (mazakdaqEntities dbsimilar = new mazakdaqEntities())
                {
                    var multiWOSelectionDataInner = dbsimilar.tbllivemultiwoselections.Where(m => m.HMIID == HMIId).ToList();

                    //string DDLIDString = string.Join(",", data.Select(x => x.ToString()).ToArray());
                    string OPString = string.Join(",", multiWOSelectionDataInner.Select(x => x.OperationNo).ToArray());


                    #region 2017-07-01
                    foreach (var rowMulti in multiWOSelectionDataInner)
                    {
                        string woNo = Convert.ToString(rowMulti.WorkOrder);
                        string opNo = Convert.ToString(rowMulti.OperationNo);
                        string partNo = Convert.ToString(rowMulti.PartNo);
                        int OperationNoInt = Convert.ToInt32(opNo);

                        //New Logic to Overcome WorkOrder Sequence Scenario 2017-02-03
                        string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + woNo + "' and MaterialDesc = '" + partNo + "' and OperationNo != '" + opNo + "' and IsCompleted = 0 order by WorkOrder,MaterialDesc,OperationNo ";
                        var WIPDDL1 = db.tblddls.SqlQuery(WIPQuery1).ToList();
                        foreach (var row in WIPDDL1)
                        {
                            int InnerOpNo = Convert.ToInt32(row.OperationNo);
                            string ddlopno = row.OperationNo;
                            if (InnerOpNo < OperationNoInt)
                            {
                                if (OPString.Contains(Convert.ToString(row.OperationNo)))
                                { }
                                else
                                {
                                    int PrvProcessQty = 0, PrvDeliveredQty = 0, TotalProcessQty = 0, ishold = 0;
                                    #region new code
                                    //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                                    int isHMIFirst = 2; //default NO History for that wo,pn,on

                                    var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == ddlopno && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                                    var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == ddlopno && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();

                                    if (hmiData.Count > 0 && mulitwoData.Count > 0) // now check for greatest amongst
                                    {
                                        DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].tbllivehmiscreen.Time);
                                        DateTime hmiDateTime = Convert.ToDateTime(hmiData[0].Time);

                                        if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                                        {
                                            isHMIFirst = 1;
                                        }
                                        else
                                        {
                                            isHMIFirst = 0;
                                        }

                                    }
                                    else if (mulitwoData.Count > 0)
                                    {
                                        isHMIFirst = 1;
                                    }
                                    else if (hmiData.Count > 0)
                                    {
                                        isHMIFirst = 0;
                                    }

                                    if (isHMIFirst == 1)
                                    {
                                        string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                                        int delivInt = 0;
                                        int.TryParse(delivString, out delivInt);

                                        string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                                        int procInt = 0;
                                        int.TryParse(processString, out procInt);

                                        PrvProcessQty += procInt;
                                        PrvDeliveredQty += delivInt;

                                        ishold = mulitwoData[0].tbllivehmiscreen.IsHold;
                                        ishold = ishold == 2 ? 0 : ishold;

                                    }
                                    else if (isHMIFirst == 0)
                                    {
                                        string delivString = Convert.ToString(hmiData[0].Delivered_Qty);
                                        int delivInt = 0;
                                        int.TryParse(delivString, out delivInt);

                                        string processString = Convert.ToString(hmiData[0].ProcessQty);
                                        int procInt = 0;
                                        int.TryParse(processString, out procInt);

                                        PrvProcessQty += procInt;
                                        PrvDeliveredQty += delivInt;

                                        ishold = hmiData[0].IsHold;
                                        ishold = ishold == 2 ? 0 : ishold;
                                    }
                                    else
                                    {
                                        //no previous delivered or processed qty so Do Nothing.
                                    }
                                    #endregion
                                    TotalProcessQty = PrvProcessQty + PrvDeliveredQty;

                                    if (Convert.ToInt32(row.TargetQty) == TotalProcessQty)
                                    {
                                        #region
                                        if (isHMIFirst == 1 && Convert.ToInt32(row.TargetQty) < Convert.ToInt32(mulitwoData[0].TargetQty))
                                        {
                                            int hmiidmultitbl = Convert.ToInt32(mulitwoData[0].HMIID);
                                            var hmiTomulittblData = db.tbllivehmiscreens.Find(hmiidmultitbl);
                                            if (hmiTomulittblData != null)
                                            {
                                                tbllivehmiscreen tblh = new tbllivehmiscreen();
                                                tblh.CorrectedDate = row.CorrectedDate;
                                                tblh.Date = DateTime.Now;
                                                tblh.Time = DateTime.Now;
                                                tblh.PEStartTime = DateTime.Now;
                                                tblh.DDLWokrCentre = hmiTomulittblData.DDLWokrCentre;
                                                tblh.Delivered_Qty = 0;
                                                tblh.DoneWithRow = 1;
                                                tblh.IsHold = 0;
                                                tblh.IsMultiWO = 0;
                                                tblh.isUpdate = 1;
                                                tblh.isWorkInProgress = 1;
                                                tblh.isWorkOrder = hmiTomulittblData.isWorkOrder;
                                                tblh.MachineID = hmiTomulittblData.MachineID;
                                                tblh.OperationNo = mulitwoData[0].OperationNo;
                                                tblh.OperatiorID = hmiTomulittblData.OperatiorID;
                                                tblh.OperatorDet = hmiTomulittblData.OperatorDet;
                                                tblh.PartNo = mulitwoData[0].PartNo;
                                                tblh.ProcessQty = TotalProcessQty;
                                                tblh.Prod_FAI = hmiTomulittblData.Prod_FAI;
                                                tblh.Project = hmiTomulittblData.Project;
                                                tblh.Rej_Qty = hmiTomulittblData.Rej_Qty;
                                                tblh.Shift = hmiTomulittblData.Shift;
                                                tblh.SplitWO = "No";
                                                tblh.Status = hmiTomulittblData.Status;
                                                tblh.Target_Qty = TotalProcessQty;
                                                tblh.Work_Order_No = mulitwoData[0].WorkOrder;

                                                db.tbllivehmiscreens.Add(tblh);
                                                db.SaveChanges();
                                            }

                                        }
                                        else if (isHMIFirst == 0 && Convert.ToInt32(row.TargetQty) < Convert.ToInt32(hmiData[0].Target_Qty))
                                        {
                                            tbllivehmiscreen tblh = new tbllivehmiscreen();
                                            tblh.CorrectedDate = row.CorrectedDate;
                                            tblh.Date = DateTime.Now;
                                            tblh.Time = DateTime.Now;
                                            tblh.PEStartTime = DateTime.Now;
                                            tblh.DDLWokrCentre = hmiData[0].DDLWokrCentre;
                                            tblh.Delivered_Qty = 0;
                                            tblh.DoneWithRow = 1;
                                            tblh.IsHold = 0;
                                            tblh.IsMultiWO = hmiData[0].IsMultiWO;
                                            tblh.isUpdate = 1;
                                            tblh.isWorkInProgress = 1;
                                            tblh.isWorkOrder = hmiData[0].isWorkOrder;
                                            tblh.MachineID = hmiData[0].MachineID;
                                            tblh.OperationNo = hmiData[0].OperationNo;
                                            tblh.OperatiorID = hmiData[0].OperatiorID;
                                            tblh.OperatorDet = hmiData[0].OperatorDet;
                                            tblh.PartNo = hmiData[0].PartNo;
                                            tblh.ProcessQty = TotalProcessQty;
                                            tblh.Prod_FAI = hmiData[0].Prod_FAI;
                                            tblh.Project = hmiData[0].Project;
                                            tblh.Rej_Qty = hmiData[0].Rej_Qty;
                                            tblh.Shift = hmiData[0].Shift;
                                            tblh.SplitWO = hmiData[0].SplitWO;
                                            tblh.Status = hmiData[0].Status;
                                            tblh.Target_Qty = TotalProcessQty;
                                            tblh.Work_Order_No = hmiData[0].Work_Order_No;

                                            db.tbllivehmiscreens.Add(tblh);
                                            db.SaveChanges();
                                        }
                                        #endregion

                                        row.IsCompleted = 1;
                                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                                        db.SaveChanges();
                                    }

                                }
                            }
                        }
                    }
                    #endregion

                    #region 2017-02-07
                    foreach (var rowMulti in multiWOSelectionDataInner)
                    {

                        string woNo = Convert.ToString(rowMulti.WorkOrder);
                        string opNo = Convert.ToString(rowMulti.OperationNo);
                        string partNo = Convert.ToString(rowMulti.PartNo);
                        int OperationNoInt = Convert.ToInt32(opNo);

                        //New Logic to Overcome WorkOrder Sequence Scenario 2017-02-03
                        string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + woNo + "' and MaterialDesc = '" + partNo + "' and OperationNo != '" + opNo + "' and IsCompleted = 0 order by WorkOrder,MaterialDesc,OperationNo ";
                        var WIPDDL1 = db.tblddls.SqlQuery(WIPQuery1).ToList();
                        foreach (var row in WIPDDL1)
                        {
                            int InnerOpNo = Convert.ToInt32(row.OperationNo);
                            if (InnerOpNo < OperationNoInt)
                            {
                                if (OPString.Contains(Convert.ToString(row.OperationNo)))
                                { }
                                else
                                {
                                    if (row.IsCompleted == 0)
                                    {
                                        Session["Error"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                        return RedirectToAction("Index");
                                    }
                                    else
                                    {
                                        Session["Error"] = null;
                                    }

                                    //bool IsItWrong = false;
                                    //string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID desc limit 1 ";
                                    //var WIP = db.tblhmiscreens.SqlQuery(WIPQueryHMI).ToList();

                                    //if (WIP.Count == 0)
                                    //{
                                    //    Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                    //    IsItWrong = true;
                                    //}
                                    //else
                                    //{
                                    //    foreach (var rowHMI in WIP)
                                    //    {
                                    //        if (rowHMI.isWorkInProgress != 1) //=> lower OpNo is in HMIScreen & not Finished.
                                    //        {
                                    //            Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                    //            //return RedirectToAction("Index");
                                    //            IsItWrong = false;
                                    //        }
                                    //        else
                                    //        {
                                    //            Session["VError"] = null;
                                    //            IsItWrong = false;
                                    //        }
                                    //    }
                                    //}
                                    //if (IsItWrong)
                                    //{
                                    //    //Strange , it might have been started in Normal WorkCenter as MultiWorkOrder.
                                    //    string WIPQueryMultiWO = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by MultiWOID desc limit 1 ";
                                    //    var WIPMWO = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWO).ToList();

                                    //    if (WIPMWO.Count == 0)
                                    //    {
                                    //        Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                    //        return RedirectToAction("Index");
                                    //    }

                                    //    foreach (var rowHMI in WIPMWO)
                                    //    {
                                    //        int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                    //        var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                    //        if (MWOHMIData != null)
                                    //        {
                                    //            if (MWOHMIData.isWorkInProgress != 1) //=> lower OpNo is not Finished.
                                    //            {
                                    //                Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                    //                return RedirectToAction("Index");
                                    //            }
                                    //            else
                                    //            {
                                    //                Session["VError"] = null;
                                    //            }
                                    //        }
                                    //    }
                                    //}

                                }
                            }
                        }

                        //string WIPQuery = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' and IsMultiWO = 0 and DDLWokrCentre is null order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo  ) order by OperationNo ;";
                        //var WIPOuter = db.tblhmiscreens.SqlQuery(WIPQuery).ToList();
                        //if (WIPOuter.Count == 0)
                        //{
                        //}
                        //else
                        //{
                        //    foreach (var row in WIPOuter)
                        //    {
                        //        int InnerOpNo = Convert.ToInt32(row.OperationNo);
                        //        if (InnerOpNo < OperationNoInt)
                        //        {
                        //            if (row.isWorkInProgress != 1) //=> lower OpNo is not JF 'ed.
                        //            {
                        //                Session["VError"] = " JobFinish WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                        //                return RedirectToAction("Index");
                        //                break;
                        //            }
                        //        }
                        //    }
                        //}
                    }
                    #endregion
                }



                using (mazakdaqEntities dbsimilar = new mazakdaqEntities())
                {
                    var multiWOSelectionDataInner = dbsimilar.tbllivemultiwoselections.Where(m => m.HMIID == HMIId).ToList();
                    foreach (var row in multiWOSelectionDataInner)
                    {
                        try
                        {
                            #region If its as SingleWO
                            var SimilarWOData = dbsimilar.tbllivehmiscreens.Where(m => m.HMIID != HMIId && m.Work_Order_No == row.WorkOrder && m.OperationNo == row.OperationNo && m.PartNo == row.PartNo && m.MachineID != machineID && m.isWorkInProgress == 2).FirstOrDefault();
                            if (SimilarWOData != null)
                            {
                                int InnerMacID = Convert.ToInt32(dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == SimilarWOData.HMIID).Select(m => m.MachineID).FirstOrDefault());
                                var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());

                                Session["Error"] = " Same WorkOrder in Machine: " + MacDispName;
                                return RedirectToAction("Index");
                            }
                            #endregion

                            #region If its as MultiWO
                            var SimilarWODataMulti = dbsimilar.tbllivemultiwoselections.Where(m => m.WorkOrder == row.WorkOrder && m.OperationNo == row.OperationNo && m.PartNo == row.PartNo && m.HMIID != HMIId && m.tbllivehmiscreen.isWorkInProgress == 2).FirstOrDefault();
                            if (SimilarWODataMulti != null)
                            {
                                int InnerHMIID = (int)SimilarWODataMulti.HMIID;

                                //Again check this hmiid in hmiscreen is finished or not
                                //if not thorw error
                                var InnerHMIDupData = dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == InnerHMIID).FirstOrDefault();
                                if (InnerHMIDupData != null)
                                {
                                    if (InnerHMIDupData.isWorkInProgress == 2)
                                    {
                                        int InnerMacID = Convert.ToInt32(InnerHMIDupData.MachineID);
                                        var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());
                                        Session["Error"] = " Same WorkOrder in Machine: " + MacDispName;
                                        return RedirectToAction("Index");
                                    }
                                }
                            }
                            #endregion
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }

                var multiWOSelectionData = db.tbllivemultiwoselections.Where(m => m.HMIID == HMIId).ToList();
                foreach (var row in multiWOSelectionData)
                {
                    string woNo = Convert.ToString(row.WorkOrder);
                    string opNo = Convert.ToString(row.OperationNo);
                    string partNo = Convert.ToString(row.PartNo);
                    int deliveredQty = Convert.ToInt32(row.DeliveredQty);
                    int targetqty = Convert.ToInt32(row.TargetQty);
                    //if (deliveredQty == targetqty)
                    {
                        row.IsCompleted = 1;
                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }

                    var DDLData = db.tblddls.Where(m => m.MaterialDesc == partNo && m.OperationNo == opNo && m.WorkOrder == woNo).FirstOrDefault();
                    if (DDLData != null)
                    {
                        DDLData.IsCompleted = 1;
                        DDLData.DeliveredQty = deliveredQty;

                        db.Entry(DDLData).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            OldWork.SplitWO = "No";
            db.Entry(OldWork).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            //insert a new row if there is no row for this machine for this shift.
            tbllivehmiscreen HMI = db.tbllivehmiscreens.Where(m => m.CorrectedDate == CorrectedDate).Where(m => m.MachineID == machineID && m.Status == 0).FirstOrDefault();
            if (HMI == null)
            {
                
                tbllivehmiscreen NewEntry = new tbllivehmiscreen();
                NewEntry.MachineID = machineID;
                NewEntry.CorrectedDate = CorrectedDate;
                NewEntry.PEStartTime = DateTime.Now;
                //NewEntry.Date = DateTime.Now;
                //NewEntry.Time = DateTime.Now;
                NewEntry.Shift = Convert.ToString(Shiftgen);
                NewEntry.OperatorDet = operatorName;
                NewEntry.Status = 0;
                NewEntry.isWorkInProgress = 2;
                NewEntry.OperatiorID = Opgid;
               // NewEntry.HMIID = (HMMID.HMIID + 1); // by Ashok
                db.tbllivehmiscreens.Add(NewEntry);
                db.SaveChanges();

                Session["FromDDL"] = 0;
                Session["SubmitClicked"] = 0;
            }
            return RedirectToAction("Index");
        }

        //control comes here when PartialFinish is pressed.
        public ActionResult EditWIP(string SplitWO = null, int id = 0, int reworkorderhidden = 0, int cjtextbox9 = 0, int cjtextbox8 = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            //Getting Shift Value
            DateTime Time = DateTime.Now;
            TimeSpan Tm = new TimeSpan(Time.Hour, Time.Minute, Time.Second);
            var ShiftDetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm && m.EndTime >= Tm);
            string Shift = null;
            foreach (var a in ShiftDetails)
            {
                Shift = a.ShiftName;
            }
            ViewBag.date = System.DateTime.Now;
            if (Shift != null)
                ViewBag.shift = Shift;
            else
                ViewBag.shift = "C";

            int machineID = 0;
            tbllivehmiscreen tblhmiscreen = db.tbllivehmiscreens.Find(id);
            machineID = Convert.ToInt32(tblhmiscreen.MachineID);
            int Uid = tblhmiscreen.OperatiorID;
            int ID = id;
            tbllivehmiscreen OldWork = db.tbllivehmiscreens.Find(ID);

            //2017-06-02 
            //OldWork.ProcessQty = cjtextbox9;
            OldWork.Delivered_Qty = cjtextbox8;
            OldWork.Status = 2;
            OldWork.Time = DateTime.Now;
            //else save workInProgress and continue
            OldWork.isWorkInProgress = 0;//work is in progress
            if (reworkorderhidden == 1)
            {
                OldWork.isWorkOrder = 1;
            }
            string Shiftgen = OldWork.Shift;
            string operatorName = OldWork.OperatorDet;
            int Opgid = OldWork.OperatiorID;

            int IsWOMultiWO = OldWork.IsMultiWO;
            //some how id is being passed to Index when redirected. so 
            id = 0;
            if (IsWOMultiWO == 0 && SplitWO.Length > 0 && !string.IsNullOrEmpty(SplitWO.Trim()))
            {
                OldWork.SplitWO = SplitWO;
            }
            int OldDeliveredQty = Convert.ToInt32(OldWork.Delivered_Qty);

            int Hmiid = OldWork.HMIID;
            List<string> MacHierarchy = GetHierarchyData(machineID);

            //Update ProcessedQty of Same WorkOrder , OpNo , PartNo In Other Machines IF(IsWorkInProgress == 2)
            using (mazakdaqEntities dbsimilar = new mazakdaqEntities())
            {
                if (IsWOMultiWO == 0)
                {

                    if (Convert.ToInt32(OldWork.Target_Qty) < (Convert.ToInt32(OldWork.Delivered_Qty) + Convert.ToInt32(OldWork.ProcessQty)))
                    {
                        Session["Error"] = " DeliveredQty + ProcessedQty should be equal to Target for WONo: " + OldWork.Work_Order_No + " OpNo: " + OldWork.OperationNo;
                        return RedirectToAction("Index");
                    }

                    #region If its as SingleWO
                    var SimilarWOData = dbsimilar.tbllivehmiscreens.Where(m => m.Work_Order_No == OldWork.Work_Order_No && m.OperationNo == OldWork.OperationNo && m.PartNo == OldWork.PartNo && m.MachineID != machineID && m.isWorkInProgress == 2).ToList();
                    foreach (var row in SimilarWOData)
                    {
                        int InnerDelivered = Convert.ToInt32(row.Delivered_Qty);
                        int InnerProcessed = Convert.ToInt32(row.ProcessQty);
                        int FinalProcessed = InnerDelivered + InnerProcessed;
                        if (FinalProcessed < row.Target_Qty)
                        {
                            if (row.isWorkInProgress == 2)
                            {
                                row.ProcessQty = FinalProcessed;
                                dbsimilar.Entry(row).State = System.Data.Entity.EntityState.Modified;
                                dbsimilar.SaveChanges();
                            }
                        }
                        else
                        {
                            Session["Error"] = " Same WorkOrder in Machine: " + MacHierarchy[3] + "->" + MacHierarchy[4] + "has ProcessedQty :" + InnerProcessed;
                            return RedirectToAction("Index");
                        }
                    }
                    #endregion

                    #region If its as MultiWO
                    var SimilarWODataMulti = dbsimilar.tbllivemultiwoselections.Where(m => m.WorkOrder == OldWork.Work_Order_No && m.OperationNo == OldWork.OperationNo && m.PartNo == OldWork.PartNo && m.HMIID != Hmiid && m.tbllivehmiscreen.isWorkInProgress == 2).ToList();
                    foreach (var row in SimilarWODataMulti)
                    {
                        int RowHMIID = Convert.ToInt32(row.HMIID);
                        var localhmiData = dbsimilar.tbllivehmiscreens.Find(RowHMIID);

                        //int InnerDelivered = Convert.ToInt32(row.DeliveredQty);
                        int InnerProcessed = Convert.ToInt32(row.ProcessQty);
                        int FinalProcessed = OldDeliveredQty + InnerProcessed;
                        if (FinalProcessed < row.TargetQty)
                        {
                            if (localhmiData.isWorkInProgress == 2)
                            {
                                row.ProcessQty = FinalProcessed;
                                dbsimilar.Entry(row).State = System.Data.Entity.EntityState.Modified;
                                dbsimilar.SaveChanges();

                                //Update tblhmiscreen table row.
                                if (localhmiData != null)
                                {
                                    localhmiData.ProcessQty += OldDeliveredQty;
                                    dbsimilar.Entry(localhmiData).State = System.Data.Entity.EntityState.Modified;
                                    dbsimilar.SaveChanges();
                                }
                            }
                        }
                        else
                        {
                            Session["Error"] = " Same WorkOrder in Machine: " + MacHierarchy[3] + "->" + MacHierarchy[4] + "has ProcessedQty :" + InnerProcessed;
                            return RedirectToAction("Index");
                        }

                        //int InnerHMIID = (int)row.HMIID;
                        //var InnerHMIDupData = dbsimilar.tblhmiscreens.Where(m => m.HMIID == InnerHMIID && m.HMIID != Hmiid).FirstOrDefault();
                        //if (InnerHMIDupData != null)
                        //{
                        //    if (InnerHMIDupData.isWorkInProgress == 2)
                        //    {
                        //        int InnerMacID = Convert.ToInt32(InnerHMIDupData.MachineID);
                        //        var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());
                        //        Session["Error"] = " Same WorkOrder in Machine: " + MacDispName + " , So you cannot JobFinish ";
                        //        return RedirectToAction("Index");
                        //    }
                        //}
                    }
                    #endregion
                }
                else
                {
                    #region
                    var multiWOSelectionData = db.tbllivemultiwoselections.Where(m => m.HMIID == Hmiid).ToList();
                    //during pf dont allow jf of higher opno
                    var multiWOSelectionData1 = multiWOSelectionData.OrderBy(m => m.PartNo).ThenBy(m => m.WorkOrder).ThenBy(m => m.OperationNo).ToList();

                    string OPString = string.Join(",", multiWOSelectionData1.Select(x => x.OperationNo).ToArray());
                    foreach (var row in multiWOSelectionData1)
                    {
                        try
                        {
                            String WONo = row.WorkOrder;
                            String Part = row.PartNo;
                            String Operation = row.OperationNo;
                            int opInt = Convert.ToInt32(Operation);

                            int TargetQtyOut = Convert.ToInt32(row.TargetQty);
                            int ProcessedQtyOut = Convert.ToInt32(row.ProcessQty);
                            int DeliveredQtyOut = Convert.ToInt32(row.DeliveredQty);
                            //if (TargetQtyOut <= (ProcessedQtyOut + DeliveredQtyOut))
                            if (TargetQtyOut == (ProcessedQtyOut + DeliveredQtyOut))
                            {
                                foreach (var rowInner in multiWOSelectionData1)
                                {
                                    String OperationInner = rowInner.OperationNo;
                                    int opInnerInt = Convert.ToInt32(OperationInner);
                                    if (opInnerInt < opInt)
                                    {
                                        String WONoInner = rowInner.WorkOrder;
                                        String PartInner = rowInner.PartNo;
                                        //now check if they are about to JF, if so throw error.
                                        int TargetQty = Convert.ToInt32(rowInner.TargetQty);
                                        int ProcessedQty = Convert.ToInt32(rowInner.ProcessQty);
                                        int DeliveredQty = Convert.ToInt32(rowInner.DeliveredQty);
                                        if ((ProcessedQty + DeliveredQty) < TargetQty)
                                        {
                                            //if (OPString.Contains(OperationInner))
                                            //{ }
                                            //else
                                            //{
                                            TempData["VError"] = "Please finish this job first. WoNo: " + rowInner.WorkOrder + " OpNo: " + rowInner.OperationNo;
                                            return RedirectToAction("Index");
                                            //}
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {

                        }
                    }

                    //Check for Total Quantity compatibility
                    foreach (var row in multiWOSelectionData)
                    {

                        string LocalWONO = row.WorkOrder;
                        string LocalPartNo = row.PartNo;
                        string LocalOpNo = row.OperationNo;
                        int LocalDelivered = Convert.ToInt32(row.DeliveredQty);

                        if (Convert.ToInt32(row.TargetQty) < (Convert.ToInt32(row.DeliveredQty) + Convert.ToInt32(row.ProcessQty)))
                        {
                            Session["Error"] = " DeliveredQty + ProcessedQty should be equal to Target for WONo: " + LocalWONO + " OpNo: " + LocalOpNo;
                            return RedirectToAction("Index");
                        }

                        try
                        {
                            var SimilarWOData = dbsimilar.tbllivehmiscreens.Where(m => m.Work_Order_No == LocalWONO && m.OperationNo == LocalOpNo && m.PartNo == LocalPartNo && m.MachineID != machineID && m.isWorkInProgress == 2).ToList();
                            foreach (var Innerrow in SimilarWOData)
                            {
                                int InnerProcessed = Innerrow.ProcessQty;
                                //int InnerDelivered = Convert.ToInt32(Innerrow.Delivered_Qty);
                                int FinalProcessed = LocalDelivered + InnerProcessed;
                                if (FinalProcessed < Innerrow.Target_Qty)
                                {
                                    if (Innerrow.isWorkInProgress == 2)
                                    {
                                        Innerrow.ProcessQty = FinalProcessed;
                                        dbsimilar.Entry(Innerrow).State = System.Data.Entity.EntityState.Modified;
                                        dbsimilar.SaveChanges();
                                    }
                                }
                                else
                                {
                                    Session["Error"] = " Same WorkOrder in Machine: " + MacHierarchy[3] + "->" + MacHierarchy[4] + " , Target Qty Exceeds.";
                                    return RedirectToAction("Index");
                                }
                            }

                            #region If its as MultiWO
                            var SimilarWODataMulti = dbsimilar.tbllivemultiwoselections.Where(m => m.WorkOrder == LocalWONO && m.OperationNo == LocalOpNo && m.PartNo == LocalPartNo && m.HMIID != Hmiid && m.tbllivehmiscreen.isWorkInProgress == 2).ToList();
                            foreach (var Innerrow in SimilarWODataMulti)
                            {
                                //update only if its still in hmiscreen
                                int RowHMIID = Convert.ToInt32(row.HMIID);
                                var localhmiData = dbsimilar.tbllivehmiscreens.Find(RowHMIID);
                                int DeliveredQtyLocal = Convert.ToInt32(Innerrow.DeliveredQty);
                                int InnerProcessed = Convert.ToInt32(Innerrow.ProcessQty);
                                int FinalProcessed = DeliveredQtyLocal + InnerProcessed;
                                if (FinalProcessed < Innerrow.TargetQty)
                                {
                                    if (localhmiData.isWorkInProgress == 2)
                                    {
                                        Innerrow.ProcessQty = FinalProcessed;
                                        dbsimilar.Entry(Innerrow).State = System.Data.Entity.EntityState.Modified;
                                        dbsimilar.SaveChanges();

                                        //Update tblhmiscreen table row.
                                        if (localhmiData != null)
                                        {
                                            localhmiData.ProcessQty += DeliveredQtyLocal;
                                            dbsimilar.Entry(localhmiData).State = System.Data.Entity.EntityState.Modified;
                                            dbsimilar.SaveChanges();
                                        }
                                    }
                                }
                                else
                                {
                                    Session["Error"] = " Same WorkOrder in Machine: " + MacHierarchy[3] + "->" + MacHierarchy[4] + "has ProcessedQty :" + InnerProcessed;
                                    return RedirectToAction("Index");
                                }

                                int InnerHMIID = (int)Innerrow.HMIID;
                                var InnerHMIDupData = dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == InnerHMIID).FirstOrDefault();
                                if (InnerHMIDupData != null)
                                {
                                    if (InnerHMIDupData.isWorkInProgress == 2)
                                    {
                                        int InnerMacID = Convert.ToInt32(InnerHMIDupData.MachineID);
                                        var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());
                                        Session["Error"] = " Same WorkOrder in Machine: " + MacDispName + " , So you cannot JobFinish ";
                                        return RedirectToAction("Index");
                                    }
                                }
                            }
                            #endregion
                        }
                        catch (Exception e)
                        {
                        }
                    }

                    #endregion
                }
            }

            db.Entry(OldWork).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            if (IsWOMultiWO == 1)
            {
                int hmiid = OldWork.HMIID;
                var multiWOSelectionData = db.tbllivemultiwoselections.Where(m => m.HMIID == hmiid).ToList();
                foreach (var row in multiWOSelectionData)
                {
                    string woNo = Convert.ToString(row.WorkOrder);
                    string opNo = Convert.ToString(row.OperationNo);
                    string partNo = Convert.ToString(row.PartNo);
                    int deliveredQty = Convert.ToInt32(row.DeliveredQty);
                    int targetqty = Convert.ToInt32(row.TargetQty);
                    int processedQty = Convert.ToInt32(row.ProcessQty);
                    if ((deliveredQty + processedQty) == targetqty)
                    {
                        try
                        {
                            row.IsCompleted = 1;
                            //row.SplitWO = SplitWO;
                            db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                        }
                        try
                        {
                            var DDLData = db.tblddls.Where(m => m.MaterialDesc == partNo && m.OperationNo == opNo && m.WorkOrder == woNo).FirstOrDefault();
                            DDLData.DeliveredQty = deliveredQty + processedQty;
                            DDLData.IsCompleted = 1;
                            db.Entry(DDLData).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }

            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            //insert a new row if there is no row for this machine for this shift.
            tbllivehmiscreen HMI = db.tbllivehmiscreens.Where(m => m.CorrectedDate == CorrectedDate).Where(m => m.MachineID == machineID && m.Status == 0).FirstOrDefault();
            if (HMI == null)
            {
                
                tbllivehmiscreen NewEntry = new tbllivehmiscreen();
                NewEntry.MachineID = machineID;
                NewEntry.CorrectedDate = CorrectedDate;
                NewEntry.PEStartTime = DateTime.Now;
                //NewEntry.Date = DateTime.Now;
                //NewEntry.Time = DateTime.Now;
                NewEntry.OperatorDet = operatorName;
                NewEntry.Shift = Convert.ToString(Shiftgen);
                NewEntry.Status = 0;
                NewEntry.isWorkInProgress = 2;
                NewEntry.OperatiorID = Opgid;
               // NewEntry.HMIID = (HMMID.HMIID + 1);  //by Ashok
                db.tbllivehmiscreens.Add(NewEntry);
                db.SaveChanges();

                Session["FromDDL"] = 0;
                Session["SubmitClicked"] = 0;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tbllivehmiscreen tblhmiscreen)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            if (ModelState.IsValid)
            {
                tblhmiscreen.isWorkInProgress = 0;
                db.Entry(tblhmiscreen).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tblhmiscreen);
        }

        public ActionResult Delete(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            tbllivehmiscreen tblhmiscreen = db.tbllivehmiscreens.Find(id);
            if (tblhmiscreen == null)
            {
                return HttpNotFound();
            }
            return View(tblhmiscreen);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            using (mazakdaqEntities dbhmi = new mazakdaqEntities())
            {
                try
                {
                    tbllivehmiscreen tblhmiscreen = dbhmi.tbllivehmiscreens.Find(id);
                    db.tbllivehmiscreens.Remove(tblhmiscreen);
                    db.SaveChanges();

                    if (tblhmiscreen != null && tblhmiscreen.IsMultiWO == 1)
                    {
                        dbhmi.tbllivemultiwoselections.RemoveRange(dbhmi.tbllivemultiwoselections.Where(m => m.HMIID == tblhmiscreen.HMIID).ToList());
                        dbhmi.SaveChanges();
                    }
                }
                catch (Exception e)
                {

                }
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create1(List<tblhmiscreen> tblstat_prodcyctime)
        {
            return RedirectToAction("Index");
        }

        public ActionResult setupentry(int id = 0)
        {
            TempData["Enable"] = null;
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            var brkdown = db.tbllivelossofentries.Where(m => m.MachineID == id).Where(m => m.CorrectedDate == CorrectedDate && m.EndDateTime == null && m.MessageCodeID == 81);
            int mdid = 0;
            foreach (var jd in brkdown)
            {
                mdid = jd.LossID;
            }
            tbllivelossofentry loss = db.tbllivelossofentries.Find(mdid);
            if (ModelState.IsValid)
            {
                loss.EndDateTime = DateTime.Now;
                db.Entry(loss).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        public ActionResult Dashboard(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            return RedirectToAction("Index", "Dashboard");
        }

        //breakdownlist
        public ActionResult BreakDownList(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            ViewBag.opid = Session["opid"];
            ViewBag.mcnid = id;
            ViewBag.coretddt = CorrectedDate;

            //bool tick = checkingIdle();
            //if (tick == true)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //    //ViewBag.tick = 1;
            //}

            int handleidleReturnValue = HandleIdle();
            if (handleidleReturnValue == 0)
            {
                return RedirectToAction("DownCodeEntry");
            }

            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == id).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);

            //var breakdown = db.tblbreakdowns.Include(t=>t.machine_master).Include(t=>t.message_code_master).Where(m=>m.MachineID==id && m.CorrectedDate==CorrectedDate).ToList();
            var breakdown = db.tblbreakdowns.Include(t => t.tbllossescode).Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate && m.DoneWithRow == 1).OrderByDescending(m => m.StartTime).ToList();
            return View(breakdown);
        }

        //IdleList
        public ActionResult IdleList(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            ViewBag.opid = Session["opid"];
            ViewBag.mcnid = id;

            string Shift = null;
            MsqlConnection mcp = new MsqlConnection();
            mcp.open();
            String queryshift = "SELECT ShiftName,StartTime,EndTime FROM tblshift_mstr WHERE IsDeleted = 0";
            MySqlDataAdapter dashift = new MySqlDataAdapter(queryshift, mcp.msqlConnection);
            DataTable dtshift = new DataTable();
            dashift.Fill(dtshift);
            String[] msgtime = System.DateTime.Now.TimeOfDay.ToString().Split(':');
            TimeSpan msgstime = System.DateTime.Now.TimeOfDay;
            //TimeSpan msgstime = new TimeSpan(Convert.ToInt32(msgtime[0]), Convert.ToInt32(msgtime[1]), Convert.ToInt32(msgtime[2]));
            TimeSpan s1t1 = new TimeSpan(0, 0, 0), s1t2 = new TimeSpan(0, 0, 0), s2t1 = new TimeSpan(0, 0, 0), s2t2 = new TimeSpan(0, 0, 0);
            TimeSpan s3t1 = new TimeSpan(0, 0, 0), s3t2 = new TimeSpan(0, 0, 0), s3t3 = new TimeSpan(0, 0, 0), s3t4 = new TimeSpan(23, 59, 59);
            for (int k = 0; k < dtshift.Rows.Count; k++)
            {
                if (dtshift.Rows[k][0].ToString().Contains("A"))
                {
                    String[] s1 = dtshift.Rows[k][1].ToString().Split(':');
                    s1t1 = new TimeSpan(Convert.ToInt32(s1[0]), Convert.ToInt32(s1[1]), Convert.ToInt32(s1[2]));
                    String[] s11 = dtshift.Rows[k][2].ToString().Split(':');
                    s1t2 = new TimeSpan(Convert.ToInt32(s11[0]), Convert.ToInt32(s11[1]), Convert.ToInt32(s11[2]));
                }
                else if (dtshift.Rows[k][0].ToString().Contains("B"))
                {
                    String[] s1 = dtshift.Rows[k][1].ToString().Split(':');
                    s2t1 = new TimeSpan(Convert.ToInt32(s1[0]), Convert.ToInt32(s1[1]), Convert.ToInt32(s1[2]));
                    String[] s11 = dtshift.Rows[k][2].ToString().Split(':');
                    s2t2 = new TimeSpan(Convert.ToInt32(s11[0]), Convert.ToInt32(s11[1]), Convert.ToInt32(s11[2]));
                }
                else if (dtshift.Rows[k][0].ToString().Contains("C"))
                {
                    String[] s1 = dtshift.Rows[k][1].ToString().Split(':');
                    s3t1 = new TimeSpan(Convert.ToInt32(s1[0]), Convert.ToInt32(s1[1]), Convert.ToInt32(s1[2]));
                    String[] s11 = dtshift.Rows[k][2].ToString().Split(':');
                    s3t2 = new TimeSpan(Convert.ToInt32(s11[0]), Convert.ToInt32(s11[1]), Convert.ToInt32(s11[2]));
                }
            }
            CorrectedDate = System.DateTime.Now.ToString("yyyy-MM-dd");
            if (msgstime >= s1t1 && msgstime < s1t2)
            {
                Shift = "A";
            }
            else if (msgstime >= s2t1 && msgstime < s2t2)
            {
                Shift = "B";
            }
            else if ((msgstime >= s3t1 && msgstime <= s3t4) || (msgstime >= s3t3 && msgstime < s3t2))
            {
                Shift = "C";
                if (msgstime >= s3t3 && msgstime < s3t2)
                {
                    CorrectedDate = System.DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                }
            }
            mcp.close();



            //checking shift end.

            //string SEnd = checkShiftEnd();
            //if (SEnd == "yes")
            //{
            //    return View();
            //}

            //bool tick = checkingIdle();
            //if (tick == true)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //}

            //int RotationCount = Convert.ToInt32(Session["Rotation"]);
            //if (RotationCount == 0)
            //{
            //    Session["Rotation"] = 1;
            //}
            //if (RotationCount == 6)
            //{
            //    Session["Rotation"] = 1;
            //    //return RedirectToAction("Index", "MachineStatus", null);
            //}

            int handleidleReturnValue = HandleIdle();
            if (handleidleReturnValue == 0)
            {
                return RedirectToAction("DownCodeEntry");
            }

            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == id).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);

            ViewBag.coretddt = CorrectedDate;
            //var idle = db.tbllossofentries.Include(t => t.machine_master).Include(t => t.message_code_master).Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate).ToList();
            var idle = db.tbllivelossofentries.Include(t => t.tbllossescode).Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate && m.DoneWithRow == 1).OrderByDescending(m => m.StartDateTime).ToList();
            return View(idle);
        }

        public ActionResult BrakDownEntry(int id = 0, int Bid = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            #region old
            ////if tblmode or tbldailyprodstatus has breakdown in them for today then update tblbreakdown now.
            ////will help in showing options on entry screen
            ////this happens because we are taking last available mode for that machine and updating it for today if we don't have mode for now.

            //var tblbreakdown = db.tblbreakdowns.Where(m => m.MachineID == id).OrderByDescending(m => m.StartTime);
            //foreach (var row in tblbreakdown)
            //{
            //    string date = row.CorrectedDate;
            //    string today = DateTime.Now.ToString("yyyy-MM-dd" );
            //    if (date != today)
            //    {
            //        var tblmode = db.tblmodes.Where(m => m.MachineID == id && m.IsDeleted == 0).OrderByDescending(m => m.InsertedOn);
            //        foreach (var rowIntblmode in tblmode)
            //        {
            //            if (rowIntblmode.Mode == "BREAKDOWN")
            //            {
            //                DateTime insertedOn = rowIntblmode.InsertedOn;

            //            }

            //            break;
            //        }

            //    }
            //    break;
            //}
            //doing this in daq is better
            #endregion

            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            Session["Mid"] = id;
            int machineid = Convert.ToInt32(Session["MachineID"]);
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == id).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);

            //Stage 1: check if we r allowd to set this mode 
            //CODE to check the current mode is allowable or not , based on MODE Priority.
            var curMode = db.tblbreakdowns.Where(m => m.MachineID == id && m.DoneWithRow == 0).OrderByDescending(m => m.BreakdownID).Take(1).ToList();
            int currentId = 0;
            foreach (var j in curMode)
            {
                currentId = j.BreakdownID;
                string mode = j.tbllossescode.MessageType;

                if (mode == "PM")
                {
                    Session["ModeError"] = "Machine is in Maintenance , cannot change mode to Breakdown";
                    return RedirectToAction("Index");
                }
                //else if (mode == "BREAKDOWN")
                //{
                //    Session["ModeError"] = "Machine is in Breakdown Mode";
                //    return RedirectToAction("Index");
                //}
                else if (mode != "BREAKDOWN")
                {
                    tblbreakdown tbd = db.tblbreakdowns.Find(currentId);
                    tbd.EndTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    db.Entry(tbd).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //tbllossofentry tle = 
                    break;
                }
            }


            //stage 2. Breakdown is running and u need to send data to view regarding that.

            var breakdownToView = db.tblbreakdowns.Where(m => m.MachineID == machineid && m.DoneWithRow == 0).OrderByDescending(m => m.BreakdownID).FirstOrDefault();
            if (breakdownToView != null) //implies brekdown is running
            {
                if (breakdownToView.DoneWithRow == 0)
                {
                    int breakdowncode = Convert.ToInt32(breakdownToView.BreakDownCode);
                    var DataToView = db.tbllossescodes.Where(m => m.LossCodeID == breakdowncode).ToList();
                    ViewBag.Level = DataToView[0].LossCodesLevel;
                    ViewBag.BreakdownCode = DataToView[0].LossCode;
                    ViewBag.BreakdownId = DataToView[0].LossCodeID;
                    ViewBag.BreakdownStartTime = breakdownToView.StartTime;
                    return View(DataToView);
                }

            }


            //var brkdown = db.tblbreakdowns.Where(m => m.MachineID == id).Where(m => m.CorrectedDate == CorrectedDate && m.EndTime == null && m.message_code_master.MessageType == "BREAKDOWN");
            //if (brkdown.Count() != 0)
            //{
            //    Session["ItsBreakDown"] = "yes";
            //    int brekdnID = 0;
            //    foreach (var a in brkdown)
            //    {
            //        brekdnID = a.BreakdownID;
            //    }
            //    tblbreakdown brekdn = db.tblbreakdowns.Find(brekdnID);
            //    CheckLastOneHourDownTime(id);
            //    ViewBag.BreakDownCode = new SelectList(db.message_code_master.Where(m => m.IsDeleted == 0).Where(m => m.MessageType == "BREAKDOWN"), "MessageCodeID", "MessageDescription", brekdn.BreakDownCode);
            //    return View(brekdn);
            //}
            //else
            //{

            //}

            //This is needed but not now.
            //CheckLastOneHourDownTime(id);


            //stage 3. Operator is selecting the breakdown by traversing down the Hierarchy of BreakdownCodes.
            if (Bid != 0)
            {
                var breakdata = db.tbllossescodes.Find(Bid);
                int level = breakdata.LossCodesLevel;
                string breakdowncode = breakdata.LossCode;

                if (level == 1)
                {
                    var level2Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel1ID == Bid && m.LossCodesLevel2ID == null && m.MessageType == "BREAKDOWN").ToList();
                    if (level2Data.Count == 0)
                    {
                        var level1Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel == 1 && m.LossCodesLevel1ID == null && m.LossCodesLevel2ID == null && m.MessageType == "BREAKDOWN").ToList();
                        ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + breakdowncode + " as reason.";
                        ViewBag.BreakDownID = Bid;
                        ViewBag.Level = level;
                        ViewBag.breadScrum = breakdowncode + "-->  ";
                        return View(level1Data);
                    }
                    ViewBag.Level = level + 1;
                    ViewBag.BreakDownID = Bid;
                    ViewBag.breadScrum = breakdowncode + "-->  ";
                    return View(level2Data);
                }
                else if (level == 2)
                {
                    var level3Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel2ID == Bid && m.MessageType == "BREAKDOWN").ToList();
                    int prevLevelId = Convert.ToInt32(breakdata.LossCodesLevel1ID);
                    var level1data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodeID == prevLevelId).Select(m => m.LossCode).FirstOrDefault();
                    if (level3Data.Count == 0)
                    {
                        var level2Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel1ID == prevLevelId && m.MessageType == "BREAKDOWN" && m.LossCodesLevel2ID == null).ToList();
                        ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + breakdowncode + " as reason.";
                        ViewBag.BreakDownID = Bid;
                        ViewBag.Level = level;
                        ViewBag.breadScrum = level1data + " --> " + breakdowncode + "-->  ";
                        return View(level2Data);
                    }
                    ViewBag.Level = level + 1;
                    ViewBag.BreakDownID = Bid;
                    ViewBag.breadScrum = level1data + " --> " + breakdowncode + "-->  ";
                    return View(level3Data);
                }
                else if (level == 3)
                {
                    int prevLevelId = Convert.ToInt32(breakdata.LossCodesLevel2ID);
                    int FirstLevelID = Convert.ToInt32(breakdata.LossCodesLevel1ID);
                    var level2scrum = db.tbllossescodes.Where(m => m.LossCodeID == prevLevelId).Select(m => m.LossCode).FirstOrDefault();
                    var level1scrum = db.tbllossescodes.Where(m => m.LossCodeID == FirstLevelID).Select(m => m.LossCode).FirstOrDefault();
                    var level2Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel2ID == prevLevelId && m.MessageType == "BREAKDOWN").ToList();
                    ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + breakdowncode + " as reason.";
                    ViewBag.BreakDownID = Bid;
                    ViewBag.Level = 3;
                    ViewBag.breadScrum = level1scrum + " --> " + level2scrum + "--> ";
                    return View(level2Data);
                }
            }
            else
            {
                var level1Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel == 1 && m.MessageType == "BREAKDOWN" && m.LossCode != "9999").ToList();
                ViewBag.Level = 1;
                return View(level1Data);
            }

            //Fail Safe: if everything else fails send level1 codes.
            ViewBag.Level = 1;
            var level10Data = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel == 1 && m.MessageType == "BREAKDOWN" && m.LossCode != "9999").ToList();
            return View(level10Data);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BrakDownEntry(tbllossescode tbdc, string EndBreakdown = null, int HiddenID = 0)
        {
            //"EndBreakdown" is for insert new row or update old one. Basically speeking its like start and Stop of Breakdown.
            //"HiddenID" is the BreakdownID of row to be set as reason.

            int MachineID = Convert.ToInt32(Session["MachineID"]);
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int RID = Convert.ToInt32(Session["RoleID"]);
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            DateTime Time = DateTime.Now;
            TimeSpan Tm = new TimeSpan(Time.Hour, Time.Minute, Time.Second);
            var ShiftDetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm && m.EndTime >= Tm);
            string Shift = "C";
            foreach (var a in ShiftDetails)
            {
                Shift = a.ShiftName;
            }


            if (HiddenID != 0 && string.IsNullOrEmpty(EndBreakdown) == true)
            {
                var breakdata = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodeID == HiddenID).FirstOrDefault();
                string msgCode = breakdata.LossCode;
                string msgDesc = breakdata.LossCodeDesc;

                tblbreakdown tb = new tblbreakdown();
                tb.BreakDownCode = HiddenID;
                tb.CorrectedDate = CorrectedDate;
                tb.DoneWithRow = 0;
                tb.MachineID = Convert.ToInt32(Session["MachineID"]);
                tb.MessageCode = msgCode;
                tb.MessageDesc = msgDesc;
                tb.Shift = Shift;
                tb.StartTime = DateTime.Now;
                db.tblbreakdowns.Add(tb);
                db.SaveChanges();

                //Code to End PreviousMode(Production Here) & save this event to tblmode table
                var modedata = db.tbllivemodedbs.Where(m => m.MachineID == MachineID && m.IsCompleted == 0).OrderByDescending(m => m.StartTime).FirstOrDefault();
                if (modedata != null)
                {
                    modedata.IsCompleted = 1;
                    modedata.EndTime = DateTime.Now;
                    db.Entry(modedata).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                tbllivemodedb tm = new tbllivemodedb();
                
                tm.MachineID = Convert.ToInt32(Session["MachineID"]);
                tm.CorrectedDate = CorrectedDate;
                tm.InsertedBy = Convert.ToInt32(Session["UserId"]);
                tm.StartTime = DateTime.Now;
                tm.ColorCode = "red";
                tm.InsertedOn = DateTime.Now;
                tm.IsDeleted = 0;
                tm.Mode = "BREAKDOWN";
                tm.IsCompleted = 0;
                
                db.tbllivemodedbs.Add(tm);
                db.SaveChanges();

            }
            else if (HiddenID != 0 && string.IsNullOrEmpty(EndBreakdown) == false)
            {
                var tb = db.tblbreakdowns.Where(m => m.BreakDownCode == HiddenID && m.MachineID == MachineID && m.DoneWithRow == 0).OrderByDescending(m => m.BreakdownID).FirstOrDefault();
                tb.EndTime = DateTime.Now;
                tb.DoneWithRow = 1;

                db.Entry(tb).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                //get the latest row and update it.
                var modedata = db.tbllivemodedbs.Where(m => m.MachineID == MachineID && m.IsCompleted == 0).OrderByDescending(m => m.StartTime).FirstOrDefault();
                if (modedata != null)
                {
                    modedata.IsCompleted = 1;
                    modedata.EndTime = DateTime.Now;
                    db.Entry(modedata).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                tbllivemodedb tmIDLE = new tbllivemodedb();
                tmIDLE.ColorCode = "yellow";
                tmIDLE.CorrectedDate = CorrectedDate;
                tmIDLE.InsertedBy = Convert.ToInt32(Session["UserId"]);
                tmIDLE.InsertedOn = DateTime.Now;
                tmIDLE.IsCompleted = 0;
                tmIDLE.IsDeleted = 0;
                tmIDLE.MachineID = MachineID;
                tmIDLE.Mode = "IDLE";
                tmIDLE.StartTime = DateTime.Now;

                db.tbllivemodedbs.Add(tmIDLE);
                db.SaveChanges();
            }

            #region OLD
            //if (string.IsNullOrEmpty(submit) == false)
            //{
            //    lossentry.CorrectedDate = CorrectedDate;
            //    lossentry.StartTime = DateTime.Now;
            //    if (RID != 1 && RID != 2)
            //    {
            //        lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
            //        MachineID = Convert.ToInt32(Session["MachineID"]);
            //    }
            //    else
            //    {
            //        lossentry.MachineID = Convert.ToInt32(Session["Mid"]);
            //        MachineID = Convert.ToInt32(Session["Mid"]);
            //    }
            //    message_code_master downcode = db.message_code_master.Find(lossentry.BreakDownCode);
            //    //lossentry.BreakDownCode = Convert.ToInt32(downcode.MessageCode);
            //    lossentry.Shift = Session["realshift"].ToString();
            //    //lossentry.BreakDownCode =Convert.ToInt32(downcode.MessageCode);
            //    lossentry.MessageCode = (downcode.MessageCode).ToString();
            //    db.tblbreakdowns.Add(lossentry);
            //    db.SaveChanges();

            //    //update the endtime for the last mode of this machine 
            //    var tblmodedata = db.tblmodes.Where(m => m.IsDeleted == 0 && m.MachineID == MachineID).OrderByDescending(m => m.StartTime).ToList();
            //    foreach (var row in tblmodedata)
            //    {
            //        row.EndTime = DateTime.Now;
            //        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
            //        db.SaveChanges();
            //    }

            //    //Code to save this event to tblmode table
            //    tblmode tm = new tblmode();
            //    tm.MachineID = MachineID;
            //    tm.CorrectedDate = CorrectedDate;
            //    tm.InsertedBy = 1;
            //    tm.StartTime = DateTime.Now;
            //    tm.ColorCode = "red";
            //    tm.InsertedOn = DateTime.Now;

            //    tm.IsDeleted = 0;
            //    tm.Mode = "BREAKDOWN";

            //    db.tblmodes.Add(tm);
            //    db.SaveChanges();


            //    //SendMail(downcode.MessageCode, downcode.MessageDescription, MachineID);
            //    return RedirectToAction("Index");
            //}
            //else
            //{
            //    lossentry.CorrectedDate = CorrectedDate;
            //    lossentry.EndTime = DateTime.Now;
            //    MachineID = Convert.ToInt32(lossentry.MachineID);
            //    //lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
            //    db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
            //    db.SaveChanges();
            //    UpdateRecordOfProduction(lossentry);
            //    int code = Convert.ToInt32(lossentry.BreakDownCode);
            //    message_code_master msg = db.message_code_master.Where(m => m.MessageCodeID == code).FirstOrDefault();
            //    //SendMailEnd(msg.MessageCode, msg.MessageDescription, MachineID);
            //    return RedirectToAction("Index");
            //}
            ////return View(lossentry);
            #endregion
            return RedirectToAction("Index");
        }

        public void UpdateRecordOfProduction(tblbreakdown a)
        {
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            var oldData = db.tbldailyprodstatus.Where(m => m.CorrectedDate == CorrectedDate).Where(m => m.StartTime >= a.StartTime).Where(m => m.EndTime <= a.EndTime).Where(m => m.MachineID == a.MachineID);
            if (oldData != null)
            {
                if (ModelState.IsValid)
                {
                    foreach (var newdata in oldData)
                    {
                        newdata.ColorCode = "yellow";
                        db.Entry(newdata).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
        }

        public bool SendMail(string messagecode, string messagedescription, int MachineID)
        {
            tblmachinedetail machin = db.tblmachinedetails.Find(MachineID);
            string MachineName = machin.MachineDispName;
            MailMessage mail = new MailMessage();

            //mail.To.Add(new MailAddress("pavan.v@srkssolutions.com"));
            //mail.To.Add(new MailAddress("deepak.Jojode@srkssolutions.com"));

            //mail.CC.Add(new MailAddress("srinidhi.kashyap@srkssolutions.com"));
            mail.To.Add(new MailAddress("janardhan.g@srkssolutions.com"));

            //mail.Bcc.Add(new MailAddress("narendra.kumar@srkssolutions.com"));
            //mail.To.Add(new MailAddress("pskumar@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("bpdesai@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("vkasinath@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("vsanghavi@tata.com"));
            //mail.CC.Add(new MailAddress("sgopu@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("soumyaagrawal@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("pkbhanja@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("tshabareesan@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("mmrafi@tataadvancedsystems.com"));
            //mail.Bcc.Add(new MailAddress("pavan.v@srkssolutions.com"));
            //mail.Bcc.Add(new MailAddress("narendramourya@live.com"));
            //mail.Bcc.Add(new MailAddress("srinidhi.kashyap@srkssolutions.com"));
            //mail.Bcc.Add(new MailAddress("deepak.Jojode@srkssolutions.com"));

            mail.From = new MailAddress("narendramourya37@gmail.com");
            mail.Subject = MachineName + " BreakDown Alert";
            mail.IsBodyHtml = true;
            mail.Body = "<p><b>Dear Concerned,</b></p>" +
                        "<b></b>" +
                        "<p><b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; This is to inform you that machine " + MachineName + " has gone into Breakdown for " + messagecode + "  ," + messagedescription + "  &nbsp;<span>.</b></p>" +
                        "<p><b><br/><br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;  Note: This Email has been sent for the demo purpose of Andon Display. &nbsp;<span>.</b></p>" +
                        "<p><b></b></p>";

            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential("narendramourya37@gmail.com", "8103097561");
            smtp.EnableSsl = true;
            smtp.Send(mail);
            return true;
        }

        public bool SendMailEnd(string messagecode, string messagedescription, int MachineID)
        {
            tblmachinedetail machin = db.tblmachinedetails.Find(MachineID);
            string MachineName = machin.MachineDispName;
            MailMessage mail = new MailMessage();

            //mail.To.Add(new MailAddress("pavan.v@srkssolutions.com"));
            //mail.To.Add(new MailAddress("deepak.Jojode@srkssolutions.com"));

            //mail.CC.Add(new MailAddress("srinidhi.kashyap@srkssolutions.com"));
            mail.To.Add(new MailAddress("janardhan.g@srkssolutions.com"));

            //mail.Bcc.Add(new MailAddress("narendra.kumar@srkssolutions.com"));
            //mail.To.Add(new MailAddress("pskumar@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("bpdesai@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("vkasinath@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("vsanghavi@tata.com"));
            //mail.CC.Add(new MailAddress("sgopu@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("soumyaagrawal@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("pkbhanja@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("tshabareesan@tataadvancedsystems.com"));
            //mail.CC.Add(new MailAddress("mmrafi@tataadvancedsystems.com"));
            //mail.Bcc.Add(new MailAddress("pavan.v@srkssolutions.com"));
            //mail.Bcc.Add(new MailAddress("narendramourya@live.com"));
            //mail.Bcc.Add(new MailAddress("srinidhi.kashyap@srkssolutions.com"));
            //mail.Bcc.Add(new MailAddress("deepak.Jojode@srkssolutions.com"));

            mail.From = new MailAddress("narendramourya37@gmail.com");
            mail.Subject = MachineName + " BreakDown Alert";
            mail.IsBodyHtml = true;
            mail.Body = "<p><b>Dear Concerned,</b></p>" +
                        "<b></b>" +
                        "<p><b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; This is to inform you that machine " + MachineName + " has been fixed for the Breakdown for " + messagecode + "  ," + messagedescription + " and is now available for production &nbsp;<span>.</b></p>" +
                        "<p><b><br/><br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;  Note: This Email has been sent for the demo purpose of Andon Display. &nbsp;<span>.</b></p>" +
                        "<p><b></b></p>";
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential("narendramourya37@gmail.com", "8103097561");
            smtp.EnableSsl = true;
            smtp.Send(mail);
            return true;
        }

        public bool CheckLastOneHourDownTime(int MachineID)
        {
            #region DownColor
            int count = 0;
            int ContinuesChecking = 0;
            var productionstatus = db.tbldailyprodstatus.Where(m => m.MachineID == MachineID).OrderByDescending(m => m.StartTime);
            foreach (var check in productionstatus)
            {
                if (check.ColorCode == "yellow")
                {
                    count++;
                    if (count == 60)
                    {
                        break;
                    }
                }
                else
                {
                    count = 0;
                }
                ContinuesChecking++;
            }
            #endregion
            if (count >= 60 && ContinuesChecking < 61)
            {

                tblmachinedetail machin = db.tblmachinedetails.Find(MachineID);
                string MachineName = machin.MachineDispName;
                MailMessage mail = new MailMessage();

                //mail.To.Add(new MailAddress("pavan.v@srkssolutions.com"));


                //mail.CC.Add(new MailAddress("srinidhi.kashyap@srkssolutions.com"));
                mail.To.Add(new MailAddress("janardhan.g@srkssolutions.com"));

                //mail.Bcc.Add(new MailAddress("narendra.kumar@srkssolutions.com"));

                //mail.To.Add(new MailAddress("pskumar@tataadvancedsystems.com"));
                //mail.CC.Add(new MailAddress("bpdesai@tataadvancedsystems.com"));
                //mail.CC.Add(new MailAddress("vkasinath@tataadvancedsystems.com"));
                //mail.CC.Add(new MailAddress("vsanghavi@tata.com"));
                //mail.CC.Add(new MailAddress("sgopu@tataadvancedsystems.com"));
                //mail.CC.Add(new MailAddress("soumyaagrawal@tataadvancedsystems.com"));
                //mail.CC.Add(new MailAddress("pkbhanja@tataadvancedsystems.com"));
                //mail.CC.Add(new MailAddress("tshabareesan@tataadvancedsystems.com"));
                //mail.CC.Add(new MailAddress("mmrafi@tataadvancedsystems.com"));
                //mail.Bcc.Add(new MailAddress("pavan.v@srkssolutions.com"));
                //mail.Bcc.Add(new MailAddress("narendramourya@live.com"));
                //mail.Bcc.Add(new MailAddress("srinidhi.kashyap@srkssolutions.com"));
                //mail.Bcc.Add(new MailAddress("deepak.Jojode@srkssolutions.com"));


                mail.From = new MailAddress("narendramourya37@gmail.com");
                mail.Subject = MachineName + " Setup Mode";
                mail.IsBodyHtml = true;
                mail.Body = "<p><b>Dear Concerned,</b></p>" +
                            "<b></b>" +
                            "<p><b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;  This is to inform you that machine Nexus " + MachineName + " has crossed an Hour being under setup. &nbsp;<span>.</b></p>" +
                            "<p><b><br/><br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;  Note: This Email has been sent for the demo purpose of Andon Display. &nbsp;<span>.</b></p>" +
                            "<p><b></b></p>";
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential("narendramourya37@gmail.com", "8103097561");
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            return true;
        }

        //code to refresh checking idle list breakdownlist
        public bool checkingIdle()
        {
            Session["idlestarttime"] = null;
            bool tick = false;
            //getting CorrectedDate
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            int Machinid = Convert.ToInt32(Session["MachineID"]);

            tbllivelossofentry DowncodeEntryTime = db.tbllivelossofentries.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == Machinid).OrderByDescending(m => m.EntryTime).FirstOrDefault();
            DateTime EnteryTime = DateTime.Now;
            if (DowncodeEntryTime != null)
            {
                EnteryTime = Convert.ToDateTime(DowncodeEntryTime.EntryTime);
                int TotalMinute = 0;
                TotalMinute = System.DateTime.Now.Subtract(EnteryTime).Minutes;
                if (TotalMinute >= 3)
                {
                    #region DownColor
                    int count = 0;
                    int ContinuesChecking = 0;
                    var productionstatus = db.tbllivedailyprodstatus.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == Machinid).OrderByDescending(m => m.StartTime);
                    foreach (var check in productionstatus)
                    {
                        if (check.ColorCode == "yellow")
                        {
                            count++;
                            if (count == 2)
                            {
                                break;
                            }
                        }
                        else
                        {
                            count = 0;
                        }
                        ContinuesChecking++;
                    }
                    if (count >= 2 && ContinuesChecking < 5)
                    {
                        tick = true;
                    }
                    #endregion
                }
            }
            else
            {
                #region DownColor
                int count = 0;
                int ContinuesChecking = 0;
                var productionstatus = db.tbllivedailyprodstatus.Where(m => m.CorrectedDate == CorrectedDate && m.MachineID == Machinid).OrderByDescending(m => m.StartTime);
                foreach (var check in productionstatus)
                {
                    if (check.ColorCode == "yellow")
                    {
                        count++;
                        if (count == 2)
                        {
                            break;
                        }
                    }
                    else
                    {
                        count = 0;
                    }
                    ContinuesChecking++;
                }
                if (count >= 2 && ContinuesChecking < 5)
                {
                    tick = true;
                }
                #endregion
            }
            return tick;
        }

        //Check for ShiftEnd.
        public JsonResult checkShiftEnd(string rep)
        {
            string isShiftEnd = "no";

            DateTime dt = DateTime.Now;
            TimeSpan tm = new TimeSpan(dt.Hour, dt.Minute, dt.Second);

            var shiftstatus = db.shift_master.Where(m => m.EndTime >= tm && m.StartTime <= tm).OrderBy(m => m.StartTime);
            string shiftfor = "someshift";
            foreach (var check in shiftstatus)
            {
                shiftfor = check.ShiftName;
            }

            if (shiftfor == "Shift1")
            {
                shiftfor = "A";
            }
            else if (shiftfor == "Shift2")
            {
                shiftfor = "B";
            }
            else if (shiftfor == "Shift3")
            {
                shiftfor = "C";
            }

            string shiftforpop = Session["shiftforpopup"].ToString();
            if (shiftforpop != shiftfor)
            {
                isShiftEnd = "yes";
                Session["shiftforpopup"] = shiftfor;
            }

            string json = JsonConvert.SerializeObject(isShiftEnd);
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        //WorkInProgressList
        public ActionResult WorkInProgressList(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            ViewBag.opid = Session["opid"];
            ViewBag.mcnid = id;
            ViewBag.coretddt = CorrectedDate;

            //bool tick = checkingIdle();
            //if (tick == true)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //    //ViewBag.tick = 1;
            //}

            int handleidleReturnValue = HandleIdle();
            if (handleidleReturnValue == 0)
            {
                return RedirectToAction("DownCodeEntry");
            }

            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == id).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);

            //var breakdown = db.tblbreakdowns.Include(t=>t.machine_master).Include(t=>t.message_code_master).Where(m=>m.MachineID==id && m.CorrectedDate==CorrectedDate).ToList();
            var WIP = db.tbllivehmiscreens.Include(t => t.tblmachinedetail).Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate && m.isWorkInProgress == 0).ToList();
            return View(WIP);
        }

        public ActionResult Setting(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            Session["Mid"] = id;
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            //CODE to check the current mode is allowable or not , based on MODE Priority.
            var curMode = db.tblbreakdowns.Where(m => m.MachineID == id).Where(m => m.CorrectedDate == CorrectedDate && m.EndTime == null).OrderByDescending(m => m.BreakdownID);
            int currentId = 0;

            foreach (var j in curMode)
            {
                currentId = j.BreakdownID;
                string mode = j.tbllossescode.MessageType;

                if (mode == "PM" || mode == "BREAKDOWN")
                {
                    Session["ModeError"] = "Machine is in " + mode + ", cannot change mode to Setting";
                    return RedirectToAction("Index");
                }
                //else if (mode == "SETUP")
                //{
                //    Session["ModeError"] = "Machine is already in Setting Mode";
                //    return RedirectToAction("Index");
                //}
                else if (mode != "SETUP")
                {
                    tblbreakdown tbd = db.tblbreakdowns.Find(currentId);
                    tbd.EndTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    db.Entry(tbd).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    break;
                }
            }

            //var brkdown = db.tblbreakdowns.Where(m => m.MachineID == id).Where(m => m.CorrectedDate == CorrectedDate && m.EndTime == null && m.message_code_master.MessageType == "SETUP");
            string mcode = "70800";
            var brkdown = db.tblbreakdowns.Where(m => m.MachineID == id).Where(m => m.CorrectedDate == CorrectedDate && m.EndTime == null && m.MessageCode == mcode);

            if (brkdown.Count() != 0)
            {
                TempData["Enable"] = "Enable";
                int brekdnID = 0;
                foreach (var a in brkdown)
                {
                    brekdnID = a.BreakdownID;
                }
                tblbreakdown brekdn = db.tblbreakdowns.Find(brekdnID);
                //CheckLastOneHourDownTime(id);
                ViewBag.BreakDownCode = new SelectList(db.message_code_master.Where(m => m.IsDeleted == 0).Where(m => m.MessageType == "SETUP"), "MessageCodeID", "MessageDescription", brekdn.BreakDownCode);
                return View(brekdn);
            }
            else
            {

            }
            //CheckLastOneHourDownTime(id);
            ViewBag.BreakDownCode = new SelectList(db.message_code_master.Where(m => m.IsDeleted == 0).Where(m => m.MessageType == "SETUP"), "MessageCodeID", "MessageDescription");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Setting(tblbreakdown lossentry, string submit = "")
        {
            int MachineID = 0;
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int RID = Convert.ToInt32(Session["RoleID"]);
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            if (string.IsNullOrEmpty(submit) == false)
            {
                lossentry.CorrectedDate = CorrectedDate;
                lossentry.StartTime = DateTime.Now;
                if (RID != 1 && RID != 2)
                {
                    lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
                    MachineID = Convert.ToInt32(Session["MachineID"]);
                }
                else
                {
                    lossentry.MachineID = Convert.ToInt32(Session["Mid"]);
                    MachineID = Convert.ToInt32(Session["Mid"]);
                }
                message_code_master downcode = db.message_code_master.Find(lossentry.BreakDownCode);
                //lossentry.BreakDownCode = Convert.ToInt32(downcode.MessageCode);
                lossentry.Shift = Session["realshift"].ToString();
                //lossentry.BreakDownCode =Convert.ToInt32(downcode.MessageCode);
                lossentry.MessageCode = (downcode.MessageCode).ToString();
                db.tblbreakdowns.Add(lossentry);
                db.SaveChanges();
                //SendMail(downcode.MessageCode, downcode.MessageDescription, MachineID);

                //update the endtime for the last mode of this machine 
                var tblmodedata = db.tbllivemodedbs.Where(m => m.IsDeleted == 0 && m.MachineID == MachineID).OrderByDescending(m => m.StartTime).ToList();
                foreach (var row in tblmodedata)
                {
                    row.EndTime = DateTime.Now;
                    db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                //Code to save this event to tblmode table
                tbllivemodedb tm = new tbllivemodedb();
                tm.MachineID = MachineID;
                tm.CorrectedDate = CorrectedDate;
                tm.InsertedBy = 1;
                tm.InsertedOn = DateTime.Now;
                tm.StartTime = DateTime.Now;
                tm.ColorCode = "green";
                tm.IsDeleted = 0;
                tm.Mode = "SETUP";

                db.tbllivemodedbs.Add(tm);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {
                lossentry.CorrectedDate = CorrectedDate;
                lossentry.EndTime = DateTime.Now;
                MachineID = Convert.ToInt32(lossentry.MachineID);
                //lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
                db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                UpdateRecordOfProduction(lossentry);
                int code = Convert.ToInt32(lossentry.BreakDownCode);
                message_code_master msg = db.message_code_master.Where(m => m.MessageCodeID == code).FirstOrDefault();
                //SendMailEnd(msg.MessageCode, msg.MessageDescription, MachineID);
                return RedirectToAction("Index");
            }
            return View(lossentry);
        }

        public ActionResult Maintenance(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            Session["Mid"] = id;
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }

            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == id).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);

            //CODE to check the current mode is allowable or not , based on MODE Priority.
            var curMode = db.tblbreakdowns.Where(m => m.MachineID == id).Where(m => m.CorrectedDate == CorrectedDate && m.EndTime == null).OrderByDescending(m => m.BreakdownID);
            int currentId = 0;

            foreach (var j in curMode)
            {
                currentId = j.BreakdownID;
                string mode = j.MessageCode;
                if (mode != "PM")
                {
                    currentId = j.BreakdownID;
                    tblbreakdown tbd = db.tblbreakdowns.Find(currentId);
                    tbd.EndTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    db.Entry(tbd).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    break;
                }
                //else if (mode == "PM")
                //{
                //    Session["ModeError"] = "Machine is in Planned Maintenance Mode";
                //    return RedirectToAction("Index");
                //}

            }

            //var brkdown = db.tblbreakdowns.Where(m => m.MachineID == id).Where(m => m.CorrectedDate == CorrectedDate && m.EndTime == null && m.message_code_master.MessageType == "PM");

            var brkdown = db.tblbreakdowns.Where(m => m.MachineID == id).Where(m => m.EndTime == null && m.MessageCode == "PM");
            if (brkdown.Count() != 0)
            {
                TempData["Enable"] = "Enable";
                int brekdnID = 0;
                foreach (var a in brkdown)
                {
                    brekdnID = a.BreakdownID;
                }
                tblbreakdown brekdn = db.tblbreakdowns.Find(brekdnID);
                //CheckLastOneHourDownTime(id);
                ViewBag.BreakDownCode = new SelectList(db.tbllossescodes.Where(m => m.IsDeleted == 0).Where(m => m.MessageType == "PM"), "LossCode", "LossCodeDesc", brekdn.BreakDownCode);
                return View(brekdn);
            }
            else
            {
            }
            //CheckLastOneHourDownTime(id);
            ViewBag.BreakDownCode = new SelectList(db.tbllossescodes.Where(m => m.IsDeleted == 0).Where(m => m.MessageType == "PM"), "LossCode", "LossCodeDesc");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Maintenance(tblbreakdown lossentry, string submit = "", string BreakDownCode = null)
        {
            int MachineID = 0;
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int RID = Convert.ToInt32(Session["RoleID"]);
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            if (string.IsNullOrEmpty(submit) == false && submit == "Start")
            {
                lossentry.CorrectedDate = CorrectedDate;
                lossentry.StartTime = DateTime.Now;
                if (RID != 1 && RID != 2)
                {
                    lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
                    MachineID = Convert.ToInt32(Session["MachineID"]);
                }
                else
                {
                    lossentry.MachineID = Convert.ToInt32(Session["Mid"]);
                    MachineID = Convert.ToInt32(Session["Mid"]);
                }
                //message_code_master downcode = db.message_code_master.Find(lossentry.BreakDownCode);
                var LossData = db.tbllossescodes.Where(m => m.LossCode == BreakDownCode).FirstOrDefault();
                lossentry.Shift = Session["realshift"].ToString();
                lossentry.MessageCode = (LossData.LossCode).ToString();
                lossentry.BreakDownCode = 120;
                lossentry.DoneWithRow = 0;
                lossentry.MessageDesc = "PM";
                db.tblbreakdowns.Add(lossentry);
                db.SaveChanges();
                //SendMail(downcode.MessageCode, downcode.MessageDescription, MachineID);

                //update the endtime for the last mode of this machine 
                var tblmodedata = db.tbllivemodedbs.Where(m => m.IsDeleted == 0 && m.MachineID == MachineID && m.IsCompleted == 0).OrderByDescending(m => m.StartTime).ToList();
                foreach (var row in tblmodedata)
                {
                    row.EndTime = DateTime.Now;
                    row.IsCompleted = 1;
                    db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();


                }
                //Code to save this event to tblmode table
                tbllivemodedb tm = new tbllivemodedb();
                tm.MachineID = MachineID;
                tm.CorrectedDate = CorrectedDate;
                tm.InsertedBy = Convert.ToInt32(Session["UserId"]);
                tm.InsertedOn = DateTime.Now;
                tm.StartTime = DateTime.Now;
                tm.ColorCode = "red";
                tm.IsCompleted = 0;
                tm.IsDeleted = 0;
                tm.Mode = "BREAKDOWN";

                db.tbllivemodedbs.Add(tm);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {
                lossentry.CorrectedDate = CorrectedDate;
                lossentry.EndTime = DateTime.Now;
                lossentry.DoneWithRow = 1;
                MachineID = Convert.ToInt32(lossentry.MachineID);
                try
                {
                    db.Entry(lossentry).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    var tblmodedata = db.tbllivemodedbs.Where(m => m.IsDeleted == 0 && m.MachineID == MachineID && m.IsCompleted == 0).OrderByDescending(m => m.StartTime).ToList();
                    foreach (var row in tblmodedata)
                    {
                        row.EndTime = DateTime.Now;
                        row.IsCompleted = 1;
                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }

                    tbllivemodedb tmIDLE = new tbllivemodedb();
                    tmIDLE.ColorCode = "yellow";
                    tmIDLE.CorrectedDate = CorrectedDate;
                    tmIDLE.InsertedBy = Convert.ToInt32(Session["UserId"]);
                    tmIDLE.InsertedOn = DateTime.Now;
                    tmIDLE.IsCompleted = 0;
                    tmIDLE.IsDeleted = 0;
                    tmIDLE.MachineID = MachineID;
                    tmIDLE.Mode = "IDLE";
                    tmIDLE.StartTime = DateTime.Now;

                    db.tbllivemodedbs.Add(tmIDLE);
                    db.SaveChanges();

                }
                catch (Exception e)
                { }
                //UpdateRecordOfProduction(lossentry);
                //int code = Convert.ToInt32(lossentry.BreakDownCode);
                // message_code_master msg = db.message_code_master.Where(m => m.MessageCodeID == code).FirstOrDefault();
                //SendMailEnd(msg.MessageCode, msg.MessageDescription, MachineID);
                return RedirectToAction("Index");
            }
            return View(lossentry);
        }

        #region set shift OLD
        ////code
        ////For changeVisibelity shift for normal user
        //public ActionResult changeVisibelityNorm()
        //{
        //    string CorrectedDate = null;
        //    tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
        //    TimeSpan Start = StartTime.StartTime;
        //    if (Start <= DateTime.Now.TimeOfDay)
        //    {
        //        CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
        //    }
        //    else
        //    {
        //        CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        //    }
        //    int MachineID = Convert.ToInt32(Session["MchnID"]);
        //    ViewBag.hide = null;
        //    //Gatting UderID
        //    int opid = Convert.ToInt32(Session["Opid"]);
        //    Session["Show"] = 1;
        //    return RedirectToAction("Index", "HMIScree");
        //}

        ////For general shift for normal user
        //public ActionResult changeShiftNorm(String Shift)
        //{
        //    Session["Show"] = null;
        //    if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
        //    {
        //        return RedirectToAction("Login", "Login", null);
        //    }
        //    ViewBag.roleid = Session["RoleID"];
        //    string CorrectedDate = null;
        //    tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
        //    TimeSpan Start = StartTime.StartTime;
        //    if (Start <= DateTime.Now.TimeOfDay)
        //    {
        //        CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
        //    }
        //    else
        //    {
        //        CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        //    }
        //    int MachineID = Convert.ToInt32(Session["MchnID"]);
        //    //Gatting UderID
        //    int opid = Convert.ToInt32(Session["Opid"]);
        //    tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid && m.Shift == Shift).Where(m => m.MachineID == MachineID).FirstOrDefault();
        //   // tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).Where(m => m.MachineID == MachineID).FirstOrDefault();
        //    if (HMI == null)
        //    {
        //        //idea is to make the status of previous shift to 2 and insert new shift
        //        string operatorname = null;
        //        var oldshiftdata = db.tblhmiscreens.Where(m => m.MachineID == MachineID && m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).OrderByDescending(m => m.HMIID).FirstOrDefault();
        //        if (oldshiftdata != null)
        //        {
        //            oldshiftdata.Status = 2;
        //            operatorname = oldshiftdata.OperatorDet;
        //            db.Entry(oldshiftdata).State = System.Data.Entity.EntityState.Modified;
        //            db.SaveChanges();
        //        }


        //        tblhmiscreen tblhmiscreen = new tblhmiscreen();
        //        tblhmiscreen.MachineID = MachineID;
        //        tblhmiscreen.CorrectedDate = CorrectedDate;
        //        tblhmiscreen.Date = DateTime.Now.Date;
        //        tblhmiscreen.Shift = Shift;
        //        tblhmiscreen.OperatorDet = operatorname;
        //        tblhmiscreen.Status = 0;
        //        tblhmiscreen.isWorkInProgress = 2;
        //        tblhmiscreen.OperatiorID = opid;
        //        tblhmiscreen.Time = DateTime.Now.TimeOfDay;
        //        db.tblhmiscreens.Add(tblhmiscreen);
        //        db.SaveChanges();

        //        //tblhmiscreen tblhmiscreenSecondRow = new tblhmiscreen();
        //        //tblhmiscreenSecondRow.MachineID = MachineID;
        //        //tblhmiscreenSecondRow.CorrectedDate = CorrectedDate;
        //        //tblhmiscreenSecondRow.Date = DateTime.Now.Date;
        //        //tblhmiscreenSecondRow.Shift = Shift;
        //        //tblhmiscreenSecondRow.Status = 1;
        //        //tblhmiscreenSecondRow.OperatiorID = opid;
        //        //tblhmiscreenSecondRow.Time = DateTime.Now.TimeOfDay;
        //        //db.tblhmiscreens.Add(tblhmiscreenSecondRow);
        //        //db.SaveChanges();
        //    }
        //    else
        //    { //do nothing.
        //        HMI.Shift = Shift;
        //        db.Entry(HMI).State = System.Data.Entity.EntityState.Modified;
        //        db.SaveChanges();
        //    }

        //    //HMIScreenForAdmin(MachineID, opid, CorrectedDate);
        //    Session["opid"] = opid;
        //    Session["gopid"] = opid;
        //    Session["gshift" + opid] = Shift;
        //    ViewBag.hide = 1;
        //    return RedirectToAction("Index", "HMIScree");
        //}

        ////For general shift for admin
        //public ActionResult changeShift(String Shift)
        //{
        //    Session["Show"] = null;
        //    if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
        //    {
        //        return RedirectToAction("Login", "Login", null);
        //    }
        //    ViewBag.roleid = Session["RoleID"];
        //    string CorrectedDate = null;
        //    tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
        //    TimeSpan Start = StartTime.StartTime;
        //    if (Start <= DateTime.Now.TimeOfDay)
        //    {
        //        CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
        //    }
        //    else
        //    {
        //        CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        //    }
        //    int MachineID = Convert.ToInt32(Session["MchnID"]);
        //    //Gatting UderID
        //    int opid = Convert.ToInt32(Session["Opid"]);
        //    //tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid && m.Shift == Shift).Where(m => m.MachineID == MachineID).FirstOrDefault();
        //    tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid ).Where(m => m.MachineID == MachineID).FirstOrDefault();
        //    if (HMI == null)
        //    {
        //        tblhmiscreen tblhmiscreen = new tblhmiscreen();
        //        tblhmiscreen.MachineID = MachineID;
        //        tblhmiscreen.CorrectedDate = CorrectedDate;
        //        tblhmiscreen.Date = DateTime.Now.Date;
        //        tblhmiscreen.Shift = Shift;
        //        tblhmiscreen.Status = 0;
        //        tblhmiscreen.OperatiorID = opid;
        //        tblhmiscreen.Time = DateTime.Now.TimeOfDay;
        //        db.tblhmiscreens.Add(tblhmiscreen);
        //        db.SaveChanges();

        //        //tblhmiscreen tblhmiscreenSecondRow = new tblhmiscreen();
        //        //tblhmiscreenSecondRow.MachineID = MachineID;
        //        //tblhmiscreenSecondRow.CorrectedDate = CorrectedDate;
        //        //tblhmiscreenSecondRow.Date = DateTime.Now.Date;
        //        //tblhmiscreenSecondRow.Shift = Shift;
        //        //tblhmiscreenSecondRow.Status = 1;
        //        //tblhmiscreenSecondRow.OperatiorID = opid;
        //        //tblhmiscreenSecondRow.Time = DateTime.Now.TimeOfDay;
        //        //db.tblhmiscreens.Add(tblhmiscreenSecondRow);
        //        //db.SaveChanges();
        //    }
        //    else
        //    {
        //        HMI.Shift = Shift;
        //        db.Entry(HMI).State = System.Data.Entity.EntityState.Modified;
        //        db.SaveChanges();
        //    }
        //    //HMIScreenForAdmin(MachineID, opid, CorrectedDate);
        //    Session["opid"] = opid;
        //    Session["gopid"] = opid;
        //    Session["gshift" + opid] = Shift;
        //    ViewBag.hide = 1;
        //    return RedirectToAction("HMIScreenForAdmin", "HMIScree", new { MachineID, opid, CorrectedDate });
        //}

        ////For changeVisibelity shift for admin
        //public ActionResult changeVisibelity()
        //{
        //    string CorrectedDate = null;
        //    tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
        //    TimeSpan Start = StartTime.StartTime;
        //    if (Start <= DateTime.Now.TimeOfDay)
        //    {
        //        CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
        //    }
        //    else
        //    {
        //        CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        //    }
        //    int MachineID = Convert.ToInt32(Session["MchnID"]);
        //    ViewBag.hide = null;
        //    //Gatting UderID
        //    int opid = Convert.ToInt32(Session["Opid"]);
        //    Session["Show"] = 1;
        //    return RedirectToAction("HMIScreenForAdmin", "HMIScree", new { MachineID, opid, CorrectedDate });
        //}
        #endregion

        public ActionResult changeShiftNorm(String Shift)
        {
            Session["Show"] = null;
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            int MachineID = Convert.ToInt32(Session["MchnID"]);
            //Gatting UderID
            int opid = Convert.ToInt32(Session["Opid"]);
            tbllivehmiscreen HMI = db.tbllivehmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid && m.Status == 0).Where(m => m.MachineID == MachineID).FirstOrDefault();
            // tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).Where(m => m.MachineID == MachineID).FirstOrDefault();
            if (HMI != null)
            {
                //idea is to update the row if set is clicked.
                var oldshiftdata = db.tbllivehmiscreens.Where(m => m.MachineID == MachineID && m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).OrderByDescending(m => m.HMIID).FirstOrDefault();
                if (oldshiftdata != null)
                {
                    oldshiftdata.Shift = Shift;
                    oldshiftdata.isUpdate = 1;
                    db.Entry(oldshiftdata).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }

            //HMIScreenForAdmin(MachineID, opid, CorrectedDate);
            Session["opid"] = opid;
            Session["gopid"] = opid;
            Session["gshift" + opid] = Shift;
            ViewBag.hide = 1;
            return RedirectToAction("Index", "HMIScree");

        }

        public ActionResult changeVisibilityNorm(int id = 0, int reworkorderhidden = 0, int cjtextbox7 = 0, int cjtextbox8 = 0, int wd = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            if (wd == 1)
            {
                //Getting Shift Value
                DateTime Time = DateTime.Now;
                TimeSpan Tm = new TimeSpan(Time.Hour, Time.Minute, Time.Second);
                var ShiftDetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm && m.EndTime >= Tm);
                string Shift = null;
                foreach (var a in ShiftDetails)
                {
                    Shift = a.ShiftName;
                }
                ViewBag.date = System.DateTime.Now;
                if (Shift != null)
                    ViewBag.shift = Shift;
                else
                    ViewBag.shift = "C";

                int machineID = 0;
                tbllivehmiscreen tblhmiscreen = db.tbllivehmiscreens.Find(id);
                machineID = Convert.ToInt32(tblhmiscreen.MachineID);

                int Uid = tblhmiscreen.OperatiorID;

                int ID = id;
                tbllivehmiscreen OldWork = db.tbllivehmiscreens.Find(ID);
                OldWork.Rej_Qty = cjtextbox7;
                OldWork.Delivered_Qty = cjtextbox8;
                OldWork.Status = 3;

                //update isWorkInProgress When WorkIs finished is clicked.
                //else save workInProgress and continue

                OldWork.isWorkInProgress = 0;//work is in progress
                if (reworkorderhidden == 1)
                {
                    OldWork.isWorkOrder = 1;
                }

                string Shiftgen = OldWork.Shift;
                string operatorName = OldWork.OperatorDet;
                int Opgid = OldWork.OperatiorID;
                DateTime pestarttime = Convert.ToDateTime(OldWork.PEStartTime);
                //get all those data for new row.
                string project = OldWork.Project;
                string PorF = OldWork.Prod_FAI;
                string partno = OldWork.PartNo;
                string wono = OldWork.Work_Order_No;
                string opno = OldWork.OperationNo;
                int target = Convert.ToInt32(OldWork.Target_Qty);
                db.Entry(OldWork).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                string CorrectedDate = null;
                tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
                TimeSpan Start = StartTime.StartTime;
                if (Start <= DateTime.Now.TimeOfDay)
                {
                    CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                }
                else
                {
                    CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                }
                tbllivehmiscreen NewEntry = new tbllivehmiscreen();
                NewEntry.Project = project;
                NewEntry.Prod_FAI = PorF;
                NewEntry.PartNo = partno;
                NewEntry.Work_Order_No = wono;
                NewEntry.OperationNo = opno;
                NewEntry.Target_Qty = target;
                NewEntry.MachineID = machineID;
                NewEntry.CorrectedDate = CorrectedDate;
                NewEntry.PEStartTime = pestarttime;
                //NewEntry.Date = DateTime.Now;
                //NewEntry.OperatorDet = operatorName;
                NewEntry.Shift = Convert.ToString(Shiftgen);
                NewEntry.Status = 0;
                NewEntry.isWorkInProgress = 2;
                NewEntry.OperatiorID = Opgid;
                //NewEntry.Time = DateTime.Now;
                db.tbllivehmiscreens.Add(NewEntry);
                db.SaveChanges();

            }
            else
            {
                tbllivehmiscreen tblhmiscreen = db.tbllivehmiscreens.Find(id);
                tblhmiscreen.isUpdate = 0;
                db.Entry(tblhmiscreen).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

            }
            return RedirectToAction("Index");
        }

        public JsonResult IsItLastLevel(int id)
        {
            string hasNextLevel = "no";
            var levelData = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodeID == id).FirstOrDefault();
            int level = levelData.LossCodesLevel;
            if (level == 1)
            {
                var NextlevelData = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel1ID == id).ToList();
                if (NextlevelData.Count > 0)
                {
                    hasNextLevel = "yes";
                }
            }
            if (level == 2)
            {
                var NextlevelData = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodesLevel2ID == id).ToList();
                if (NextlevelData.Count > 0)
                {
                    hasNextLevel = "yes";
                }
            }
            if (level == 3)
            {
                hasNextLevel = "no";
            }
            return Json(hasNextLevel, JsonRequestBehavior.AllowGet);
        }

        public JsonResult JsoncheckingIdle()
        {
            string IsIdle = "false";
            bool isidleMethodRetVal = checkingIdle();
            if (isidleMethodRetVal)
            {
                IsIdle = "true";
            }
            return Json(IsIdle, JsonRequestBehavior.AllowGet);
        }

        //DDL List 2016-08-11 Janardhan
        public ActionResult DDLList(int DDLID = 0, string MacInvNo = null, int ToHMI = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            int machineId = Convert.ToInt32(Session["MachineID"]);
            ViewBag.opid = Session["opid"];
            ViewBag.mcnid = machineId;
            ViewBag.coretddt = CorrectedDate;

            //int handleidleReturnValue = HandleIdle();
            //if (handleidleReturnValue == 0)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //}
            var a = TempData["VError"];
            Session["VError"] = null;
            Session["VError"] = TempData["VError"];
            //Step 1: If DDLID is given then insert that data into HMIScreen table , take its HMIID and redirect to Index 

            #region doing this in post method.(2017-05-09)
            if (DDLID != 0)
            {
                int Hmiid = 0;
                var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                //String SplitWO = ddldata.SplitWO;

                String WONo = ddldata.WorkOrder;
                String Part = ddldata.MaterialDesc;
                String Operation = ddldata.OperationNo;

                #region 2017-02-07 doing this in post method.(2017-05-09)
                //bool IsInHMI = false;
                //var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONo && m.MaterialDesc == Part && m.OperationNo != Operation).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                //foreach (var row in Siblingddldata)
                //{
                //    IsInHMI = true; //reinitialize
                //    int localOPNo = Convert.ToInt32(row.OperationNo);
                //    string localOPNoString = Convert.ToString(row.OperationNo);
                //    if (localOPNo < Convert.ToInt32(Operation))
                //    {
                //        #region //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                //        //which case is valid.
                //        var SiblingHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == localOPNoString).OrderByDescending(m => m.HMIID).FirstOrDefault();
                //        if (SiblingHMIdata == null)
                //        {
                //            Session["VError"] = "Please Select Below WorkOrder, WONo: " + WONo + " PartNo: " + Part + " OperationNo: " + localOPNo;
                //            IsInHMI = false;
                //            //break;
                //        }
                //        else
                //        {
                //            if (SiblingHMIdata.Date == null) //=> lower OpNo is not submitted.
                //            {
                //                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                //                //return RedirectToAction("Index");
                //                IsInHMI = false;
                //                //break;
                //            }
                //            else
                //            {
                //                IsInHMI = true;
                //            }
                //        }
                //        #endregion

                //        if (!IsInHMI)
                //        {
                //            #region //also check in MultiWO table
                //            string WIPQueryMultiWO = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + localOPNo + "' order by MultiWOID desc limit 1 ";
                //            var WIPMWO = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWO).ToList();

                //            if (WIPMWO.Count == 0)
                //            {
                //                Session["VError"] = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                //                //return RedirectToAction("Index");
                //                //IsInHMI = false;
                //                //break;
                //                return View();
                //            }

                //            foreach (var rowHMI in WIPMWO)
                //            {
                //                int hmiid = Convert.ToInt32(rowHMI.HMIID);
                //                var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                //                if (MWOHMIData != null) //obviously != 0
                //                {
                //                    if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                //                    {
                //                        Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                //                        //return RedirectToAction("Index");
                //                        return View();
                //                        //break;
                //                    }
                //                    else
                //                    {
                //                    }
                //                }
                //            }
                //            #endregion
                //        }
                //        else
                //        {
                //            //continue with other execution
                //        }
                //    }
                //}

                /////to Catch those Manual WorkOrders 
                //string WIPQuery1 = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo != '" + Operation + "' and  IsMultiWO = 0 and DDLWokrCentre is null order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo ) order by OperationNo ;";
                //var WIPDDL1 = db.tblhmiscreens.SqlQuery(WIPQuery1).ToList();
                //foreach (var row in WIPDDL1)
                //{
                //    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                //    if (InnerOpNo < Convert.ToInt32(Operation))
                //    {
                //        string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + InnerOpNo + "' order by HMIID desc limit 1 ";
                //        var WIP1 = db.tblhmiscreens.SqlQuery(WIPQueryHMI).ToList();
                //        if (WIP1.Count == 0)
                //        {
                //            Session["VError"] = " Select & Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                //            //return RedirectToAction("Index");
                //            return View();
                //        }
                //        foreach (var rowHMI in WIP1)
                //        {
                //            if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                //            {
                //                Session["VError"] = " Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                //                //return RedirectToAction("Index");
                //                return View();
                //            }
                //        }
                //    }
                //}
                #endregion

                //int PrvProcessQty = 0, PrvDeliveredQty = 0;
                //var getProcessQty = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.HMIID).Take(1).ToList();
                //if (getProcessQty.Count > 0)
                //{
                //    PrvProcessQty = Convert.ToInt32(getProcessQty[0].ProcessQty);
                //    PrvDeliveredQty = Convert.ToInt32(getProcessQty[0].Delivered_Qty);
                //}

                #region new code
                int PrvProcessQty = 0, PrvDeliveredQty = 0;
                //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                int isHMIFirst = 2; //default NO History for that wo,pn,on

                var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();

                if (hmiData.Count > 0 && mulitwoData.Count > 0) // now check for greatest amongst
                {
                    //DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn); //2017-06-02
                    //Based on hmiid of  multiwotable get  Time Column of tblhmiscreen 
                    //int localhmiid = Convert.ToInt32(mulitwoData[0].HMIID);
                    //var hmiiData = db.tblhmiscreens.Find(localhmiid);

                    DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].tbllivehmiscreen.Time);
                    DateTime hmiDateTime = Convert.ToDateTime(hmiData[0].Time);

                    if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                    {
                        isHMIFirst = 1;
                    }
                    else
                    {
                        isHMIFirst = 0;
                    }

                }
                else if (mulitwoData.Count > 0)
                {
                    isHMIFirst = 1;
                }
                else if (hmiData.Count > 0)
                {
                    isHMIFirst = 0;
                }

                if (isHMIFirst == 1)
                {
                    string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                    int delivInt = 0;
                    int.TryParse(delivString, out delivInt);

                    string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                    int procInt = 0;
                    int.TryParse(processString, out procInt);

                    PrvProcessQty += procInt;
                    PrvDeliveredQty += delivInt;
                }
                else if (isHMIFirst == 0)
                {
                    string delivString = Convert.ToString(hmiData[0].Delivered_Qty);
                    int delivInt = 0;
                    int.TryParse(delivString, out delivInt);

                    string processString = Convert.ToString(hmiData[0].ProcessQty);
                    int procInt = 0;
                    int.TryParse(processString, out procInt);

                    PrvProcessQty += procInt;
                    PrvDeliveredQty += delivInt;
                }
                else
                {
                    //no previous delivered or processed qty so Do Nothing.
                }

                #endregion

                int ProcessQty = PrvProcessQty + PrvDeliveredQty;

                int TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);

                var hmidata = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();
                //hmidata.Date = DateTime.Now;

                int Hmiid1 = hmidata.HMIID;
                //delete if any IsSubmit = 0 for this hmiid.
                db.tbllivemultiwoselections.RemoveRange(db.tbllivemultiwoselections.Where(x => x.HMIID == Hmiid1 && x.IsSubmit == 0));
                db.SaveChanges();

                hmidata.OperationNo = ddldata.OperationNo;
                hmidata.PartNo = ddldata.MaterialDesc;
                //hmidata.PEStartTime = DateTime.Now;
                hmidata.Project = ddldata.Project;
                hmidata.SplitWO = "0";
                hmidata.Target_Qty = Convert.ToInt32(ddldata.TargetQty);
                hmidata.Work_Order_No = ddldata.WorkOrder;
                hmidata.ProcessQty = TotalProcessQty;
                hmidata.Delivered_Qty = 0;
                hmidata.DDLWokrCentre = ddldata.WorkCenter;
                Hmiid = hmidata.HMIID;
                hmidata.IsMultiWO = 0;
                db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                Session["FromDDL"] = 1;
                Session["SubmitClicked"] = 0;
                return RedirectToAction("Index", Hmiid);
            }
            #endregion

            //Step2: If DDLID == 0 and ToHMI == 1 then go to HMIScreen "Index" With Normal HMI Flow
            // This means Operator opted for Manual Entry
            if (DDLID == 0 && ToHMI == 1)
            {
                var hmidata = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();

                int Hmiid = hmidata.HMIID;
                //delete if any IsSubmit = 0 for this hmiid.
                db.tbllivemultiwoselections.RemoveRange(db.tbllivemultiwoselections.Where(x => x.HMIID == Hmiid && x.IsSubmit == 0));
                db.SaveChanges();

                hmidata.OperationNo = null;
                hmidata.PartNo = null;
                hmidata.Project = null;
                hmidata.Target_Qty = null;
                hmidata.Work_Order_No = null;
                hmidata.ProcessQty = 0;
                hmidata.Delivered_Qty = 0;
                hmidata.DDLWokrCentre = null;
                hmidata.IsMultiWO = 0;
                db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                Session["FromDDL"] = 2;
                return RedirectToAction("Index");
            }

            //Step 3: If DDLID == 0, then go to DDLList page.

            int MacId = Convert.ToInt32(Session["MachineID"]);
            ViewBag.machineData = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == MacId).Select(m => m.MachineInvNo).ToList();
            var oneMacData = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == MacId).FirstOrDefault();
            string cellidstring = Convert.ToString(oneMacData.CellID);
            string shopidstring = Convert.ToString(oneMacData.ShopID);
            int shopid;
            int.TryParse(shopidstring, out shopid);
            int cellid;
            if (int.TryParse(cellidstring, out cellid) && int.TryParse(shopidstring, out shopid))
            {
                List<string> macList = new List<string>();
                macList.AddRange(db.tblmachinedetails.Where(m => m.CellID == cellid && m.IsDeleted == 0 && !m.ManualWCID.HasValue).Select(m => m.MachineInvNo).ToList());
                macList.AddRange(db.tblmachinedetails.Where(m => m.ShopID == shopid && m.CellID != cellid && m.IsDeleted == 0 && !m.ManualWCID.HasValue).Select(m => m.MachineInvNo).ToList());

                //ViewBag.machineData = db.tblmachinedetails.Where(m => m.CellID == cellid && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList();
                //ViewBag.machineData += db.tblmachinedetails.Where(m => m.ShopID == shopid &&  m.CellID != cellid  && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList();
                ViewBag.machineData = macList;
            }
            else
            {
                if (int.TryParse(shopidstring, out shopid))
                {
                    //ViewBag.machineData = db.tblmachinedetails.Where(m => m.ShopID == shopid && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList();
                    ViewBag.machineData = from row in db.tblmachinedetails
                                          where row.ShopID == shopid && row.IsDeleted == 0 && row.CellID.Equals(null) && !row.ManualWCID.HasValue
                                          select row.MachineInvNo;
                }
                else
                {
                    string plantidstring = Convert.ToString(oneMacData.PlantID);
                    int plantid;
                    if (int.TryParse(plantidstring, out plantid))
                    {
                        //ViewBag.machineData = db.tblmachinedetails.Where(m => m.PlantID == plantid && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList();
                        ViewBag.machineData = from row in db.tblmachinedetails
                                              where row.PlantID == plantid && row.IsDeleted == 0 && row.ShopID.Equals(null) && row.CellID.Equals(null) && !row.ManualWCID.HasValue
                                              select row.MachineInvNo;
                    }
                }
            }
            string machineInvNo = null;
            if (MacInvNo == null)
            {
                var machinedata = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == machineId && m.IsNormalWC == 0).FirstOrDefault();
                Session["macDispName"] = Convert.ToString(machinedata.MachineDispName);
                machineInvNo = machinedata.MachineInvNo;
            }
            else
            {
                machineInvNo = MacInvNo;
            }
            Session["Error"] = TempData["Error"];
            //ViewBag.MacInvNo = machineInvNo;
            List<tblddl> ddlDataList = new List<tblddl>();

            //string WIPQuery = @"SELECT * from tblhmiscreen where isWorkInProgress = 0 and HMIID IN ( SELECT HMIID from tblhmiscreen where MachineID = " + machineId + " group by Work_Order_No,PartNo,OperationNo order by PEStartTime desc ) ";
            string WIPQuery = @"SELECT * from tbllivehmiscreen where isWorkInProgress = 0 and HMIID IN ( SELECT HMIID from tbllivehmiscreen as h where h.MachineID = " + machineId + " order by h.Date)  ";

            var WIP = db.tbllivehmiscreens.SqlQuery(WIPQuery).ToList();
            List<string> ExceptionDDLs = new List<string>();
            var ddldata1 = db.tblddls.Where(m => m.WorkCenter == machineInvNo && m.IsCompleted == 0).ToList();
            if(ddldata1!=null)
            {

                foreach (var row in ddldata1)
                {
                    string ddlid = row.DDLID.ToString();
                    ExceptionDDLs.Add(ddlid);
                }

                ddlDataList = ddldata1;
            }

            foreach (var row in WIP)
            {
                string wono = row.Work_Order_No;
                string partno = row.PartNo;
                string opno = row.OperationNo;

                var ddldata = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno && m.OperationNo == opno && m.IsCompleted == 0).FirstOrDefault();
                if (ddldata != null)
                {
                    string ddlid = ddldata.DDLID.ToString();
                    ExceptionDDLs.Add(ddlid);
                    //ddlDataList.Add(ddldata);
                }
            }
            string ExceptionDDLsArray = null;
            if (ExceptionDDLs.Count > 0)
            {
                ExceptionDDLsArray = String.Join(",", ExceptionDDLs);
            }
            else
            {
                ExceptionDDLsArray = "0";
            }


            String Query = "select * " +
                            "from tblddl WHERE WorkCenter = '" + machineInvNo + "' AND IsCompleted = 0  AND DDLID NOT IN (" + ExceptionDDLsArray + ")" +
                            "order by DaysAgeing = '', Convert(DaysAgeing , SIGNED INTEGER) asc ,FlagRushInd = '',FlagRushInd = 0 ,Convert(FlagRushInd , SIGNED INTEGER) asc  , MADDateInd = '' , MADDateInd asc , MADDate asc";
            //"order by DaysAgeing = \"\", DaysAgeing asc ,FlagRushInd = \"\",FlagRushInd = 0 ,FlagRushInd asc  , MADDateInd = \"\" , MADDateInd asc , MADDate asc";
            ddlDataList.AddRange(db.tblddls.SqlQuery(Query).ToList());
            ViewBag.MacInvNo = machineInvNo;
            Session["MacInvNo"] = machineInvNo;
            // Used in Else Before 2017-01-07 // var WIP = db.tblddls.Where(m => m.WorkCenter == machineInvNo && m.IsCompleted == 0).ToList();
            if (ddlDataList.Count != 0)
            {
                return View(ddlDataList.ToList());
            }
            else
            {
                return View();
            }
        }
        [HttpPost]
        //public ActionResult DDLList(List<int> data1)
        public ActionResult DDLList(string data1)
        {
            //List<string> data2 = new List<string>(data1.Split(','));
            //List<int> data = data1.Select(int.Parse).ToList();
            //List<int> data = data1.ToList();
            List<int> data = new List<int>();
            data = JsonConvert.DeserializeObject<List<int>>(data1);

            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            Session["VError"] = null;

            int WOCount = data.Count;
            int machineId = Convert.ToInt32(Session["MachineID"]);
            if (WOCount == 1)
            {
                #region
                int DDLID = data.First();
                int Hmiid = 0;
                var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                //String SplitWO = ddldata.SplitWO;
                String WONo = ddldata.WorkOrder;
                String Part = ddldata.MaterialDesc;
                String Operation = ddldata.OperationNo;
                int TargetQtyNew = Convert.ToInt32(ddldata.TargetQty);

                //OpNo sequence
                #region 2017-02-07
                bool IsInHMI = false;
                var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONo && m.MaterialDesc == Part && m.OperationNo != Operation && m.IsCompleted == 0).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                foreach (var row in Siblingddldata)
                {
                    IsInHMI = true; //reinitialize
                    int localOPNo = Convert.ToInt32(row.OperationNo);
                    string localOPNoString = Convert.ToString(row.OperationNo);
                    if (localOPNo < Convert.ToInt32(Operation))
                    {
                        #region //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                        //which case is valid.
                        var SiblingHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == localOPNoString).OrderBy(m => m.HMIID).FirstOrDefault();
                        //var SiblingHMIdatahistorian = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == localOPNoString).FirstOrDefault(); //added by Ashok
                        if (SiblingHMIdata == null)// || SiblingHMIdatahistorian==null)
                        {
                            TempData["VError"] = "Please Select Below WorkOrder, WONo: " + WONo + " PartNo: " + Part + " OperationNo: " + localOPNo;
                            IsInHMI = false;
                            //break;
                        }
                        else
                        {
                            if (SiblingHMIdata.Date == null)// || SiblingHMIdatahistorian.Date==null) //=> lower OpNo is not submitted.
                            {
                                TempData["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                //return RedirectToAction("Index");
                                IsInHMI = false;
                                //break;
                            }
                            else
                            {
                                TempData["VError"] = null;
                                IsInHMI = true;
                            }
                        }
                        #endregion

                        if (!IsInHMI)
                        {
                            #region //also check in MultiWO table
                            string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                            var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();

                            if (WIPMWO.Count == 0)
                            {
                                TempData["VError"] = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                //return RedirectToAction("Index");
                                return RedirectToAction("DDLList");
                                //IsInHMI = false;
                                //break;
                            }

                            foreach (var rowHMI in WIPMWO)
                            {
                                int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                if (MWOHMIData != null) //obviously != 0
                                {
                                    if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                    {
                                        TempData["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                        //return RedirectToAction("Index");
                                        //break;
                                        return RedirectToAction("DDLList");
                                    }
                                    else
                                    {
                                        TempData["VError"] = null;
                                        //This WO,OPNo is in MultiWO Table so clear its crime record.
                                    }
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            //continue with other execution
                            //return RedirectToAction("Index");
                        }
                    }
                }

                if (IsInHMI == false)
                {
                    //return View();
                    return RedirectToAction("DDLList");
                }

                ///to Catch those Manual WorkOrders 
                //string WIPQuery1 = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo != '" + Operation + "' and  IsMultiWO = 0 and DDLWokrCentre is null order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo ) order by OperationNo ;";
                //var WIPDDL1 = db.tblhmiscreens.SqlQuery(WIPQuery1).ToList();
                //foreach (var row in WIPDDL1)
                //{
                //    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                //    if (InnerOpNo < Convert.ToInt32(Operation))
                //    {
                //        string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
                //        var WIP1 = db.tblhmiscreens.SqlQuery(WIPQueryHMI).ToList();
                //        if (WIP1.Count == 0)
                //        {
                //            TempData["VError"] = " Select & Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                //            //return RedirectToAction("Index");
                //            return RedirectToAction("DDLList");
                //        }
                //        foreach (var rowHMI in WIP1)
                //        {
                //            if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                //            {
                //                TempData["VError"] = " Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                //                //return RedirectToAction("Index");
                //                return RedirectToAction("DDLList");
                //            }
                //        }
                //    }
                //}
                #endregion

                int PrvProcessQty = 0, PrvDeliveredQty = 0, TotalProcessQty = 0;
                //var getProcessQty = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.HMIID).Take(1).ToList();
                //if (getProcessQty.Count > 0)
                //{
                //    string delivString = Convert.ToString(getProcessQty[0].Delivered_Qty);
                //    int.TryParse(delivString, out PrvDeliveredQty);
                //    string processString = Convert.ToString(getProcessQty[0].ProcessQty);
                //    int.TryParse(processString, out PrvProcessQty);
                //    TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);
                //}

                //New Code To Update ProcessedQty 2017-05-11

                var hmidata = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();
                //hmidata.Date = DateTime.Now;
                Hmiid = hmidata.HMIID;

                string woNo = WONo;
                string partNo = Part;
                string opNo = Operation;

                var getProcessQty1 = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.HMIID != Hmiid && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).FirstOrDefault();
                if (getProcessQty1 != null)
                {
                    #region new code
                    //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                    int isHMIFirst = 2; //default NO History for that wo,pn,on

                    var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                    //var hmiData = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 0).OrderByDescending(m => m.HMIID).Take(1).ToList();

                    if (getProcessQty1 != null && mulitwoData.Count > 0) // now check for greatest amongst
                    {

                        //DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn); //2017-06-02
                        //Based on hmiid of  multiwotable get  Time Column of tblhmiscreen 
                        //int localhmiid = Convert.ToInt32(mulitwoData[0].HMIID);
                        //var hmiiData = db.tblhmiscreens.Find(localhmiid);
                        DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].tbllivehmiscreen.Time);
                        DateTime hmiDateTime = Convert.ToDateTime(getProcessQty1.Time);
                        if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                        {
                            isHMIFirst = 1;
                        }
                        else
                        {
                            isHMIFirst = 0;
                        }

                    }
                    else if (mulitwoData.Count > 0)
                    {
                        isHMIFirst = 1;
                    }
                    else if (getProcessQty1 != null)
                    {
                        isHMIFirst = 0;
                    }

                    if (isHMIFirst == 1)
                    {
                        string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                        int delivInt = 0;
                        int.TryParse(delivString, out delivInt);

                        string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                        int procInt = 0;
                        int.TryParse(processString, out procInt);

                        PrvProcessQty += procInt;
                        PrvDeliveredQty += delivInt;
                    }
                    else if (isHMIFirst == 0)
                    {
                        string delivString = Convert.ToString(getProcessQty1.Delivered_Qty);
                        int delivInt = 0;
                        int.TryParse(delivString, out delivInt);

                        string processString = Convert.ToString(getProcessQty1.ProcessQty);
                        int procInt = 0;
                        int.TryParse(processString, out procInt);

                        PrvProcessQty += procInt;
                        PrvDeliveredQty += delivInt;
                    }
                    else
                    {
                        //no previous delivered or processed qty so Do Nothing.
                    }
                    #endregion

                    int newProcessedQty = PrvProcessQty + PrvDeliveredQty;
                    if (Convert.ToInt32(getProcessQty1.isWorkInProgress) == 1 )
                    {
                        Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                        hmidata.Prod_FAI = null;
                        hmidata.Target_Qty = null;
                        hmidata.OperationNo = null;
                        hmidata.PartNo = null;
                        hmidata.Work_Order_No = null;
                        hmidata.Project = null;
                        hmidata.Date = null;
                        hmidata.DDLWokrCentre = null;
                        hmidata.ProcessQty = 0;
                        hmidata.Delivered_Qty = 0;
                        Session["FromDDL"] = 2;
                        TempData["ForDDL2"] = 2;
                        db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        return RedirectToAction("Index");
                    }

                    if (TargetQtyNew < newProcessedQty)
                    {
                        Session["Error"] = "Previous ProcessedQty :" + newProcessedQty + ". TargetQty Cannot be Less than Processed";
                        hmidata.ProcessQty = 0;
                        hmidata.Delivered_Qty = 0;
                        hmidata.Date = null;
                        Session["FromDDL"] = 2;
                        TempData["ForDDL2"] = 2;
                        db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }
                else //Its not in HMIScreen , then it may be in multiwoselection table.
                {
                    #region new code

                    //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                    int isHMIFirst = 2; //default NO History for that wo,pn,on

                    var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                    //var hmiData = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 0).OrderByDescending(m => m.HMIID).Take(1).ToList();

                    if (getProcessQty1 != null && mulitwoData.Count > 0) // now check for greatest amongst
                    {
                        //DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn); //2017-06-02
                        //Based on hmiid of  multiwotable get  Time Column of tblhmiscreen 
                        //int localhmiid = Convert.ToInt32(mulitwoData[0].HMIID);
                        //var hmiiData = db.tblhmiscreens.Find(localhmiid);
                        DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].tbllivehmiscreen.Time);
                        DateTime hmiDateTime = Convert.ToDateTime(getProcessQty1.Time);

                        if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                        {
                            isHMIFirst = 1;
                        }
                        else
                        {
                            isHMIFirst = 0;
                        }
                    }
                    else if (mulitwoData.Count > 0)
                    {
                        isHMIFirst = 1;
                    }
                    if (isHMIFirst == 1)
                    {
                        string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                        int delivInt = 0;
                        int.TryParse(delivString, out delivInt);

                        string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                        int procInt = 0;
                        int.TryParse(processString, out procInt);

                        PrvProcessQty += procInt;
                        PrvDeliveredQty += delivInt;
                    }
                    #endregion
                }
                //hmiidData.ProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);
                TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);

                //delete if any IsSubmit = 0 for this hmiid.
                db.tbllivemultiwoselections.RemoveRange(db.tbllivemultiwoselections.Where(x => x.HMIID == Hmiid && x.IsSubmit == 0));
                db.SaveChanges();

                //hmidata.PEStartTime = DateTime.Now;
                hmidata.OperationNo = ddldata.OperationNo;
                hmidata.PartNo = ddldata.MaterialDesc;
                hmidata.DDLWokrCentre = ddldata.WorkCenter;
                hmidata.Project = ddldata.Project;
                hmidata.Target_Qty = Convert.ToInt32(ddldata.TargetQty);
                hmidata.Work_Order_No = ddldata.WorkOrder;
                hmidata.ProcessQty = TotalProcessQty;
                hmidata.Delivered_Qty = 0;
                hmidata.IsMultiWO = 0;
                Hmiid = hmidata.HMIID;
                db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                Session["FromDDL"] = 1;
                //Session["VError"] = "ToHMI";
                return RedirectToAction("Index", Hmiid);
                #endregion
            }
            else if (WOCount > 1)
            {
                #region
                int TotalTargetQty = 0;
                int TotalProcessQty = 0;
                var hmidata = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();
                int Hmiid = hmidata.HMIID;
                int i = 0;
                String MainOpearationNo = null;
                String MainWorkOrder = null;
                String MainPartNo = null;
                String MainProject = null;

                //delete if any IsSubmit = 0 for this hmiid.
                db.tbllivemultiwoselections.RemoveRange(db.tbllivemultiwoselections.Where(x => x.HMIID == Hmiid && x.IsSubmit == 0));
                db.SaveChanges();

                ////Check if this WONo, PartNo, OpNo was Previously JF'ed
                //foreach (int DDLID in data)
                //{
                //    int PrvProcessQty = 0, PrvDeliveredQty = 0;

                //    var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                //    //String SplitWO = ddldata.SplitWO;
                //    //int SplitWOInt = SplitWO.StartsWith("y", StringComparison.CurrentCultureIgnoreCase) == true ? 1 : 0;
                //    String WONo = ddldata.WorkOrder;
                //    String Part = ddldata.MaterialDesc;
                //    String Operation = ddldata.OperationNo;
                //    var hmiData = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 1).FirstOrDefault();
                //    if (hmidata != null)
                //    {
                //        TempData["Error"] = "It's already JobFinished for WorkOrder:" + WONo + " OpNo: " + Operation;
                //        //return RedirectToAction("DDLList",0,Session["MacInvNo"],0);
                //        string MacInvNo = Convert.ToString(Session["MacInvNo"]);
                //        return RedirectToActionPermanent("DDLList", new { DDLID = 0,MacInvNo,ToHMI = 0 });
                //    }
                //}


                //multiWO OpNo sequence
                string DDLIDString = string.Join(",", data.Select(x => x.ToString()).ToArray());
                #region 2017-02-07
                foreach (var DDLID in data)
                {
                    //int DDLID = DDLRow;
                    var ddldataInner = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    String SplitWOInner = ddldataInner.SplitWO;
                    String WONoInner = ddldataInner.WorkOrder;
                    String PartInner = ddldataInner.MaterialDesc;
                    String OperationInner = ddldataInner.OperationNo;

                    bool IsInHMI = true;
                    var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONoInner && m.MaterialDesc == PartInner && m.OperationNo != OperationInner && m.IsCompleted == 0).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                    foreach (var row in Siblingddldata)
                    {
                        string localddlid = Convert.ToString(row.DDLID);
                        int localOPNo = Convert.ToInt32(row.OperationNo);
                        string localOPNoString = Convert.ToString(row.OperationNo);
                        if (localOPNo < Convert.ToInt32(OperationInner))
                        {
                            if (DDLIDString.Contains(localddlid))
                            { }
                            else
                            {
                                //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                                //which case is valid.
                                var SiblingHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == localOPNoString).FirstOrDefault();
                                //var SiblingHMIdatahistorian = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == localOPNoString).FirstOrDefault(); //added by Ashok
                                if (SiblingHMIdata == null)// || SiblingHMIdatahistorian==null)
                                {
                                    TempData["VError"] = "Please Select Below WorkOrder , WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + localOPNo;
                                    //isValid = false;
                                    //break;
                                    IsInHMI = false;
                                }
                                else
                                {
                                    if (SiblingHMIdata.Date == null)// || SiblingHMIdatahistorian.Date==null) //=> lower OpNo is not submitted.
                                    {
                                        TempData["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                        //return RedirectToAction("Index");
                                        IsInHMI = false;
                                        //break;
                                    }
                                }

                                if (!IsInHMI)
                                {
                                    //also check in MultiWO table
                                    string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + WONoInner + "' and PartNo = '" + PartInner + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                    var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();

                                    if (WIPMWO.Count == 0)
                                    {
                                        TempData["VError"] = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                        //return RedirectToAction("Index");
                                        return RedirectToAction("DDLList");
                                        //IsInHMI = false;
                                        //break;
                                    }
                                    foreach (var rowHMI in WIPMWO)
                                    {
                                        int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                        var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                        if (MWOHMIData != null)
                                        {
                                            if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                TempData["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                //return RedirectToAction("Index");
                                                return RedirectToAction("DDLList");
                                                //break;
                                            }
                                            else
                                            {
                                                TempData["VError"] = null;
                                                IsInHMI = true;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    TempData["VError"] = null;
                                    //continue with other execution
                                }

                            }
                        }
                    }
                }
                #endregion

                string ddlWorkCenter = null;
                int StartWO = 1;
                foreach (int DDLID in data)
                {
                    int PrvProcessQty = 0, PrvDeliveredQty = 0;
                    var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    //String SplitWO = ddldata.SplitWO;
                    //int SplitWOInt = SplitWO.StartsWith("y", StringComparison.CurrentCultureIgnoreCase) == true ? 1 : 0;
                    String WONo = ddldata.WorkOrder;
                    String Part = ddldata.MaterialDesc;
                    String Operation = ddldata.OperationNo;
                    ddlWorkCenter = ddldata.WorkCenter;
                    int TargetQty = Convert.ToInt32(ddldata.TargetQty);
                    TotalTargetQty += TargetQty;
                    if (i == 0)
                    {
                        MainOpearationNo = Operation;
                        MainWorkOrder = WONo;
                        MainPartNo = Part;
                        MainProject = ddldata.Project;
                    }

                    var WorkOrderData = db.tblddls.Where(m => m.MaterialDesc == MainPartNo && m.OperationNo == MainOpearationNo && m.WorkOrder == MainWorkOrder && m.IsCompleted == 1).FirstOrDefault();
                    if (WorkOrderData != null)
                    {
                        var DDLList = db.tblddls.Where(m => m.DDLID == DDLID).FirstOrDefault();
                        if (DDLList != null)
                        {
                            DDLList.IsCompleted = 1;
                            db.Entry(DDLList).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }

                        TempData["VError"] = "Job is Finished for WONO.:" + MainWorkOrder + " OperationNo.:" + MainOpearationNo + " PartNo" + MainPartNo;
                        return RedirectToAction("DDLList");
                    }
                    ////var getMultiWoPorcessQty = db.tbl_multiwoselection.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation).OrderByDescending(m => m.MultiWOID).FirstOrDefault();
                    ////int ProcessQty = Convert.ToInt32(getMultiWoPorcessQty.ProcessQty + getMultiWoPorcessQty.DeliveredQty);

                    #region OLD
                    //int ProcessQty = 0;
                    //var getMultiWoPorcessQty = db.tbl_multiwoselection.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation).OrderByDescending(m => m.MultiWOID).Take(1).ToList();
                    //if (getMultiWoPorcessQty.Count > 0)
                    //{
                    //    string delivString = Convert.ToString(getMultiWoPorcessQty[0].DeliveredQty);
                    //    int delivInt = 0;
                    //    int.TryParse(delivString, out delivInt);

                    //    string processString = Convert.ToString(getMultiWoPorcessQty[0].ProcessQty);
                    //    int procInt = 0;
                    //    int.TryParse(processString, out procInt);
                    //    ProcessQty = Convert.ToInt32(procInt + delivInt);
                    //}
                    #endregion

                    #region new code

                    //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                    int isHMIFirst = 2; //default NO History for that wo,pn,on

                    var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                    var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();

                    if (hmiData.Count > 0 && mulitwoData.Count > 0) // now check for greatest amongst
                    {
                        //DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn); //2017-06-02
                        //Based on hmiid of  multiwotable get  Time Column of tblhmiscreen 
                        int localhmiid = Convert.ToInt32(mulitwoData[0].HMIID);
                        var hmiiData = db.tbllivehmiscreens.Find(localhmiid);

                        DateTime multiwoDateTime = Convert.ToDateTime(hmiiData.Time);
                        DateTime hmiDateTime = Convert.ToDateTime(hmiData[0].Time);

                        if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                        {
                            isHMIFirst = 1;
                        }
                        else
                        {
                            isHMIFirst = 0;
                        }

                    }
                    else if (mulitwoData.Count > 0)
                    {
                        isHMIFirst = 1;
                    }
                    else if (hmiData.Count > 0)
                    {
                        isHMIFirst = 0;
                    }

                    int MachineID = 0, OperatorID = 0;
                    string OperatorDet = null, Prod_FAI = null;

                    if (isHMIFirst == 1)
                    {
                        string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                        int delivInt = 0;
                        int.TryParse(delivString, out delivInt);

                        string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                        int procInt = 0;
                        int.TryParse(processString, out procInt);

                        PrvProcessQty += procInt;
                        PrvDeliveredQty += delivInt;
                        int localHMIIDFromMultiWO = Convert.ToInt32(mulitwoData[0].HMIID);
                        var RespectiveHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == localHMIIDFromMultiWO).FirstOrDefault();
                        if (RespectiveHMIData != null)
                        {
                            MachineID = RespectiveHMIData.MachineID;
                            OperatorID = RespectiveHMIData.OperatiorID;
                            OperatorDet = RespectiveHMIData.OperatorDet;
                            Prod_FAI = RespectiveHMIData.Prod_FAI;
                        }
                    }
                    else if (isHMIFirst == 0)
                    {
                        string delivString = Convert.ToString(hmiData[0].Delivered_Qty);
                        int delivInt = 0;
                        int.TryParse(delivString, out delivInt);

                        string processString = Convert.ToString(hmiData[0].ProcessQty);
                        int procInt = 0;
                        int.TryParse(processString, out procInt);

                        PrvProcessQty += procInt;
                        PrvDeliveredQty += delivInt;
                        int localHMI = hmiData[0].HMIID;
                        var RespectiveHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == localHMI).FirstOrDefault();
                        if (RespectiveHMIData != null)
                        {
                            MachineID = RespectiveHMIData.MachineID;
                            OperatorID = RespectiveHMIData.OperatiorID;
                            OperatorDet = RespectiveHMIData.OperatorDet;
                            Prod_FAI = RespectiveHMIData.Prod_FAI;
                        }
                    }
                    else
                    {
                        //no previous delivered or processed qty so Do Nothing.
                    }

                    #endregion

                    int ProcessQty = PrvProcessQty + PrvDeliveredQty;
                    TotalProcessQty += ProcessQty;
                    if (TargetQty == ProcessQty)
                    {
                        TempData["VError"] = " Start WONo: " + WONo + " and PartNo: " + Part + " and OperationNo: " + Operation;

                        //code to get CorrectedDate
                        #region
                        string CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                        if (DateTime.Now.Hour < 6 && DateTime.Now.Hour >= 0)
                        {
                            CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                        }
                        #endregion
                        //Code to get Shift
                        string Shift = db.tbllivehmiscreens.Where(m => m.MachineID == MachineID).OrderByDescending(m => m.PEStartTime).Select(m => m.Shift).FirstOrDefault();
                        #region
                        //tblhmiscreen tblh = new tblhmiscreen();
                        //tblh.CorrectedDate = CorrectedDate;
                        //tblh.Date = DateTime.Now;
                        //tblh.Time = DateTime.Now;
                        //tblh.PEStartTime = DateTime.Now;
                        //tblh.DDLWokrCentre = ddldata.WorkCenter;
                        //tblh.DoneWithRow = 1;
                        //tblh.IsHold = 0;
                        //tblh.IsMultiWO = 0;
                        //tblh.isUpdate = 1;
                        //tblh.isWorkInProgress = 1;
                        //tblh.isWorkOrder = 0;
                        //tblh.MachineID = MachineID;
                        //tblh.OperationNo = ddldata.OperationNo;
                        //tblh.OperatiorID = OperatorID;
                        //tblh.OperatorDet = OperatorDet;
                        //tblh.PartNo = ddldata.MaterialDesc;
                        //tblh.Target_Qty = ProcessQty;
                        //tblh.ProcessQty = ProcessQty;
                        //tblh.Delivered_Qty = 0;
                        //tblh.Prod_FAI = Prod_FAI;
                        //tblh.Project = ddldata.Project;
                        //tblh.Rej_Qty = 0;
                        //tblh.Shift = Shift;
                        //tblh.SplitWO = "No";
                        //tblh.Status = 2;
                        //tblh.Work_Order_No = ddldata.WorkOrder;

                        //db.tblhmiscreens.Add(tblh);
                        //db.SaveChanges();
                        #endregion
                        int localDDLID = ddldata.DDLID;
                        //if it existing in DDLList Update 
                        var DDLList = db.tblddls.Where(m => m.DDLID == localDDLID).ToList();
                        foreach (var row in DDLList)
                        {
                            row.IsCompleted = 1;
                            db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }
                        StartWO = 0;
                        
                    }
                    else
                    { 
                        tbllivemultiwoselection MultiWORow = new tbllivemultiwoselection();
                        MultiWORow.DDLWorkCentre = ddldata.WorkCenter;
                        MultiWORow.OperationNo = Operation;
                        MultiWORow.PartNo = Part;
                        MultiWORow.SplitWO = "0";
                        MultiWORow.TargetQty = TargetQty;
                        MultiWORow.WorkOrder = WONo;
                        MultiWORow.ProcessQty = ProcessQty;
                        MultiWORow.HMIID = Hmiid;
                        MultiWORow.IsCompleted = 0;
                        MultiWORow.CreatedOn = System.DateTime.Now;
                        db.tbllivemultiwoselections.Add(MultiWORow);
                        db.SaveChanges();
                    }

                }
                if (StartWO == 1)
                {
                    //hmidata.Date = DateTime.Now;
                    //hmidata.PEStartTime = DateTime.Now;
                    hmidata.OperationNo = MainOpearationNo + " - " + WOCount;
                    hmidata.PartNo = MainPartNo + " - " + WOCount;
                    hmidata.Project = MainProject;
                    hmidata.Target_Qty = TotalTargetQty;
                    hmidata.Work_Order_No = MainWorkOrder + " - " + WOCount;
                    hmidata.SplitWO = "0";
                    hmidata.ProcessQty = TotalProcessQty;
                    hmidata.Delivered_Qty = 0;
                    Hmiid = hmidata.HMIID;
                    hmidata.DDLWokrCentre = ddlWorkCenter;
                    hmidata.IsMultiWO = 1;
                    db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    Session["FromDDL"] = 4;
                    return RedirectToAction("Index", Hmiid);
                }
                else
                {
                    return RedirectToAction("DDLList");
                }
                #endregion
            }
            return RedirectToAction("Index");
        }

        //public JsonResult GetMacSiblings()
        //{
        //    string MacData = null;
        //    int MacId = Convert.ToInt32(Session["MachineID"]);
        //    var machineData = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == MacId).ToList();
        //    var oneMacData = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == MacId).FirstOrDefault();
        //    string cellidstring = Convert.ToString(oneMacData.CellID);
        //    if (cellidstring != null)
        //    {
        //        int cellid = Convert.ToInt32(oneMacData.CellID);
        //        machineData = db.tblmachinedetails.Where(m => m.CellID == cellid && m.IsDeleted == 0).ToList();
        //    }
        //    else
        //    {
        //        string shopidstring = Convert.ToString(oneMacData.ShopID);
        //        if (shopidstring != null)
        //        {
        //            int shopid = Convert.ToInt32(oneMacData.ShopID);
        //            machineData = db.tblmachinedetails.Where(m => m.ShopID == shopid && m.IsDeleted == 0).ToList();
        //        }
        //        else
        //        {
        //            string plantidstring = Convert.ToString(oneMacData.PlantID);
        //            if (plantidstring != null)
        //            {
        //                int plantid = Convert.ToInt32(oneMacData.PlantID);
        //                machineData = db.tblmachinedetails.Where(m => m.PlantID == plantid && m.IsDeleted == 0).ToList();
        //            }
        //        }
        //    }

        //    foreach (var row in machineData)
        //    {
        //        //MacData = "yes";
        //        MacData += @"<Button class='BringWo'> <span id='" + row.MachineInvNo + "' class='macInvNo'> " + row.MachineInvNo + " </span> </Button>";
        //    }

        //    return Json(MacData, JsonRequestBehavior.AllowGet);
        //}

        public JsonResult JsonIdleChecker()
        {
            string retStatus = "false";
            var machineID = Convert.ToInt32(Session["MachineID"]);
            var CurrentStatusData = db.tbllivelossofentries.Where(m => m.MachineID == machineID && (m.IsScreen == 1 || m.IsStart == 1)).OrderByDescending(m => m.StartDateTime).Take(1).ToList();
            if (CurrentStatusData.Count > 0)
            {
                int IsStart = Convert.ToInt32(CurrentStatusData[0].IsStart);
                int IsScreen = Convert.ToInt32(CurrentStatusData[0].IsScreen);
                int forRefresh = Convert.ToInt32(CurrentStatusData[0].ForRefresh);
                if (IsStart == 1 && IsScreen == 0 && forRefresh == 0)
                {
                    retStatus = "true";
                    CurrentStatusData[0].ForRefresh = 1;
                    db.Entry(CurrentStatusData[0]).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
                if (IsStart == 1 && IsScreen == 1 && forRefresh == 1) //loss code has been repeadtly entered , so dont popup just show screen
                {
                    retStatus = "true";
                    CurrentStatusData[0].ForRefresh = 2;
                    db.Entry(CurrentStatusData[0]).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        public JsonResult JsonIdleEndChecker()
        {
            string retStatus = "false";
            var machineID = Convert.ToInt32(Session["MachineID"]);
            using (mazakdaqEntities dbloss = new mazakdaqEntities())
            {
                var CurrentStatusData = db.tbllivelossofentries.Where(m => m.MachineID == machineID).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
                if (CurrentStatusData != null)
                {
                    int IsDone = Convert.ToInt32(CurrentStatusData.DoneWithRow);
                    if (IsDone == 1)
                    {
                        retStatus = "true";
                    }
                }
            }
            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        public JsonResult JsonRemoveWO(int hmiid) // Remove WorkOrder if Its Not Started.
        {
            string retStatus = "false";
            using (mazakdaqEntities dbhmi = new mazakdaqEntities())
            {
                var CurrentStatusData = dbhmi.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                if (CurrentStatusData != null && CurrentStatusData.IsMultiWO == 1)
                {
                    try
                    {
                        dbhmi.tbllivemultiwoselections.RemoveRange(dbhmi.tbllivemultiwoselections.Where(m => m.HMIID == hmiid).ToList());
                        dbhmi.SaveChanges();
                    }
                    catch (Exception e)
                    {

                    }
                }
                if (CurrentStatusData != null && CurrentStatusData.Date == null)
                {
                    dbhmi.tbllivehmiscreens.Remove(CurrentStatusData);
                    dbhmi.SaveChanges();
                    retStatus = "true";
                }

            }
            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        public JsonResult IsMultiWOAllowable(string id)
        {
            string status = "no";
            int machineId = Convert.ToInt32(Session["MachineID"]);
            var machineDATA = db.tblmachinedetails.Where(m => m.MachineInvNo == id).FirstOrDefault();
            string PlantID = Convert.ToString(machineDATA.PlantID);
            string ShopID = Convert.ToString(machineDATA.ShopID);
            string CellID = Convert.ToString(machineDATA.CellID);
            string WCID = Convert.ToString(machineDATA.MachineID);
            bool tick = false;

            int value = 0;
            if (int.TryParse(WCID, out value))
            {
                var MultiWoWCData = db.tblmultipleworkorders.Where(m => m.IsDeleted == 0 && m.WCID == value).ToList();
                if (MultiWoWCData.Count > 0)
                {
                    status = "yes";
                }
            }
            if (int.TryParse(CellID, out value))
            {
                var MultiWoCellData = db.tblmultipleworkorders.Where(m => m.IsDeleted == 0 && m.CellID == value && m.WCID == null).ToList();
                if (MultiWoCellData.Count > 0)
                {
                    status = "yes";
                }
            }
            if (int.TryParse(ShopID, out value))
            {
                var MultiWoShopData = db.tblmultipleworkorders.Where(m => m.IsDeleted == 0 && m.ShopID == value && m.CellID == null && m.WCID == null).ToList();
                if (MultiWoShopData.Count > 0)
                {
                    status = "yes";
                }
            }
            if (int.TryParse(PlantID, out value))
            {
                var MultiWoPlantData = db.tblmultipleworkorders.Where(m => m.IsDeleted == 0 && m.PlantID == value && m.ShopID == null && m.CellID == null && m.WCID == null).ToList();
                if (MultiWoPlantData.Count > 0)
                {
                    status = "yes";
                }
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }

        public ActionResult MultiWOQtyEntry(int id)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            int macID = Convert.ToInt32(Session["MachineID"]);
            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == macID).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);

            var MultiWOList = db.tbllivemultiwoselections.Where(m => m.HMIID == id).ToList();
            return View(MultiWOList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MultiWOQtyEntry(List<tbllivemultiwoselection> MultiWO)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            int deliveredQtySummationValue = 0;
            int targetQtySummationValue = 0;
            int hmiID = 0;
            if (MultiWO != null)
            {
                foreach (var row in MultiWO)
                {
                    hmiID = Convert.ToInt32(row.HMIID);
                    int DelQty = (int)row.DeliveredQty;
                    deliveredQtySummationValue += DelQty;
                    int TarQty = (int)row.TargetQty;
                    targetQtySummationValue += TarQty;
                    using (mazakdaqEntities dbMWO = new mazakdaqEntities())
                    {
                        var IndividualMultiWOIDData = dbMWO.tbllivemultiwoselections.Find(row.MultiWOID);
                        IndividualMultiWOIDData.IsCompleted = 0;
                        IndividualMultiWOIDData.DeliveredQty = DelQty;

                        var splitWO = row.SplitWO;
                        dbMWO.Entry(IndividualMultiWOIDData).State = System.Data.Entity.EntityState.Modified;
                        dbMWO.SaveChanges();
                    }
                }
            }
            tbllivehmiscreen thmidata = db.tbllivehmiscreens.Where(m => m.HMIID == hmiID).FirstOrDefault();
            if (thmidata != null)
            {
                thmidata.Delivered_Qty = deliveredQtySummationValue;
                db.Entry(thmidata).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public JsonResult AutoSaveMultiWOSplitWO(int multiwoID, string SplitWO)
        {
            bool retStatus = false;
            int HMIID = 0;
            var thisrow = db.tbllivemultiwoselections.Where(m => m.MultiWOID == multiwoID).FirstOrDefault();
            if (thisrow != null)
            {
                if (SplitWO.Equals("Yes") || SplitWO.Equals("No"))
                {
                    thisrow.SplitWO = SplitWO;
                    db.Entry(thisrow).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
                HMIID = (int)thisrow.HMIID;
            }
            if (HMIID != 0)
            {
                var AllRows = db.tbllivemultiwoselections.Where(m => m.MultiWOID == multiwoID).ToList();
                string tblhmiscreenRowSplitStatus = "No";
                foreach (var row in AllRows)
                {
                    if (row.SplitWO == "Yes")
                    {
                        tblhmiscreenRowSplitStatus = "Yes";
                        break;
                    }
                }
                if (AllRows.Count > 0)
                {
                    var tblhmiscreenRow = db.tbllivehmiscreens.Find(HMIID);
                    if (tblhmiscreenRow != null)
                    {
                        tblhmiscreenRow.SplitWO = tblhmiscreenRowSplitStatus;
                        db.Entry(tblhmiscreenRow).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        public JsonResult JsonBreakdownChecker()
        {
            string retStatus = null;
            var machineID = Convert.ToInt32(Session["MachineID"]);

            string correcteddate = null;
            tbldaytiming StartTime1 = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime1.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                correcteddate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                correcteddate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            var CurrentStatusData = db.tblbreakdowns.Where(m => m.MachineID == machineID && m.DoneWithRow == 0).OrderByDescending(m => m.StartTime).FirstOrDefault();
            if (CurrentStatusData != null)
            {
                int value = 1;
                string doneWithRowString = Convert.ToString(CurrentStatusData.DoneWithRow);

                Int32.TryParse(doneWithRowString, out value);
                if (CurrentStatusData.MessageCode == "PM")
                {
                    if (value == 0)
                    {
                        retStatus = "PM";
                    }
                }
                else
                {
                    if (value == 0)
                    {
                        retStatus = "BREAKDOWN";
                    }
                }
            }
            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        //PartialFinished WO List 2017-01-07 Janardhan
        public ActionResult PartialFinishedList(int DDLID = 0, string MacInvNo = null, int ToHMI = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).FirstOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            int machineId = Convert.ToInt32(Session["MachineID"]);
            ViewBag.opid = Session["opid"];
            ViewBag.mcnid = machineId;
            ViewBag.coretddt = CorrectedDate;

            //int handleidleReturnValue = HandleIdle();
            //if (handleidleReturnValue == 0)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //}

            //Step 1: If DDLID is given then insert that data into HMIScreen table , take its HMIID and redirect to Index 
            if (DDLID != 0)
            {
                int Hmiid = 0;
                var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                // String SplitWO = ddldata.SplitWO;
                String WONo = ddldata.WorkOrder;
                String Part = ddldata.MaterialDesc;
                String Operation = ddldata.OperationNo;

                int PrvProcessQty = 0, PrvDeliveredQty = 0;
                var getProcessQty = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.HMIID).Take(1).ToList();
                if (getProcessQty.Count > 0)
                {
                    PrvProcessQty = Convert.ToInt32(getProcessQty[0].ProcessQty);
                    PrvDeliveredQty = Convert.ToInt32(getProcessQty[0].Delivered_Qty);
                }
                int TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);

                var hmidata = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();
                //hmidata.Date = DateTime.Now;

                int Hmiid1 = hmidata.HMIID;
                //delete if any IsSubmit = 0 for this hmiid.
                db.tbllivemultiwoselections.RemoveRange(db.tbllivemultiwoselections.Where(x => x.HMIID == Hmiid1 && x.IsSubmit == 0));
                db.SaveChanges();

                hmidata.OperationNo = ddldata.OperationNo;
                hmidata.PartNo = ddldata.MaterialDesc;
                //hmidata.PEStartTime = DateTime.Now;
                hmidata.Project = ddldata.Project;
                hmidata.Target_Qty = Convert.ToInt32(ddldata.TargetQty);
                hmidata.Work_Order_No = ddldata.WorkOrder;
                hmidata.ProcessQty = TotalProcessQty;
                hmidata.DDLWokrCentre = ddldata.WorkCenter;
                Hmiid = hmidata.HMIID;
                hmidata.IsMultiWO = 0;
                db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                Session["FromDDL"] = 1;
                Session["SubmitClicked"] = 0;
                return RedirectToAction("Index", Hmiid);
            }

            //Step2: If DDLID == 0 and ToHMI == 1 then go to HMIScreen "Index" With Normal HMI Flow
            // This means Operator opted for Manual Entry
            if (DDLID == 0 && ToHMI == 1)
            {
                var hmidata = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();

                int Hmiid = hmidata.HMIID;
                //delete if any IsSubmit = 0 for this hmiid.
                db.tbllivemultiwoselections.RemoveRange(db.tbllivemultiwoselections.Where(x => x.HMIID == Hmiid && x.IsSubmit == 0));
                db.SaveChanges();

                hmidata.OperationNo = null;
                hmidata.PartNo = null;
                hmidata.Project = null;
                hmidata.Target_Qty = null;
                hmidata.Work_Order_No = null;
                hmidata.ProcessQty = 0;
                hmidata.DDLWokrCentre = null;
                hmidata.IsMultiWO = 0;
                db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                Session["FromDDL"] = 2;
                return RedirectToAction("Index");
            }

            //Step 3: If DDLID == 0, then go to DDLList page.

            int MacId = Convert.ToInt32(Session["MachineID"]);
            ViewBag.machineData = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == MacId).Select(m => m.MachineInvNo).ToList();
            var oneMacData = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == MacId).FirstOrDefault();
            string cellidstring = Convert.ToString(oneMacData.CellID);
            string shopidstring = Convert.ToString(oneMacData.ShopID);
            int shopid;
            int.TryParse(shopidstring, out shopid);
            int cellid;
            if (int.TryParse(cellidstring, out cellid) && int.TryParse(shopidstring, out shopid))
            {
                List<string> macList = new List<string>();
                macList.AddRange(db.tblmachinedetails.Where(m => m.CellID == cellid && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList());
                macList.AddRange(db.tblmachinedetails.Where(m => m.ShopID == shopid && m.CellID != cellid && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList());

                //ViewBag.machineData = db.tblmachinedetails.Where(m => m.CellID == cellid && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList();
                //ViewBag.machineData += db.tblmachinedetails.Where(m => m.ShopID == shopid &&  m.CellID != cellid  && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList();
                ViewBag.machineData = macList;
            }
            else
            {
                if (int.TryParse(shopidstring, out shopid))
                {
                    //ViewBag.machineData = db.tblmachinedetails.Where(m => m.ShopID == shopid && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList();
                    ViewBag.machineData = from row in db.tblmachinedetails
                                          where row.ShopID == shopid && row.IsDeleted == 0 && row.CellID.Equals(null)
                                          select row.MachineInvNo;
                }
                else
                {
                    string plantidstring = Convert.ToString(oneMacData.PlantID);
                    int plantid;
                    if (int.TryParse(plantidstring, out plantid))
                    {
                        //ViewBag.machineData = db.tblmachinedetails.Where(m => m.PlantID == plantid && m.IsDeleted == 0).Select(m => m.MachineInvNo).ToList();
                        ViewBag.machineData = from row in db.tblmachinedetails
                                              where row.PlantID == plantid && row.IsDeleted == 0 && row.ShopID.Equals(null) && row.CellID.Equals(null)
                                              select row.MachineInvNo;
                    }
                }
            }
            string machineInvNo = null;
            if (MacInvNo == null)
            {
                var machinedata = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == machineId).FirstOrDefault();
                Session["macDispName"] = Convert.ToString(machinedata.MachineDispName);
                machineInvNo = machinedata.MachineInvNo;
            }
            else
            {
                machineInvNo = MacInvNo;
            }

            ////ViewBag.MacInvNo = machineInvNo;
            //String Query = "select * " +
            //                "from tblddl WHERE WorkCenter = '" + machineInvNo + "' AND IsCompleted = 0 " +
            //                "order by DaysAgeing = '', Convert(DaysAgeing , SIGNED INTEGER) asc ,FlagRushInd = '',FlagRushInd = 0 ,Convert(FlagRushInd , SIGNED INTEGER) asc  , MADDateInd = '' , MADDateInd asc , MADDate asc";
            ////"order by DaysAgeing = \"\", DaysAgeing asc ,FlagRushInd = \"\",FlagRushInd = 0 ,FlagRushInd asc  , MADDateInd = \"\" , MADDateInd asc , MADDate asc";
            //var data = db.tblddls.SqlQuery(Query).ToList();

            ViewBag.MacInvNo = machineInvNo;
            //New Logic 2017-01-07
            List<tblddl> ddlDataList = new List<tblddl>();
            int macID = Convert.ToInt32(Session["MachineID"]);
            string WIPQuery = @"SELECT * from tbllivehmiscreen where isWorkInProgress = 0 and HMIID IN ( SELECT HMIID from tbllivehmiscreen where MachineID = " + macID + " group by Work_Order_No,PartNo,OperationNo order by HMIID desc ) ";
            var WIP = db.tbllivehmiscreens.SqlQuery(WIPQuery).ToList();

            foreach (var row in WIP)
            {
                string wono = row.Work_Order_No;
                string partno = row.PartNo;
                string opno = row.OperationNo;

                var ddldata = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno && m.OperationNo == opno && m.IsCompleted == 0).FirstOrDefault();
                if (ddldata != null)
                {
                    ddlDataList.Add(ddldata);
                }
            }
            if (ddlDataList.Count != 0)
            {
                return View(ddlDataList.ToList());
            }
            else
            {
                return View();
            }
        }
        [HttpPost]
        public ActionResult PartialFinishedList(List<int> data)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            int WOCount = data.Count;
            int machineId = Convert.ToInt32(Session["MachineID"]);
            if (WOCount == 1)
            {
                int DDLID = data.First();

                int Hmiid = 0;
                var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                //String SplitWO = ddldata.SplitWO;
                String WONo = ddldata.WorkOrder;
                String Part = ddldata.MaterialDesc;
                String Operation = ddldata.OperationNo;

                int PrvProcessQty = 0, PrvDeliveredQty = 0, TotalProcessQty = 0;
                var getProcessQty = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.HMIID).Take(1).ToList();
                if (getProcessQty.Count > 0)
                {
                    string delivString = Convert.ToString(getProcessQty[0].Delivered_Qty);
                    int.TryParse(delivString, out PrvDeliveredQty);

                    string processString = Convert.ToString(getProcessQty[0].ProcessQty);
                    int.TryParse(processString, out PrvProcessQty);

                    TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);
                }

                var hmidata = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();
                //hmidata.Date = DateTime.Now;
                Hmiid = hmidata.HMIID;

                //delete if any IsSubmit = 0 for this hmiid.
                db.tbllivemultiwoselections.RemoveRange(db.tbllivemultiwoselections.Where(x => x.HMIID == Hmiid && x.IsSubmit == 0));
                db.SaveChanges();

                //hmidata.PEStartTime = DateTime.Now;
                hmidata.OperationNo = ddldata.OperationNo;
                hmidata.PartNo = ddldata.MaterialDesc;
                hmidata.Project = ddldata.Project;
                hmidata.Target_Qty = Convert.ToInt32(ddldata.TargetQty);
                hmidata.Work_Order_No = ddldata.WorkOrder;
                hmidata.ProcessQty = TotalProcessQty;
                hmidata.IsMultiWO = 0;
                Hmiid = hmidata.HMIID;
                db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                Session["FromDDL"] = 1;
                return RedirectToAction("Index", Hmiid);
            }
            else if (WOCount > 1)
            {
                int TotalTargetQty = 0;
                int TotalProcessQty = 0;
                var hmidata = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();
                int Hmiid = hmidata.HMIID;
                int i = 0;
                String MainOpearationNo = null;
                String MainWorkOrder = null;
                String MainPartNo = null;
                String MainProject = null;

                //delete if any IsSubmit = 0 for this hmiid.
                db.tbllivemultiwoselections.RemoveRange(db.tbllivemultiwoselections.Where(x => x.HMIID == Hmiid && x.IsSubmit == 0));
                db.SaveChanges();

                string ddlWorkCenter = null;
                foreach (int DDLID in data)
                {
                    int PrvProcessQty = 0, PrvDeliveredQty = 0;

                    var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    //String SplitWO = ddldata.SplitWO;
                    //int SplitWOInt = SplitWO.StartsWith("y", StringComparison.CurrentCultureIgnoreCase) == true ? 1 : 0;
                    String WONo = ddldata.WorkOrder;
                    String Part = ddldata.MaterialDesc;
                    String Operation = ddldata.OperationNo;
                    ddlWorkCenter = ddldata.WorkCenter;
                    int TargetQty = Convert.ToInt32(ddldata.TargetQty);
                    TotalTargetQty += TargetQty;
                    if (i == 0)
                    {
                        MainOpearationNo = Operation;
                        MainWorkOrder = WONo;
                        MainPartNo = Part;
                        MainProject = ddldata.Project;
                    }

                    ////var getMultiWoPorcessQty = db.tbl_multiwoselection.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation).OrderByDescending(m => m.MultiWOID).FirstOrDefault();
                    ////int ProcessQty = Convert.ToInt32(getMultiWoPorcessQty.ProcessQty + getMultiWoPorcessQty.DeliveredQty);

                    #region OLD
                    //int ProcessQty = 0;
                    //var getMultiWoPorcessQty = db.tbl_multiwoselection.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation).OrderByDescending(m => m.MultiWOID).Take(1).ToList();
                    //if (getMultiWoPorcessQty.Count > 0)
                    //{
                    //    string delivString = Convert.ToString(getMultiWoPorcessQty[0].DeliveredQty);
                    //    int delivInt = 0;
                    //    int.TryParse(delivString, out delivInt);

                    //    string processString = Convert.ToString(getMultiWoPorcessQty[0].ProcessQty);
                    //    int procInt = 0;
                    //    int.TryParse(processString, out procInt);
                    //    ProcessQty = Convert.ToInt32(procInt + delivInt);
                    //}
                    #endregion

                    #region new code

                    //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                    int isHMIFirst = 2; //default NO History for that wo,pn,on

                    var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                    var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();

                    if (hmiData.Count > 0 && mulitwoData.Count > 0) // now check for greatest amongst
                    {
                        //DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].CreatedOn); //2017-06-02
                        //Based on hmiid of  multiwotable get  Time Column of tblhmiscreen 
                        //int localhmiid = Convert.ToInt32(mulitwoData[0].HMIID);
                        //var hmiiData = db.tblhmiscreens.Find(localhmiid);
                        DateTime multiwoDateTime = Convert.ToDateTime(mulitwoData[0].tbllivehmiscreen.Time);
                        DateTime hmiDateTime = Convert.ToDateTime(hmiData[0].Time);

                        if (Convert.ToInt32(multiwoDateTime.Subtract(hmiDateTime).TotalSeconds) > 0)
                        {
                            isHMIFirst = 1;
                        }
                        else
                        {
                            isHMIFirst = 0;
                        }

                    }
                    else if (mulitwoData.Count > 0)
                    {
                        isHMIFirst = 1;
                    }
                    else if (hmiData.Count > 0)
                    {
                        isHMIFirst = 0;
                    }

                    if (isHMIFirst == 1)
                    {
                        string delivString = Convert.ToString(mulitwoData[0].DeliveredQty);
                        int delivInt = 0;
                        int.TryParse(delivString, out delivInt);

                        string processString = Convert.ToString(mulitwoData[0].ProcessQty);
                        int procInt = 0;
                        int.TryParse(processString, out procInt);

                        PrvProcessQty += procInt;
                        PrvDeliveredQty += delivInt;
                    }
                    else if (isHMIFirst == 0)
                    {
                        string delivString = Convert.ToString(hmiData[0].Delivered_Qty);
                        int delivInt = 0;
                        int.TryParse(delivString, out delivInt);

                        string processString = Convert.ToString(hmiData[0].ProcessQty);
                        int procInt = 0;
                        int.TryParse(processString, out procInt);

                        PrvProcessQty += procInt;
                        PrvDeliveredQty += delivInt;
                    }
                    else
                    {
                        //no previous delivered or processed qty so Do Nothing.
                    }

                    #endregion

                    int ProcessQty = PrvProcessQty + PrvDeliveredQty;

                    TotalProcessQty += ProcessQty;
                    tbllivemultiwoselection MultiWORow = new tbllivemultiwoselection();
                    MultiWORow.DDLWorkCentre = ddldata.WorkCenter;
                    MultiWORow.OperationNo = Operation;
                    MultiWORow.PartNo = Part;
                    MultiWORow.SplitWO = "0";
                    MultiWORow.TargetQty = TargetQty;
                    MultiWORow.WorkOrder = WONo;
                    MultiWORow.ProcessQty = ProcessQty;
                    MultiWORow.HMIID = Hmiid;
                    MultiWORow.IsCompleted = 0;
                    MultiWORow.CreatedOn = System.DateTime.Now;
                    db.tbllivemultiwoselections.Add(MultiWORow);
                    db.SaveChanges();
                }

                //hmidata.Date = DateTime.Now;
                //hmidata.PEStartTime = DateTime.Now;
                hmidata.OperationNo = MainOpearationNo + " - " + WOCount;
                hmidata.PartNo = MainPartNo + " - " + WOCount;
                hmidata.Project = MainProject;
                hmidata.Target_Qty = TotalTargetQty;
                hmidata.Work_Order_No = MainWorkOrder;
                hmidata.ProcessQty = TotalProcessQty;
                Hmiid = hmidata.HMIID;
                hmidata.DDLWokrCentre = ddlWorkCenter;
                hmidata.IsMultiWO = 1;
                db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                Session["FromDDL"] = 4;
                return RedirectToAction("Index", Hmiid);
            }
            return RedirectToAction("Index");
        }

        public bool ContainsChar(string s)
        {
            bool retVal = false;
            char[] a = s.ToCharArray();
            foreach (char alpha in a)
            {
                if (Convert.ToInt32(alpha) >= 48 && Convert.ToInt32(alpha) <= 57)
                {
                }
                else
                {
                    retVal = true;
                    break;
                }
            }

            //this also works
            bool result = !s.Any(x => char.IsLetter(x));

            return retVal;
        }

        //public JsonResult PopulateWODetails(int hmiid, string WOData)
        //{
        //    string retStatus = "false";

        //    int MachineID = Convert.ToInt32(Session["MachineID"]);
        //    var hmiData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
        //    string OpNo = null, WONo = null, SorM = null;

        //    if (ContainsChar(WOData))
        //    {
        //        OpNo = WOData.Substring(WOData.Length - 5 + 2, 2); //x-length of WONo , 4-length of OpNo (2-Zeros in Opno) , 1-length of SorM.
        //        int opNoInt = Convert.ToInt32(OpNo);
        //        OpNo = Convert.ToString(opNoInt);
        //        WONo = WOData.Substring(0, WOData.Length - 5); //4-length of opno and 1-length of SorM
        //        Int64 WONoInt = Convert.ToInt64(WONo);
        //        WONo = Convert.ToString(WONoInt);
        //    }
        //    else
        //    {
        //        OpNo = WOData.Substring(WOData.Length - 4 + 2, 2); //x-length of WONo , 4-length of OpNo (2-Zeros in Opno)
        //        int opNoInt = Convert.ToInt32(OpNo);
        //        OpNo = Convert.ToString(opNoInt);
        //        WONo = WOData.Substring(0, WOData.Length - 4); //4-length of opno 
        //        Int64 WONoInt = Convert.ToInt64(WONo);
        //        WONo = Convert.ToString(WONoInt);
        //    }

        //    if(OpNo != null && WONo != null)
        //    {
        //        var ddlData = db.tblddls.Where(m => m.WorkOrder == WONo && m.OperationNo == OpNo && m.IsCompleted == 0).FirstOrDefault();
        //        if (ddlData != null)
        //        {
        //            hmiData.DDLWokrCentre = ddlData.WorkCenter;
        //            hmiData.DoneWithRow = 0;
        //            hmiData.IsHold = 0;
        //            hmiData.IsMultiWO = 0;
        //            hmiData.isUpdate = 0;
        //            hmiData.isWorkInProgress = 2;
        //            hmiData.isWorkOrder = 0;
        //            hmiData.Target_Qty = Convert.ToInt32(ddlData.TargetQty);
        //            hmiData.Prod_FAI = ddlData.Type;
        //            hmiData.MachineID = MachineID;
        //            hmiData.OperationNo = OpNo;
        //            hmiData.PartNo = ddlData.MaterialDesc;
        //            hmiData.ProcessQty = 0;
        //            hmiData.Prod_FAI = ddlData.Type;
        //            hmiData.Project = ddlData.Project;
        //            hmiData.Status = 0;
        //            hmiData.Work_Order_No = WONo;
        //            hmiData.PEStartTime = DateTime.Now;

        //            db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
        //            db.SaveChanges();

        //            retStatus = "true";
        //        }
        //        else
        //        {
        //            hmiData.PartNo = null;
        //            hmiData.OperationNo = OpNo;
        //            hmiData.Work_Order_No = WONo;
        //            db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
        //            db.SaveChanges();

        //            retStatus = "true";
        //        }

        //    }
        //    return Json(retStatus,JsonRequestBehavior.AllowGet);
        //}

        public JsonResult PopulateWODetails(int hmiid, string WOData)
        {
            string retStatus = "false";

            int MachineID = Convert.ToInt32(Session["MachineID"]);
            var hmiData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
            string OpNo = null, WONo = null, SorM = null;

            bool isValidWOData = false;
            var regexItem = new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9]*$");
            if (regexItem.IsMatch(WOData))
            {
                isValidWOData = true;
            }

            if (WOData.Length > 6 && isValidWOData)
            {
                if (ContainsChar(WOData))
                {
                    OpNo = WOData.Substring(WOData.Length - 5, 4); //x-length of WONo , 4-length of OpNo (2-Zeros in Opno) , 1-length of SorM.
                    int opNoInt = Convert.ToInt32(OpNo);
                    OpNo = Convert.ToString(opNoInt);
                    WONo = WOData.Substring(0, WOData.Length - 5); //4-length of opno and 1-length of SorM
                    Int64 WONoInt = Convert.ToInt64(WONo);
                    WONo = Convert.ToString(WONoInt);
                }
                else
                {
                    OpNo = WOData.Substring(WOData.Length - 4, 4); //x-length of WONo , 4-length of OpNo (2-Zeros in Opno)
                    int opNoInt = Convert.ToInt32(OpNo);
                    OpNo = Convert.ToString(opNoInt);
                    WONo = WOData.Substring(0, WOData.Length - 4); //4-length of opno 
                    Int64 WONoInt = Convert.ToInt64(WONo);
                    WONo = Convert.ToString(WONoInt);
                }
            }
            if (OpNo != null && WONo != null)
            {
                //Check if it's in screen
                bool isDuplicate = false;
                var hmiDataDuplicate = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.OperationNo == OpNo && m.MachineID == MachineID).OrderByDescending(m => m.PEStartTime).FirstOrDefault();
                if (hmiDataDuplicate != null)
                {
                    if (hmiDataDuplicate.isWorkInProgress == 2)
                    {
                        isDuplicate = true;
                        retStatus = "Duplicate WorkOrder. WONo.= " + WONo + " , OperationNo. = " + OpNo;
                    }
                }
                if (!isDuplicate)
                {
                    //Check if its in Hold in HMIScreen
                    int isHold = 0;
                    var hmiDataDup = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.OperationNo == OpNo).OrderByDescending(m => m.PEStartTime).FirstOrDefault();
                    if (hmiDataDup != null)
                    {
                        isHold = hmiDataDup.IsHold;
                    }
                    var ddlData = db.tblddls.Where(m => m.WorkOrder == WONo && m.OperationNo == OpNo && m.IsCompleted == 0).FirstOrDefault();
                    if (ddlData != null)
                    {
                        string ddlWorkCenter = Convert.ToString(ddlData.WorkCenter);
                        if (ddlWorkCenter != null)
                        {
                            var ddlWorkCenterMacDetails = db.tblmachinedetails.Where(m => m.MachineInvNo == ddlWorkCenter && m.IsDeleted == 0).FirstOrDefault();
                            if (ddlWorkCenterMacDetails != null)
                            {
                                var thisMacDetails = db.tblmachinedetails.Where(m => m.MachineID == MachineID && m.IsDeleted == 0).FirstOrDefault();
                                if (ddlWorkCenterMacDetails.ShopID == thisMacDetails.ShopID)
                                {
                                    hmiData.DDLWokrCentre = ddlData.WorkCenter;
                                    hmiData.DoneWithRow = 0;
                                    hmiData.IsHold = isHold;
                                    hmiData.IsMultiWO = 0;
                                    hmiData.isUpdate = 0;
                                    hmiData.isWorkInProgress = 2;
                                    hmiData.isWorkOrder = 0;
                                    hmiData.Target_Qty = Convert.ToInt32(ddlData.TargetQty);
                                    hmiData.Prod_FAI = ddlData.Type;
                                    hmiData.MachineID = MachineID;
                                    hmiData.OperationNo = OpNo;
                                    hmiData.PartNo = ddlData.MaterialDesc;
                                    hmiData.ProcessQty = 0;
                                    hmiData.Prod_FAI = ddlData.Type;
                                    hmiData.Project = ddlData.Project;
                                    hmiData.Status = 0;
                                    hmiData.Work_Order_No = WONo;
                                    //hmiData.PEStartTime = DateTime.Now;

                                    db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
                                    db.SaveChanges();

                                    retStatus = "true";
                                }
                                else
                                {
                                    retStatus = "This WorkOrder doesnot belong to this Shop";
                                }
                            }

                        }
                    }
                    else
                    {
                        var thisHMIDetails = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.OperationNo == OpNo).OrderByDescending(m => m.PEStartTime).FirstOrDefault();
                        var DDLFinished = db.tblddls.Where(m => m.WorkOrder == WONo && m.OperationNo == OpNo && m.IsCompleted == 1).FirstOrDefault();
                        if (DDLFinished == null)
                        {
                            if (thisHMIDetails != null)
                            {
                                hmiData.DDLWokrCentre = thisHMIDetails.DDLWokrCentre;
                                hmiData.DoneWithRow = 0;
                                hmiData.IsHold = isHold;
                                hmiData.IsMultiWO = 0;
                                hmiData.isUpdate = 0;
                                hmiData.isWorkInProgress = 2;
                                hmiData.isWorkOrder = 0;
                                hmiData.Target_Qty = Convert.ToInt32(thisHMIDetails.Target_Qty);
                                hmiData.Prod_FAI = thisHMIDetails.Prod_FAI;
                                hmiData.MachineID = MachineID;
                                hmiData.OperationNo = OpNo;
                                hmiData.PartNo = thisHMIDetails.PartNo;
                                hmiData.ProcessQty = 0;
                                hmiData.Project = thisHMIDetails.Project;
                                hmiData.Status = 0;
                                hmiData.Work_Order_No = WONo;
                                //hmiData.PEStartTime = DateTime.Now;

                                db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
                                db.SaveChanges();

                                retStatus = "true";
                            }
                            else
                            {
                                hmiData.PartNo = null;
                                hmiData.OperationNo = OpNo;
                                hmiData.Work_Order_No = WONo;
                                db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
                                db.SaveChanges();
                                retStatus = "false";
                            }
                        }
                        else
                        {
                            retStatus = "This WorkOrder is Finished. WoNo: " + WONo + " OpNo.: " + OpNo;
                        }
                    }
                }
            }
            else
            {
                retStatus = "WorkOrder is Not Appropriate.";
            }
            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        List<string> GetHierarchyData(int MachineID)
        {
            List<string> HierarchyData = new List<string>();
            //1st get PlantName or -
            //2nd get ShopName or -
            //3rd get CellName or -
            //4th get MachineName.

            using (mazakdaqEntities dbMac = new mazakdaqEntities())
            {
                var machineData = dbMac.tblmachinedetails.Where(m => m.MachineID == MachineID).FirstOrDefault();
                int PlantID = Convert.ToInt32(machineData.PlantID);
                string name = "-";
                name = dbMac.tblplants.Where(m => m.PlantID == PlantID).Select(m => m.PlantName).FirstOrDefault();
                HierarchyData.Add(name);

                string ShopIDString = Convert.ToString(machineData.ShopID);
                int value;
                if (int.TryParse(ShopIDString, out value))
                {
                    name = dbMac.tblshops.Where(m => m.ShopID == value).Select(m => m.ShopName).FirstOrDefault();
                    HierarchyData.Add(name + "-".ToString());
                }
                //else
                //{
                //    HierarchyData.Add("-");
                //}

                string CellIDString = Convert.ToString(machineData.CellID);
                if (int.TryParse(CellIDString, out value))
                {
                    name = dbMac.tblcells.Where(m => m.CellID == value).Select(m => m.CellName).FirstOrDefault();
                    HierarchyData.Add(name + "-".ToString());
                }
                //else
                //{
                //    HierarchyData.Addy
                //}
                HierarchyData.Add(Convert.ToString(machineData.MachineInvNo));
                HierarchyData.Add(Convert.ToString(machineData.MachineDispName));
            }
            return HierarchyData;
        }

    }
}
