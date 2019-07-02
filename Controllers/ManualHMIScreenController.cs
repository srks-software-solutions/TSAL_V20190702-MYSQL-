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
using System.Web.UI;
using System.Web.SessionState;
using Tata.App_Start;

namespace Tata.Controllers
{
    //public class HoldCodesList
    //{
    //    public int HoldCodeID { get; set; }
    //    public string RL1 { get; set; }
    //    public string RL2 { get; set; }
    //    public string RL3 { get; set; }
    //    public int IsWOLevel { get; set; }
    //}

    public class ManualHMIScreenController : Controller
    {
        //ManualHMIScreen  //2016-12-22 
        mazakdaqEntities db = new mazakdaqEntities();

        public ActionResult Index(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            var alsdkfj = Session["isWorkOrder"];

            //Session["FromDDL"] = 0;
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int opid = Convert.ToInt32(Session["UserId"]);
            int ShiftID = 0;

            //Testing Login Status
            var aj = User.Identity.IsAuthenticated;
            var jb = Request.IsAuthenticated;
            bool val1 = System.Web.HttpContext.Current.User.Identity.IsAuthenticated‌;
            var c = User.Identity.Name;
            var d = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            var e = System.Web.HttpContext.Current.User;

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

            //insert a new row if there is no row for this machine for this date.
            // tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).Where(m => m.MachineID == Machinid && m.Status == 0).FirstOrDefault();
            //tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.Status == 0).OrderByDescending(m => m.HMIID).FirstOrDefault();

            var HMI = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.Status == 0).OrderByDescending(m => m.HMIID).ToList();
            if (HMI.Count == 0)
            {
                var HMIOpName = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.Status == 0 && m.isUpdate == 1).OrderByDescending(m => m.HMIID).FirstOrDefault();
                if (HMIOpName != null)
                {
                    Session["OpName"] = HMIOpName.OperatorDet;
                }

                tbllivehmiscreen tblhmiscreen = new tbllivehmiscreen();
                
                tblhmiscreen.MachineID = Machinid;
                tblhmiscreen.CorrectedDate = CorrectedDate;
                tblhmiscreen.PEStartTime = DateTime.Now;
                tblhmiscreen.OperatorDet = Convert.ToString(Session["OpName"]);
                tblhmiscreen.Shift = Convert.ToString(ViewBag.shift);
                tblhmiscreen.Status = 0;
               // tblhmiscreen.HMIID = ; //by Ashok
                int ishideInt = 0;
                int.TryParse(Convert.ToString(Session["isHide"]), out ishideInt);
                if (ishideInt == 1)
                {
                    tblhmiscreen.isUpdate = 1;
                }
                tblhmiscreen.isWorkInProgress = 2;
                //tblhmiscreen.isWorkOrder = Convert.ToInt32(Session["isWorkOrder"]);
                tblhmiscreen.OperatiorID = Convert.ToInt32(Session["UserId"]);

                db.tbllivehmiscreens.Add(tblhmiscreen);
                db.SaveChanges();

                Session["FromDDL"] = 0;
            }
            else
            {
                //If there is no nonsubmited row then insert new
                var HMIOpName = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.Status == 0 && m.isUpdate == 1).OrderByDescending(m => m.HMIID).FirstOrDefault();
                if (HMIOpName != null)
                {
                    Session["OpName"] = HMIOpName.OperatorDet;
                    //Shift will be stale one, dont take it from here.
                }

                bool isEmptyAvailable = false;
                foreach (var row in HMI)
                {
                    //if (row.isUpdate == 0 && row.Date == null)
                    if (row.Date == null)
                    {
                        isEmptyAvailable = true;
                        break;
                    }
                }

                if (!isEmptyAvailable)
                {
                    tbllivehmiscreen tblhmiscreen = new tbllivehmiscreen();
                    
                    tblhmiscreen.MachineID = Machinid;
                    tblhmiscreen.CorrectedDate = CorrectedDate;
                    tblhmiscreen.PEStartTime = DateTime.Now;
                    tblhmiscreen.OperatorDet = Convert.ToString(Session["OpName"]);
                    tblhmiscreen.Shift = Convert.ToString(ViewBag.shift);
                    tblhmiscreen.Status = 0;
                   // tblhmiscreen.HMIID = (HMMID.HMIID+1);
                    int ishideInt = 0;
                    int.TryParse(Convert.ToString(Session["isHide"]), out ishideInt);
                    if (ishideInt == 1)
                    {
                        tblhmiscreen.isUpdate = 1;
                    }

                    tblhmiscreen.isWorkInProgress = 2;
                    //tblhmiscreen.isWorkOrder = Convert.ToInt32(Session["isWorkOrder"]);
                    tblhmiscreen.OperatiorID = Convert.ToInt32(Session["UserId"]);

                    db.tbllivehmiscreens.Add(tblhmiscreen);
                    db.SaveChanges();
                }

                //Below code may be required based on how we handle WO's 2016-12-28
                foreach (var row in HMI)
                {
                    if (row.CorrectedDate != CorrectedDate && row.Date == null)
                    {
                        row.Shift = Convert.ToString(ViewBag.shift);
                        row.CorrectedDate = CorrectedDate;
                        row.PEStartTime = DateTime.Now;
                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            ViewBag.DATE = DateTime.Now.Date;
            List<SelectListItem> Prod_FAI = new List<SelectListItem>();

            //Prod_FAI.Add(new SelectListItem
            //{
            //    Text = "FAI",
            //    Value = "FAI",
            //});
            //Prod_FAI.Add(new SelectListItem
            //{
            //    Text = "PRR",
            //    Value = "PRR"
            //});
            //Prod_FAI.Add(new SelectListItem
            //{
            //    Text = "Prod",
            //    Value = "Prod",
            //});
            //Prod_FAI.Add(new SelectListItem
            //{
            //    Text = "Trial",
            //    Value = "Trial"
            //});
            //Prod_FAI.Add(new SelectListItem
            //{
            //    Text = "RM Sizeing",
            //    Value = "RM Sizeing",
            //});

            Prod_FAI.Add(new SelectListItem
            {
                Text = "Production",
                Value = "Production",
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "FAI",
                Value = "FAI",
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "DeltaFAI",
                Value = "DeltaFAI",
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "ProtoType",
                Value = "ProtoType",
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "TASLRequirement",
                Value = "TASLRequirement",
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "InWork",
                Value = "InWork",
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "ReleasedLev1",
                Value = "ReleasedLev1",
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "ProtoTypeLev1",
                Value = "ProtoTypeLev1"
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "Trial",
                Value = "Trial"
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "RM Sizeing",
                Value = "RM Sizeing",
            });
            Prod_FAI.Add(new SelectListItem
            {
                Text = "PRR",
                Value = "PRR"
            });

            ViewBag.Prod_FAI = Prod_FAI;
            //this is needed ...... like (now) no never.
            //int handleidleReturnValue = HandleIdleManualWC();
            //if (handleidleReturnValue == 0)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //}

            ViewBag.machineID = Convert.ToInt32(Session["MachineID"]);
            int macid = Convert.ToInt32(Session["MachineID"]);
            ViewBag.MacDispName = db.tblmachinedetails.Where(m => m.MachineID == macid).Select(m => m.MachineDispName).FirstOrDefault();

            tbluser tbl = db.tblusers.Find(opid);
            ViewBag.operatordisplay = tbl.DisplayName;

            if (Session["VError"] != null)
            {

            }
            if (TempData["Err"] != null)
            {

            }

            //Code to resume particular workOrder . Probabily ...... Never 
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

            #region . if data not selected.

            //var data = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status != 2).Where(m => m.CorrectedDate == CorrectedDate).Where(m => m.Shift == gshift).ToList();

            //I am removing the CorrectedDate from condition so 2016-12-30 becarefull about what it retrieves(stale data)
            var data = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status == 0).OrderByDescending(m => m.HMIID).ToList();
            if (data.Count > 0)
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
                        if (data[0].Date == null) //Before Submit
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
                        if (data[0].Date == null) //Before Submit
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
                        if (data[0].Date == null) //Before Submit
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
                        if (data[0].Date == null) //Before Submit
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
                    #endregion

                }
                else //After Auto Logout or Session out.
                {
                    #region
                    if (data[0].Date == null) //Before Submit
                    {
                        if (data[0].IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                        {
                            Session["FromDDL"] = 4;
                        }
                        else //Its a single WO
                        {
                            string P = data[0].Project;
                            string wo = data[0].Work_Order_No;
                            string pno = data[0].PartNo;
                            string opno = data[0].OperationNo;

                            if (data[0].DDLWokrCentre != null) //Its a single WO from DDL
                            {
                                Session["FromDDL"] = 1;
                                Session["SubmitClicked"] = 0;
                            }
                            else if ((!string.IsNullOrEmpty(data[0].Project)) || (!string.IsNullOrEmpty(data[0].PartNo)) || (!string.IsNullOrEmpty(data[0].OperationNo)) || (!string.IsNullOrEmpty(data[0].Work_Order_No)))
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
                        if (data[0].IsMultiWO == 1) //Its a MultiWO //need "Enter Delivered Qty button"
                        {
                            Session["FromDDL"] = 4;
                            Session["SubmitClicked"] = 1;
                        }
                        else //Its a single WO
                        {
                            Session["FromDDL"] = 1;
                            Session["SubmitClicked"] = 1;
                        }
                    }
                    #endregion

                }
                ViewBag.shift = data[0].Shift;
                Session["realshift"] = data[0].Shift;

                bool isHide = false; //make it true when one of the rows as isupdate = 1
                foreach (var dat in data)
                {
                    if (dat.isUpdate == 1)
                    {
                        isHide = true;
                        Session["isHide"] = 1;
                        break;
                    }
                }
                if (isHide)
                {
                    ViewBag.hide = 1;
                }
                else
                {
                    ViewBag.hide = null;
                }

                string shifthere = data[0].Shift;
                //I am removing the CorrectedDate from condition so 2016-12-30 becarefull about what it retrieves(stale data)
                //var data2 = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status == 0).OrderBy(u => u.Work_Order_No).ThenBy(u=>u.PartNo).ThenBy(u=>u.OperationNo).ToList();
                var data2 = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.OperatiorID == opid).Where(m => m.Status == 0).OrderBy(m => m.Date).ToList();
                ViewBag.ProdFAI = data2[0].Prod_FAI;

                //See if Previously they were doing ReworkOrder
                if (data2.Count == 1)
                {
                    int a1 = 0;
                    if (int.TryParse(Convert.ToString(Session["WorkOrderClicked"]), out a1))
                    {
                        if (a1 == 1) //1st time After reworkorder clicked
                        {
                            int a = 0;
                            if (int.TryParse(Convert.ToString(Session["isWorkOrder"]), out a))
                            {
                                if (a == 1)
                                {
                                    Session["isWorkOrder"] = 1;
                                }
                            }
                            else
                            {
                                Session["isWorkOrder"] = 0;
                            }
                        }
                        else
                        {
                            Session["isWorkOrder"] = 0;
                        }
                    }
                    else //When You login.
                    {
                        Session["isWorkOrder"] = 0;
                    }
                }
                else
                {
                    Session["isWorkOrder"] = 0;
                    foreach (var row in data2)
                    {
                        if (row.isWorkOrder == 1)
                        {
                            Session["isWorkOrder"] = 1;
                            break;
                        }
                    }
                }

                //Loop to get WO's Details Counts
                ViewBag.TotalWOCount = data2.Where(m => m.PartNo != null && m.OperationNo != null && m.Work_Order_No != null).Count();
                ViewBag.WOStartedCount = data2.Where(m => m.Date != null && m.isWorkInProgress != 3).Count();
                //ViewBag.WOOnHoldCount = data2.Where(m => m.IsHold == 1).Count();
                ViewBag.WONotStartedCount = data2.Where(m => m.Date == null && (m.PartNo != null || m.OperationNo != null || m.Work_Order_No != null)).Count();

                return View(data2);
            }
            else
            {
                bool tick = false;
                //var data1 = db.tblhmiscreens.Where(m => m.MachineID == Machinid && m.Status == 0).OrderBy(u => u.Work_Order_No).ThenBy(u=>u.PartNo).ThenBy(u=>u.OperationNo).ToList();
                var data1 = db.tbllivehmiscreens.Where(m => m.MachineID == Machinid && m.Status == 0).OrderBy(m => m.Date).ToList();
                foreach (var a in data1)
                {
                    if (a.isUpdate == 1)
                    { tick = true; }
                }
                if (tick)
                {
                    TempData["saveORUpdate"] = 1;
                }

                bool isHide = false; //make it true when one of the rows as isupdate = 1
                //2017-01-19
                // foreach (var dat in data)
                foreach (var dat in data1)
                {
                    if (dat.isUpdate == 1)
                    {
                        isHide = true;
                        break;
                    }
                }
                if (isHide)
                {
                    ViewBag.hide = 1;
                }
                else
                {
                    ViewBag.hide = null;
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
                        }
                    }
                    #endregion
                }
                ViewBag.ProdFAI = data1[0].Prod_FAI;
                if (data1.Count > 1)
                {
                    int b = 0;
                    if (int.TryParse(Convert.ToString(Session["isWorkOrder"]), out b))
                    {
                        if (b == 1)
                        {
                            Session["isWorkOrder"] = 1;
                        }
                    }
                    else
                    {
                        Session["isWorkOrder"] = 0;
                    }
                }
                else
                {
                    Session["isWorkOrder"] = 0;
                    foreach (var row in data1)
                    {
                        if (row.isWorkOrder == 1)
                        {
                            Session["isWorkOrder"] = 1;
                            break;
                        }
                    }
                }

                //Loop to get WO's Details Counts
                ViewBag.TotalWOCount = data1.Where(m => m.PartNo != null && m.OperationNo != null && m.Work_Order_No != null).Count();
                ViewBag.WOStartedCount = data1.Where(m => m.Date != null && m.isWorkInProgress != 3).Count();
                //ViewBag.WOOnHoldCount = data1.Where(m => m.IsHold == 1).Count();
                ViewBag.WONotStartedCount = data1.Where(m => m.Date == null && (m.PartNo != null || m.OperationNo != null || m.Work_Order_No != null)).Count();

                return View(data1);
            }

            #endregion
        }

        //Control comes here when StartAll button is clicked.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult UpdateData(IEnumerable<tblHMIScreen> tbldaily_plan, int Line1 = 0)
        public ActionResult Index(IList<tbllivehmiscreen> tbldaily_plan)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
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
            int machineID = Convert.ToInt32(Session["MachineID"]);
            ViewBag.DPIsMenu = 0;
            if (tbldaily_plan != null)
            {
                int count = 0;
                int ExceptionHMIID = 0;
                foreach (var plan in tbldaily_plan)
                {
                    //1) Check eligibility to Start (Even if 1 fails reject All)
                    if (plan.IsHold == 1 && plan.Date == null)
                    //if (plan.isWorkInProgress == 3)
                    {
                        Session["VError"] = "End HOLD Before StartAll is Clicked";
                        return RedirectToAction("Index");
                    }
                    else if (plan.OperatorDet != null && plan.Shift != null && plan.Project != null && plan.Prod_FAI != null && plan.PartNo != null && plan.Work_Order_No != null && plan.OperationNo != null && plan.Work_Order_No != null && plan.Target_Qty.HasValue)
                    {
                    }
                    else if (plan.Date == null && plan.Work_Order_No == null && plan.PartNo == null && plan.OperationNo == null)
                    {// Do Nothing. Just to Skip our Extra Empty Row
                        if (ExceptionHMIID == 0)
                        {
                            ExceptionHMIID = Convert.ToInt32(plan.HMIID);
                        }
                        else
                        {
                            Session["VError"] = "Please enter all Details Before StartAll is Clicked.";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        Session["VError"] = "Please enter all Details Before StartAll is Clicked.";
                        return RedirectToAction("Index");
                    }
                }

                //checking for duplicate WorkOrders
                #region
                foreach (var plan in tbldaily_plan)
                {
                    int hmiid = Convert.ToInt32(plan.HMIID);
                    if (ExceptionHMIID != hmiid)
                    {
                        string woNo = Convert.ToString(plan.Work_Order_No);
                        string opNo = Convert.ToString(plan.OperationNo);
                        string partNo = Convert.ToString(plan.PartNo);

                        var InHMIData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.Status == 0 && m.MachineID == machineID && m.HMIID != hmiid).FirstOrDefault();

                        if (InHMIData != null)
                        {
                            Session["Error"] = "Duplicate WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                            //db.tblhmiscreens.Remove(plan);
                            db.Entry(plan).State = System.Data.Entity.EntityState.Deleted;
                            db.SaveChanges();
                            return RedirectToAction("Index");
                        }

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

                    }
                }
                #endregion

                //Order Them Prior to checking them for " Sequence of Start "  Condition 2017-01-09
                tbldaily_plan = tbldaily_plan.OrderBy(m => m.Work_Order_No).ThenBy(m => m.PartNo).ThenBy(m => m.OperationNo).ToList();

                foreach (var plan in tbldaily_plan)
                {
                    int hmiid = plan.HMIID;
                    if (hmiid != ExceptionHMIID)
                    {
                        tbllivehmiscreen hmiidData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                        string WONo = Convert.ToString(hmiidData.Work_Order_No);
                        string PNo = Convert.ToString(hmiidData.PartNo);
                        int Opno = Convert.ToInt32(hmiidData.OperationNo);
                        //var HoldData = db.tblmanuallossofentries.Where(m => m.PartNo == PNo && m.OpNo == Opno && m.WONo == WONo && m.EndDateTime == null).OrderByDescending(m => m.MLossID).FirstOrDefault();
                        //if (HoldData != null)
                        //{
                        //    HoldData.EndDateTime = DateTime.Now;
                        //    db.Entry(HoldData).State = System.Data.Entity.EntityState.Modified;
                        //    db.SaveChanges();
                        //}
                        if (hmiidData.Date == null)
                        {
                            hmiidData.Date = DateTime.Now;
                        }
                        else
                        {
                            continue;
                        }

                        //Get Processed Qty
                        int newProcessedQty = 0;
                        int PrvProcessQty = 0, PrvDeliveredQty = 0;
                        string woNo = Convert.ToString(hmiidData.Work_Order_No);
                        string opNo = Convert.ToString(hmiidData.OperationNo);
                        string partNo = Convert.ToString(hmiidData.PartNo);
                        int OperationNoInt = Convert.ToInt32(opNo);

                        #region 2017-02-07

                        //Logic to check sequence of Submit Based on WONo, PartNo and OpNo.
                        //2017-01-21
                        using (Tata.Models.mazakdaqEntities dbHMI = new Tata.Models.mazakdaqEntities())
                        {
                            string WIPQuery = @"SELECT * from tbllivehmiscreen where  HMIID IN ( SELECT HMIID from tbllivehmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + opNo + "' group by Work_Order_No,PartNo,OperationNo order by HMIID )";
                            var WIP = dbHMI.tbllivehmiscreens.SqlQuery(WIPQuery).ToList();
                            string WIPQueryHistorian = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT HMIID from tbllivehmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + opNo + "' group by Work_Order_No,PartNo,OperationNo order by HMIID )";
                            var WIPHistorian = dbHMI.tblhmiscreens.SqlQuery(WIPQueryHistorian).ToList();
                            foreach (var row in WIP)
                            {
                                int InnerOpNo = Convert.ToInt32(row.OperationNo);
                                if (OperationNoInt > InnerOpNo)
                                {
                                    if (row.Date == null) //=> lower OpNo is not submitted.
                                    {
                                        Session["VError"] = " Submit WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                                        return RedirectToAction("Index");
                                    }
                                }
                            }
                            if(WIPHistorian.Count>0)
                            {
                                foreach (var row in WIPHistorian)
                                {
                                    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                                    if (OperationNoInt > InnerOpNo)
                                    {
                                        if (row.Date == null) //=> lower OpNo is not submitted.
                                        {
                                            Session["VError"] = " Submit WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                                            return RedirectToAction("Index");
                                        }
                                    }
                                }
                            }
                        }

                        //New Logic to Check Sequential Submit 2017-01-21
                        bool IsInHMI = true;
                        //int OperationNoInt = Convert.ToInt32(opNo);
                        string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + woNo + "' and MaterialDesc = '" + partNo + "' and OperationNo != '" + opNo + "'  and IsCompleted = 0 order by WorkOrder,MaterialDesc,OperationNo  ";
                        var WIPDDL = db.tblddls.SqlQuery(WIPQuery1).ToList();
                        foreach (var row in WIPDDL)
                        {
                            IsInHMI = true; //reinitialize
                            int InnerOpNo = Convert.ToInt32(row.OperationNo);
                            if (InnerOpNo < OperationNoInt)
                            {
                                bool IsItWrong = false;
                                string WIPQueryHMI = @"SELECT * from tbllivehmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
                                var WIP = db.tbllivehmiscreens.SqlQuery(WIPQueryHMI).ToList();
                                string WIPQueryHMIHistorian = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
                                var WIPHistorian = db.tblhmiscreens.SqlQuery(WIPQueryHMIHistorian).ToList();
                                if (WIP.Count == 0 || WIPHistorian.Count==0)
                                {
                                    //Session["VError"] = " Select & Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                    //return RedirectToAction("Index");
                                    IsInHMI = false;
                                }
                                else
                                {
                                    foreach (var rowHMI in WIP)
                                    {
                                        if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                                        {
                                            Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;

                                            //If its in current list then its not error.
                                            bool inCurrentList = false;
                                            foreach (var planrow in tbldaily_plan)
                                            {
                                                if (InnerOpNo == Convert.ToInt32(planrow.OperationNo))
                                                {
                                                    inCurrentList = true;
                                                    break;
                                                }
                                            }
                                            if (!inCurrentList)
                                            {
                                                return RedirectToAction("Index");
                                            }
                                        }
                                    }
                                    if(WIPHistorian.Count>0)
                                    {
                                        foreach (var rowHMI in WIPHistorian)
                                        {
                                            if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;

                                                //If its in current list then its not error.
                                                bool inCurrentList = false;
                                                foreach (var planrow in tbldaily_plan)
                                                {
                                                    if (InnerOpNo == Convert.ToInt32(planrow.OperationNo))
                                                    {
                                                        inCurrentList = true;
                                                        break;
                                                    }
                                                }
                                                if (!inCurrentList)
                                                {
                                                    return RedirectToAction("Index");
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!IsInHMI)
                                {
                                    //Strange , it might have been started in Normal WorkCenter as MultiWorkOrder.
                                    #region //also check in MultiWO table
                                    string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by MultiWOID limit 1 ";
                                    var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();
                                    string WIPQueryMultiWOHistorian = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by MultiWOID limit 1 ";
                                    var WIPMWOHistorian = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWOHistorian).ToList();
                                    if (WIPMWO.Count == 0 || WIPMWOHistorian.Count==0)
                                    {
                                        Session["VError"] = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                        return RedirectToAction("Index");
                                        //IsInHMI = false;
                                        //break;
                                    }

                                    foreach (var rowHMI in WIPMWO)
                                    {
                                        int hmiid1 = Convert.ToInt32(rowHMI.HMIID);
                                        var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid1).FirstOrDefault();
                                        if (MWOHMIData != null) //obviously != 0
                                        {
                                            if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                                return RedirectToAction("Index");
                                                //break;
                                            }
                                            else
                                            {
                                                Session["VError"] = null;
                                                IsInHMI = true;
                                            }
                                        }
                                    }
                                    foreach (var rowHMI in WIPMWOHistorian)
                                    {
                                        int hmiid1 = Convert.ToInt32(rowHMI.HMIID);
                                        var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid1).FirstOrDefault();
                                        if (MWOHMIData != null) //obviously != 0
                                        {
                                            if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                                return RedirectToAction("Index");
                                                //break;
                                            }
                                            else
                                            {
                                                Session["VError"] = null;
                                                IsInHMI = true;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                        }

                        //string WIPQuery1 = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' order by Work_Order_No,PartNo,OperationNo";

                        //Commented on 2017-05-29
                        /////to Catch those Manual WorkOrders 
                        //string WIPQuery2 = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' and  IsMultiWO = 0 and DDLWokrCentre is null order by HMIID ) group by Work_Order_No,PartNo,OperationNo ) order by OperationNo ;";
                        //var WIPDDL1 = db.tblhmiscreens.SqlQuery(WIPQuery2).ToList();
                        //foreach (var row in WIPDDL1)
                        //{
                        //    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                        //    if (InnerOpNo < OperationNoInt)
                        //    {
                        //        string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
                        //        var WIP = db.tblhmiscreens.SqlQuery(WIPQueryHMI).ToList();
                        //        if (WIP.Count == 0)
                        //        {
                        //            Session["VError"] = " Select & Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                        //            return RedirectToAction("Index");
                        //        }
                        //        foreach (var rowHMI in WIP)
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

                        int deliveredQty = 0;
                        int TargetQtyNew = Convert.ToInt32(hmiidData.Target_Qty);
                        int DeliveredNew = Convert.ToInt32(hmiidData.Delivered_Qty);
                        int ProcessedNew = Convert.ToInt32(hmiidData.ProcessQty);
                        newProcessedQty = DeliveredNew + ProcessedNew;
                        if (Convert.ToInt32(hmiidData.isWorkInProgress) == 1)
                        {
                            Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                            db.tbllivehmiscreens.Remove(hmiidData);
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

                        #region NOt Using 2017-06-03
                        //var getProcessQty = db.tblhmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(2).ToList();
                        //if (getProcessQty.Count == 2)
                        //{
                        //    string delivString = Convert.ToString(getProcessQty[1].Delivered_Qty);
                        //    int.TryParse(delivString, out PrvDeliveredQty);

                        //    string processString = Convert.ToString(getProcessQty[1].ProcessQty);
                        //    int.TryParse(processString, out PrvProcessQty);

                        //    newProcessedQty = PrvProcessQty + PrvDeliveredQty;
                        //    if (Convert.ToInt32(getProcessQty[1].isWorkInProgress) == 1 || TargetQtyNew == newProcessedQty)
                        //    {
                        //        Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;

                        //        //2017-01-07
                        //        //hmiidData.Prod_FAI = null;
                        //        //hmiidData.Target_Qty = null;
                        //        //hmiidData.OperationNo = null;
                        //        //hmiidData.PartNo = null;
                        //        //hmiidData.Work_Order_No = null;
                        //        //hmiidData.Project = null;
                        //        //hmiidData.Date = null;
                        //        //hmiidData.DDLWokrCentre = null;
                        //        //hmiidData.isWorkOrder = 0;
                        //        //hmiidData.ProcessQty = 0;
                        //        //Session["FromDDL"] = 2;
                        //        //TempData["ForDDL2"] = 2;

                        //        //db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                        //        //db.SaveChanges();

                        //        db.tblhmiscreens.Remove(hmiidData);
                        //        db.SaveChanges();
                        //        return RedirectToAction("Index");
                        //    }
                        //}
                        #endregion

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

                        //////////////////////////////////hmiidData.ProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);

                        int ReworkOrder = 0;
                        string ReworkOrderString = Convert.ToString(Session["isWorkOrder"]);
                        if (int.TryParse(ReworkOrderString, out ReworkOrder))
                        {
                            if (ReworkOrderString == "1")
                            {
                                hmiidData.isWorkOrder = 1;
                            }
                            else
                            {
                                hmiidData.isWorkOrder = 0;
                            }
                        }

                        Session["WorkOrderClicked"] = 0;

                        //2017-03-14
                        DateTime EndTime = DateTime.Now;
                        int hmiiid = hmiidData.HMIID;
                        //1) 1remove hold on current one 
                        var hmiData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiiid).FirstOrDefault();
                        hmiData.IsHold = 0;
                        db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        int OldHMIID = 0;
                        //update the ishold and end time in old one.
                        var tblhmi = db.tbllivehmiscreens.Where(m => m.HMIID != hmiiid && m.Work_Order_No == hmiData.Work_Order_No && m.PartNo == hmiData.PartNo && m.OperationNo == hmiData.OperationNo).OrderByDescending(m => m.PEStartTime).FirstOrDefault();
                        if (tblhmi != null)
                        {
                            OldHMIID = tblhmi.HMIID;
                            tblhmi.IsHold = 2;
                            db.Entry(tblhmi).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();

                            //3) update the EndDateTime column in manuallossofentry table.
                            var tblmanualLossData = db.tblmanuallossofentries.Where(m => m.HMIID == OldHMIID).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
                            if (tblmanualLossData != null)
                            {
                                tblmanualLossData.EndHMIID = hmiiid;
                                tblmanualLossData.EndDateTime = EndTime;
                                db.Entry(tblmanualLossData).State = System.Data.Entity.EntityState.Modified;
                                db.SaveChanges();
                            }
                        }

                        db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                Session["SubmitClicked"] = 1;
                return RedirectToAction("Index");
            }
            return View();
        }

        public ActionResult IndividualSubmit(int HMIID = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int MachineId = Convert.ToInt32(Session["MachineID"]);

            //Check if similar WorkOrder is in View
            tbllivehmiscreen hmiidDataDup = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
            if (hmiidDataDup != null)
            {
                int hmiid = Convert.ToInt32(hmiidDataDup.HMIID);
                string woNo = Convert.ToString(hmiidDataDup.Work_Order_No);
                string opNo = Convert.ToString(hmiidDataDup.OperationNo);
                string partNo = Convert.ToString(hmiidDataDup.PartNo);

                //cheack in DDL if isCompleted = 1
                var DDLCompletedData = db.tblddls.Where(m => m.WorkOrder == woNo && m.MaterialDesc == partNo && m.OperationNo == opNo && m.IsCompleted == 1).FirstOrDefault();
                if (DDLCompletedData != null)
                {
                    Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;

                    db.tbllivehmiscreens.Remove(hmiidDataDup);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

                //2017-06-22
                var HMICompletedData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.isWorkInProgress == 1).FirstOrDefault();
                if (HMICompletedData != null)
                {
                    Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;

                    db.tbllivehmiscreens.Remove(hmiidDataDup);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

                #region 2017-02-07
                bool isValid = true, IsInHMI = true;
                string IssueMsg = null;
                if (isValid)
                {
                    var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == woNo && m.MaterialDesc == partNo && m.OperationNo != opNo && m.IsCompleted == 0).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                    foreach (var row in Siblingddldata)
                    {
                        IsInHMI = true; //reinitialize
                        int localOPNo = Convert.ToInt32(row.OperationNo);
                        string localOPNoString = Convert.ToString(row.OperationNo);
                        if (localOPNo < Convert.ToInt32(opNo))
                        {
                            #region //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                            //which case is valid.
                            var SiblingHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == localOPNoString).FirstOrDefault();
                            var SiblingHMIdatahistorian = db.tblhmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == localOPNoString).FirstOrDefault(); //added by Ashok
                            if (SiblingHMIdata == null || SiblingHMIdatahistorian==null)
                            {
                                //IssueMsg = "Please Select Below WorkOrder, WONo: " + WONo + " PartNo: " + Part + " OperationNo: " + localOPNo;
                                IsInHMI = false;
                                //break;
                            }
                            else
                            {
                                if (SiblingHMIdata.Date == null )//|| SiblingHMIdatahistorian.Date==null) //=> lower OpNo is not submitted.
                                {
                                    IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                    //return RedirectToAction("Index");
                                    IsInHMI = false;
                                    break;
                                }
                                if(SiblingHMIdatahistorian!=null)
                                {
                                    if (SiblingHMIdatahistorian.Date == null)//|| SiblingHMIdatahistorian.Date==null) //=> lower OpNo is not submitted.
                                    {
                                        IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                        //return RedirectToAction("Index");
                                        IsInHMI = false;
                                        break;
                                    }
                                }
                            }
                            #endregion

                            if (!IsInHMI)
                            {
                                #region //also check in MultiWO table
                                string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();
                                string WIPQueryMultiWOHistorian = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                var WIPMWOHistorian = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWOHistorian).ToList();
                                if (WIPMWO.Count == 0 || WIPMWOHistorian.Count==0)
                                {
                                    IssueMsg = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                    //return RedirectToAction("Index");
                                    IsInHMI = false;
                                    break;
                                }

                                foreach (var rowHMI in WIPMWO)
                                {
                                    int hmiidInner = Convert.ToInt32(rowHMI.HMIID);
                                    var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiidInner).FirstOrDefault();
                                    if (MWOHMIData != null) //obviously != 0
                                    {
                                        if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                        {
                                            IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                            //return RedirectToAction("Index");
                                            break;
                                        }
                                        else
                                        {
                                            IsInHMI = true;
                                        }
                                    }
                                }

                                foreach (var rowHMI in WIPMWOHistorian)
                                {
                                    int hmiidInner = Convert.ToInt32(rowHMI.HMIID);
                                    var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiidInner).FirstOrDefault();
                                    if (MWOHMIData != null) //obviously != 0
                                    {
                                        if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                        {
                                            IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                            //return RedirectToAction("Index");
                                            break;
                                        }
                                        else
                                        {
                                            IsInHMI = true;
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                    }

                    //commented on 2017-05-29
                    /////to Catch those Manual WorkOrders 
                    //string WIPQuery1 = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' and  IsMultiWO = 0 and DDLWokrCentre is null order by HMIID ) group by Work_Order_No,PartNo,OperationNo ) order by OperationNo ;";
                    //var WIPDDL1 = db.tblhmiscreens.SqlQuery(WIPQuery1).ToList();
                    //foreach (var row in WIPDDL1)
                    //{
                    //    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                    //    if (InnerOpNo < Convert.ToInt32(opNo))
                    //    {
                    //        string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
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
                }
                #endregion

                if (!string.IsNullOrEmpty(woNo) && !string.IsNullOrEmpty(opNo) && !string.IsNullOrEmpty(partNo))
                {
                    var InHMIData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo && m.Status == 0 && m.HMIID != hmiid && m.MachineID == MachineId).FirstOrDefault();
                    if (InHMIData != null)
                    {
                        Session["Error"] = "Duplicate WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                        //db.tblhmiscreens.Remove(hmiidDataDup);
                        db.Entry(hmiidDataDup).State = System.Data.Entity.EntityState.Deleted;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }
            }

            tbllivehmiscreen hmiidData = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
            if (hmiidData.IsHold == 0)
            {
                if (hmiidData.Date == null)
                {
                    if (hmiidData.OperatorDet != null && hmiidData.Shift != null && hmiidData.Project != null && hmiidData.Prod_FAI != null && hmiidData.PartNo != null && hmiidData.Work_Order_No != null && hmiidData.OperationNo != null && hmiidData.Target_Qty.HasValue)
                    {
                        hmiidData.Date = DateTime.Now;

                        //Get Processed Qty
                        int newProcessedQty = 0;
                        int PrvProcessQty = 0, PrvDeliveredQty = 0;
                        string woNo = Convert.ToString(hmiidData.Work_Order_No);
                        string opNo = Convert.ToString(hmiidData.OperationNo);
                        string partNo = Convert.ToString(hmiidData.PartNo);
                        int deliveredQty = 0;
                        int TargetQtyNew = Convert.ToInt32(hmiidData.Target_Qty);
                        int DeliveredQty = Convert.ToInt32(hmiidData.Delivered_Qty);
                        int ProcessedQty = Convert.ToInt32(hmiidData.ProcessQty);
                        newProcessedQty = DeliveredQty + ProcessedQty;

                        if (Convert.ToInt32(hmiidData.isWorkInProgress) == 1)
                        {
                            Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;
                            db.tbllivehmiscreens.Remove(hmiidData);
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

                        //2017-06-03
                        //var getProcessQty = db.tblhmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo == opNo).OrderByDescending(m => m.HMIID).Take(2).ToList();
                        //if (getProcessQty.Count == 2)
                        //{
                        //    string delivString = Convert.ToString(getProcessQty[1].Delivered_Qty);
                        //    int.TryParse(delivString, out PrvDeliveredQty);

                        //    string processString = Convert.ToString(getProcessQty[1].ProcessQty);
                        //    int.TryParse(processString, out PrvProcessQty);

                        //    newProcessedQty = PrvProcessQty + PrvDeliveredQty;
                        //    if (Convert.ToInt32(getProcessQty[1].isWorkInProgress) == 1 || TargetQtyNew == newProcessedQty)
                        //    {
                        //        Session["Error"] = "Job is Finished for WorkOrder:" + woNo + " OpNo: " + opNo + " PartNo:" + partNo;

                        //        //2017-01-07
                        //        //hmiidData.Prod_FAI = null;
                        //        //hmiidData.Target_Qty = null;
                        //        //hmiidData.OperationNo = null;
                        //        //hmiidData.PartNo = null;
                        //        //hmiidData.Work_Order_No = null;
                        //        //hmiidData.Project = null;
                        //        //hmiidData.Date = null;
                        //        //hmiidData.DDLWokrCentre = null;
                        //        //hmiidData.isWorkOrder = 0;
                        //        //hmiidData.ProcessQty = 0;
                        //        //Session["FromDDL"] = 2;
                        //        //TempData["ForDDL2"] = 2;

                        //        //db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                        //        //db.SaveChanges();

                        //        db.tblhmiscreens.Remove(hmiidData);
                        //        db.SaveChanges();
                        //        return RedirectToAction("Index");
                        //    }
                        //}

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

                        //hmiidData.ProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);


                        #region 2017-02-07
                        //Check if Lower Operation No is submitted or JF  or PF 'ed Prior to this.
                        //1) Get Related WO's , order by Ascending.
                        //2) Check if current Opno is Allowed to be Submitted. 
                        //(CONDITION : All Opno's lesser than this has to be submitted ( => || PF or JF 'ed Atleast Once).)

                        int OperationNoInt = Convert.ToInt32(opNo);
                        //var hmiDataInAscendingOrder = db.tblhmiscreens.Where(m => m.Work_Order_No == woNo && m.PartNo == partNo && m.OperationNo != opNo && m.OperationNo.toInt < opNo)).OrderBy(m => m.OperationNo).ToList(); //&& m.OperationNo < opNo "< Cannot be applied to Strings"
                        string WIPQuery = @"SELECT * from tbllivehmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' and MachineID = '" + MachineId + "' order by HMIID   ";
                        var WIP = db.tbllivehmiscreens.SqlQuery(WIPQuery).ToList();
                        string WIPQueryHistorian = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' and MachineID = '" + MachineId + "' order by HMIID   ";
                        var WIPHistorian = db.tblhmiscreens.SqlQuery(WIPQueryHistorian).ToList();
                        foreach (var row in WIP)
                        {
                            int InnerOpNo = Convert.ToInt32(row.OperationNo);
                            if (OperationNoInt > InnerOpNo)
                            {
                                if (row.Date == null) //=> lower OpNo is not submitted.
                                {
                                    Session["VError"] = " Submit WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                                    return RedirectToAction("Index");
                                }
                            }
                        }
                        foreach (var row in WIPHistorian)
                        {
                            int InnerOpNo = Convert.ToInt32(row.OperationNo);
                            if (OperationNoInt > InnerOpNo)
                            {
                                if (row.Date == null) //=> lower OpNo is not submitted.
                                {
                                    Session["VError"] = " Submit WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                                    return RedirectToAction("Index");
                                }
                            }
                        }
                        bool IsInHMI = true;
                        string WIPQuery2 = @"SELECT * from tblddl where WorkOrder = '" + woNo + "' and MaterialDesc = '" + partNo + "' and OperationNo != '" + opNo + "'  and IsCompleted = 0 order by WorkOrder,MaterialDesc,OperationNo ";
                        var WIPDDL = db.tblddls.SqlQuery(WIPQuery2).ToList();
                        foreach (var row in WIPDDL)
                        {
                            Session["VError"] = null;
                            IsInHMI = true; //reinitialize
                            int InnerOpNo = Convert.ToInt32(row.OperationNo);
                            if (InnerOpNo < OperationNoInt)
                            {
                                bool IsItWrong = false;
                                string WIPQueryHMI = @"SELECT * from tbllivehmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
                                var WIP1 = db.tbllivehmiscreens.SqlQuery(WIPQueryHMI).ToList();
                                string WIPQueryHMIHistorian = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
                                var WIP1Historian = db.tblhmiscreens.SqlQuery(WIPQueryHMIHistorian).ToList();
                                if (WIP1.Count == 0 || WIP1Historian.Count==0)
                                {
                                    //Session["VError"] = " Select & Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                    IsItWrong = true;
                                    IsInHMI = false;
                                }
                                else
                                {
                                    foreach (var rowHMI in WIP1)
                                    {
                                        if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                                        {
                                            Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                            return RedirectToAction("Index");
                                        }
                                    }
                                    if(WIP1Historian!=null)
                                    {
                                        foreach (var rowHMI in WIP1Historian)
                                        {
                                            if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                                return RedirectToAction("Index");
                                            }
                                        }
                                    }
                                }

                                if (!IsInHMI)
                                {
                                    #region //also check in MultiWO table
                                    string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by MultiWOID limit 1 ";
                                    var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();
                                    string WIPQueryMultiWOHistroian = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by MultiWOID limit 1 ";
                                    var WIPMWOHistorian = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWOHistroian).ToList();
                                    if (WIPMWO.Count == 0 || WIPMWOHistorian.Count==0)
                                    {
                                        Session["VError"] = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                        return RedirectToAction("Index");
                                        //IsInHMI = false;
                                        break;
                                    }

                                    foreach (var rowHMI in WIPMWO)
                                    {
                                        int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                        var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                        if (MWOHMIData != null) //obviously != 0
                                        {
                                            if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                                return RedirectToAction("Index");
                                                //break;
                                            }
                                            else
                                            {
                                                IsInHMI = true;
                                            }
                                        }
                                    }

                                    foreach (var rowHMI in WIPMWOHistorian)
                                    {
                                        int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                        var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                        if (MWOHMIData != null) //obviously != 0
                                        {
                                            if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                                return RedirectToAction("Index");
                                                //break;
                                            }
                                            else
                                            {
                                                IsInHMI = true;
                                            }
                                        }
                                    }
                                    #endregion
                                }

                            }
                        }

                        //commmented On 2017-05-29
                        ////2017-01-21 I guess when its purely ManualEntry
                        ////string WIPQuery1 = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' order by Work_Order_No,PartNo,OperationNo";
                        //string WIPQuery1 = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' and IsMultiWO = 0 and DDLWokrCentre is null order by HMIID ) group by Work_Order_No,PartNo,OperationNo  ) order by OperationNo;";
                        //var WIPDDL1 = db.tblhmiscreens.SqlQuery(WIPQuery1).ToList();
                        //foreach (var row in WIPDDL1)
                        //{
                        //    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                        //    if (InnerOpNo < OperationNoInt)
                        //    {
                        //        string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo = '" + InnerOpNo + "' order by HMIID limit 1 ";
                        //        var WIP2 = db.tblhmiscreens.SqlQuery(WIPQueryHMI).ToList();
                        //        foreach (var rowHMI in WIP2)
                        //        {
                        //            if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                        //            {
                        //                Session["VError"] = " Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                        //                return RedirectToAction("Index");
                        //            }
                        //        }
                        //        if (WIP2.Count == 0)
                        //        {
                        //            Session["VError"] = " Select & Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                        //            return RedirectToAction("Index");
                        //        }
                        //    }
                        //}

                        #endregion

                        int ReworkOrder = 0;
                        string ReworkOrderString = Convert.ToString(Session["isWorkOrder"]);
                        if (int.TryParse(ReworkOrderString, out ReworkOrder))
                        {
                            if (ReworkOrderString == "1")
                            {
                                hmiidData.isWorkOrder = 1;
                            }
                            else
                            {
                                hmiidData.isWorkOrder = 0;
                            }
                        }

                        Session["WorkOrderClicked"] = 0;
                        hmiidData.Status = 0;
                        db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        Session["VError"] = "Please enter all Details Before StartAll is Clicked.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    Session["VError"] = "WorkOrder is already Started.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                Session["VError"] = "Please end HOLD before Start";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        public ActionResult IndividualHold(int HMIID = 0, int cjtextbox8 = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int machineId = Convert.ToInt32(Session["MachineID"]);

            if (string.IsNullOrEmpty(cjtextbox8.ToString().Trim()))
            {
                Session["VError"] = " Please Enter All Details Before Hold ";
                return RedirectToAction("Index");
            }

            tbllivehmiscreen hmiidData = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
            //Add server side validations.
            if (hmiidData != null)
            {
                if (Convert.ToInt32(hmiidData.IsHold) != 1)
                {
                    if (hmiidData.Date != null)
                    {
                        hmiidData.Delivered_Qty = cjtextbox8;
                        ////Do it after entering the code.

                        //hmiidData.isWorkInProgress = 0;
                        //hmiidData.Time = DateTime.Now; 
                        db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        return RedirectToAction("HoldCodeEntry", "ManualHMIScreen", new { Hmiid = HMIID, Bid = 0 });

                    }
                    else
                    {
                        Session["VError"] = " Please Click Start Before Clicking Hold ";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    Session["VError"] = "WorkOrder is already in HOLD ";
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult IndividualFinish(int HMIID = 0, int cjtextbox8 = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

            int machineID = Convert.ToInt32(Session["MachineID"]);
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int machineId = Convert.ToInt32(Session["MachineID"]);

            if (string.IsNullOrEmpty(cjtextbox8.ToString().Trim()))
            {
                Session["VError"] = " Please Enter All Details Before Job Finish ";
                return RedirectToAction("Index");
            }

            tbllivehmiscreen hmiidData = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
            //Add server side validations.
            if (hmiidData != null)
            {
                if (hmiidData.IsHold != 1)
                {
                    if (hmiidData.Date != null)
                    {
                        int ProcessQty = 0;
                        string processString1 = Convert.ToString(hmiidData.ProcessQty);
                        int.TryParse(processString1, out ProcessQty);

                        if ((cjtextbox8 + ProcessQty) == hmiidData.Target_Qty)
                        {
                            string wono = hmiidData.Work_Order_No;
                            string partno = hmiidData.PartNo;
                            string opno = hmiidData.OperationNo;
                            int OperationNoInt = Convert.ToInt32(opno);

                            #region 2017-07-01
                            //New Logic to Overcome WorkOrder Sequence Scenario 2017-02-03
                            string WIPQuery6 = @"SELECT * from tblddl where WorkOrder = '" + wono + "' and MaterialDesc = '" + partno + "' and OperationNo != '" + opno + "' and IsCompleted = 0 order by OperationNo ";
                            var WIPDDL6 = db.tblddls.SqlQuery(WIPQuery6).ToList();
                            foreach (var row in WIPDDL6)
                            {
                                int InnerOpNo = Convert.ToInt32(row.OperationNo);
                                string ddlOpno = row.OperationNo;
                                if (InnerOpNo < OperationNoInt)
                                {
                                    int PrvProcessQty = 0, PrvDeliveredQty = 0, TotalProcessQty = 0, ishold = 0;
                                    #region new code
                                    //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                                    int isHMIFirst = 2; //default NO History for that wo,pn,on

                                    var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == wono && m.PartNo == partno && m.OperationNo == ddlOpno && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                                    var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == wono && m.PartNo == partno && m.OperationNo == ddlOpno && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();

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
                                                //tblh.HMIID = ;
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
                                           // tblh.HMIID = ;
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

                            #region 2017-02-07
                            //New Logic to Overcome WorkOrder Sequence Scenario 2017-02-03
                            string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + wono + "' and MaterialDesc = '" + partno + "' and OperationNo != '" + opno + "' and IsCompleted = 0 order by OperationNo ";
                            var WIPDDL1 = db.tblddls.SqlQuery(WIPQuery1).ToList();
                            foreach (var row in WIPDDL1)
                            {
                                int InnerOpNo = Convert.ToInt32(row.OperationNo);
                                if (InnerOpNo < OperationNoInt)
                                {
                                    if (row.IsCompleted == 0)
                                    {
                                        Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                        return RedirectToAction("Index");
                                    }
                                    else
                                    {
                                        Session["VError"] = null;
                                    }

                                    //bool IsItWrong = false;
                                    //string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + wono + "' and PartNo = '" + partno + "' and OperationNo = '" + InnerOpNo + "' order by HMIID desc limit 1 ";
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
                                    //    string WIPQueryMultiWO = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + wono + "' and PartNo = '" + partno + "' and OperationNo = '" + InnerOpNo + "' order by MultiWOID desc limit 1 ";
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

                            // Commented on 2017-05-29
                            //string WIPQuery = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where Work_Order_No = '" + wono + "' and PartNo = '" + partno + "' and OperationNo != '" + opno + "' order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo ";
                            // 2017-01-21
                            //string WIPQuery = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + wono + "' and PartNo = '" + partno + "' and OperationNo != '" + opno + "' and IsMultiWO = 0 and DDLWokrCentre is null order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo  ) order by OperationNo ;";
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

                            int target = 0, delivered = 0, processed = 0;
                            int.TryParse(hmiidData.Target_Qty.ToString(), out target);
                            int.TryParse(hmiidData.Delivered_Qty.ToString(), out delivered);
                            int.TryParse(hmiidData.ProcessQty.ToString(), out processed);

                            int HMIId = hmiidData.HMIID;
                            using (mazakdaqEntities dbsimilar = new mazakdaqEntities())
                            {
                                #region If its as SingleWO
                                var SimilarWOData = dbsimilar.tbllivehmiscreens.Where(m => m.Work_Order_No == hmiidData.Work_Order_No && m.OperationNo == hmiidData.OperationNo && m.PartNo == hmiidData.PartNo && m.MachineID != machineID && m.isWorkInProgress == 2).FirstOrDefault();
                                if (SimilarWOData != null)
                                {
                                    int InnerMacID = Convert.ToInt32(dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == SimilarWOData.HMIID).Select(m => m.MachineID).FirstOrDefault());
                                    var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());

                                    Session["Error"] = " Same WorkOrder in Machine: " + MacDispName + " , So you cannot JobFinish ";
                                    return RedirectToAction("Index");
                                }
                                #endregion

                                #region If its as MultiWO
                                var SimilarWODataMulti = dbsimilar.tbllivemultiwoselections.Where(m => m.WorkOrder == hmiidData.Work_Order_No && m.OperationNo == hmiidData.OperationNo && m.PartNo == hmiidData.PartNo && m.HMIID != HMIId && m.tbllivehmiscreen.isWorkInProgress == 2).FirstOrDefault();
                                if (SimilarWODataMulti != null)
                                {
                                    int InnerHMIID = (int)SimilarWODataMulti.HMIID;
                                    int InnerMacID = Convert.ToInt32(dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == InnerHMIID).Select(m => m.MachineID).FirstOrDefault());
                                    var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());

                                    Session["Error"] = " Same WorkOrder in Machine: " + MacDispName + " , So you cannot JobFinish ";
                                    return RedirectToAction("Index");
                                }
                                #endregion
                            }

                            var ddldata = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno && m.OperationNo == opno).FirstOrDefault();
                            if (ddldata != null)
                            {
                                ddldata.IsCompleted = 1;
                                db.Entry(ddldata).State = System.Data.Entity.EntityState.Modified;
                            }

                            hmiidData.Delivered_Qty = cjtextbox8;
                            hmiidData.SplitWO = "No";
                            hmiidData.isWorkInProgress = 1;
                            hmiidData.Status = 2;
                            hmiidData.Time = DateTime.Now;
                            db.Entry(hmiidData).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                        }
                        else
                        {
                            Session["VError"] = " Delivered + Processed Qty Must be Equal to Target Qty";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        Session["VError"] = " Please Click Start Before Clicking Job Finish ";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    Session["VError"] = " Please End HOLD Before JobFinish";
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult EndHoldIndividual(int hmiiid)
        {
            int MacID = Convert.ToInt32(Session["MachineID"]);
            DateTime EndTime = DateTime.Now;
            //1) 1remove hold on current one 
            var hmiData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiiid).FirstOrDefault();
            hmiData.IsHold = 0;
            db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            int OldHMIID = 0;
            //update the ishold and end time in old one.
            var tblhmi = db.tbllivehmiscreens.Where(m => m.HMIID != hmiiid && m.Work_Order_No == hmiData.Work_Order_No && m.PartNo == hmiData.PartNo && m.OperationNo == hmiData.OperationNo).OrderByDescending(m => m.PEStartTime).FirstOrDefault();
            if (tblhmi != null)
            {
                OldHMIID = tblhmi.HMIID;
                tblhmi.IsHold = 2;
                db.Entry(tblhmi).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                //3) update the EndDateTime column in manuallossofentry table.
                var tblmanualLossData = db.tbllivemanuallossofentries.Where(m => m.HMIID == OldHMIID).OrderByDescending(m => m.StartDateTime).FirstOrDefault();
                if (tblmanualLossData != null)
                {
                    tblmanualLossData.EndHMIID = hmiiid;
                    tblmanualLossData.EndDateTime = EndTime;
                    db.Entry(tblmanualLossData).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }

        public int HandleIdleManualWC()
        {
            int retval = 12;

            return retval;
        }
        public JsonResult ReworkOrderClicked(int HMIID)
        {
            var nothing = 1;
            //Get All rows with status = 0 for this correctedDate and Operator and machine.
            int macId = Convert.ToInt32(Session["MachineID"]);
            int userId = Convert.ToInt32(Session["UserId"]);
            Session["isWorkOrder"] = 1;
            Session["WorkOrderClicked"] = 1;
            //var HMIData = db.tblhmiscreens.Where(m => m.MachineID == macId && m.OperatiorID == userId && m.Status == 0).ToList();
            //foreach (var row in HMIData)
            //{
            //    row.isWorkOrder = 1;
            //    db.Entry(row).State = System.Data.Entity.EntityState.Modified;
            //    db.SaveChanges();
            //}
            var a = Session["isWorkOrder"];
            return Json(nothing, JsonRequestBehavior.AllowGet);
        }
        public JsonResult AutoSave(int HMIID, string field, string value)
        {
            //var selectedRow = new SelectList(db.tblmachinedetails.Where(m => m.ShopNo == countryId).Where(m => m.IsDeleted == 0), "MachineDispName", "MachineDispName");
            //int hmiid = HMIID - 1;
            var thisrow = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).ToList();
            var nothing = 1;
            switch (field)
            {
                case "cjtextboxshift":
                    {
                        thisrow[0].Shift = value;
                        break;
                    }
                case "cjtextboxop":
                    {
                        if (value != "")
                        {
                            thisrow[0].OperatorDet = value;
                            thisrow[0].PEStartTime = DateTime.Now;
                        }
                        else
                        {
                            thisrow[0].OperatorDet = null;
                            thisrow[0].PEStartTime = DateTime.Now;
                        }
                        break;
                    }
                case "cjtextbox1":
                    {
                        if (value != "")
                        {
                            //thisrow[0].PEStartTime = DateTime.Now;
                            thisrow[0].Project = value;
                        }
                        else
                        {
                            thisrow[0].Project = null;
                        }
                        break;
                    }
                case "Prod_FAI":
                    {
                        if (value != "")
                        {
                            thisrow[0].Prod_FAI = value;
                        }
                        break;
                    }
                case "cjtextbox3":
                    {
                        if (value != "")
                        {
                            thisrow[0].PartNo = value;
                        }
                        else
                        {
                            thisrow[0].PartNo = null;
                        }
                        break;
                    }
                case "cjtextbox4":
                    {
                        if (value != "")
                        {
                            thisrow[0].Work_Order_No = value;
                        }
                        else
                        {
                            thisrow[0].Work_Order_No = null;
                        }
                        break;
                    }
                case "cjtextbox5":
                    {
                        if (value != "")
                        {
                            thisrow[0].OperationNo = value;
                        }
                        else
                        {
                            thisrow[0].OperationNo = null;
                        }
                        break;
                    }
                case "cjtextbox6":
                    {
                        if (value != "")
                        {
                            thisrow[0].Target_Qty = Convert.ToInt32(value);
                        }
                        else
                        {
                            thisrow[0].Target_Qty = null;
                        }
                        break;
                    }
                //case "cjtextbox7":
                //    {
                //        if (value != "")
                //            thisrow[0].Rej_Qty = Convert.ToInt32(value);
                //        break;
                //    }
                case "cjtextbox8":
                    {
                        if (value != "")
                        {
                            thisrow[0].Delivered_Qty = Convert.ToInt32(value);
                        }
                        else
                        {
                            thisrow[0].Delivered_Qty = null;
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            db.Entry(thisrow[0]).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return Json(nothing, JsonRequestBehavior.AllowGet);
        }
        public JsonResult AutoSaveOPName(string field, string value)
        {
            //var selectedRow = new SelectList(db.tblmachinedetails.Where(m => m.ShopNo == countryId).Where(m => m.IsDeleted == 0), "MachineDispName", "MachineDispName");
            //int hmiid = HMIID - 1;
            int macID = Convert.ToInt32(Session["MachineID"]);
            var thisrow = db.tbllivehmiscreens.Where(m => m.MachineID == macID && m.Status == 0).ToList();
            var nothing = 1;

            foreach (var row in thisrow)
            {
                switch (field)
                {
                    case "cjtextboxshift":
                        {
                            row.Shift = value;
                            Session["OpName"] = value;
                            break;
                        }
                    case "cjtextboxop":
                        {
                            row.OperatorDet = value;
                            row.PEStartTime = DateTime.Now;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            return Json(nothing, JsonRequestBehavior.AllowGet);
        }
        public string[] GetDateShift()
        {

            string[] dateShift = new string[2];
            //Get CorrectedDate & shift

            #region
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
            String CorrectedDate = System.DateTime.Now.ToString("yyyy-MM-dd");
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

            #endregion

            dateShift[0] = Shift;
            dateShift[1] = CorrectedDate;
            return dateShift;
        }
        public JsonResult GetWorkOrders(string Prefix, string AllVal)
        {
            List<string> existingWO = new List<string>();
            if (AllVal.Contains(','))
            {
                string[] wos = AllVal.Split(',');
                existingWO.AddRange(wos);
            }

            //can improvise it by inter changing the sequence.
            //tblddl[] hmiData = (from c in db.tblddls
            //                        where !existingWO.Contains(c.WorkOrder) && c.IsDeleted == 0
            //                        select c).ToArray();

            //var WODataToView = (from N in hmiData
            //                      where N.WorkOrder.StartsWith(Prefix)
            //                      select new { N.WorkOrder });


            var WODataToView = (from N in db.tblddls
                                where N.WorkOrder.StartsWith(Prefix) && !existingWO.Contains(N.WorkOrder) && N.IsDeleted == 0
                                select new { N.WorkOrder }).Distinct();

            return Json(WODataToView, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPartNos(string Prefix, string AllVal)
        {
            List<string> existingWO = new List<string>();
            if (AllVal.Contains(','))
            {
                string[] wos = AllVal.Split(',');
                existingWO.AddRange(wos);
            }
            var WODataToView = (from N in db.tblddls
                                where N.WorkOrder.StartsWith(Prefix) && !existingWO.Contains(N.MaterialDesc) && N.IsDeleted == 0
                                select new { N.MaterialDesc }).Distinct();

            return Json(WODataToView, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetOpNos(string Prefix, string AllVal)
        {
            List<string> existingWO = new List<string>();
            if (AllVal.Contains(','))
            {
                string[] wos = AllVal.Split(',');
                existingWO.AddRange(wos);
            }
            var WODataToView = (from N in db.tblddls
                                where N.WorkOrder.StartsWith(Prefix) && !existingWO.Contains(N.OperationNo) && N.IsDeleted == 0
                                select new { N.OperationNo }).Distinct();

            return Json(WODataToView, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPartNosFromWo(string WoNo)
        {
            var WODataToView = (from N in db.tblddls
                                where N.WorkOrder == WoNo && N.IsDeleted == 0
                                select new { N.OperationNo }).Distinct();

            return Json(WODataToView, JsonRequestBehavior.AllowGet);
        }

        public ActionResult HoldCodeEntry(int Hmiid = 0, int Bid = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            ViewBag.Hmiid = Hmiid;

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

            int handleidleReturnValue = HandleIdleManualWC();
            if (handleidleReturnValue == 1)
            {
                Session["showIdlePopUp"] = 0;
                return RedirectToAction("Index");
            }

            ////Get Previous Loss to Display.
            //var PrevIdleToView = db.tbllossofentries.Where(m => m.MachineID == macid && m.DoneWithRow == 0).OrderByDescending(m => m.LossID).FirstOrDefault();
            //if (PrevIdleToView != null)
            //{
            //    int losscode = PrevIdleToView.MessageCodeID;
            //    ViewBag.PrevLossName = GetLossPath(losscode);
            //    ViewBag.PrevLossStartTime = PrevIdleToView.StartDateTime;
            //}

            //Data of Current HMI to HoldScreen
            var HMIDataToView = db.tbllivehmiscreens.Where(m => m.HMIID == Hmiid).FirstOrDefault();
            if (HMIDataToView != null)
            {
                ViewBag.WONo = HMIDataToView.Work_Order_No;
                ViewBag.PartNo = HMIDataToView.PartNo;
                ViewBag.OpNo = HMIDataToView.OperationNo;
                ViewBag.SplitWO = HMIDataToView.SplitWO;
            }

            // 2017-01-03 stage 2. Idle is running and u need to send data to view regarding that.
            //var IdleToView = db.tbllossofentries.Where(m => m.MachineID == macid).OrderByDescending(m => m.LossID).FirstOrDefault();
            //if (IdleToView != null) //implies idle is running
            //{
            //    if (IdleToView.DoneWithRow == 0 && IdleToView.MessageCodeID != 999)
            //    {
            //        int idlecode = Convert.ToInt32(IdleToView.MessageCodeID);
            //        var DataToView = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.HoldCodeID == idlecode).ToList();
            //        ViewBag.Level = DataToView[0].LossCodesLevel;
            //        ViewBag.HoldCode = DataToView[0].HoldCode;
            //        ViewBag.LossId = DataToView[0].HoldCodeID;
            //        ViewBag.IdleStartTime = IdleToView.StartDateTime;
            //    }
            //}

            //stage 3. Operator is selecting the Idle by traversing down the Hierarchy of LossCodes.
            if (Bid != 0)
            {
                var lossdata = db.tblholdcodes.Find(Bid);
                int level = lossdata.HoldCodesLevel;
                string losscode = lossdata.HoldCode;
                if (level == 1)
                {
                    var level2Data = db.tblholdcodes.Where(m => m.IsDeleted == 0 && m.HoldCodesLevel1ID == Bid && m.HoldCodesLevel == 2 && m.HoldCodesLevel2ID == null).ToList();
                    if (level2Data.Count == 0)
                    {
                        var level1Data = db.tblholdcodes.Where(m => m.IsDeleted == 0 && m.HoldCodesLevel == level && m.HoldCodesLevel1ID == null && m.HoldCodesLevel2ID == null).ToList();
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
                    var level3Data = db.tblholdcodes.Where(m => m.IsDeleted == 0 && m.HoldCodesLevel2ID == Bid && m.HoldCodesLevel == 3).ToList();
                    int prevLevelId = Convert.ToInt32(lossdata.HoldCodesLevel1ID);
                    var level1data = db.tblholdcodes.Where(m => m.HoldCodeID == prevLevelId).Select(m => m.HoldCode).FirstOrDefault();
                    if (level3Data.Count == 0)
                    {
                        var level2Data = db.tblholdcodes.Where(m => m.IsDeleted == 0 && m.HoldCodesLevel1ID == prevLevelId && m.HoldCodesLevel2ID == null).ToList();
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
                    int prevLevelId = Convert.ToInt32(lossdata.HoldCodesLevel2ID);
                    int FirstLevelID = Convert.ToInt32(lossdata.HoldCodesLevel1ID);
                    var level2scrum = db.tblholdcodes.Where(m => m.HoldCodeID == prevLevelId).Select(m => m.HoldCode).FirstOrDefault();
                    var level1scrum = db.tblholdcodes.Where(m => m.HoldCodeID == FirstLevelID).Select(m => m.HoldCode).FirstOrDefault();
                    var level2Data = db.tblholdcodes.Where(m => m.IsDeleted == 0 && m.HoldCodesLevel2ID == prevLevelId && m.HoldCodesLevel == 3).ToList();
                    ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + losscode + " as reason.";
                    ViewBag.LossID = Bid;
                    ViewBag.Level = 3;
                    ViewBag.breadScrum = level1scrum + " --> " + level2scrum + " --> ";
                    return View(level2Data);
                }
            }
            else
            {
                var level1Data = db.tblholdcodes.Where(m => m.IsDeleted == 0 && m.HoldCodesLevel == 1).ToList();
                ViewBag.Level = 1;
                return View(level1Data);
            }

            //Fail Safe: if everything else fails send level1 codes.
            ViewBag.Level = 1;
            var level10Data = db.tblholdcodes.Where(m => m.IsDeleted == 0 && m.HoldCodesLevel == 1).ToList();
            return View(level10Data);

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HoldCodeEntry(string Hmiid = null, int HiddenID = 0)
        {

            //ViewData["MessageCodeID"] = new SelectList(db.message_code_master.Where(m => m.IsDeleted == 0).Where(m => m.MessageType == "IDLE" || m.MessageType == "SETUP"), "MessageCodeID", "MessageDescription");

            //corrected date
            string CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            if (DateTime.Now.Hour < 6 && DateTime.Now.Hour >= 0)
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            int HMIID = Convert.ToInt32(Hmiid);

            var tblhmi = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
            //tblhmi.isWorkInProgress = 3;
            tblhmi.Time = DateTime.Now;
            tblhmi.isWorkInProgress = 0; //PF
            tblhmi.IsHold = 1;
            tblhmi.Status = 2;
            //tblhmi.Time = DateTime.Now;
            db.Entry(tblhmi).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            tbllivemanuallossofentry tmloe = new tbllivemanuallossofentry();
            tmloe.CorrectedDate = CorrectedDate;
            tmloe.HMIID = Convert.ToInt32(HMIID);
            tmloe.MachineID = Convert.ToInt32(Session["MachineID"]);

            var lossdata = db.tblholdcodes.Where(m => m.HoldCodeID == HiddenID).FirstOrDefault();
            tmloe.MessageCodeID = HiddenID;
            tmloe.MessageDesc = lossdata.HoldCodeDesc.ToString();
            tmloe.MessageCode = lossdata.HoldCode.ToString();
            tmloe.WONo = tblhmi.Work_Order_No;
            tmloe.OpNo = Convert.ToInt32(tblhmi.OperationNo);
            tmloe.PartNo = tblhmi.PartNo;

            tmloe.StartDateTime = DateTime.Now;
            string[] GetDateShift1 = GetDateShift();
            tmloe.Shift = GetDateShift1[0];

            db.tbllivemanuallossofentries.Add(tmloe);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // Using in HOLD Screen
        public ActionResult DirectPartialFinish(int Hmiid)
        {
            int MachineID = Convert.ToInt32(Session["MachineID"]);
            List<string> MacHierarchy = GetMacHierarchyData(MachineID);
            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

            var tblhmi = db.tbllivehmiscreens.Where(m => m.HMIID == Hmiid).FirstOrDefault();
            //if (tblhmi.isWorkInProgress != 3)
            if (tblhmi.IsHold == 0)
            {
                string wono = tblhmi.Work_Order_No;
                string partno = tblhmi.PartNo;
                string opno = tblhmi.OperationNo;
                //Check if its eligiable to PF based on OpNo Condition
                int OperationNoInt = Convert.ToInt32(opno);

                //var hmiDataInAscendingOrder = db.tblhmiscreens.Where(m => m.Work_Order_No == wono && m.PartNo == partno && m.OperationNo != opno && (Convert.ToInt32(m.OperationNo) < OperationNoInt)).OrderBy(m => m.OperationNo).ToList(); //&& m.OperationNo < opNo "< Cannot be applied to Strings"
                //string WIPQuery = @"SELECT * from tblhmiscreen where Work_Order_No = '" + wono + "' and PartNo = '" + partno + "' and OperationNo != '" + opno + "' order by Work_Order_No,PartNo,OperationNo ";

                #region 2017-02-07
                //string WIPQuery = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + wono + "' and PartNo = '" + partno + "' and OperationNo != '" + opno + "' order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo ) order by OperationNo  ;";
                //var WIP = db.tblhmiscreens.SqlQuery(WIPQuery).ToList();
                //foreach (var row in WIP)
                //{
                //    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                //    if (OperationNoInt > InnerOpNo)
                //    {
                //        if (row.Time == null) //=> lower OpNo is not PF or JF 'ed.
                //        {
                //            Session["VError"] = " PartialFinish WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                //            return RedirectToAction("Index");
                //        }
                //    }
                //}
                #endregion

                int target = 0, delivered = 0, processed = 0;
                int.TryParse(tblhmi.Target_Qty.ToString(), out target);
                int.TryParse(tblhmi.Delivered_Qty.ToString(), out delivered);
                int.TryParse(tblhmi.ProcessQty.ToString(), out processed);

                if (target > (delivered + processed))
                {
                    //var ddldata = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno && m.OperationNo == opno).FirstOrDefault();
                    //if (ddldata != null)
                    //{
                    //    ddldata.IsCompleted = 1;
                    //    db.Entry(ddldata).State = System.Data.Entity.EntityState.Modified;
                    //}

                    using (mazakdaqEntities dbsimilar = new mazakdaqEntities())
                    {

                        #region if its as Single WO
                        var SimilarWOData = dbsimilar.tbllivehmiscreens.Where(m => m.Work_Order_No == tblhmi.Work_Order_No && m.OperationNo == tblhmi.OperationNo && m.PartNo == tblhmi.PartNo && m.MachineID != MachineID && m.isWorkInProgress == 2).ToList();
                        foreach (var row in SimilarWOData)
                        {

                            int InnerProcessed = row.ProcessQty;
                            int FinalProcessed = delivered + InnerProcessed;
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
                        int hmiidi = tblhmi.HMIID;
                        var SimilarWODataMulti = dbsimilar.tbllivemultiwoselections.Where(m => m.WorkOrder == tblhmi.Work_Order_No && m.OperationNo == tblhmi.OperationNo && m.PartNo == tblhmi.PartNo && m.HMIID != hmiidi && m.tbllivehmiscreen.isWorkInProgress == 2).ToList();
                        foreach (var row in SimilarWODataMulti)
                        {
                            //update only if its hmiscreen row is still in screen.
                            int RowHMIID = Convert.ToInt32(row.HMIID);
                            var localhmiData = dbsimilar.tbllivehmiscreens.Find(RowHMIID);
                            int DeliveredQtyLocal = Convert.ToInt32(row.DeliveredQty);
                            int InnerProcessed = Convert.ToInt32(row.ProcessQty);
                            int FinalProcessed = DeliveredQtyLocal + InnerProcessed;
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

                            //int InnerHMIID = (int)row.HMIID;
                            //var InnerHMIDupData = dbsimilar.tblhmiscreens.Where(m => m.HMIID == InnerHMIID && m.HMIID != hmiidi).FirstOrDefault();
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

                    tblhmi.Status = 2;
                    tblhmi.Time = DateTime.Now;
                    tblhmi.isWorkInProgress = 0;
                    db.Entry(tblhmi).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    Session["VError"] = "Delivered + Processed  should be less than Target.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                Session["VError"] = " Please End HOLD Before PartialFinish";
                return RedirectToAction("Index");
            }
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

            //Check if any WorkOrder is in Hold.
            var hmiData = db.tbllivehmiscreens.Where(m => m.MachineID == id && m.Status == 0).ToList();
            foreach (var row in hmiData)
            {
                //if (row.isWorkInProgress == 3)
                if (row.IsHold == 1)
                {
                    Session["VError"] = "End HOLD before starting Planned Maintenance ";
                    return RedirectToAction("Index");
                }
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
            }
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

        //BREAKDOWN codes
        public ActionResult BreakDownEntry(int id = 0, int Bid = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

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

            //Check if any WorkOrder is in Hold.
            var hmiData = db.tbllivehmiscreens.Where(m => m.MachineID == id && m.Status == 0).ToList();
            foreach (var row in hmiData)
            {
                //if (row.isWorkInProgress == 3)
                if (row.IsHold == 1)
                {
                    Session["VError"] = "End HOLD before starting Breakdown ";
                    return RedirectToAction("Index");
                }
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
        public ActionResult BreakDownEntry(tbllossescode tbdc, string EndBreakdown = null, int HiddenID = 0)
        {
            //"EndBreakdown" is for insert new row or update old one. Basically speeking its like start and Stop of Breakdown.
            //"HiddenID" is the BreakdownID of row to be set as reason.

            int MachineID = Convert.ToInt32(Session["MachineID"]);
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
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
            return RedirectToAction("Index");
        }

        //Breakdown List
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

            int handleidleReturnValue = HandleIdleManualWC();
            if (handleidleReturnValue == 0)
            {
                return RedirectToAction("DownCodeEntry");
            }

            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == id).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);

            //var breakdown = db.tblbreakdowns.Include(t=>t.machine_master).Include(t=>t.message_code_master).Where(m=>m.MachineID==id && m.CorrectedDate==CorrectedDate).ToList();
            var breakdown = db.tblbreakdowns.Include(t => t.tbllossescode).Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate && m.DoneWithRow == 1).ToList();
            return View(breakdown);
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

            ////Get Previous Loss to Display.
            //var PrevIdleToView = db.tbllossofentries.Where(m => m.MachineID == macid && m.DoneWithRow == 0).OrderByDescending(m => m.LossID).FirstOrDefault();
            //if (PrevIdleToView != null)
            //{
            //    int losscode = PrevIdleToView.MessageCodeID;
            //    ViewBag.PrevLossName = GetLossPath(losscode);
            //    ViewBag.PrevLossStartTime = PrevIdleToView.StartDateTime;
            //}

            //Condition to Start OverAll Hold is 
            //1) No Individual Holds should be running.
            //var HMIData = db.tblhmiscreens.Where(m => m.MachineID == macid && m.isWorkInProgress == 3).ToList();
            //var HMIData = db.tblhmiscreens.Where(m => m.MachineID == macid && m.IsHold == 1).ToList();
            //foreach (var row in HMIData)
            //{
            //    Session["VError"] = "Please End Hold For WONo  " + row.Work_Order_No;
            //    return RedirectToAction("Index");
            //}

            //stage 2. Idle is running and u need to send data to view regarding that.
            var IdleToView = db.tbllivelossofentries.Where(m => m.MachineID == macid).OrderByDescending(m => m.LossID).FirstOrDefault();
            if (IdleToView != null) //implies idle is running
            {
                if (IdleToView.DoneWithRow == 0 && IdleToView.MessageCodeID != 999)
                {
                    int breakdowncode = Convert.ToInt32(IdleToView.MessageCodeID);
                    var DataToView = db.tbllossescodes.Where(m => m.IsDeleted == 0 && m.LossCodeID == breakdowncode).ToList();
                    ViewBag.Level = DataToView[0].LossCodesLevel;
                    ViewBag.BreakdownCode = DataToView[0].LossCode;
                    ViewBag.BreakdownId = DataToView[0].LossCodeID;
                    ViewBag.BreakdownStartTime = IdleToView.StartDateTime;
                    return View(DataToView);
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
                        ViewBag.BreakdownId = Bid;
                        ViewBag.Level = level;
                        ViewBag.breadScrum = losscode + "-->  ";
                        return View(level1Data);
                    }
                    ViewBag.Level = level + 1;
                    ViewBag.BreakdownId = Bid;
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
                        ViewBag.BreakdownId = Bid;
                        ViewBag.Level = level;
                        ViewBag.breadScrum = level1data + " --> " + losscode + " --> ";
                        return View(level2Data);
                    }
                    ViewBag.breadScrum = level1data + " --> " + losscode;
                    ViewBag.Level = level + 1;
                    ViewBag.BreakdownId = Bid;
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
                    ViewBag.BreakdownId = Bid;
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

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DownCodeEntry(tbllossescode tbdc, string EndBreakdown = null, int HiddenID = 0)
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

                tbllivelossofentry lossentry = new tbllivelossofentry();
                lossentry.StartDateTime = DateTime.Now;
                lossentry.EntryTime = DateTime.Now;
                lossentry.EndDateTime = DateTime.Now;
                lossentry.CorrectedDate = CorrectedDate;
                lossentry.MachineID = Convert.ToInt32(Session["MachineID"]);
                lossentry.Shift = Shift;
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
                db.tbllivelossofentries.Add(lossentry);
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
                tm.ColorCode = "yellow";
                tm.InsertedOn = DateTime.Now;
                tm.IsDeleted = 0;
                tm.Mode = "IDLE";
                tm.IsCompleted = 0;

                db.tbllivemodedbs.Add(tm);
                db.SaveChanges();
            }
            else if (HiddenID != 0 && string.IsNullOrEmpty(EndBreakdown) == false)
            {
                var tb = db.tbllivelossofentries.Where(m => m.MessageCodeID == HiddenID && m.MachineID == MachineID && m.DoneWithRow == 0).OrderByDescending(m => m.LossID).FirstOrDefault();
                tb.EndDateTime = DateTime.Now;
                tb.DoneWithRow = 1;
                tb.IsUpdate = 1;
                tb.IsScreen = 0;
                tb.IsStart = 0;
                tb.ForRefresh = 0;

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
                tmIDLE.ColorCode = "green";
                tmIDLE.CorrectedDate = CorrectedDate;
                tmIDLE.InsertedBy = Convert.ToInt32(Session["UserId"]);
                tmIDLE.InsertedOn = DateTime.Now;
                tmIDLE.IsCompleted = 0;
                tmIDLE.IsDeleted = 0;
                tmIDLE.MachineID = MachineID;
                tmIDLE.Mode = "PowerOn";
                tmIDLE.StartTime = DateTime.Now;

                db.tbllivemodedbs.Add(tmIDLE);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
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
            string CorrectedDate = null, Shift = null;
            string[] DateShiftValues = GetDateShift();
            CorrectedDate = DateShiftValues[1];
            Shift = DateShiftValues[0];
            //int handleidleReturnValue = HandleIdleManualWC();
            //if (handleidleReturnValue == 0)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //}

            var machinedispname = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == id).Select(m => m.MachineDispName).FirstOrDefault();
            ViewBag.macDispName = Convert.ToString(machinedispname);
            var vm = new HoldList();
            ViewBag.coretddt = CorrectedDate;
            //var idle = db.tbllossofentries.Include(t => t.machine_master).Include(t => t.message_code_master).Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate).ToList();
            //vm.HoldListDetailsWO = db.tblmanuallossofentries.Include(t => t.tblholdcode).Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate).ToList();
            vm.HoldListDetailsWC = db.tbllivelossofentries.Include(t => t.tbllossescode).Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate && m.DoneWithRow == 1).OrderByDescending(m => m.StartDateTime).ToList();

            #region another technique. didnot finish
            ////    HoldCodesList : HoldCodeID  RL1  RL2  RL3  IsWOLevel 
            //List<HoldCodesList> HoldCodesToView = new List<HoldCodesList>();
            //foreach (var row in WOLossDetails)
            //{
            //    int LevelCurrentCodeInt = row.MessageCodeID;
            //    string Level1CodeString = row.MessageCode;
            //    string Level2CodeString = null, Level3CodeString = null;

            //    var LossCodeDetails = db.tbllossescodes.Where(m => m.LossCodeID == LevelCurrentCodeInt).FirstOrDefault();

            //    int Level1CodeInt = 0;
            //    if (int.TryParse(Convert.ToString(LossCodeDetails.LossCodesLevel1ID), out Level1CodeInt))
            //    {
            //        Level2CodeString = db.tbllossescodes.Where(m => m.LossCodeID == Level1CodeInt).Select(m => m.LossCode).FirstOrDefault();
            //    }

            //    int Level2CodeInt = 0;
            //    if (int.TryParse(Convert.ToString(LossCodeDetails.LossCodesLevel2ID), out Level2CodeInt))
            //    {
            //        Level2CodeString = db.tbllossescodes.Where(m => m.LossCodeID == Level2CodeInt).Select(m => m.LossCode).FirstOrDefault();
            //    }

            //    HoldCodesToView.Add(new HoldCodesList { HoldCodeID = LevelCurrentCodeInt , RL1 = Level1CodeString , RL2 = Level2CodeString , RL3 = Level3CodeString ,  IsWOLevel = 1 });
            //}

            //var LossDataDetails = db.tbllossofentries.Where(m => m.MachineID == id && m.CorrectedDate == CorrectedDate).ToList();
            //foreach (var row in LossDataDetails)
            //{
            //    int LevelCurrentCodeInt = row.MessageCodeID;
            //    string Level1CodeString = row.MessageCode;
            //    string Level2CodeString = null, Level3CodeString = null;

            //    var LossCodeDetails = db.tbllossescodes.Where(m => m.LossCodeID == LevelCurrentCodeInt).FirstOrDefault();

            //    int Level1CodeInt = 0;
            //    if (int.TryParse(Convert.ToString(LossCodeDetails.LossCodesLevel1ID), out Level1CodeInt))
            //    {
            //        Level2CodeString = db.tbllossescodes.Where(m => m.LossCodeID == Level1CodeInt).Select(m => m.LossCode).FirstOrDefault();
            //    }

            //    int Level2CodeInt = 0;
            //    if (int.TryParse(Convert.ToString(LossCodeDetails.LossCodesLevel2ID), out Level2CodeInt))
            //    {
            //        Level2CodeString = db.tbllossescodes.Where(m => m.LossCodeID == Level2CodeInt).Select(m => m.LossCode).FirstOrDefault();
            //    }
            //    HoldCodesToView.Add(new HoldCodesList { HoldCodeID = LevelCurrentCodeInt, RL1 = Level1CodeString, RL2 = Level2CodeString, RL3 = Level3CodeString, IsWOLevel = 0 });
            //}
            #endregion

            //var HoldCodesToView = new HoldList() { HoldListDetailsWO = WOLossDetails, HoldListDetailsWC = LossDataDetails };
            return View(vm);
        }

        public ActionResult OverAllPartial(string HMIIDs)
        {
            //1) extract hmiids from string
            //2) Check if eligible for  PartialFinish (Eligible only if all are eligible)
            //3) Now Partial Finish all of them
            int MachineID = Convert.ToInt32(Session["MachineID"]);
            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

            //1)
            string[] HMIIDArray = HMIIDs.Split(',');
            List<string> HMIIDList = HMIIDs.Split(',').ToList();

            //2)
            int ExceptionHMIID = 0;
            foreach (var hmiid in HMIIDArray)
            {
                int hmiiid = Convert.ToInt32(hmiid);
                var HMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiiid).FirstOrDefault();
                //if (HMIData.isWorkInProgress == 3)
                if (HMIData.IsHold == 1)
                {
                    Session["VError"] = "End HOLD before Partial Finish";
                    return RedirectToAction("Index");
                }
                else if (HMIData.Date != null)
                {
                    int deliveredQty = 0;
                    if (int.TryParse(Convert.ToString(HMIData.Delivered_Qty), out deliveredQty))
                    {
                        int processed = 0;
                        int.TryParse(Convert.ToString(HMIData.ProcessQty), out processed);
                        if ((deliveredQty + processed) > Convert.ToInt32(HMIData.Target_Qty))
                        {
                            Session["VError"] = "DeliveredQty Must be less than Target for " + HMIData.Work_Order_No;
                            return RedirectToAction("Index");
                        }

                        #region 2017-02-07
                        else if ((deliveredQty + processed) == Convert.ToInt32(HMIData.Target_Qty))
                        {
                            //Check if highest of opno's for sibling wo's is eligible for JF, if Qty satisfies.
                            //1) if already lower op is jobfinished
                            //2) or  its in current HMIIDArray and valid for jf

                            string wono = HMIData.Work_Order_No;
                            string partno = HMIData.PartNo;
                            string opno = HMIData.OperationNo;
                            int opnoInt = Convert.ToInt32(HMIData.OperationNo);

                            //Gen , Seperated String from HMIIDArray
                            string hmiids = null;
                            for (int hmiidLooper = 0; hmiidLooper < HMIIDArray.Length; hmiidLooper++)
                            {
                                if (hmiids == null)
                                {
                                    string localhmiidString = HMIIDArray[hmiidLooper];
                                    if (ExceptionHMIID.ToString() != localhmiidString)
                                    {
                                        hmiids = HMIIDArray[hmiidLooper].ToString();
                                    }
                                }
                                else
                                {
                                    if (ExceptionHMIID.ToString() != HMIIDArray[hmiidLooper])
                                    {
                                        hmiids += "," + HMIIDArray[hmiidLooper].ToString();
                                    }
                                }
                            }
                            //Get hmiids (as comma seperated string) in ascending order based on wono,partno,Opno.
                            hmiids = GetOrderedHMIIDs(hmiids);
                            using (Tata.Models.mazakdaqEntities dbHMI = new Tata.Models.mazakdaqEntities())
                            {
                                string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + wono + "' and MaterialDesc = '" + partno + "'  and OperationNo != '" + opno + "' and IsCompleted = 0 order by OperationNo ";
                                var WIP1 = dbHMI.tblddls.SqlQuery(WIPQuery1).ToList();
                                foreach (var row in WIP1)
                                {
                                    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                                    string InnerOpNoString = Convert.ToString(row.OperationNo);
                                    bool IsInHMI = true;
                                    if (opnoInt > InnerOpNo)
                                    {
                                        string WIPQuery2 = @"SELECT * from tbllivehmiscreen where Work_Order_No = '" + wono + "' and PartNo = '" + partno + "'  and OperationNo = '" + InnerOpNo + "' and isWorkInProgress = 1 limit 1";
                                        var WIP2 = dbHMI.tbllivehmiscreens.SqlQuery(WIPQuery2).FirstOrDefault();
                                        if (WIP2 != null)
                                        {
                                            IsInHMI = true;
                                        }
                                        else
                                        {
                                            IsInHMI = false;
                                        }

                                        //We have to check for MultiWorkOrder.
                                        if (!IsInHMI)
                                        {
                                            string WIPQuery3 = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + wono + "' and PartNo = '" + partno + "'  and OperationNo = '" + InnerOpNo + "' and IsCompleted = 1 limit 1";
                                            var WIP3 = dbHMI.tbllivemultiwoselections.SqlQuery(WIPQuery3).FirstOrDefault();
                                            if (WIP3 != null)
                                            {

                                            }
                                            else
                                            {
                                                Session["VError"] = "Finish WorkOrder: " + wono + ": OpNo: " + InnerOpNo;
                                                return RedirectToAction("Index");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //else
                        //{ //Do Nothing
                        //}
                        #endregion
                    }
                    else
                    {
                        Session["VError"] = "Enter Delivered Quantity";
                        return RedirectToAction("Index");
                    }
                }
                //else if (HMIData.Date == null && HMIData.Work_Order_No == null && HMIData.PartNo == null && HMIData.OperationNo == null)
                else if (HMIData.Date == null)
                {// Do Nothing. Just to Skip our Extra Empty Row
                    if (ExceptionHMIID == 0)
                    {
                        ExceptionHMIID = Convert.ToInt32(HMIData.HMIID);
                    }
                    else
                    {
                        //Session["VError"] = "Please enter all Details Before PartialFinish is Clicked.";
                        Session["VError"] = "Please Start All WorkOrders Before PartialFinish";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    Session["VError"] = "Please Start All WorkOrders Before PartialFinish";
                    return RedirectToAction("Index");
                }
            }

            //Check if there are any row to partialFinish (Empty Screen)
            if (HMIIDList.Count == 1)
            {
                foreach (var hmiid in HMIIDArray)
                {
                    int hmiidi = Convert.ToInt32(hmiid);
                    if (hmiidi == ExceptionHMIID)
                    {
                        Session["VError"] = "There are no WorkOrder to Finish";
                        return RedirectToAction("Index");
                    }
                }
            }

            ////Check if there are any row to partialFinish (Empty Screen)
            //if (HMIIDList.Count == 1)
            //{
            //    foreach (var hmiid in HMIIDArray)
            //    {
            //        int hmiidi = Convert.ToInt32(hmiid);
            //        if (hmiidi == ExceptionHMIID)
            //        {
            //            Session["VError"] = "There are no WorkOrder to Finish";
            //            return RedirectToAction("Index");
            //        }
            //    }
            //}

            //Prevent JF is lower Opno is Not Elegible for JF
            //Check if all are eligable to JF, if not throw err.
            bool IsSequenceOK = true;
            foreach (var hmiid in HMIIDArray)
            {
                int hmiidi = Convert.ToInt32(hmiid);
                if (hmiidi != ExceptionHMIID)
                {
                    var HMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiidi).FirstOrDefault();
                    int DelivQty = 0, TargQty = 0, ProcessedQty = 0;

                    int.TryParse(HMIData.Target_Qty.ToString(), out TargQty);
                    int.TryParse(HMIData.Delivered_Qty.ToString(), out DelivQty);
                    int.TryParse(HMIData.ProcessQty.ToString(), out ProcessedQty);

                    if (TargQty >= (DelivQty + ProcessedQty))
                    {
                        string WoNo = HMIData.Work_Order_No;
                        string partNo = HMIData.PartNo;
                        int OpNo = Convert.ToInt32(HMIData.OperationNo);
                        foreach (var hmiid1 in HMIIDArray)
                        {
                            int hmiidiInner = Convert.ToInt32(hmiid1);
                            if (hmiidi != ExceptionHMIID)
                            {
                                using (mazakdaqEntities dbhmi = new mazakdaqEntities())
                                {
                                    var HMIDataInner = dbhmi.tbllivehmiscreens.Where(m => m.HMIID == hmiidiInner && m.Work_Order_No == WoNo && m.PartNo == partNo && m.HMIID != hmiidi).FirstOrDefault();
                                    if (HMIDataInner != null)
                                    {
                                        int opNoInner = Convert.ToInt32(HMIDataInner.OperationNo);
                                        if (opNoInner < OpNo)
                                        {
                                            int DelivQty1 = 0, TargQty1 = 0, ProcessedQty1 = 0;
                                            int.TryParse(HMIDataInner.Target_Qty.ToString(), out TargQty1);
                                            int.TryParse(HMIDataInner.Delivered_Qty.ToString(), out DelivQty1);
                                            int.TryParse(HMIDataInner.ProcessQty.ToString(), out ProcessedQty1);
                                            if (TargQty1 < (DelivQty + ProcessedQty))
                                            {
                                                Session["VError"] = "Click JobFinish, All WorkOrders have (Delivered + Processed) = Target.";
                                                IsSequenceOK = false;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!IsSequenceOK)
                        {
                            break;
                        }
                    }
                    else
                    {
                        IsSequenceOK = false;
                        break;
                    }
                }
            }

            if (!IsSequenceOK)
            {
                return RedirectToAction("Index");
            }

            //Check if all are eligable to JF, if so throw erro.
            bool AllREligableForJF = true;
            foreach (var hmiid in HMIIDArray)
            {
                int hmiidi = Convert.ToInt32(hmiid);
                if (hmiidi != ExceptionHMIID)
                {
                    var HMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiidi).FirstOrDefault();
                    int DelivQty = 0;
                    int TargQty = 0;
                    int ProcessedQty = 0;

                    int.TryParse(HMIData.Target_Qty.ToString(), out TargQty);
                    int.TryParse(HMIData.Delivered_Qty.ToString(), out DelivQty);
                    int.TryParse(HMIData.ProcessQty.ToString(), out ProcessedQty);

                    if (TargQty == (DelivQty + ProcessedQty))
                    {
                    }
                    else
                    {
                        AllREligableForJF = false;
                        break;
                    }
                }
            }
            if (AllREligableForJF)
            {
                Session["VError"] = "Click JobFinish, All WorkOrders have (Delivered + Processed) = Target.";
                return RedirectToAction("Index");
            }

            List<string> MacHierarchy = GetMacHierarchyData(MachineID);

            //Update ProcessedQty if WorkOrder Available in Different WorkCenter
            foreach (var hmiid in HMIIDArray)
            {
                int hmiidi = Convert.ToInt32(hmiid);
                if (hmiidi != ExceptionHMIID)
                {
                    var HMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiidi).FirstOrDefault();
                    int DelivQty = 0, TargQty = 0, ProcessedQty = 0;
                    int.TryParse(HMIData.Delivered_Qty.ToString(), out DelivQty);
                    int HMIMacID = HMIData.MachineID;

                    using (mazakdaqEntities dbsimilar = new mazakdaqEntities())
                    {
                        #region If Its as SingleWO
                        var SimilarWOData = dbsimilar.tbllivehmiscreens.Where(m => m.Work_Order_No == HMIData.Work_Order_No && m.OperationNo == HMIData.OperationNo && m.PartNo == HMIData.PartNo && m.MachineID != MachineID && m.isWorkInProgress == 2).ToList();
                        foreach (var row in SimilarWOData)
                        {
                            int InnerProcessed = row.ProcessQty;
                            int FinalProcessed = DelivQty + InnerProcessed;
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
                                Session["Error"] = " Same WorkOrder in Machine: " + MacHierarchy[3] + "->" + MacHierarchy[4] + " has ProcessedQty :" + InnerProcessed;
                                return RedirectToAction("Index");
                            }
                        }
                        #endregion

                        #region If its as MultiWO
                        var SimilarWODataMulti = dbsimilar.tbllivemultiwoselections.Where(m => m.WorkOrder == HMIData.Work_Order_No && m.OperationNo == HMIData.OperationNo && m.PartNo == HMIData.PartNo && m.HMIID != hmiidi && m.tbllivehmiscreen.isWorkInProgress == 2).ToList();
                        foreach (var row in SimilarWODataMulti)
                        {
                            //update only if its still in hmiscreen
                            int RowHMIID = Convert.ToInt32(row.HMIID);
                            var localhmiData = dbsimilar.tbllivehmiscreens.Find(RowHMIID);
                            int DelivQtyLocal = Convert.ToInt32(row.DeliveredQty);
                            int InnerProcessed = Convert.ToInt32(row.ProcessQty);
                            int FinalProcessed = DelivQtyLocal + InnerProcessed;
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
                                        localhmiData.ProcessQty += DelivQtyLocal;
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
                            //var InnerHMIDupData = dbsimilar.tblhmiscreens.Where(m => m.HMIID == InnerHMIID && m.HMIID != hmiidi).FirstOrDefault();
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

                }
            }

            //2017-01-09
            //3) 
            //I think you have to rearrange these HMIID's based on wono,partno,opno ascending.
            //Because they may select wo's 
            //var ddlDummy = db.tblddls.Where(m=> HMIIDList.Contains(m.))
            foreach (var hmiid in HMIIDArray)
            {
                int hmiidi = Convert.ToInt32(hmiid);
                if (hmiidi != ExceptionHMIID)
                {
                    var HMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiidi).FirstOrDefault();
                    int DelivQty = 0;
                    int TargQty = 0;
                    int ProcessedQty = 0;

                    int.TryParse(HMIData.Target_Qty.ToString(), out TargQty);
                    int.TryParse(HMIData.Delivered_Qty.ToString(), out DelivQty);
                    int.TryParse(HMIData.ProcessQty.ToString(), out ProcessedQty);

                    if (TargQty == (DelivQty + ProcessedQty))
                    {
                        string wono = HMIData.Work_Order_No;
                        string partno = HMIData.PartNo;
                        string opno = HMIData.OperationNo;
                        int opnoInt = Convert.ToInt32(HMIData.OperationNo);

                        //2017-01-19
                        //var ddldata1 = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno).FirstOrDefault();
                        //if (ddldata1 != null)
                        //{
                        //    int opLocal = 0;
                        //    int.TryParse(Convert.ToString(ddldata1.OperationNo), out opLocal);
                        //    if (opLocal < opnoInt)
                        //    {
                        //        if (ddldata1.IsCompleted == 0)
                        //        {
                        //            Session["VError"] = "First, JobFinish WONo :" + ddldata1.WorkOrder;
                        //            return RedirectToAction("Index");
                        //        }
                        //    }
                        //}

                        HMIData.Status = 2;
                        HMIData.isWorkInProgress = 1;

                        var ddldata = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno && m.OperationNo == opno).FirstOrDefault();
                        if (ddldata != null)
                        {
                            ddldata.IsCompleted = 1;
                            db.Entry(ddldata).State = System.Data.Entity.EntityState.Modified;
                        }
                    }
                    else if (TargQty > (DelivQty + ProcessedQty))
                    {
                        HMIData.Status = 2;
                        HMIData.isWorkInProgress = 0;
                    }
                    else
                    {
                        Session["VError"] = "Delivered + Processed Cannot be Greater than Target for :" + HMIData.Work_Order_No;
                        return RedirectToAction("Index");
                    }
                    HMIData.Time = DateTime.Now;
                    db.Entry(HMIData).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
                Session["isWorkOrder"] = 0;
            }

            //var HMIDataAll = from r in  ( from row in db.tblhmiscreens
            //                    where HMIIDList.Contains( row.HMIID.ToString() ) 
            //                    orderby row.Work_Order_No, row.PartNo , row.OperationNo
            //                    select row).ToList() );


            return RedirectToAction("Index");
        }
        public ActionResult OverAllFinish(string HMIIDs)
        {
            //1) extract hmiids from string
            //2) Check if eligible for JobFinish (Eligible only if all are eligible)
            //3) Now Job Finish all of them

            int machineID = Convert.ToInt32(Session["MachineID"]);
            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

            //1)
            string[] HMIIDArray = HMIIDs.Split(',');

            //2)
            int ExceptionHMIID = 0;
            foreach (var hmiid in HMIIDArray)
            {
                int hmiid1 = Convert.ToInt32(hmiid);
                var HMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid1).FirstOrDefault();

                //if (HMIData.isWorkInProgress == 3)
                if (HMIData.IsHold == 1)
                {
                    Session["VError"] = "End HOLD before Clicking Job Finish";
                    return RedirectToAction("Index");
                }
                else if (HMIData.Date != null)
                {
                    int deliveredQty = 0, processQty = 0;
                    int.TryParse(Convert.ToString(HMIData.ProcessQty), out processQty);
                    if (int.TryParse(Convert.ToString(HMIData.Delivered_Qty), out deliveredQty))
                    {
                        if ((deliveredQty + processQty) == Convert.ToInt32(HMIData.Target_Qty))
                        {
                        }
                        else
                        {
                            Session["VError"] = "DeliveredQty Must be equal to Target Qty";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        Session["VError"] = "Enter Delivered Quantity";
                        return RedirectToAction("Index");
                    }
                }
                //else if (HMIData.Date == null && HMIData.Work_Order_No == null && HMIData.PartNo == null && HMIData.OperationNo == null)
                else if (HMIData.Date == null)
                {// Do Nothing. Just to Skip our Extra Empty Row
                    if (ExceptionHMIID == 0)
                    {
                        ExceptionHMIID = Convert.ToInt32(HMIData.HMIID);
                    }
                    else
                    {
                        Session["VError"] = "Please enter all Details Before StartAll is Clicked.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    Session["VError"] = "Please Start All WorkOrders Before JobFinish";
                    return RedirectToAction("Index");
                }
            }

            //Check if its  Empty Screen and JobFinish is clicked.
            if (HMIIDArray.Length == 1)
            {
                foreach (var hmiid in HMIIDArray)
                {
                    int hmiidi = Convert.ToInt32(hmiid);
                    if (hmiidi == ExceptionHMIID)
                    {
                        Session["VError"] = "There are no WorkOrder to Finish";
                        return RedirectToAction("Index");
                    }
                }
            }


            //3)
            //foreach (var hmiid in HMIIDArray)
            //{
            //    int hmiid1 = Convert.ToInt32(hmiid);
            //    if (hmiid1 != ExceptionHMIID)
            //    {
            //        var HMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid1).FirstOrDefault();

            //        string wono = HMIData.Work_Order_No;
            //        string partno = HMIData.PartNo;
            //        string opno = HMIData.OperationNo;
            //        var ddldata = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno && m.OperationNo == opno).FirstOrDefault();
            //        if (ddldata != null)
            //        {
            //            ddldata.IsCompleted = 1;
            //            db.Entry(ddldata).State = System.Data.Entity.EntityState.Modified;
            //        }

            //        HMIData.Status = 2;
            //        HMIData.isWorkInProgress = 1;
            //        HMIData.Time = DateTime.Now;
            //        db.Entry(HMIData).State = System.Data.Entity.EntityState.Modified;
            //        db.SaveChanges();
            //    }
            //}

            //Gen , Seperated String from HMIIDArray
            string hmiids = null;
            for (int hmiid = 0; hmiid < HMIIDArray.Length; hmiid++)
            {
                if (hmiids == null)
                {
                    string localhmiidString = HMIIDArray[hmiid];
                    if (ExceptionHMIID.ToString() != localhmiidString)
                    {
                        hmiids = HMIIDArray[hmiid].ToString();
                    }
                }
                else
                {
                    if (ExceptionHMIID.ToString() != HMIIDArray[hmiid])
                    {
                        hmiids += "," + HMIIDArray[hmiid].ToString();
                    }
                }
            }

            //Get hmiids (as comma seperated string) in ascending order based on wono,partno,Opno.
            hmiids = GetOrderedHMIIDs(hmiids);


            // this and one below , both were there so 2017-02-03 //string WIPQueryOuter = @"SELECT * from tblhmiscreen where HMIID IN ( ' " + HMIIDArray + " ' ) order by  Work_Order_No,PartNo,OperationNo ; ";
            string WIPQueryOuter = @"SELECT * from tbllivehmiscreen where HMIID IN ( " + hmiids + " ) order by  Work_Order_No,PartNo,OperationNo ; ";
            var WIPOuter = db.tbllivehmiscreens.SqlQuery(WIPQueryOuter).ToList();

            foreach (var rowOuter in WIPOuter)
            {
                using (mazakdaqEntities dbsimilar = new mazakdaqEntities())
                {
                    int HMIId = rowOuter.HMIID;
                    #region If its as SingleWO
                    var SimilarWOData = dbsimilar.tbllivehmiscreens.Where(m => m.Work_Order_No == rowOuter.Work_Order_No && m.OperationNo == rowOuter.OperationNo && m.PartNo == rowOuter.PartNo && m.MachineID != machineID && m.isWorkInProgress == 2).FirstOrDefault();
                    if (SimilarWOData != null)
                    {
                        int InnerMacID = Convert.ToInt32(dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == SimilarWOData.HMIID).Select(m => m.MachineID).FirstOrDefault());
                        var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());

                        Session["Error"] = " Same WorkOrder in Machine: " + MacDispName + " , So you cannot JobFinish ";
                        return RedirectToAction("Index");
                    }
                    #endregion

                    #region If its as MultiWO
                    var SimilarWODataMulti = dbsimilar.tbllivemultiwoselections.Where(m => m.WorkOrder == rowOuter.Work_Order_No && m.OperationNo == rowOuter.OperationNo && m.PartNo == rowOuter.PartNo && m.HMIID != HMIId && m.tbllivehmiscreen.isWorkInProgress == 2).FirstOrDefault();
                    if (SimilarWODataMulti != null)
                    {
                        int InnerHMIID = (int)SimilarWODataMulti.HMIID;
                        int InnerMacID = Convert.ToInt32(dbsimilar.tbllivehmiscreens.Where(m => m.HMIID == InnerHMIID).Select(m => m.MachineID).FirstOrDefault());
                        var MacDispName = Convert.ToString(dbsimilar.tblmachinedetails.Where(m => m.MachineID == InnerMacID).Select(m => m.MachineDispName).FirstOrDefault());

                        Session["Error"] = " Same WorkOrder in Machine: " + MacDispName + " , So you cannot JobFinish ";
                        return RedirectToAction("Index");
                    }
                    #endregion
                }
            }

            #region 2017-07-01
            foreach (var rowOuter in WIPOuter)
            {
                int hmiid1 = Convert.ToInt32(rowOuter.HMIID);
                string woNo = Convert.ToString(rowOuter.Work_Order_No);
                string opNo = Convert.ToString(rowOuter.OperationNo);
                string partNo = Convert.ToString(rowOuter.PartNo);

                //Logic to check sequence of JF Based on WONo, PartNo and OpNo.
                int OperationNoInt = Convert.ToInt32(opNo);

                using (Tata.Models.mazakdaqEntities dbHMI = new Tata.Models.mazakdaqEntities())
                {
                    string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + woNo + "' and MaterialDesc = '" + partNo + "'  and OperationNo != '" + opNo + "' and IsCompleted = 0 order by OperationNo ";
                    var WIP1 = dbHMI.tblddls.SqlQuery(WIPQuery1).ToList();
                    foreach (var row in WIP1)
                    {
                        int InnerOpNo = Convert.ToInt32(row.OperationNo);
                        string ddlopno = row.OperationNo;
                        string InnerOpNoString = Convert.ToString(row.OperationNo);
                        if (hmiids.Contains(InnerOpNoString))
                        { }
                        else
                        {
                            if (OperationNoInt > InnerOpNo)
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
                                            //tblh.HMIID = ;
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
                                        //tblh.HMIID = ;
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
            }
            #endregion

            foreach (var rowOuter in WIPOuter)
            {
                int hmiid1 = Convert.ToInt32(rowOuter.HMIID);
                #region 2017-02-07

                string woNo = Convert.ToString(rowOuter.Work_Order_No);
                string opNo = Convert.ToString(rowOuter.OperationNo);
                string partNo = Convert.ToString(rowOuter.PartNo);

                //Logic to check sequence of JF Based on WONo, PartNo and OpNo.
                int OperationNoInt = Convert.ToInt32(opNo);

                using (Tata.Models.mazakdaqEntities dbHMI = new Tata.Models.mazakdaqEntities())
                {

                    //Commmented on 2017-06-01 . Just the DDL List will do.

                    ////This may cause the problem due to multiple times PartialFinish not if use the new query 2017-02-03 . Helps with Manual Entry.
                    ////string WIPQuery = @"SELECT * from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' order by OperationNo desc ";
                    //string WIPQuery = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + woNo + "' and PartNo = '" + partNo + "' and OperationNo != '" + opNo + "' and IsMultiWO = 0 and DDLWokrCentre is null order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo  ) order by OperationNo;";
                    //var WIP = dbHMI.tblhmiscreens.SqlQuery(WIPQuery).ToList();
                    //bool IsItWrong = false;
                    //foreach (var row in WIP)
                    //{
                    //    int InnerOpNo = Convert.ToInt32(row.OperationNo);
                    //    if (OperationNoInt > InnerOpNo)
                    //    {
                    //        if (row.isWorkInProgress != 1) //=> lower OpNo is not Finished.
                    //        {
                    //            Session["VError"] = " Finish WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                    //            //return RedirectToAction("Index");
                    //        }
                    //    }
                    //}

                    //?2017-01-19

                    //if (rowOuter.IsMultiWO == 0) //Its a single WO
                    //{

                    //if its  finished in MultiWorkOrder then we can catch it here. (Above code can help with Manual Entry)
                    string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + woNo + "' and MaterialDesc = '" + partNo + "'  and OperationNo != '" + opNo + "' and IsCompleted = 0 order by OperationNo ";
                    var WIP1 = dbHMI.tblddls.SqlQuery(WIPQuery1).ToList();
                    foreach (var row in WIP1)
                    {
                        int InnerOpNo = Convert.ToInt32(row.OperationNo);
                        string InnerOpNoString = Convert.ToString(row.OperationNo);
                        if (hmiids.Contains(InnerOpNoString))
                        { }
                        else
                        {
                            if (OperationNoInt > InnerOpNo)
                            {
                                if (row.IsCompleted != 1) //=> lower OpNo is not Finished.
                                {
                                    Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                                    return RedirectToAction("Index");
                                }
                                else
                                {
                                    Session["VError"] = null;
                                }
                            }
                        }
                    }

                }
                //there are no MultiWOs in Manual HMI Screen. // 2017-06-01 But they may be , because of Normal HMIScreen's MultiWO type

                //else  //Its a Multi-WorkOrder
                //{
                //    var MultiWorkOrderData = db.tbl_multiwoselection.Where(m => m.HMIID == hmiid1).FirstOrDefault();
                //    if (MultiWorkOrderData != null)
                //    {
                //        string multiWO = MultiWorkOrderData.WorkOrder;
                //        string multiPart = MultiWorkOrderData.PartNo;
                //        string multiOP = MultiWorkOrderData.OperationNo;

                //        string WIPQuery1 = @"SELECT * from tblddl where WorkOrder = '" + multiWO + "' and MaterialDesc = '" + multiPart + "'  and OperationNo != '" + multiOP + "' order by OperationNo ";
                //        var WIP1 = dbHMI.tblddls.SqlQuery(WIPQuery1).ToList();
                //        foreach (var row in WIP1)
                //        {
                //            int InnerOpNo = Convert.ToInt32(row.OperationNo);
                //            string InnerOpNoString = Convert.ToString(row.OperationNo);
                //            if (hmiids.Contains(InnerOpNoString))
                //            { }
                //            else
                //            {
                //                if (OperationNoInt > InnerOpNo)
                //                {
                //                    if (row.IsCompleted != 1) //=> lower OpNo is not Finished.
                //                    {
                //                        Session["VError"] = " Finish WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + InnerOpNo;
                //                        return RedirectToAction("Index");
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}

                #endregion

                var HMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid1).FirstOrDefault();
                string wono = HMIData.Work_Order_No;
                string partno = HMIData.PartNo;
                string opno = HMIData.OperationNo;
                var ddldata = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno && m.OperationNo == opno).FirstOrDefault();
                if (ddldata != null)
                {
                    ddldata.IsCompleted = 1;
                    db.Entry(ddldata).State = System.Data.Entity.EntityState.Modified;
                }

                HMIData.Status = 2;
                HMIData.isWorkInProgress = 1;
                HMIData.SplitWO = "No";
                HMIData.Time = DateTime.Now;
                db.Entry(HMIData).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                // }
            }
            Session["isWorkOrder"] = 0;
            return RedirectToAction("Index");
        }

        public ActionResult DDLList(int DDLID = 0, string MacInvNo = null, int ToHMI = 0 , int take = 10 , int skip = 1)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            Session["MacINVNo"] = MacInvNo;
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int opID = Convert.ToInt32(Session["UserId"]);
            string CorrectedDate = null;
            string[] DateShiftDetails = GetDateShift();
            CorrectedDate = DateShiftDetails[1];
            int machineId = Convert.ToInt32(Session["MachineID"]);
            ViewBag.opid = Session["opid"];
            ViewBag.mcnid = machineId;
            ViewBag.coretddt = CorrectedDate;

            //int handleidleReturnValue = HandleIdle();
            //if (handleidleReturnValue == 0)
            //{
            //    return RedirectToAction("DownCodeEntry");
            //}

            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

            //1st check for eligibility for wo,part,opno sequence condition.
            bool isValid = true, IsInHMI = true;
            string IssueMsg = null;
            if (DDLID != 0)
            {
                var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                String SplitWO = ddldata.SplitWO;
                String WONo = ddldata.WorkOrder;
                String Part = ddldata.MaterialDesc;
                String Operation = ddldata.OperationNo;
                int opNo = Convert.ToInt32(Operation);
                //var DuplicateHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && (m.isWorkInProgress == 2 || m.isWorkInProgress == 3)).FirstOrDefault();
                var DuplicateHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress == 2 && m.MachineID == machineId).FirstOrDefault();
                if (DuplicateHMIdata != null)
                {
                    isValid = false;
                    IssueMsg = "This Below WorkOrder, WONo: " + WONo + " PartNo: " + Part + " OperationNo: " + Operation + " Exists in PartEntry Screen";
                }

                if (TempData["Err"] != null)
                {
                    Session["VError"] = TempData["Err"];
                    TempData["Err"] = null;
                    isValid = false;
                }
                else
                {
                    #region 2017-02-07
                    //if (isValid)
                    //{
                    //    var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONo && m.MaterialDesc == Part && m.OperationNo != Operation).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                    //    foreach (var row in Siblingddldata)
                    //    {
                    //        IsInHMI = true; //reinitialize
                    //        int localOPNo = Convert.ToInt32(row.OperationNo);
                    //        string localOPNoString = Convert.ToString(row.OperationNo);
                    //        if (localOPNo < Convert.ToInt32(Operation))
                    //        {
                    //            #region //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                    //            //which case is valid.
                    //            var SiblingHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == localOPNoString).OrderByDescending(m => m.HMIID).FirstOrDefault();
                    //            if (SiblingHMIdata == null)
                    //            {
                    //                Session["VError"] = "Please Select Below WorkOrder, WONo: " + WONo + " PartNo: " + Part + " OperationNo: " + localOPNo;
                    //                IsInHMI = false;
                    //                //break;
                    //                isValid = false;
                    //            }
                    //            else
                    //            {
                    //                if (SiblingHMIdata.Date == null) //=> lower OpNo is not submitted.
                    //                {
                    //                    Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                    //                    //return RedirectToAction("Index");
                    //                    IsInHMI = false;
                    //                    isValid = false;
                    //                    break;
                    //                }
                    //            }
                    //            #endregion

                    //            if (!IsInHMI)
                    //            {
                    //                #region //also check in MultiWO table
                    //                string WIPQueryMultiWO = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + localOPNo + "' order by MultiWOID desc limit 1 ";
                    //                var WIPMWO = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWO).ToList();

                    //                if (WIPMWO.Count == 0)
                    //                {
                    //                    Session["VError"] = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                    //                    //return RedirectToAction("Index");
                    //                    IsInHMI = false;
                    //                    isValid = false;
                    //                    break;
                    //                }

                    //                foreach (var rowHMI in WIPMWO)
                    //                {
                    //                    int hmiid = Convert.ToInt32(rowHMI.HMIID);
                    //                    var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                    //                    if (MWOHMIData != null) //obviously != 0
                    //                    {
                    //                        if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                    //                        {
                    //                            Session["VError"] = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                    //                            //return RedirectToAction("Index");
                    //                            isValid = false;
                    //                            break;
                    //                        }
                    //                        else
                    //                        {
                    //                            IsInHMI = true;
                    //                        }
                    //                    }
                    //                }
                    //                #endregion
                    //            }
                    //        }
                    //    }

                    //    ///to Catch those Manual WorkOrders 
                    //    string WIPQuery1 = @"SELECT * from tblhmiscreen where  HMIID IN ( SELECT Max(HMIID) from tblhmiscreen where  HMIID IN  ( SELECT HMIID from tblhmiscreen where Work_Order_No = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo != '" + opNo + "' and  IsMultiWO = 0 and DDLWokrCentre is null order by HMIID desc ) group by Work_Order_No,PartNo,OperationNo ) order by OperationNo ;";
                    //    var WIPDDL1 = db.tblhmiscreens.SqlQuery(WIPQuery1).ToList();
                    //    foreach (var row in WIPDDL1)
                    //    {
                    //        int InnerOpNo = Convert.ToInt32(row.OperationNo);
                    //        if (InnerOpNo < Convert.ToInt32(Operation))
                    //        {
                    //            string WIPQueryHMI = @"SELECT * from tblhmiscreen where Work_Order_No = '" + WONo + "' and PartNo = '" + Part + "' and OperationNo = '" + InnerOpNo + "' order by HMIID desc limit 1 ";
                    //            var WIP1 = db.tblhmiscreens.SqlQuery(WIPQueryHMI).ToList();
                    //            if (WIP1.Count == 0)
                    //            {
                    //                Session["VError"] = " Select & Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                    //                //return RedirectToAction("Index");
                    //                isValid = false;
                    //            }
                    //            foreach (var rowHMI in WIP1)
                    //            {
                    //                if (rowHMI.Date == null) //=> lower OpNo is not submitted.
                    //                {
                    //                    Session["VError"] = " Start WONo: " + row.Work_Order_No + " and PartNo: " + row.PartNo + " and OperationNo: " + InnerOpNo;
                    //                    //return RedirectToAction("Index");
                    //                    isValid = false;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    #endregion
                }
            }
            //if (isValid && IsInHMI) //2017-02-07
            if (isValid) //2017-02-04
            {
                //Step 1: If DDLID is given then insert that data into HMIScreen table , take its HMIID and redirect to Index 
                if (DDLID != 0)
                {
                    var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    String SplitWO = ddldata.SplitWO;
                    String WONo = ddldata.WorkOrder;
                    String Part = ddldata.MaterialDesc;
                    String Operation = ddldata.OperationNo;

                    int PrvProcessQty = 0, PrvDeliveredQty = 0;
                    int isHold = 0;
                    //var getProcessQty = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();
                    //if (getProcessQty.Count > 0)
                    //{
                    //    PrvProcessQty = Convert.ToInt32(getProcessQty[0].ProcessQty);
                    //    PrvDeliveredQty = Convert.ToInt32(getProcessQty[0].Delivered_Qty);
                    //    isHold = getProcessQty[0].IsHold;
                    //    isHold = isHold == 2 ? 0 : isHold;
                    //}

                    #region new code

                    //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                    int isHMIFirst = 2; //default NO History for that wo,pn,on

                    var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == WONo && m.PartNo == Part && m.OperationNo == Operation && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                    var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();

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

                        isHold = mulitwoData[0].tbllivehmiscreen.IsHold;
                        isHold = isHold == 2 ? 0 : isHold;

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

                        isHold = hmiData[0].IsHold;
                        isHold = isHold == 2 ? 0 : isHold;
                    }
                    else
                    {
                        //no previous delivered or processed qty so Do Nothing.
                    }

                    #endregion

                    int TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);

                    //var hmidata = db.tblhmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();
                    //hmidata.Date = DateTime.Now;

                    //int Hmiid1 = hmidata.HMIID;
                    //delete if any IsSubmit = 0 for this hmiid.
                    //db.tbl_multiwoselection.RemoveRange(db.tbl_multiwoselection.Where(x => x.HMIID == Hmiid1 && x.IsSubmit == 0));
                    //db.SaveChanges();

                    //2017-01-03 Just insert new Row.
                    string OpName = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.Status == 0).Select(m => m.OperatorDet).FirstOrDefault();
                    string Shift = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.Status == 0).Select(m => m.Shift).FirstOrDefault();

                    tbllivehmiscreen hmidata = new tbllivehmiscreen();
                    
                    hmidata.OperationNo = ddldata.OperationNo;
                    hmidata.PartNo = ddldata.MaterialDesc;
                    hmidata.PEStartTime = DateTime.Now;
                    hmidata.CorrectedDate = CorrectedDate;
                    hmidata.Shift = Shift;
                    hmidata.OperatiorID = opID;
                    hmidata.OperatorDet = OpName;
                    hmidata.Project = ddldata.Project;
                    hmidata.Target_Qty = Convert.ToInt32(ddldata.TargetQty);
                    hmidata.Work_Order_No = ddldata.WorkOrder;
                    hmidata.ProcessQty = TotalProcessQty;
                    hmidata.Delivered_Qty = 0;
                    hmidata.DDLWokrCentre = ddldata.WorkCenter;
                    hmidata.MachineID = machineId;
                    hmidata.IsMultiWO = 0;
                    hmidata.IsHold = isHold;
                    hmidata.Status = 0;
                    hmidata.isWorkInProgress = 2;
                   // hmidata.HMIID = ;
                    int ReworkOrder = 0;
                    string ReworkOrderString = Convert.ToString(Session["isWorkOrder"]);
                    if (int.TryParse(ReworkOrderString, out ReworkOrder))
                    {
                        if (ReworkOrderString == "1")
                        {
                            hmidata.isWorkOrder = 1;
                        }
                        else
                        {
                            hmidata.isWorkOrder = 0;
                        }
                    }

                    db.tbllivehmiscreens.Add(hmidata);
                    db.SaveChanges();
                    Session["FromDDL"] = 1;
                    Session["SubmitClicked"] = 0;
                    // return RedirectToAction("Index", Hmiid);
                    return RedirectToAction("Index");
                }
            }
            else
            {
                Session["Error"] = TempData["Err"];
            }

            //if (Convert.ToString(TempData["Err"]) != null)
            //{
            //    Session["Error"] = Convert.ToString(TempData["Err"]);
            //}


            //Step2: If DDLID == 0 and ToHMI == 1 then go to HMIScreen "Index" With Normal HMI Flow
            // This means Operator opted for Manual Entry
            if (DDLID == 0 && ToHMI == 1)
            {
                //2017-01-03 Dont do any thing just redirect.
                #region Not Using
                //var hmidata = db.tblhmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();

                ////int Hmiid = hmidata.HMIID;
                //////delete if any IsSubmit = 0 for this hmiid.
                ////db.tbl_multiwoselection.RemoveRange(db.tbl_multiwoselection.Where(x => x.HMIID == Hmiid && x.IsSubmit == 0));
                ////db.SaveChanges();
                //if (hmidata != null)
                //{
                //    hmidata.OperationNo = null;
                //    hmidata.PartNo = null;
                //    hmidata.Project = null;
                //    hmidata.Target_Qty = null;
                //    hmidata.Work_Order_No = null;
                //    hmidata.ProcessQty = 0;
                //    hmidata.DDLWokrCentre = null;
                //    hmidata.IsMultiWO = 0;
                //    db.Entry(hmidata).State = System.Data.Entity.EntityState.Modified;
                //    db.SaveChanges();
                //    Session["FromDDL"] = 2;
                //}
                #endregion

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
                macList.AddRange(db.tblmachinedetails.Where(m => m.CellID == cellid && m.IsDeleted == 0 && m.ManualWCID == null).Select(m => m.MachineInvNo).ToList());
                macList.AddRange(db.tblmachinedetails.Where(m => m.ShopID == shopid && m.CellID != cellid && m.IsDeleted == 0 && m.ManualWCID == null).Select(m => m.MachineInvNo).ToList());

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
                                          where row.ShopID == shopid && row.IsDeleted == 0 && row.ManualWCID == null && row.CellID.Equals(null)
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
                                              where row.PlantID == plantid && row.IsDeleted == 0 && row.ManualWCID == null && row.ShopID.Equals(null) && row.CellID.Equals(null)
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
                string machineParentID = Convert.ToString(machinedata.ManualWCID);

                if (machineParentID != null)
                {
                    int macParentIdInt = 0;
                    int.TryParse(machineParentID, out macParentIdInt);
                    var macInvdata = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == macParentIdInt).FirstOrDefault();
                    machineInvNo = macInvdata.MachineInvNo;
                }

            }
            else
            {
                machineInvNo = MacInvNo;
            }

            //ViewBag.MacInvNo = machineInvNo;
            List<tblddl> ddlDataList = new List<tblddl>();
            //string WIPQuery = @"SELECT * from tblhmiscreen where isWorkInProgress = 0 and HMIID IN ( SELECT HMIID from tblhmiscreen where MachineID = " + machineId + " group by Work_Order_No,PartNo,OperationNo order by HMIID desc ) ";
            //2017-01-13
         
            string WIPQuery = @"SELECT * from tbllivehmiscreen where isWorkInProgress = 0 and HMIID IN ( SELECT HMIID from tblhmiscreen where MachineID = " + machineId + " group by Work_Order_No,PartNo,OperationNo order by HMIID desc )";

            var WIP = db.tbllivehmiscreens.SqlQuery(WIPQuery).ToList();
            
            List<string> ExceptionDDLs = new List<string>();
            var ddldata1 = db.tblddls.Where(m => m.WorkCenter == machineInvNo && m.IsCompleted == 0).ToList();
            if (ddldata1 != null)
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

            //2017-01-13
            //if WO is not partialFinished(Not even once) but already in hmiscreen , then you don't have to show them in DDLList

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
                            "from tblddl WHERE WorkCenter = '" + machineInvNo + "' AND IsCompleted = 0 AND DDLID NOT IN (" + ExceptionDDLsArray + ")" +
                            "order by DaysAgeing = '', Convert(DaysAgeing , SIGNED INTEGER) asc ,FlagRushInd = '',FlagRushInd = 0 ,Convert(FlagRushInd , SIGNED INTEGER) asc  , MADDateInd = '' , MADDateInd asc , MADDate asc";
            //"order by DaysAgeing = \"\", DaysAgeing asc ,FlagRushInd = \"\",FlagRushInd = 0 ,FlagRushInd asc  , MADDateInd = \"\" , MADDateInd asc , MADDate asc";

            ddlDataList.AddRange(db.tblddls.SqlQuery(Query).ToList());
            ViewBag.MacInvNo = machineInvNo;

            CommonFunction cFunc = new CommonFunction();
            TakeSkip objPag = new TakeSkip();
            objPag = cFunc.Pagination(take, skip);

            var items = ddlDataList.ToList();

            //var items = ddlDataList.Skip(objPag.skip).Take(objPag.take).ToList();

            //var WIP = db.tblddls.Where(m => m.WorkCenter == machineInvNo && m.IsCompleted == 0).ToList();
            if (ddlDataList.Count != 0)
            {
                return View(items);
            }
            else
            {
                return View();
            }
        }
        [HttpPost]
        public ActionResult DDLList(string data1)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            List<int> data = new List<int>();
            data = JsonConvert.DeserializeObject<List<int>>(data1);

            if (IDLEorGenericWorkisON())
            {
                Session["Error"] = "Please End IDLE/GenericWork Before Selecting New Work Orders";
                return RedirectToAction("Index");
            }

            string[] DateShiftValues = GetDateShift();
            string CorrectedDate = DateShiftValues[1];
            int WOCount = data.Count;
            int machineId = Convert.ToInt32(Session["MachineID"]);

            string OpName = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.Status == 0).Select(m => m.OperatorDet).FirstOrDefault();
            string Shift = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.Status == 0).Select(m => m.Shift).FirstOrDefault();

            int invalidDDLID = 0;
            if (WOCount == 1)
            {
                int DDLID = data.First();

                //1st check for eligibility for wo,part,opno sequence condition.
                bool isValid = true, IsInHMI = true;
                string IssueMsg = null;

                if (DDLID != 0)
                {
                    var ddldataInner = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    String SplitWOInner = ddldataInner.SplitWO;
                    String WONoInner = ddldataInner.WorkOrder;
                    String PartInner = ddldataInner.MaterialDesc;
                    String OperationInner = ddldataInner.OperationNo;

                    //var DuplicateHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == OperationInner && (m.isWorkInProgress == 2 || m.isWorkInProgress == 3)).FirstOrDefault();
                    var DuplicateHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == OperationInner && m.isWorkInProgress == 2 && m.MachineID == machineId).FirstOrDefault();
                    if (DuplicateHMIdata != null)
                    {
                        isValid = false;
                        IssueMsg = "This Below WorkOrder, WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + OperationInner + " Exists in PartEntry Screen";
                    }

                    #region 2017-02-07
                    if (isValid)
                    {
                        var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONoInner && m.MaterialDesc == PartInner && m.OperationNo != OperationInner && m.IsCompleted == 0).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                        foreach (var row in Siblingddldata)
                        {
                            int localOPNo = Convert.ToInt32(row.OperationNo);
                            string localOPNoString = Convert.ToString(row.OperationNo);
                            if (localOPNo < Convert.ToInt32(OperationInner))
                            {
                                IsInHMI = true; //reinitialize
                                //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                                //which case is valid.
                                var SiblingHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == localOPNoString).FirstOrDefault();
                                //var SiblingHMIdatahistorian = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == localOPNoString).FirstOrDefault(); //added by Ashok
                                if (SiblingHMIdata == null)// || SiblingHMIdatahistorian==null) //its not in hmi Screen as Individual WO so Error
                                {
                                    invalidDDLID = ddldataInner.DDLID;
                                    //IssueMsg = "Please Select Below WorkOrder , WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + localOPNo;
                                    IsInHMI = false;
                                    isValid = false;
                                    //break;
                                }
                                else
                                {
                                    if (SiblingHMIdata.Date == null )//|| SiblingHMIdatahistorian.Date == null) //=> lower OpNo is not submitted.
                                    {
                                        IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                        //return RedirectToAction("Index");
                                        IsInHMI = false;
                                        isValid = false;
                                        break;
                                    }
                                }
                                if (!IsInHMI)
                                {
                                    //also check in MultiWO table
                                    string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + WONoInner + "' and PartNo = '" + PartInner + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                    var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();

                                    if (WIPMWO.Count == 0)
                                    {
                                        IssueMsg = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                        //return RedirectToAction("Index");
                                        isValid = false;
                                        IsInHMI = false;
                                        break;
                                    }

                                    foreach (var rowHMI in WIPMWO)
                                    {
                                        int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                        var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                        if (MWOHMIData != null)
                                        {
                                            if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                            {
                                                IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                //return RedirectToAction("Index");
                                                isValid = false;
                                                break;
                                            }
                                            else
                                            {
                                                IsInHMI = true;
                                            }
                                        }
                                    }
                                }

                            }
                        }
                        //if is's a Manual Entry then u r allowing so directly check in HMIScreen. Do This Please.

                    }
                    #endregion

                }
                //if (isValid && IsInHMI)
                if (isValid)
                {
                    #region StartWO

                    var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    String SplitWO = ddldata.SplitWO;
                    String WONo = ddldata.WorkOrder;
                    String Part = ddldata.MaterialDesc;
                    String Operation = ddldata.OperationNo;

                    int PrvProcessQty = 0, PrvDeliveredQty = 0, TotalProcessQty = 0, ishold = 0;
                    //var getProcessQty = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == Part && m.OperationNo == Operation && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();
                    //if (getProcessQty.Count > 0)
                    //{
                    //    string delivString = Convert.ToString(getProcessQty[0].Delivered_Qty);
                    //    int.TryParse(delivString, out PrvDeliveredQty);

                    //    string processString = Convert.ToString(getProcessQty[0].ProcessQty);
                    //    int.TryParse(processString, out PrvProcessQty);

                    //    TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);

                    //    ishold = getProcessQty[0].IsHold;
                    //    ishold = ishold == 2 ? 0 : ishold;
                    //}

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

                    //var hmidata = db.tblhmiscreens.Where(m => m.MachineID == machineId && m.isWorkInProgress == 2).OrderByDescending(m => m.HMIID).FirstOrDefault();
                    //hmidata.Date = DateTime.Now;
                    //Hmiid = hmidata.HMIID;

                    ////delete if any IsSubmit = 0 for this hmiid.
                    //db.tbl_multiwoselection.RemoveRange(db.tbl_multiwoselection.Where(x => x.HMIID == Hmiid && x.IsSubmit == 0));
                    //db.SaveChanges();

                    tbllivehmiscreen hmidata = new tbllivehmiscreen();
                    //
                    hmidata.PEStartTime = DateTime.Now;
                    hmidata.OperationNo = ddldata.OperationNo;
                    hmidata.PartNo = ddldata.MaterialDesc;
                    hmidata.Project = ddldata.Project;
                    hmidata.CorrectedDate = CorrectedDate;
                    hmidata.Shift = Shift;
                    hmidata.Status = 0;
                    hmidata.OperatiorID = Convert.ToInt32(Session["UserID"]);
                    hmidata.OperatorDet = OpName;
                    hmidata.Target_Qty = Convert.ToInt32(ddldata.TargetQty);
                    hmidata.Work_Order_No = ddldata.WorkOrder;
                    hmidata.ProcessQty = TotalProcessQty;
                    hmidata.Delivered_Qty = 0;
                    hmidata.IsMultiWO = 0;
                    hmidata.isWorkInProgress = 2;
                    hmidata.IsHold = ishold;
                    hmidata.DDLWokrCentre = ddldata.WorkCenter;
                    hmidata.MachineID = machineId;
                    int ReworkOrder = 0;
                    string ReworkOrderString = Convert.ToString(Session["isWorkOrder"]);
                    if (int.TryParse(ReworkOrderString, out ReworkOrder))
                    {
                        if (ReworkOrderString == "1")
                        {
                            hmidata.isWorkOrder = 1;
                        }
                        else
                        {
                            hmidata.isWorkOrder = 0;
                        }
                    }
                   // hmidata.HMIID = ;
                    db.tbllivehmiscreens.Add(hmidata);
                    db.SaveChanges();
                    return RedirectToAction("Index");

                    #endregion
                }
                else
                {
                    Session["Error"] = IssueMsg;
                    TempData["Err"] = IssueMsg;
                    ViewBag.Err = IssueMsg;
                    return RedirectToAction("DDLList", "ManualHMIScreen", new { DDLID = invalidDDLID });
                }
                Session["FromDDL"] = 1;

            }
            else if (WOCount > 1)
            {
                //1st check for sequence of start condition
                //order the DDLs in WONo,PNo,OpNo order.
                //check if they are violating the Rules.
                //Don't sort by ID's desc becauase they may start later but select 1st in different wc
                string DDLIDString = string.Join(",", data.Select(x => x.ToString()).ToArray());

                string DDLQuery = @"SELECT * from tblddl where  DDLID IN ( " + DDLIDString + " ) order by WorkOrder,MaterialDesc,OperationNo ";
                var DDLData = db.tblddls.SqlQuery(DDLQuery).ToList();

                bool isValid = true, IsInHMI = true;
                string IssueMsg = null;

                foreach (var DDLRow in DDLData)
                {
                    int DDLID = DDLRow.DDLID;
                    var ddldataInner = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    String SplitWOInner = ddldataInner.SplitWO;
                    String WONoInner = ddldataInner.WorkOrder;
                    String PartInner = ddldataInner.MaterialDesc;
                    String OperationInner = ddldataInner.OperationNo;

                    //var DuplicateHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == OperationInner && (m.isWorkInProgress == 2 || m.isWorkInProgress == 3)).FirstOrDefault();
                    var DuplicateHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == OperationInner && m.isWorkInProgress == 2 && m.MachineID == machineId).FirstOrDefault();
                    if (DuplicateHMIdata != null)
                    {
                        invalidDDLID = ddldataInner.DDLID;
                        isValid = false;
                        IssueMsg = "This Below WorkOrder, WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + OperationInner + " Exists in PartEntry Screen";
                    }
                }

                #region 2017-02-07
                if (isValid)
                {
                    foreach (var DDLRow in DDLData)
                    {
                        int DDLID = DDLRow.DDLID;
                        var ddldataInner = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                        String SplitWOInner = ddldataInner.SplitWO;
                        String WONoInner = ddldataInner.WorkOrder;
                        String PartInner = ddldataInner.MaterialDesc;
                        String OperationInner = ddldataInner.OperationNo;

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
                                        IssueMsg = "Please Select Below WorkOrder , WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + localOPNo;
                                        //isValid = false;
                                        //break;
                                        isValid = false;
                                        IsInHMI = false;
                                        invalidDDLID = Convert.ToInt32(localddlid);
                                    }
                                    else
                                    {
                                        if (SiblingHMIdata.Date == null)// || SiblingHMIdatahistorian.Date==null) //=> lower OpNo is not submitted.
                                        {
                                            IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                            //return RedirectToAction("Index");
                                            IsInHMI = false;
                                            isValid = false;
                                            invalidDDLID = Convert.ToInt32(localddlid);
                                            break;
                                        }
                                    }

                                    if (!IsInHMI)
                                    {
                                        //also check in MultiWO table
                                        string WIPQueryMultiWO = @"SELECT * from tbllivemultiwoselection where WorkOrder = '" + WONoInner + "' and PartNo = '" + PartInner + "' and OperationNo = '" + localOPNo + "' order by MultiWOID limit 1 ";
                                        var WIPMWO = db.tbllivemultiwoselections.SqlQuery(WIPQueryMultiWO).ToList();

                                        if (WIPMWO.Count == 0)
                                        {
                                            IssueMsg = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                            //return RedirectToAction("Index");
                                            IsInHMI = false;
                                            isValid = false;
                                            invalidDDLID = Convert.ToInt32(localddlid);
                                            break;
                                        }

                                        foreach (var rowHMI in WIPMWO)
                                        {
                                            int hmiid = Convert.ToInt32(rowHMI.HMIID);
                                            var MWOHMIData = db.tbllivehmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                                            if (MWOHMIData != null)
                                            {
                                                if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                                                {
                                                    IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                                                    //return RedirectToAction("Index");
                                                    isValid = false;
                                                    invalidDDLID = Convert.ToInt32(localddlid);
                                                    break;
                                                }
                                                else
                                                {
                                                    IsInHMI = true;
                                                    IssueMsg = null;
                                                    isValid = true;
                                                    invalidDDLID = 0;
                                                }
                                            }
                                        }
                                        //if(!isValid)
                                        //{
                                        //    break;
                                        //}
                                    }

                                }
                            }
                        }
                        //if (!isValid)
                        //{
                        //    break;
                        //}
                    }
                }
                #endregion

                //if (isValid && IsInHMI)
                if (isValid)
                {
                    string ddlWorkCenter = null;
                    foreach (int DDLID in data)
                    {
                        int PrvProcessQty = 0, PrvDeliveredQty = 0, ishold = 0;
                        var ddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                        String SplitWO = ddldata.SplitWO;
                        String WONo = ddldata.WorkOrder;
                        String PartNo = ddldata.MaterialDesc;
                        String OperationNo = ddldata.OperationNo;
                        string target = ddldata.TargetQty;
                        ddlWorkCenter = ddldata.WorkCenter;

                        #region new code
                        ////here get latest of delivered and processed among row in tblHMIScreen 
                        //var hmiDataLocal = db.tblhmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == PartNo && m.OperationNo == OperationNo).OrderByDescending(m => m.HMIID).FirstOrDefault();
                        //if (hmiDataLocal != null)
                        //{
                        //    string delivString = Convert.ToString(hmiDataLocal.Delivered_Qty);
                        //    int.TryParse(delivString, out PrvDeliveredQty);

                        //    string processString = Convert.ToString(hmiDataLocal.ProcessQty);
                        //    int.TryParse(processString, out PrvProcessQty);
                        //    ishold = hmiDataLocal.IsHold;
                        //    ishold = ishold == 2 ? 0 : ishold;
                        //}
                        #endregion

                        #region new code

                        //here 1st get latest of delivered and processed among row in tblHMIScreen & tblmulitwoselection
                        int isHMIFirst = 2; //default NO History for that wo,pn,on

                        var mulitwoData = db.tbllivemultiwoselections.Where(m => m.WorkOrder == WONo && m.PartNo == PartNo && m.OperationNo == OperationNo && m.tbllivehmiscreen.isWorkInProgress != 2).OrderByDescending(m => m.tbllivehmiscreen.Time).Take(1).ToList();
                        var hmiData = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONo && m.PartNo == PartNo && m.OperationNo == OperationNo && m.isWorkInProgress != 2).OrderByDescending(m => m.Time).Take(1).ToList();

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

                        int ProcessQty = PrvProcessQty + PrvDeliveredQty;
                        try
                        {
                            using (mazakdaqEntities dbNewHMI = new mazakdaqEntities())
                            {
                                tbllivehmiscreen hmidataNew = new tbllivehmiscreen();
                                
                                hmidataNew.OperatiorID = Convert.ToInt32(Session["UserID"]);
                                hmidataNew.PEStartTime = DateTime.Now;
                                hmidataNew.OperationNo = OperationNo;
                                hmidataNew.PartNo = PartNo;
                                hmidataNew.Project = ddldata.Project;
                                hmidataNew.Target_Qty = Convert.ToInt32(target);
                                hmidataNew.Work_Order_No = WONo;
                                hmidataNew.ProcessQty = ProcessQty;
                                hmidataNew.Delivered_Qty = 0;
                                hmidataNew.DDLWokrCentre = ddlWorkCenter;
                                hmidataNew.IsMultiWO = 0;
                                hmidataNew.isWorkInProgress = 2;
                                hmidataNew.Status = 0;
                                hmidataNew.IsHold = ishold;
                                hmidataNew.CorrectedDate = CorrectedDate;
                                hmidataNew.OperatiorID = Convert.ToInt32(Session["UserID"]);
                                //hmidataNew.OperatorDet = Convert.ToString(Session["OpName"]);
                                int MachineID = Convert.ToInt32(Session["MachineID"]);
                                hmidataNew.MachineID = MachineID;
                                hmidataNew.Shift = Shift;
                                hmidataNew.OperatorDet = OpName;
                               
                                int ReworkOrder = 0;
                                string ReworkOrderString = Convert.ToString(Session["isWorkOrder"]);
                                if (int.TryParse(ReworkOrderString, out ReworkOrder))
                                {
                                    if (ReworkOrderString == "1")
                                    {
                                        hmidataNew.isWorkOrder = 1;
                                    }
                                    else
                                    {
                                        hmidataNew.isWorkOrder = 0;
                                    }
                                }
                                dbNewHMI.tbllivehmiscreens.Add(hmidataNew);
                                //dbNewHMI.Entry(hmidataNew).State = System.Data.Entity.EntityState.Modified;
                                dbNewHMI.SaveChanges();
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
                else
                {
                    TempData["Err"] = IssueMsg;
                    return RedirectToAction("DDLList", "ManualHMIScreen", new { DDLID = invalidDDLID });
                }

                Session["FromDDL"] = 4;
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        public ActionResult PartialFinishedList(int HMIID = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int opID = Convert.ToInt32(Session["UserId"]);
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

            if (HMIID != 0)
            {
                int PrvProcessQty = 0, PrvDeliveredQty = 0;
                var HmiData = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
                if (HmiData != null)
                {
                    PrvProcessQty = Convert.ToInt32(HmiData.ProcessQty);
                    PrvDeliveredQty = Convert.ToInt32(HmiData.Delivered_Qty);
                }
                int TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);

                //2017-01-03 Just insert new Row.
                string OpName = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.Status == 0).Select(m => m.OperatorDet).FirstOrDefault();
                string Shift = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.Status == 0).Select(m => m.Shift).FirstOrDefault();

                tbllivehmiscreen hmidata = new tbllivehmiscreen();
                
                hmidata.OperationNo = HmiData.OperationNo;
                hmidata.PartNo = HmiData.PartNo;
                hmidata.PEStartTime = DateTime.Now;
                hmidata.CorrectedDate = CorrectedDate;
                hmidata.Shift = Shift;
                hmidata.OperatiorID = opID;
                hmidata.OperatorDet = OpName;
                hmidata.Project = HmiData.Project;
                hmidata.Target_Qty = Convert.ToInt32(HmiData.Target_Qty);
                hmidata.Work_Order_No = HmiData.Work_Order_No;
                hmidata.ProcessQty = TotalProcessQty;
                hmidata.Delivered_Qty = 0;
                hmidata.DDLWokrCentre = HmiData.DDLWokrCentre;
                hmidata.MachineID = machineId;
                hmidata.IsMultiWO = 0;
                hmidata.isWorkInProgress = 2;
                hmidata.Status = 0;
                //hmidata.HMIID = ;
                db.tbllivehmiscreens.Add(hmidata);
                db.SaveChanges();
                Session["FromDDL"] = 1;
                Session["SubmitClicked"] = 0;
                return RedirectToAction("Index");
            }

            ViewBag.MacINV = db.tblmachinedetails.Where(m => m.MachineID == machineId).Select(m => m.MachineInvNo).FirstOrDefault();
            int macID = Convert.ToInt32(Session["MachineID"]);
            //string WIPQuery = @"select * from tblhmiscreen where MachineID = " + macID + " and isWorkInProgress = 0 and CorrectedDate = '" + CorrectedDate + "' group by Work_Order_No,PartNo,OperationNo order by HMIID desc";
            string WIPQuery = @"SELECT * from tbllivehmiscreen where isWorkInProgress = 0 and HMIID IN ( SELECT HMIID from tbllivehmiscreen where MachineID = " + macID + " group by Work_Order_No,PartNo,OperationNo order by HMIID desc ) ";
            var WIP = db.tbllivehmiscreens.SqlQuery(WIPQuery).ToList();
            if (WIP.Count != 0)
            {
                return View(WIP);
            }
            return View();
        }
        [HttpPost]
        public ActionResult PartialFinishedList(List<int> data)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            int opID = Convert.ToInt32(Session["UserId"]);
            string[] DateShiftDetails = GetDateShift();
            string CorrectedDate = DateShiftDetails[0];

            int WOCount = data.Count;
            int machineId = Convert.ToInt32(Session["MachineID"]);
            if (WOCount == 1)
            {
                int HMIID = data.First();
                int PrvProcessQty = 0, PrvDeliveredQty = 0;
                var HmiData = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
                if (HmiData != null)
                {
                    PrvProcessQty = Convert.ToInt32(HmiData.ProcessQty);
                    PrvDeliveredQty = Convert.ToInt32(HmiData.Delivered_Qty);
                }
                int TotalProcessQty = Convert.ToInt32(PrvProcessQty + PrvDeliveredQty);

                //2017-01-03 Just insert new Row.
                string OpName = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.Status == 0).Select(m => m.OperatorDet).FirstOrDefault();
                string Shift = db.tbllivehmiscreens.Where(m => m.MachineID == machineId && m.Status == 0).Select(m => m.Shift).FirstOrDefault();

                tbllivehmiscreen hmidata = new tbllivehmiscreen();
                
                hmidata.OperationNo = HmiData.OperationNo;
                hmidata.PartNo = HmiData.PartNo;
                hmidata.PEStartTime = DateTime.Now;
                hmidata.CorrectedDate = CorrectedDate;
                hmidata.Shift = Shift;
                hmidata.OperatiorID = opID;
                hmidata.OperatorDet = OpName;
                hmidata.Project = HmiData.Project;
                hmidata.Target_Qty = Convert.ToInt32(HmiData.Target_Qty);
                hmidata.Work_Order_No = HmiData.Work_Order_No;
                hmidata.ProcessQty = TotalProcessQty;
                hmidata.Delivered_Qty = 0;
                hmidata.DDLWokrCentre = HmiData.DDLWokrCentre;
                hmidata.MachineID = machineId;
                hmidata.IsMultiWO = 0;
                hmidata.isWorkInProgress = 2;
                hmidata.Status = 0;
                //hmidata.HMIID = ;
                db.tbllivehmiscreens.Add(hmidata);
                db.SaveChanges();
                Session["FromDDL"] = 1;
                Session["SubmitClicked"] = 0;
                return RedirectToAction("Index");

            }
            else if (WOCount > 1)
            {
                string ddlWorkCenter = null;
                foreach (int HMIID in data)
                {
                    int PrvProcessQty = 0, PrvDeliveredQty = 0;
                    var HMIData = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
                    String WONo = HMIData.Work_Order_No;
                    String PartNo = HMIData.PartNo;
                    String OperationNo = HMIData.OperationNo;
                    string target = Convert.ToString(HMIData.Target_Qty);
                    ddlWorkCenter = HMIData.DDLWokrCentre;

                    #region new code
                    //here get latest of delivered and processed among row in tblHMIScreen 
                    if (HMIData != null)
                    {
                        string delivString = Convert.ToString(HMIData.Delivered_Qty);
                        int.TryParse(delivString, out PrvDeliveredQty);

                        string processString = Convert.ToString(HMIData.ProcessQty);
                        int.TryParse(processString, out PrvProcessQty);
                    }
                    #endregion

                    int ProcessQty = PrvProcessQty + PrvDeliveredQty;
                    try
                    {
                        using (mazakdaqEntities dbNewHMI = new mazakdaqEntities())
                        {
                            tbllivehmiscreen hmidataNew = new tbllivehmiscreen();
                            
                            hmidataNew.OperatiorID = Convert.ToInt32(Session["UserID"]);
                            hmidataNew.PEStartTime = DateTime.Now;
                            hmidataNew.OperationNo = OperationNo;
                            hmidataNew.PartNo = PartNo;
                            hmidataNew.Project = HMIData.Project;
                            hmidataNew.Target_Qty = Convert.ToInt32(target);
                            hmidataNew.Work_Order_No = WONo;
                            hmidataNew.ProcessQty = ProcessQty;
                            hmidataNew.Delivered_Qty = 0;
                            hmidataNew.DDLWokrCentre = ddlWorkCenter;
                            hmidataNew.IsMultiWO = 1;
                            hmidataNew.Status = 0;
                            hmidataNew.CorrectedDate = CorrectedDate;
                            hmidataNew.IsMultiWO = 0;
                            hmidataNew.isWorkInProgress = 2;
                            hmidataNew.MachineID = Convert.ToInt32(Session["MachineID"]); ;
                            hmidataNew.OperatorDet = HMIData.OperatorDet;
                            hmidataNew.Shift = HMIData.Shift;
                           
                            dbNewHMI.tbllivehmiscreens.Add(hmidataNew);
                            //dbNewHMI.Entry(hmidataNew).State = System.Data.Entity.EntityState.Modified;
                            dbNewHMI.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
                Session["FromDDL"] = 4;
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

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
            int MachineID = Convert.ToInt32(Session["MachineID"]);

            //Gatting UderID
            int opid = Convert.ToInt32(Session["Opid"]);
            var HMIData = db.tbllivehmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.Status == 0 && m.MachineID == MachineID).ToList();
            // tblhmiscreen HMI = db.tblhmiscreens.Where(m => m.CorrectedDate == CorrectedDate && m.OperatiorID == opid).Where(m => m.MachineID == MachineID).FirstOrDefault();
            foreach (var HMIRow in HMIData)
            {
                //idea is to update the row if set is clicked.
                HMIRow.Shift = Shift;
                HMIRow.isUpdate = 1;
                HMIRow.PEStartTime = DateTime.Now;
                db.Entry(HMIRow).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                //HMIScreenForAdmin(MachineID, opid, CorrectedDate); //not doing
                Session["opid"] = opid;
                Session["gopid"] = opid;
                Session["gshift" + opid] = Shift;
                ViewBag.hide = 1;
            }
            return RedirectToAction("Index");
        }
        public ActionResult changeVisibilityNorm(int wd = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            //Getting Shift Value
            string[] ShiftAndCorrectedDate = GetDateShift();
            ViewBag.shift = ShiftAndCorrectedDate[0];

            int machineID = Convert.ToInt32(Session["MachineID"]);

            //New Logic 2017-01-06 . Let them PF or JF Using respective Buttons.
            //var HMIData = db.tblhmiscreens.Where(m => m.MachineID == machineID && m.Status == 0).ToList();
            //if (HMIData.Count > 1)
            //{
            //    Session["VError"] = "Please Click JobFinish or PartialFinish Before Changing Shift";
            //    return RedirectToAction("Index");
            //}
            //else if (HMIData.Count == 1) //This one may be our empty row or not
            //{
            //    string project = Convert.ToString(HMIData[0].Project);
            //    string PartNo = Convert.ToString(HMIData[0].PartNo);
            //    string Work_Order_No = Convert.ToString(HMIData[0].Work_Order_No);
            //    if (HMIData[0].Date != null)
            //    {
            //        Session["VError"] = "Please Finish CurrentJob Before Changing Shift";
            //        return RedirectToAction("Index");
            //    }
            //}

            //2017-02-06 even if 1 of them is submitted then don't allow. Else Delete those rows and Allow.
            var HMIData = db.tbllivehmiscreens.Where(m => m.MachineID == machineID && m.Status == 0).ToList();

            foreach (var row in HMIData)
            {
                if (row.Date == null)
                {//Do Nothing

                }
                else //Implies one of them is submitted
                {
                    Session["VError"] = "Please Click JobFinish or PartialFinish Before Changing Shift";
                    return RedirectToAction("Index");
                }
            }

            if (HMIData.Count > 0)
            {
                foreach (var row in HMIData)
                {
                    if (row.DDLWokrCentre == null)
                    {
                        row.isUpdate = 0;
                        row.Prod_FAI = null;
                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        db.tbllivehmiscreens.Remove(row);
                        db.SaveChanges();
                    }
                }
                return RedirectToAction("Index");
            }



            //1) Check if Basic Details Available for All WO's and are eligible for PF
            //2) else All fields are empty then Allow to change the shift and OpName.

            #region 2017-01-06
            //wd = 0;
            //var HMIData = db.tblhmiscreens.Where(m => m.Status == 0 && m.MachineID == machineID).ToList();
            //foreach (var hmiid in HMIData)
            //{
            //    string project = hmiid.Project;
            //    string WONo = hmiid.Work_Order_No;
            //    string partNo = hmiid.PartNo;
            //    string OpNo = hmiid.OperationNo;

            //    if (string.IsNullOrEmpty(project) && string.IsNullOrEmpty(WONo) && string.IsNullOrEmpty(partNo) && string.IsNullOrEmpty(OpNo))
            //    {
            //    }
            //    else { wd = 1; break; }
            //}

            //if (wd == 1)
            //{
            //    //1.1 check if all rows are elegible for PF (or null)
            //    //1.2 do PF

            //    #region Main Logic OLD Usefull
            //    //OldWork.Status = 3;
            //    //OldWork.isWorkInProgress = 0;//work is in progress
            //    //string Shiftgen = OldWork.Shift;
            //    //string operatorName = OldWork.OperatorDet;
            //    //int Opgid = OldWork.OperatiorID;
            //    //DateTime pestarttime = Convert.ToDateTime(OldWork.PEStartTime);

            //    ////get all those data for new row.
            //    //string project = OldWork.Project;
            //    //string PorF = OldWork.Prod_FAI;
            //    //string partno = OldWork.PartNo;
            //    //string wono = OldWork.Work_Order_No;
            //    //string opno = OldWork.OperationNo;
            //    //int target = Convert.ToInt32(OldWork.Target_Qty);
            //    //db.Entry(OldWork).State = System.Data.Entity.EntityState.Modified;
            //    //db.SaveChanges();

            //    //tblhmiscreen NewEntry = new tblhmiscreen();
            //    //NewEntry.Project = project;
            //    //NewEntry.Prod_FAI = PorF;
            //    //NewEntry.PartNo = partno;
            //    //NewEntry.Work_Order_No = wono;
            //    //NewEntry.OperationNo = opno;
            //    //NewEntry.Target_Qty = target;
            //    //NewEntry.MachineID = machineID;
            //    //NewEntry.CorrectedDate = ShiftAndCorrectedDate[1];
            //    //NewEntry.PEStartTime = pestarttime;
            //    ////NewEntry.Date = DateTime.Now;
            //    ////NewEntry.OperatorDet = operatorName;
            //    //NewEntry.Shift = Convert.ToString(Shiftgen);
            //    //NewEntry.Status = 0;
            //    //NewEntry.isWorkInProgress = 2;
            //    //NewEntry.OperatiorID = Opgid;
            //    ////NewEntry.Time = DateTime.Now;
            //    //db.tblhmiscreens.Add(NewEntry);
            //    //db.SaveChanges();
            //    #endregion

            //    //1.1
            //    foreach (var row in HMIData)
            //    {
            //        int hmiiid = Convert.ToInt32(row.HMIID);
            //        var HMIData1 = db.tblhmiscreens.Where(m => m.HMIID == hmiiid).FirstOrDefault();
            //        if (HMIData1.isWorkInProgress == 3)
            //        {
            //            Session["VError"] = " Please Release HOLD for WONo" + HMIData1.Work_Order_No;
            //            return RedirectToAction("Index");
            //        }
            //        if (HMIData1.Date != null)
            //        {
            //            int deliveredQty = 0;
            //            if (int.TryParse(Convert.ToString(HMIData1.Delivered_Qty), out deliveredQty))
            //            {
            //                if (deliveredQty < Convert.ToInt32(HMIData1.Target_Qty))
            //                {
            //                }
            //                else
            //                {
            //                    Session["VError"] = "DeliveredQty Must be less than Target";
            //                    return RedirectToAction("Index");
            //                }
            //            }
            //            else{
            //                Session["VError"] = " Please enter Delivered Qty for " + HMIData1.Work_Order_No;
            //                return RedirectToAction("Index");
            //            }
            //        }
            //        else
            //        {
            //            string project = HMIData1.Project;
            //            string WONo = HMIData1.Work_Order_No;
            //            string partNo = HMIData1.PartNo;
            //            string OpNo = HMIData1.OperationNo;

            //            if ((!string.IsNullOrEmpty(project)) || (!string.IsNullOrEmpty(WONo)) || (!string.IsNullOrEmpty(partNo)) || (!string.IsNullOrEmpty(OpNo)))
            //            {
            //                Session["VError"] = " Please Click Submit Before Clicking Change ";
            //                return RedirectToAction("Index");
            //            }

            //        }
            //    }
            //    //1.2
            //    foreach (var row in HMIData)
            //    {
            //        int hmiidi = Convert.ToInt32(row.HMIID);
            //        var HMIData1 = db.tblhmiscreens.Where(m => m.HMIID == hmiidi).FirstOrDefault();
            //        HMIData1.Status = 2;
            //        HMIData1.isWorkInProgress = 0;
            //        HMIData1.Time = DateTime.Now;
            //        db.Entry(HMIData1).State = System.Data.Entity.EntityState.Modified;
            //        db.SaveChanges();


            //        //Insert new into HMIScreen with same details for NON Null Rows (?For All)

            //        //tblhmiscreen NewEntry = new tblhmiscreen();
            //        //NewEntry.Project = HMIData1.Project;
            //        //NewEntry.Prod_FAI = HMIData1.Prod_FAI;
            //        //NewEntry.PartNo = HMIData1.PartNo;
            //        //NewEntry.Work_Order_No = HMIData1.Work_Order_No;
            //        //NewEntry.OperationNo = HMIData1.OperationNo;
            //        //NewEntry.Target_Qty = HMIData1.Target_Qty;
            //        //NewEntry.MachineID = machineID;
            //        //NewEntry.CorrectedDate = ShiftAndCorrectedDate[1]; 
            //        //NewEntry.PEStartTime = DateTime.Now;
            //        ////NewEntry.Date = DateTime.Now;
            //        ////NewEntry.OperatorDet = operatorName;
            //        //NewEntry.Shift = ShiftAndCorrectedDate[0];
            //        //NewEntry.Status = 0;
            //        //NewEntry.isWorkInProgress = 2;
            //        //NewEntry.OperatiorID = HMIData1.OperatiorID;
            //        ////NewEntry.Time = DateTime.Now;
            //        //db.tblhmiscreens.Add(NewEntry);
            //        //db.SaveChanges();

            //    }
            //}
            //else
            //{
            //foreach (var row in HMIData)
            //{
            //    row.isUpdate = 0;
            //    db.Entry(row).State = System.Data.Entity.EntityState.Modified;
            //    db.SaveChanges();
            //}

            //}
            #endregion

            foreach (var row in HMIData)
            {
                row.isUpdate = 0;
                db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }

            return RedirectToAction("Index");
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
            var CurrentStatusData = db.tblbreakdowns.Where(m => m.MachineID == machineID && m.DoneWithRow == 0).OrderByDescending(m => m.BreakdownID).FirstOrDefault();
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
        public JsonResult JsonIdleChecker()
        {
            string retStatus = "false";
            var machineID = Convert.ToInt32(Session["MachineID"]);
            string[] GetDateShiftValues = GetDateShift();
            string CorrectedDate = GetDateShiftValues[1];
            var CurrentStatusData = db.tbllivelossofentries.Where(m => m.MachineID == machineID && m.DoneWithRow == 0).OrderByDescending(m => m.LossID).FirstOrDefault();
            if (CurrentStatusData != null)
            {
                retStatus = "true";
            }

            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }
        public JsonResult JsonGenericWorkChecker()
        {
            string retStatus = "false";
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

            var CurrentStatusData = db.tblgenericworkentries.Where(m => m.MachineID == machineID).OrderByDescending(m => m.GWEntryID).FirstOrDefault();
            if (CurrentStatusData != null)
            {
                if (CurrentStatusData.DoneWithRow == 0)
                {
                    retStatus = "true";
                }
            }
            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        public JsonResult JsonCheckerRemoveWO(int Hmiid)
        {
            string retStatus = "false";
            using (mazakdaqEntities dbhmi = new mazakdaqEntities())
            {
                var hmidata = dbhmi.tbllivehmiscreens.Where(m => m.HMIID == Hmiid).FirstOrDefault();
                if (hmidata != null)
                {
                    if (string.IsNullOrEmpty(Convert.ToString(hmidata.Date)))
                    {
                        retStatus = "true";
                        dbhmi.tbllivehmiscreens.Remove(hmidata);
                        dbhmi.SaveChanges();

                        if (hmidata.IsMultiWO == 1)
                        {
                            try
                            {
                                dbhmi.tbllivemultiwoselections.Remove(dbhmi.tbllivemultiwoselections.Where(m => m.HMIID == hmidata.HMIID).FirstOrDefault());
                                dbhmi.SaveChanges();
                            }
                            catch (Exception e)
                            {

                            }
                        }

                    }
                    else
                    {
                        retStatus = "You cannnot remove WorkOrder Once its Started.";
                    }
                }
            }

            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        //Generic Work codes
        public ActionResult GenericWork(int Hmiid = 0, int Bid = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            ViewBag.Hmiid = Hmiid;

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


            //Conditions to start GenericWork
            //1) All WO's should be PF or JF 'ed before GenericWork
            var HMIData = db.tbllivehmiscreens.Where(m => m.MachineID == macid && m.Status == 0).ToList();
            if (HMIData.Count > 1)
            {
                Session["VError"] = "Please Click JobFinish or PartialFinish Before Starting Generic Work";
                return RedirectToAction("Index");
            }
            else if (HMIData.Count == 1) //This one may be our empty row or not
            {
                string project = Convert.ToString(HMIData[0].Project);
                //string Prod_FAI = Convert.ToString(HMIData[0].Prod_FAI).Trim();
                string PartNo = Convert.ToString(HMIData[0].PartNo);
                string Work_Order_No = Convert.ToString(HMIData[0].Work_Order_No);
                //string OperationNo = Convert.ToString(HMIData[0].OperationNo).Trim();
                //string Target_Qty = Convert.ToString(HMIData[0].Target_Qty).Trim();
                //if (!string.IsNullOrEmpty(project) || !string.IsNullOrEmpty(Prod_FAI) || !string.IsNullOrEmpty(PartNo) || !string.IsNullOrEmpty(Work_Order_No) || !string.IsNullOrEmpty(OperationNo) || !string.IsNullOrEmpty(Target_Qty))
                //{
                //    Session["VError"] = "Please Finish CurrentJob Before Starting GenericWork";
                //    return RedirectToAction("Index");
                //}

                if (HMIData[0].Date != null)
                {
                    Session["VError"] = "Please Finish CurrentJob Before Starting GenericWork";
                    return RedirectToAction("Index");
                }
            }

            //int handleidleReturnValue = HandleIdleManualWC();
            //if (handleidleReturnValue == 1)
            //{
            //    Session["showIdlePopUp"] = 0;
            //    return RedirectToAction("Index");
            //}

            ////Get Previous Loss to Display.
            //var PrevIdleToView = db.tbllossofentries.Where(m => m.MachineID == macid && m.DoneWithRow == 0).OrderByDescending(m => m.LossID).FirstOrDefault();
            //if (PrevIdleToView != null)
            //{
            //    int losscode = PrevIdleToView.MessageCodeID;
            //    ViewBag.PrevLossName = GetLossPath(losscode);
            //    ViewBag.PrevLossStartTime = PrevIdleToView.StartDateTime;
            //}

            //Data of Current HMI to HoldScreen
            var HMIDataToView = db.tbllivehmiscreens.Where(m => m.HMIID == Hmiid).FirstOrDefault();
            if (HMIDataToView != null)
            {
                ViewBag.WONo = HMIDataToView.Work_Order_No;
                ViewBag.PartNo = HMIDataToView.PartNo;
                ViewBag.OpNo = HMIDataToView.OperationNo;
            }
            //2017-01-03 stage 2. Generic Work is running and u need to send data to view regarding that.
            var IdleToView = db.tblgenericworkentries.Where(m => m.MachineID == macid).OrderByDescending(m => m.GWEntryID).FirstOrDefault();
            if (IdleToView != null) //implies idle is running
            {
                if (IdleToView.DoneWithRow == 0)
                {
                    int idlecode = Convert.ToInt32(IdleToView.GWCodeID);
                    var DataToView = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GenericWorkID == idlecode).ToList();
                    ViewBag.Level = DataToView[0].GWCodesLevel;
                    ViewBag.BreakdownCode = DataToView[0].GenericWorkCode;
                    ViewBag.BreakDownID = DataToView[0].GenericWorkID;
                    ViewBag.BreakdownStartTime = IdleToView.StartDateTime;
                }
            }

            //stage 3. Operator is selecting the Idle by traversing down the Hierarchy of LossCodes.
            if (Bid != 0)
            {
                var lossdata = db.tblgenericworkcodes.Find(Bid);
                int level = lossdata.GWCodesLevel;
                string losscode = lossdata.GenericWorkCode;
                if (level == 1)
                {
                    var level2Data = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GWCodesLevel1ID == Bid && m.GWCodesLevel == 2 && m.GWCodesLevel2ID == null).ToList();
                    if (level2Data.Count == 0)
                    {
                        var level1Data = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GWCodesLevel == level && m.GWCodesLevel1ID == null && m.GWCodesLevel2ID == null).ToList();
                        ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + losscode + " as reason.";
                        ViewBag.BreakDownID = Bid;
                        ViewBag.Level = level;
                        ViewBag.breadScrum = losscode + "-->  ";
                        return View(level1Data);
                    }
                    ViewBag.Level = level + 1;
                    ViewBag.BreakDownID = Bid;
                    ViewBag.breadScrum = losscode + "-->  ";
                    return View(level2Data);
                }
                else if (level == 2)
                {
                    var level3Data = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GWCodesLevel2ID == Bid && m.GWCodesLevel == 3).ToList();
                    int prevLevelId = Convert.ToInt32(lossdata.GWCodesLevel1ID);
                    var level1data = db.tblgenericworkcodes.Where(m => m.GenericWorkID == prevLevelId).Select(m => m.GenericWorkCode).FirstOrDefault();
                    if (level3Data.Count == 0)
                    {
                        var level2Data = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GWCodesLevel1ID == prevLevelId && m.GWCodesLevel2ID == null).ToList();
                        ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + losscode + " as reason.";
                        ViewBag.BreakDownID = Bid;
                        ViewBag.Level = level;
                        ViewBag.breadScrum = level1data + " --> " + losscode + " --> ";
                        return View(level2Data);
                    }
                    ViewBag.breadScrum = level1data + " --> " + losscode;
                    ViewBag.Level = level + 1;
                    ViewBag.BreakDownID = Bid;

                    return View(level3Data);
                }
                else if (level == 3)
                {
                    int prevLevelId = Convert.ToInt32(lossdata.GWCodesLevel2ID);
                    int FirstLevelID = Convert.ToInt32(lossdata.GWCodesLevel1ID);
                    var level2scrum = db.tblgenericworkcodes.Where(m => m.GenericWorkID == prevLevelId).Select(m => m.GenericWorkCode).FirstOrDefault();
                    var level1scrum = db.tblgenericworkcodes.Where(m => m.GenericWorkID == FirstLevelID).Select(m => m.GenericWorkCode).FirstOrDefault();
                    var level2Data = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GWCodesLevel2ID == prevLevelId && m.GWCodesLevel == 3).ToList();
                    ViewBag.ItsLastLevel = "No Further Levels . Do you want to set " + losscode + " as reason.";
                    ViewBag.BreakDownID = Bid;
                    ViewBag.Level = 3;
                    ViewBag.breadScrum = level1scrum + " --> " + level2scrum + " --> ";
                    return View(level2Data);
                }
            }
            else
            {
                var level1Data = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GWCodesLevel == 1).ToList();
                ViewBag.Level = 1;
                return View(level1Data);
            }

            //Fail Safe: if everything else fails send level1 codes.
            ViewBag.Level = 1;
            var level10Data = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GWCodesLevel == 1).ToList();
            return View(level10Data);

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenericWork(tblgenericworkcode tbdc, string EndBreakdown = null, int HiddenID = 0)
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
            if (HiddenID != 0 && string.IsNullOrEmpty(EndBreakdown) == true) //Comes here while GenericWork is Started
            {
                var breakdata = db.tblgenericworkcodes.Where(m => m.IsDeleted == 0 && m.GenericWorkID == HiddenID).FirstOrDefault();
                string msgCode = breakdata.GenericWorkCode;
                string msgDesc = breakdata.GenericWorkDesc;

                tblgenericworkentry tb = new tblgenericworkentry();
                tb.GWCodeID = HiddenID;
                tb.CorrectedDate = CorrectedDate;
                tb.DoneWithRow = 0;
                tb.MachineID = Convert.ToInt32(Session["MachineID"]);
                tb.GWCode = msgCode;
                tb.GWCodeDesc = msgDesc;
                tb.Shift = Shift;
                tb.StartDateTime = DateTime.Now;
                db.tblgenericworkentries.Add(tb);
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

                tbllivemodedb tm = new  tbllivemodedb();
                tm.MachineID = Convert.ToInt32(Session["MachineID"]);
                tm.CorrectedDate = CorrectedDate;
                tm.InsertedBy = Convert.ToInt32(Session["UserId"]);
                tm.StartTime = DateTime.Now;
                tm.ColorCode = "green";
                tm.InsertedOn = DateTime.Now;
                tm.IsDeleted = 0;
                tm.Mode = "PowerOn";
                tm.IsCompleted = 0;

                db.tbllivemodedbs.Add(tm);
                db.SaveChanges();

            }
            else if (HiddenID != 0 && string.IsNullOrEmpty(EndBreakdown) == false) // comes here while End GenericWork is Clicked
            {
                var tb = db.tblgenericworkentries.Where(m => m.GWCodeID == HiddenID && m.MachineID == MachineID && m.DoneWithRow == 0).OrderByDescending(m => m.GWCodeID).FirstOrDefault();
                tb.EndDateTime = DateTime.Now;
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
                tmIDLE.ColorCode = "green";
                tmIDLE.CorrectedDate = CorrectedDate;
                tmIDLE.InsertedBy = Convert.ToInt32(Session["UserId"]);
                tmIDLE.InsertedOn = DateTime.Now;
                tmIDLE.IsCompleted = 0;
                tmIDLE.IsDeleted = 0;
                tmIDLE.MachineID = MachineID;
                tmIDLE.Mode = "PowerOn";
                tmIDLE.StartTime = DateTime.Now;

                db.tbllivemodedbs.Add(tmIDLE);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        string GetOrderedHMIIDs(string hmiids)
        {
            string retHMIIDs = null;
            if (hmiids != null)
            {

                string WIPQueryOuter = @"SELECT * from tbllivehmiscreen where HMIID IN ( " + hmiids + " ) order by Work_Order_No,PartNo,OperationNo ; ";
                var WIPOuter = db.tbllivehmiscreens.SqlQuery(WIPQueryOuter).ToList();

                for (int id = 0; id < WIPOuter.Count; id++)
                {
                    if (retHMIIDs == null)
                    {
                        retHMIIDs = Convert.ToString(WIPOuter[id].HMIID);
                    }
                    else
                    {
                        retHMIIDs += "," + Convert.ToString(WIPOuter[id].HMIID);
                    }
                }
            }

            return retHMIIDs;
        }

        public JsonResult SiblingValidation(List<int> data)
        {
            int WOCount = data.Count;
            string status = null;
            bool isValid = true, IsInHMI = true; ;
            string IssueMsg = null;
            int MachineID = Convert.ToInt32(Session["MachineID"]);
            if (WOCount == 1)
            {
                #region Single WO
                int DDLID = data.First();
                //1st check for eligibility for wo,part,opno sequence condition.
                int invalidDDLID = 0;
                if (DDLID != 0)
                {
                    var ddldataInner = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    String SplitWOInner = ddldataInner.SplitWO;
                    String WONoInner = ddldataInner.WorkOrder;
                    String PartInner = ddldataInner.MaterialDesc;
                    String OperationInner = ddldataInner.OperationNo;

                    //var DuplicateHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == OperationInner && (m.isWorkInProgress == 2 || m.isWorkInProgress == 3)).FirstOrDefault();
                    var DuplicateHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == OperationInner && m.isWorkInProgress == 2 && m.MachineID == MachineID).FirstOrDefault();
                    if (DuplicateHMIdata != null)
                    {
                        isValid = false;
                        IssueMsg = "This Below WorkOrder, WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + OperationInner + " Exists in PartEntry Screen";
                    }
                    //if (isValid)
                    //{
                    //    //Code to test the Manual WorkOrders.

                    //    //to test those in DDL List
                    //    var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONoInner && m.MaterialDesc == PartInner && m.OperationNo != OperationInner).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                    //    foreach (var row in Siblingddldata)
                    //    {
                    //        int localOPNo = Convert.ToInt32(row.OperationNo);
                    //        string localOPNoString = Convert.ToString(row.OperationNo);
                    //        if (localOPNo < Convert.ToInt32(OperationInner))
                    //        {
                    //            //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                    //            //which case is valid.
                    //            IsInHMI = true; //reinitialize
                    //            var SiblingHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == localOPNoString).FirstOrDefault();
                    //            if (SiblingHMIdata == null)
                    //            {
                    //                invalidDDLID = ddldataInner.DDLID;
                    //                IssueMsg = "Please Select Below WorkOrder , WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + localOPNo;
                    //                //isValid = false;
                    //                //break;
                    //                IsInHMI = false;
                    //            }
                    //            else
                    //            {
                    //                if (SiblingHMIdata.Date == null) //=> lower OpNo is not submitted.
                    //                {
                    //                    IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                    //                    //return RedirectToAction("Index");
                    //                    IsInHMI = false;
                    //                    break;
                    //                }
                    //            }

                    //            if (!IsInHMI)
                    //            {
                    //                //also check in MultiWO table
                    //                string WIPQueryMultiWO = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + WONoInner + "' and PartNo = '" + PartInner + "' and OperationNo = '" + localOPNo + "' order by MultiWOID desc limit 1 ";
                    //                var WIPMWO = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWO).ToList();

                    //                if (WIPMWO.Count == 0)
                    //                {
                    //                    IssueMsg = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                    //                    //return RedirectToAction("Index");
                    //                    IsInHMI = false;
                    //                    break;
                    //                }

                    //                foreach (var rowHMI in WIPMWO)
                    //                {
                    //                    int hmiid = Convert.ToInt32(rowHMI.HMIID);
                    //                    var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                    //                    if (MWOHMIData != null)
                    //                    {
                    //                        if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                    //                        {
                    //                            IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                    //                            //return RedirectToAction("Index");
                    //                            break;
                    //                        }
                    //                        else
                    //                        {
                    //                            IsInHMI = true;
                    //                        }
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                }
                #endregion
            }
            else if (WOCount > 1)
            {
                #region Its Multiple WorkOrders
                //1st check for sequence of start condition
                //order the DDLs in WONo,PNo,OpNo order.
                //check if they are violating the Rules.
                string DDLIDString = string.Join(",", data.Select(x => x.ToString()).ToArray());

                string DDLQuery = @"SELECT * from tblddl where  DDLID IN ( " + DDLIDString + " ) order by WorkOrder,MaterialDesc,OperationNo ";
                var DDLData = db.tblddls.SqlQuery(DDLQuery).ToList();

                foreach (var DDLRow in DDLData)
                {
                    int DDLID = DDLRow.DDLID;
                    var ddldataInner = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                    String SplitWOInner = ddldataInner.SplitWO;
                    String WONoInner = ddldataInner.WorkOrder;
                    String PartInner = ddldataInner.MaterialDesc;
                    String OperationInner = ddldataInner.OperationNo;

                    //var DuplicateHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == OperationInner && (m.isWorkInProgress == 2 || m.isWorkInProgress == 3)).FirstOrDefault();
                    var DuplicateHMIdata = db.tbllivehmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == OperationInner && m.isWorkInProgress == 2 && m.MachineID == MachineID).FirstOrDefault();
                    if (DuplicateHMIdata != null)
                    {
                        isValid = false;
                        IssueMsg = "This Below WorkOrder, WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + OperationInner + " Exists in PartEntry Screen";
                        break;
                    }

                }

                //if (isValid)
                //{
                //    foreach (var DDLRow in DDLData)
                //    {
                //        int DDLID = DDLRow.DDLID;
                //        var ddldataInner = db.tblddls.Where(m => m.IsCompleted == 0 && m.DDLID == DDLID).FirstOrDefault();
                //        String SplitWOInner = ddldataInner.SplitWO;
                //        String WONoInner = ddldataInner.WorkOrder;
                //        String PartInner = ddldataInner.MaterialDesc;
                //        String OperationInner = ddldataInner.OperationNo;

                //        var Siblingddldata = db.tblddls.Where(m => m.IsCompleted == 0 && m.WorkOrder == WONoInner && m.MaterialDesc == PartInner && m.OperationNo != OperationInner).OrderBy(m => new { m.WorkOrder, m.MaterialDesc, m.OperationNo }).ToList();
                //        foreach (var row in Siblingddldata)
                //        {
                //            string localddlid = Convert.ToString(row.DDLID);
                //            int localOPNo = Convert.ToInt32(row.OperationNo);
                //            string localOPNoString = Convert.ToString(row.OperationNo);
                //            if (localOPNo < Convert.ToInt32(OperationInner))
                //            {
                //                //Here Check in HMIScreen Table. There are chances that this one is started prior to this round of ddl selection ,
                //                //which case is valid.
                //                if (DDLIDString.Contains(localddlid))
                //                { }
                //                else
                //                {
                //                    //may be already selected ( It may be in HMIScreen )
                //                    var SiblingHMIdata = db.tblhmiscreens.Where(m => m.Work_Order_No == WONoInner && m.PartNo == PartInner && m.OperationNo == localOPNoString).FirstOrDefault();
                //                    if (SiblingHMIdata == null)
                //                    {
                //                        IssueMsg = "Please Select Below WorkOrder , WONo: " + WONoInner + " PartNo: " + PartInner + " OperationNo: " + localOPNo;
                //                        isValid = false;
                //                        IsInHMI = false;
                //                    }
                //                    else
                //                    {
                //                        if (SiblingHMIdata.Date == null) //=> lower OpNo is not submitted.
                //                        {
                //                            IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                //                            //return RedirectToAction("Index");
                //                            IsInHMI = false;
                //                            break;
                //                        }
                //                    }

                //                    if (!IsInHMI)
                //                    {
                //                        //also check in MultiWO table
                //                        string WIPQueryMultiWO = @"SELECT * from tbl_multiwoselection where WorkOrder = '" + WONoInner + "' and PartNo = '" + PartInner + "' and OperationNo = '" + localOPNo + "' order by MultiWOID desc limit 1 ";
                //                        var WIPMWO = db.tbl_multiwoselection.SqlQuery(WIPQueryMultiWO).ToList();

                //                        if (WIPMWO.Count == 0)
                //                        {
                //                            IssueMsg = " Select  WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                //                            //return RedirectToAction("Index");
                //                            IsInHMI = false;
                //                            break;
                //                        }

                //                        foreach (var rowHMI in WIPMWO)
                //                        {
                //                            int hmiid = Convert.ToInt32(rowHMI.HMIID);
                //                            var MWOHMIData = db.tblhmiscreens.Where(m => m.HMIID == hmiid).FirstOrDefault();
                //                            if (MWOHMIData != null)
                //                            {
                //                                if (MWOHMIData.Date == null) //=> lower OpNo is not submitted.
                //                                {
                //                                    IssueMsg = " Start WONo: " + row.WorkOrder + " and PartNo: " + row.MaterialDesc + " and OperationNo: " + localOPNoString;
                //                                    //return RedirectToAction("Index");
                //                                    break;
                //                                }
                //                                else
                //                                {
                //                                    IsInHMI = true;
                //                                }
                //                            }
                //                        }
                //                    }


                //                }
                //            }
                //        }
                //    }
                //}
                #endregion
            }

            if (isValid && IsInHMI)
            {
                status = "valid";
            }
            else
            {
                status = IssueMsg;
            }
            return Json(status, JsonRequestBehavior.AllowGet);
        }

        public JsonResult partBasedOnWONo(string wono)
        {
            string partno = null;
            string PartnoFromDB = db.tblddls.Where(m => m.WorkOrder == wono).Select(m => m.MaterialDesc).FirstOrDefault();
            if (PartnoFromDB != null)
            {
                partno = PartnoFromDB;
            }

            return Json(partno, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAllPFWOs(string id)
        {
            List<string> DDLIDsList = new List<string>();
            //1st get machineID from MachineInvNo
            string MacDetailsQuery = @"SELECT * from tblmachinedetails where MachineInvNo = '" + id + "' and IsDeleted = 0";
            var MacDetails = db.tblmachinedetails.SqlQuery(MacDetailsQuery).FirstOrDefault();
            if (MacDetails != null)
            {
                int MacID = MacDetails.MachineID;
                MacID = Convert.ToInt32(Session["MachineID"]);
                string WIPQuery = @"SELECT * from tbllivehmiscreen where isWorkInProgress = 0 and HMIID IN ( SELECT HMIID from tbllivehmiscreen as h where h.MachineID = " + MacID + " group by Work_Order_No,PartNo,OperationNo order by h.Date)  ";
                var WIP = db.tbllivehmiscreens.SqlQuery(WIPQuery).ToList();
                foreach (var row in WIP)
                {
                    string wono = row.Work_Order_No;
                    string partno = row.PartNo;
                    string opno = row.OperationNo;
                    var ddldata = db.tblddls.Where(m => m.WorkOrder == wono && m.MaterialDesc == partno && m.OperationNo == opno && m.IsCompleted == 0).FirstOrDefault();
                    if (ddldata != null)
                    {
                        DDLIDsList.Add(ddldata.DDLID.ToString());
                    }
                }
            }

            return Json(DDLIDsList, JsonRequestBehavior.AllowGet);
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

        public JsonResult MacStatus()
        {
            string status = null;

            book b = new book();
            b.BookName = Convert.ToString(Session["UserId"]);
            db.books.Add(b);
            db.SaveChanges();

            ////var blah1 = Page.Session;
            //var blah2 = HttpContext.CurrentHandler;
            ////DateTime? blahDate = blah2;
            //HttpSessionState ses = new System.Web.SessionState.HttpSessionState();

            //int sessionTimeout = System.Web.HttpContext.Current.Session.Timeout;
            //DateTime timeoutDate = DateTime.Now.AddMinutes(sessionTimeout);
            //double TotalMinutes = timeoutDate.Subtract(DateTime.Now).TotalMinutes;
            //if ( TotalMinutes < 3)
            //{
            //    status = TotalMinutes.ToString();
            //}

            //var session = System.Web.HttpContext.Current.Session;//Return current sesion
            //DateTime? sessionStart = session[sessionStart.SessionStart] as DateTime?;//Convert into DateTime object
            //if (sessionStart.HasValue)//Check if session has not expired
            //{
            //    var aa = sessionStart.Value - DateTime.Now;//Get the remaining time
            //}

            return Json(status, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult SessionTimeOutChecker()
        //{
        //    string duration = "0";

        //    int sessionTimeout = System.Web.HttpContext.Current.Session.Timeout;
        //    DateTime timeoutDate = DateTime.Now.AddMinutes(sessionTimeout);
        //    double TotalMinutes = timeoutDate.Subtract(DateTime.Now).TotalMinutes;
        //    //if (TotalMinutes < 3)
        //    //{
        //    duration = TotalMinutes.ToString();
        //    //}
        //    //TotalMinutes = 2;
        //    if (TotalMinutes < 3)
        //    {

        //        alldatatype adt = new alldatatype();
        //        adt.DateTimeType = DateTime.Now;
        //        adt.IntType = 666;
        //        adt.StringType = duration;
        //        db.alldatatypes.Add(adt);
        //        db.SaveChanges();
        //    }
        //    return Json(duration, JsonRequestBehavior.AllowGet);
        //}

        public bool IDLEorGenericWorkisON()
        {
            //Check if the Machine is in IDLE or GenericWork , We shouldn't allow them to do other activities if these are ON.
            //bool ItsOn = false;
            int MacID = Convert.ToInt32(Session["MachineID"]);
            var GWToView = db.tblgenericworkentries.Where(m => m.MachineID == MacID).OrderByDescending(m => m.GWEntryID).FirstOrDefault();
            if (GWToView != null) //implies genericwork is running
            {
                if (GWToView.DoneWithRow == 0)
                {
                    //ItsOn = true;
                    return true;
                }
            }

            var IdleToView = db.tbllivelossofentries.Where(m => m.MachineID == MacID).OrderByDescending(m => m.LossID).FirstOrDefault();
            if (IdleToView != null) //implies idle is running
            {
                if (IdleToView.DoneWithRow == 0)
                {
                    //ItsOn = true;
                    return true;
                }
            }

            return false;
        }

        //[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public HttpSessionState Session
        //{
        //    get
        //    {
        //        HttpSessionState session = null;

        //        if (this._session != null)
        //        {
        //            session = this._session;
        //        }
        //        else if (this._context != null)
        //        {
        //            session = this._context.Session;
        //        }

        //        if (session == null)
        //        {
        //            throw new HttpException(SR.GetString("Session_not_available"));
        //        }

        //        return session;
        //    }
        //}

        public bool ContainsChar(string s)
        {
            bool retVal = false;
            bool result = s.Any(x => char.IsLetter(x));

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

            return retVal;
        }

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
                        isHold = isHold == 2 ? 0 : isHold; //just to make sure 2 is not set as isHold value for brand new row.
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
                                    tbllivehmiscreen tblhmiNewRow = new tbllivehmiscreen();
                                    
                                    tblhmiNewRow.DDLWokrCentre = ddlData.WorkCenter;
                                    tblhmiNewRow.DoneWithRow = 0;
                                    tblhmiNewRow.IsHold = isHold;
                                    tblhmiNewRow.IsMultiWO = 0;
                                    tblhmiNewRow.OperatiorID = hmiData.OperatiorID;
                                    tblhmiNewRow.OperatorDet = hmiData.OperatorDet;
                                    tblhmiNewRow.isUpdate = hmiData.isUpdate;
                                    tblhmiNewRow.isWorkInProgress = 2;
                                    tblhmiNewRow.isWorkOrder = 0;
                                    tblhmiNewRow.Target_Qty = Convert.ToInt32(ddlData.TargetQty);
                                    tblhmiNewRow.Prod_FAI = ddlData.Type;
                                    tblhmiNewRow.MachineID = MachineID;
                                    tblhmiNewRow.OperationNo = OpNo;
                                    tblhmiNewRow.PartNo = ddlData.MaterialDesc;
                                    tblhmiNewRow.ProcessQty = 0;
                                    tblhmiNewRow.Delivered_Qty = 0;
                                    tblhmiNewRow.Prod_FAI = ddlData.Type;
                                    tblhmiNewRow.Project = ddlData.Project;
                                    tblhmiNewRow.Status = 0;
                                    tblhmiNewRow.Work_Order_No = WONo;
                                    //tblhmiNewRow.HMIID = ;
                                    //tblhmiNewRow.PEStartTime = DateTime.Now;
                                    db.tbllivehmiscreens.Add(tblhmiNewRow);
                                    //db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
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
                                tbllivehmiscreen tblhmiNewRow = new tbllivehmiscreen();
                                
                                tblhmiNewRow.DDLWokrCentre = thisHMIDetails.DDLWokrCentre;
                                tblhmiNewRow.DoneWithRow = 0;
                                tblhmiNewRow.IsHold = isHold;
                                tblhmiNewRow.IsMultiWO = 0;
                                tblhmiNewRow.isUpdate = hmiData.isUpdate;
                                tblhmiNewRow.isWorkInProgress = 2;
                                tblhmiNewRow.OperatiorID = hmiData.OperatiorID;
                                tblhmiNewRow.OperatorDet = hmiData.OperatorDet;
                                tblhmiNewRow.isWorkOrder = 0;
                                tblhmiNewRow.Target_Qty = Convert.ToInt32(thisHMIDetails.Target_Qty);
                                tblhmiNewRow.Prod_FAI = thisHMIDetails.Prod_FAI;
                                tblhmiNewRow.MachineID = MachineID;
                                tblhmiNewRow.OperationNo = OpNo;
                                tblhmiNewRow.PartNo = thisHMIDetails.PartNo;
                                tblhmiNewRow.ProcessQty = 0;
                                tblhmiNewRow.Delivered_Qty = 0;
                                tblhmiNewRow.Project = thisHMIDetails.Project;
                                tblhmiNewRow.Status = 0;
                                tblhmiNewRow.Work_Order_No = WONo;
                                //tblhmiNewRow.HMIID = ;
                                //tblhmiNewRow.PEStartTime = DateTime.Now;
                                db.tbllivehmiscreens.Add(tblhmiNewRow);
                                //db.Entry(hmiData).State = System.Data.Entity.EntityState.Modified;
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

        public JsonResult AutoSaveSplitWO(int HMIID, string SplitWO)
        {
            bool retStatus = false;
            var thisrow = db.tbllivehmiscreens.Where(m => m.HMIID == HMIID).FirstOrDefault();
            if (thisrow != null)
            {
                if (!string.IsNullOrEmpty(thisrow.PartNo) && !string.IsNullOrEmpty(thisrow.Work_Order_No))
                {
                    if (SplitWO.Equals("Yes") || SplitWO.Equals("No"))
                    {
                        retStatus = true;
                        thisrow.SplitWO = SplitWO;
                        db.Entry(thisrow).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            return Json(retStatus, JsonRequestBehavior.AllowGet);
        }

        List<string> GetMacHierarchyData(int MachineID)
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
                    HierarchyData.Add(name.ToString());
                }
                else
                {
                    HierarchyData.Add("-");
                }

                string CellIDString = Convert.ToString(machineData.CellID);
                if (int.TryParse(CellIDString, out value))
                {
                    name = dbMac.tblcells.Where(m => m.CellID == value).Select(m => m.CellName).FirstOrDefault();
                    HierarchyData.Add(name.ToString());
                }
                else
                {
                    HierarchyData.Add("-");
                }
                HierarchyData.Add(Convert.ToString(machineData.MachineInvNo));
                HierarchyData.Add(Convert.ToString(machineData.MachineDispName));
            }
            return HierarchyData;
        }

    }

}
