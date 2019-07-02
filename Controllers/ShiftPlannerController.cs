using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Data;
using System.Web.Mvc;
using Tata.Models;
using TataMySqlConnection;

namespace Tata.Controllers
{
    public class ShiftPlannerController : Controller
    {
        //
        // GET: /ShiftPlanner/
        mazakdaqEntities db = new mazakdaqEntities();

        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserId"]);
            //string todaysdate = DateTime.Now.Date.ToString("yyyy-MM-dd");
            DateTime onlyDate = DateTime.Now.Date;
            var data = db.tblshiftplanners.Where(m => m.IsDeleted == 0 && m.EndDate > onlyDate).OrderBy(m => m.ShiftPlannerID).ToList();

            //DataTable dataHolder = new DataTable();
            //MsqlConnection mc = new MsqlConnection();
            //mc.open();
            //String sql = "SELECT * FROM tblShiftPlanner WHERE IsDeleted = 0 AND EndDate >'" + todaysdate + "' ORDER BY ShiftPlannerID ASC";
            //MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
            //da.Fill(dataHolder);
            //mc.close();

            //var data = ConvertTotblshiftplanner(dataHolder);
            return View(data.ToList());
        }

        #region not sure will work(because of plant, shop(DBNull)). No time.
        //Method 1:
        // Can user IEnumerable<tblshiftplanner> || List<tblshiftplanner>
        //IEnumerable tables = ds.Tables;
        //IEnumerable rows = ds.Tables[0].Rows;

        //Method 2:
        //private IEnumerable<tblshiftplanner> ConvertTotblshiftplanner(DataTable dataTable)
        //{
        //    foreach (DataRow row in dataTable.Rows)
        //    {
        //        yield return new tblshiftplanner
        //        {
        //            TankReadingsID = Convert.ToInt32(row["TRReadingsID"]),
        //            TankID = Convert.ToInt32(row["TankID"]),
        //            ReadingDateTime = Convert.ToDateTime(row["ReadingDateTime"]),
        //            ReadingFeet = Convert.ToInt32(row["ReadingFeet"]),
        //            ReadingInches = Convert.ToInt32(row["ReadingInches"]),
        //            MaterialNumber = row["MaterialNumber"].ToString(),
        //            EnteredBy = row["EnteredBy"].ToString(),
        //            ReadingPounds = Convert.ToDecimal(row["ReadingPounds"]),
        //            MaterialID = Convert.ToInt32(row["MaterialID"]),
        //            Submitted = Convert.ToBoolean(row["Submitted"]),
        //        };
        //    }

        //}
        #endregion

        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();

            ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName");
            ViewBag.SelectedDropDown = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName");

            ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName");
            ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == 999), "ShopID", "ShopName");
            ViewBag.Cell = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0 && m.PlantID == 999), "CellID", "CellName");
            ViewBag.WCID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.PlantID == 999), "MachineID", "MachineInvNo");
            
            
            return View();
        }
        [HttpPost]
        public ActionResult Create(tblshiftplanner tsp, string PlantID, string ShopID, string CellID, string MachineID, int ShiftMethod = 0, int SelectedDropDown = 0, string method = null, int shiftOverrideConfirm = 0)
        {
            string WorkCenterID = MachineID;
            ShiftDetails sd = new ShiftDetails();
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserId"]);
            ViewBag.radiobutton = method;

            #region ActiveLog Code
            // ActiveLogStorage Obj = new ActiveLogStorage();
            // Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            #endregion

            //validate plan overlapping
            List<int> DoesThisPlanOverlapUpwards = new List<int>(), DoesThisPlanOverlapDownwards = new List<int>(), DoesThisPlanOverlapAll = new List<int>();
            string startdatestring = tsp.StartDate.ToString("yyyy-MM-dd");
            string enddatestring = tsp.EndDate.ToString("yyyy-MM-dd");
            string oldEndDate = tsp.StartDate.AddDays(-1).ToString("yyyy-MM-dd");
            int FactorID = SelectedDropDown;

            tsp.ShiftMethodID = ShiftMethod;

            //New Code: 2016-10-01
            #region
            if (!String.IsNullOrEmpty(ShopID))
            {
                if (!String.IsNullOrEmpty( CellID))
                {
                    if (!String.IsNullOrEmpty(WorkCenterID))
                    {
                        int wcid = Convert.ToInt32(WorkCenterID);
                        DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForMachine(startdatestring, enddatestring, wcid);
                        DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForMachineDownwards(startdatestring, enddatestring, wcid);
                    }
                    else
                    {
                        int cellid = Convert.ToInt32(CellID);
                        DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForCell(startdatestring, enddatestring, cellid);
                        DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForCellDownwards(startdatestring, enddatestring, cellid);
                    }
                }
                else
                {
                    int shopid = Convert.ToInt32(ShopID);
                    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForShop(startdatestring, enddatestring, shopid);
                    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForShopDownwards(startdatestring, enddatestring, shopid);
                }
            }
            else
            {
                int plantid = Convert.ToInt32(PlantID);
                DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForPlant(startdatestring, enddatestring, plantid);
                DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForPlantDownwards(startdatestring, enddatestring, plantid);
            }
            #endregion

            #region OLD
            //if (method == "Plant")
            //{
            //    tsp.PlantID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", SelectedDropDown);
            //    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForPlant(startdatestring, enddatestring, FactorID);
            //    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForPlantDownwards(startdatestring, enddatestring, FactorID);
            //}
            //else if (method == "Shop")
            //{
            //    tsp.ShopID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName", SelectedDropDown);
            //    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForShop(startdatestring, enddatestring, FactorID);
            //    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForShopDownwards(startdatestring, enddatestring, FactorID);
            //}
            //else if (method == "Cell")
            //{
            //    tsp.CellID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0), "CellID", "CellName", SelectedDropDown);
            //    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForCell(startdatestring, enddatestring, FactorID);
            //    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForCellDownwards(startdatestring, enddatestring, FactorID);
            //}
            //else if (method == "Machine")
            //{
            //    tsp.MachineID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName", SelectedDropDown);
            //    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForMachine(startdatestring, enddatestring, FactorID);
            //    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForMachineDownwards(startdatestring, enddatestring, FactorID);
            //}
            #endregion

            //move all id's into one list.
            DoesThisPlanOverlapAll.AddRange(DoesThisPlanOverlapUpwards);
            DoesThisPlanOverlapAll.AddRange(DoesThisPlanOverlapDownwards);

            if (DoesThisPlanOverlapAll.Count == 0) //plan doesn't ovelap. So commit.
            {
                tsp.StartDate = Convert.ToDateTime(startdatestring).Date;
                tsp.EndDate = Convert.ToDateTime(enddatestring).Date;
                tsp.CreatedBy = UserID;
                tsp.CreatedOn = DateTime.Now;
                tsp.IsDeleted = 0;

                db.tblshiftplanners.Add(tsp);
                db.SaveChanges();
            }
            else
            {

                //get details of ovelapping plans and send for confirmation, If confirmed(shiftOverrideConfirm == 1) commit.
                if (shiftOverrideConfirm == 1)
                {
                    tsp.StartDate = Convert.ToDateTime(startdatestring).Date;
                    tsp.EndDate = Convert.ToDateTime(enddatestring).Date;
                    tsp.CreatedBy = UserID;
                    tsp.CreatedOn = DateTime.Now;
                    tsp.IsDeleted = 0;

                    db.tblshiftplanners.Add(tsp);
                    db.SaveChanges();

                    //now remove the old plans.
                    var results = db.tblshiftplanners.Where(m => m.IsDeleted == 0).Where(x => DoesThisPlanOverlapAll.Contains(x.ShiftPlannerID));

                    foreach (var row in results)
                    {
                        int id = row.ShiftPlannerID;
                        bool tick = sd.IsThisPlanInAction(id);
                        if (tick)
                        {
                            row.PlanStoppedDate = Convert.ToDateTime(row.EndDate);
                            row.EndDate = Convert.ToDateTime(oldEndDate);
                            row.IsPlanRemoved = 0;
                            row.IsPlanStopped = 1;
                            row.IsDeleted = 0;
                        }
                        else
                        {
                            row.PlanStoppedDate = Convert.ToDateTime(oldEndDate);
                            row.IsPlanStopped = 0;
                            row.IsPlanRemoved = 1;
                            row.IsDeleted = 1;
                        }
                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                else
                {
                    TempData["Error"] = "Shift Planner exists for this Duration";

                    //string OLPD = "<div><p><span>Planner Name</span><span>StartDate</span><span>End Date</span></p></div>";
                    var results = db.tblshiftplanners.Where(m=>m.IsDeleted == 0).Where(x => DoesThisPlanOverlapAll.Contains(x.ShiftPlannerID));

                    string OLPD = "<div  style='font-size:.75vw'>";
                    foreach (var row in results)
                    {
                        int planId = row.ShiftPlannerID;
                        bool tick = sd.IsThisPlanInAction(planId);

                        OLPD += "<p><span>Shift_Planner Name : " + row.ShiftPlannerName + "</span></p>";
                        if (tick)
                        {
                            OLPD += "<span></br>This Plan is In Action</br></span>";
                        }
                        OLPD += "<span> Start Date : " + row.StartDate.ToString("yyyy-MM-dd") + "</span></p>";
                        OLPD += "<span>End Date : " + row.EndDate.ToString("yyyy-MM-dd") + "</span></p>";
                    }
                    OLPD += "</div>";

                    ViewBag.OverLappingPlanDetails = OLPD;
                    
                    ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName", tsp.ShiftMethodID);

                    ViewBag.PlantID = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", tsp.PlantID);
                    ViewBag.ShopID = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == tsp.PlantID), "ShopID", "ShopName", tsp.ShopID);
                    ViewBag.CellID = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == tsp.ShopID), "CellID", "CellName", tsp.CellID);
                    if (tsp.CellID != null || tsp.CellID != 0)
                    {
                        ViewBag.WCID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.CellID == tsp.CellID), "MachineID", "MachineInvNo", tsp.MachineID);
                    }
                    else
                    {
                        ViewBag.WCID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == tsp.ShopID), "MachineID", "MachineInvNo", tsp.MachineID);
                    }

                    return View(tsp);
                }
            }

            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {

            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            tblshiftplanner tsp = db.tblshiftplanners.Find(id);
            if (tsp == null)
            {
                return HttpNotFound();
            }

            //this is a Seperate Class
            ShiftDetails sd = new ShiftDetails();
            if (sd.IsThisPlanInAction(id))
            {
                TempData["Error"] = "This Plan is in Action Cannot Edit";
                return RedirectToAction("Index");
            }

            //int ShiftMethod = 0;
            //if ( !DBNull.Value.Equals(tsp.PlantID))
            //{
            //    ShiftMethod = Convert.ToInt32(tsp.PlantID);
            //    ViewBag.SelectedDropDown = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", ShiftMethod);
            //    ViewBag.radiobutton = "Plant";
            //}
            //else if (!DBNull.Value.Equals(tsp.ShopID))
            //{
            //    ShiftMethod = Convert.ToInt32(tsp.ShopID);
            //    ViewBag.SelectedDropDown = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName", ShiftMethod);
            //    ViewBag.radiobutton = "Shop";
            //}
            //else if (!DBNull.Value.Equals(tsp.CellID))
            //{
            //    ShiftMethod = Convert.ToInt32(tsp.CellID);
            //    ViewBag.SelectedDropDown = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0), "CellID", "CellName", ShiftMethod);
            //    ViewBag.radiobutton = "Cell";
            //}
            //else if (!DBNull.Value.Equals(tsp.MachineID))
            //{
            //    ShiftMethod = Convert.ToInt32(tsp.MachineID);
            //    ViewBag.SelectedDropDown = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName", ShiftMethod);
            //    ViewBag.radiobutton = "Machine";
            //}


            ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", tsp.PlantID);
            ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == tsp.PlantID), "ShopID", "ShopName", tsp.ShopID);
            ViewBag.Cell = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == tsp.ShopID), "CellID", "CellName", tsp.CellID);
            if (tsp.CellID != null || tsp.CellID != 0)
            {
                ViewBag.WCID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.CellID == tsp.CellID), "MachineID", "MachineInvNo", tsp.MachineID);
            }
            else
            {
                ViewBag.WCID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == tsp.ShopID), "MachineID", "MachineInvNo", tsp.MachineID);
            }


            ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName", tsp.ShiftMethodID);

            #region same using try catch
            //try
            //{
            //    ShiftMethod = Convert.ToInt32(tsp.PlantID);
            //}
            //catch
            //{
            //    try
            //    {
            //        ShiftMethod = Convert.ToInt32(tsp.ShopID);
            //    }
            //    catch
            //    {
            //        try
            //        {
            //            ShiftMethod = Convert.ToInt32(tsp.CellID);
            //        }
            //        catch
            //        {
            //            ShiftMethod = Convert.ToInt32(tsp.MachineID);
            //        }
            //    }
            //}

            //tsp.ShiftMethodID = ShiftMethod;
            //if (method == "Plant")
            //{
            //    SelectedDropDown = tsp.PlantID;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", SelectedDropDown);
            //}
            //else if (method == "Shop")
            //{
            //    tsp.ShopID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName", SelectedDropDown);
            //}
            //else if (method == "Cell")
            //{
            //    tsp.CellID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0), "CellID", "CellName", SelectedDropDown);
            //}
            //else if (method == "Machine")
            //{
            //    SelectedDropDown = tsp.MachineID;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName", SelectedDropDown);
            //}
            //ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName", tsp.ShiftMethodID);
            #endregion

            return View(tsp);
        }
        [HttpPost]
        public ActionResult Edit(tblshiftplanner tsp, string PlantID, string ShopID, string CellID, string MachineID, int ShiftMethod = 0, int SelectedDropDown = 0, string method = null, int shiftOverrideConfirm = 0)
        {
            ShiftDetails sd = new ShiftDetails();
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserId"]);
            ViewBag.radiobutton = method;

            #region ActiveLog Code
            // ActiveLogStorage Obj = new ActiveLogStorage();
            // Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            #endregion

            //validate plan overlapping
            List<int> DoesThisPlanOverlapUpwards = new List<int>(), DoesThisPlanOverlapDownwards = new List<int>(), DoesThisPlanOverlapAll = new List<int>();
            string startdatestring = tsp.StartDate.ToString("yyyy-MM-dd");
            string oldEndDate = tsp.StartDate.AddDays(-1).ToString("yyyy-MM-dd");
            string enddatestring = tsp.EndDate.ToString("yyyy-MM-dd");

            int FactorID = SelectedDropDown;
            tsp.ShiftMethodID = ShiftMethod;
           
            int ShiftPlannerID = tsp.ShiftPlannerID;

            //New Code: 2016-10-01
            #region
            if (!String.IsNullOrEmpty(ShopID))
            {
                if (!String.IsNullOrEmpty(CellID))
                {
                    if (!String.IsNullOrEmpty(MachineID))
                    {
                        int wcid = Convert.ToInt32(MachineID);
                        DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForMachine(startdatestring, enddatestring, wcid, ShiftPlannerID);
                        DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForMachineDownwards(startdatestring, enddatestring, wcid, ShiftPlannerID);
                    }
                    else
                    {
                        int cellid = Convert.ToInt32(CellID);
                        DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForCell(startdatestring, enddatestring, cellid, ShiftPlannerID);
                        DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForCellDownwards(startdatestring, enddatestring, cellid, ShiftPlannerID);
                    }
                }
                else
                {
                    int shopid = Convert.ToInt32(ShopID);
                    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForShop(startdatestring, enddatestring, shopid, ShiftPlannerID);
                    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForShopDownwards(startdatestring, enddatestring, shopid, ShiftPlannerID);
                }
            }
            else
            {
                int plantid = Convert.ToInt32(PlantID);
                DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForPlant(startdatestring, enddatestring, plantid, ShiftPlannerID);
                DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForPlantDownwards(startdatestring, enddatestring, plantid, ShiftPlannerID);
            }
            #endregion

            #region OLD
            //if (method == "Plant")
            //{
            //    tsp.PlantID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", SelectedDropDown);
            //    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForPlant(startdatestring, enddatestring, FactorID);
            //    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForPlantDownwards(startdatestring, enddatestring, FactorID);
            //}
            //else if (method == "Shop")
            //{
            //    tsp.ShopID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName", SelectedDropDown);
            //    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForShop(startdatestring, enddatestring, FactorID);
            //    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForShopDownwards(startdatestring, enddatestring, FactorID);
            //}
            //else if (method == "Cell")
            //{
            //    tsp.CellID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0), "CellID", "CellName", SelectedDropDown);
            //    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForCell(startdatestring, enddatestring, FactorID);
            //    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForCellDownwards(startdatestring, enddatestring, FactorID);
            //}
            //else if (method == "Machine")
            //{
            //    tsp.MachineID = SelectedDropDown;
            //    ViewBag.SelectedDropDown = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName", SelectedDropDown);
            //    DoesThisPlanOverlapUpwards = Plan_OverlapCheckerForMachine(startdatestring, enddatestring, FactorID);
            //    DoesThisPlanOverlapDownwards = Plan_OverlapCheckerForMachineDownwards(startdatestring, enddatestring, FactorID);
            //}

            //move all id's into one list.
            //int count = 0;
            //for (int j = 0; j < DoesThisPlanOverlapUpwards.Count(); j++)
            //{
            //    if (DoesThisPlanOverlapUpwards[j] > 0)
            //    {
            //        DoesThisPlanOverlapAll[j] = DoesThisPlanOverlapUpwards[j];
            //        count++;
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}
            //for (int j = 0, k = count; j < (DoesThisPlanOverlapDownwards.Count() + count); j++)
            //{
            //    if (DoesThisPlanOverlapDownwards[j] > 0)
            //    {
            //        DoesThisPlanOverlapAll[k++] = DoesThisPlanOverlapDownwards[j];
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}
            #endregion

            DoesThisPlanOverlapAll.AddRange(DoesThisPlanOverlapUpwards);
            DoesThisPlanOverlapAll.AddRange(DoesThisPlanOverlapDownwards);

            if (DoesThisPlanOverlapAll.Count == 0) //plan doesn't ovelap. So commit.
            {
                tsp.StartDate = Convert.ToDateTime(startdatestring).Date;
                tsp.EndDate = Convert.ToDateTime(enddatestring).Date;
                tsp.CreatedBy = UserID;
                tsp.CreatedOn = DateTime.Now;
                tsp.IsDeleted = 0;

                db.Entry(tsp).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                //get details of ovelapping plans and send for confirmation, If confirmed(shiftOverrideConfirm == 1) commit.
                if (shiftOverrideConfirm == 1)
                {
                    tsp.StartDate = Convert.ToDateTime(startdatestring).Date;
                    tsp.EndDate = Convert.ToDateTime(enddatestring).Date;
                    tsp.CreatedBy = UserID;
                    tsp.CreatedOn = DateTime.Now;
                    tsp.IsDeleted = 0;

                    db.Entry(tsp).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //now remove the old plans.
                    var results = db.tblshiftplanners.Where(m => m.IsDeleted == 0).Where(x => DoesThisPlanOverlapAll.Contains(x.ShiftPlannerID));

                    foreach (var row in results)
                    {
                        int id = row.ShiftPlannerID;
                        bool tick = sd.IsThisPlanInAction(id);
                        if (tick)
                        {
                            row.PlanStoppedDate = Convert.ToDateTime(row.EndDate);
                            row.EndDate = Convert.ToDateTime(oldEndDate);
                            row.IsPlanStopped = 1;
                            row.IsDeleted = 0;
                        }
                        else
                        {
                            row.PlanStoppedDate = Convert.ToDateTime(oldEndDate);
                            row.IsPlanRemoved = 1;
                            row.IsDeleted = 1;
                        }
                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                else
                {
                    TempData["Error"] = "Shift Planner exists for this Duration";

                    //string OLPD = "<div><p><span>Planner Name</span><span>StartDate</span><span>End Date</span></p></div>";
                    var results = db.tblshiftplanners.Where(m => m.IsDeleted == 0).Where(x => DoesThisPlanOverlapAll.Contains(x.ShiftPlannerID));

                    string OLPD = "<div style='font-size:.75vw'>";
                    foreach (var row in results)
                    {
                        int planId = row.ShiftPlannerID;
                        bool tick = sd.IsThisPlanInAction(planId);

                        OLPD += "<p><span>Shift_Planner Name : " + row.ShiftPlannerName + "</span></p>";
                        if (tick)
                        {
                            OLPD += "<span></br>This Plan is In Action</br></span>";
                        }
                        OLPD += "</p><span> Start Date : " + row.StartDate.ToString("yyyy-MM-dd") + "</span></p>";
                        OLPD += "</p><span>End Date : " + row.EndDate.ToString("yyyy-MM-dd") + "</span></p>";
                    }
                    OLPD += "</div>";
                    ViewBag.OverLappingPlanDetails = OLPD;
                    ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName", tsp.ShiftMethodID);

                    ViewBag.PlantID = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", tsp.PlantID);
                    ViewBag.ShopID = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == tsp.PlantID), "ShopID", "ShopName", tsp.ShopID);
                    ViewBag.CellID = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == tsp.ShopID), "CellID", "CellName", tsp.CellID);
                    if (tsp.CellID != null || tsp.CellID != 0)
                    {
                        ViewBag.WCID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.CellID == tsp.CellID), "MachineID", "MachineInvNo", tsp.MachineID);
                    }
                    else
                    {
                        ViewBag.WCID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == tsp.ShopID), "MachineID", "MachineInvNo", tsp.MachineID);
                    }


                    return View(tsp);
                }
            }
            return RedirectToAction("Index");
        }
        
        public ActionResult Delete(int id)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID1 = id;
            //ViewBag.IsConfigMenu = 0;

            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "Deleted Role";
            //Action = "Delete";
            // ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End

            //check if this plan is in action before deleting.

            ShiftDetails sd = new ShiftDetails();
            bool tick = sd.IsThisPlanInAction(id);

            if (!tick)
            {
                tblshiftplanner tblmc = db.tblshiftplanners.Find(id);
                tblmc.IsDeleted = 1;
                tblmc.ModifiedBy = UserID;
                tblmc.ModifiedOn = DateTime.Now;
                db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                
            }
            else
            {
                TempData["Error"] = "Opps! This plan is in Action . Cannot Delete.";
            }
            return RedirectToAction("Index");
        }

        //Not Using 2016-10-01
        public JsonResult GetDropDownValues(string method)
        {
            ////selectedRow has been initialized because as var initialization workaround.
            var selectedRow = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName");

            //if (method == "Plant")
            //{
            //    selectedRow = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName");
            //}
            //else if (method == "Shop")
            //{
            //    selectedRow = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName");
            //}
            //else if (method == "Cell")
            //{
            //    selectedRow = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0), "CellID", "CellName");
            //}
            //else if (method == "Machine")
            //{
            //    selectedRow = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName");
            //}
            return Json(selectedRow, JsonRequestBehavior.AllowGet);
        }

        public List<int> Plan_OverlapCheckerForPlant(string startdatestring, string enddatestring, int FactorID, int ShiftPlannerID = 0)
        {
            List<int> overlappingPlanId = new List<int>();
            int PlantID = FactorID;
            DataTable dataHolder = new DataTable();
            MsqlConnection mc = new MsqlConnection();
            mc.open();
            String sql = null;
            if (ShiftPlannerID != 0)
            {
                sql = "SELECT ShiftPlannerID FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "')OR (StartDate <='" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND PlantID =" + PlantID + " AND ShopID is null AND CellID is null AND MachineID is null  and IsDeleted = 0 and ShiftPlannerID != " + ShiftPlannerID + "  ORDER BY ShiftPlannerID ASC";
            }
            else
            {
                sql = "SELECT ShiftPlannerID FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "')OR (StartDate <='" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND PlantID =" + PlantID + " AND ShopID is null AND CellID is null AND MachineID is null  and IsDeleted = 0 ORDER BY ShiftPlannerID ASC";
            }
            MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
            da.Fill(dataHolder);
            mc.close();
            for (int i = 0; i < dataHolder.Rows.Count; i++)
            {
                overlappingPlanId.Add( Convert.ToInt32(dataHolder.Rows[i][0]));
            }
            return overlappingPlanId;
        }
        public List<int> Plan_OverlapCheckerForShop(string startdatestring, string enddatestring, int FactorID, int ShiftPlannerID = 0)
        {
            List<int> overlappingPlanId = new List<int>();
            int ShopID = FactorID;
            
            //1st check if its Plant has a Plan.
            //so get its plantid.
            var plantdetails = db.tblshops.Where(m => m.IsDeleted == 0 && m.ShopID == ShopID).FirstOrDefault();
            int plantId = plantdetails.PlantID;
            overlappingPlanId = Plan_OverlapCheckerForPlant(startdatestring, enddatestring, plantId,ShiftPlannerID);

            if (overlappingPlanId.Count == 0)
            {
                DataTable dataHolder = new DataTable();
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                String sql = null;
                if (ShiftPlannerID != 0)
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "')OR( StartDate <='" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND ShopID =" + ShopID + " AND CellID is null AND MachineID is null   and IsDeleted = 0 and ShiftPlannerID != " + ShiftPlannerID + "  ORDER BY ShiftPlannerID ASC";
                }
                else
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "')OR( StartDate <='" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND ShopID =" + ShopID + " AND CellID is null AND MachineID is null  and IsDeleted = 0  ORDER BY ShiftPlannerID ASC";
                }
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();

                for (int i = 0; i < dataHolder.Rows.Count; i++)
                {
                    overlappingPlanId.Add(Convert.ToInt32(dataHolder.Rows[i][0]));
                }
            }
            return overlappingPlanId;
        }
        public List<int> Plan_OverlapCheckerForCell(string startdatestring, string enddatestring, int FactorID, int ShiftPlannerID = 0)
        {
            List<int> overlappingPlanId = new List<int>();
            int CellID = FactorID;
            DataTable dataHolder = new DataTable();
            //1st check if its Shop has a Plan.
            //so get its shopid.
            var Celldetails = db.tblcells.Where(m => m.IsDeleted == 0 && m.CellID == CellID).FirstOrDefault();
            int shopId = Celldetails.ShopID;
            overlappingPlanId = Plan_OverlapCheckerForShop(startdatestring, enddatestring, shopId, ShiftPlannerID);

            if (overlappingPlanId.Count == 0)
            {
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                String sql = null;
                if (ShiftPlannerID != 0)
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR ( StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND CellID =" + CellID + " AND MachineID is null  and IsDeleted = 0  and ShiftPlannerID != " + ShiftPlannerID + "  ORDER BY ShiftPlannerID ASC"; 
                }
                else
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR ( StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND CellID =" + CellID + " AND MachineID is null  and  IsDeleted = 0  ORDER BY ShiftPlannerID ASC"; 
                }
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();

                for (int i = 0; i < dataHolder.Rows.Count; i++)
                {
                    overlappingPlanId.Add(Convert.ToInt32(dataHolder.Rows[i][0]));
                }

            }
            return overlappingPlanId;
        }
        public List<int> Plan_OverlapCheckerForMachine(string startdatestring, string enddatestring, int FactorID, int ShiftPlannerID = 0)
        {
            List<int> overlappingPlanId = new List<int>(), overlappingPlanId1 = new List<int>(), overlappingPlanId2 = new List<int>();
            int MachineID = FactorID;
            DataTable dataHolder = new DataTable();

            //1st check if it has a Cell else go for Shop

            var machinedetails = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.MachineID == MachineID).FirstOrDefault();
            if (machinedetails.CellID.HasValue)
            {
                int cellId = Convert.ToInt32(machinedetails.CellID);
                overlappingPlanId = Plan_OverlapCheckerForCell(startdatestring, enddatestring, cellId, ShiftPlannerID);
            }
            else
            {
                int shopId = Convert.ToInt32(machinedetails.ShopID);
                overlappingPlanId1 = Plan_OverlapCheckerForShop(startdatestring, enddatestring, shopId, ShiftPlannerID);
            }

            //move all id's into one list.
            overlappingPlanId2.AddRange(overlappingPlanId);
            overlappingPlanId2.AddRange(overlappingPlanId1);

            if (overlappingPlanId2.Count == 0)
            {
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                String sql = null;
                if (ShiftPlannerID != 0)
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND MachineID =" + MachineID + "  and IsDeleted = 0 and ShiftPlannerID != " + ShiftPlannerID + "  ORDER BY ShiftPlannerID ASC";
                }
                else
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND MachineID =" + MachineID + "  and IsDeleted = 0  ORDER BY ShiftPlannerID ASC";
                }
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();

                for (int i = 0; i < dataHolder.Rows.Count; i++)
                {
                    overlappingPlanId2.Add(Convert.ToInt32(dataHolder.Rows[i][0]));
                }
            }
            return overlappingPlanId2;
        }

        public List<int> Plan_OverlapCheckerForPlantDownwards(string startdatestring, string enddatestring, int FactorID, int ShiftPlannerID = 0)
        {
            List<int> overlappingPlanId = new List<int>();
            int PlantID = FactorID;
            DataTable dataHolder = new DataTable();


            //1st check if its shop has a Plan.
            //so get its shopid.
            var shopdetails = db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == PlantID).ToList();
            foreach (var shoprow in shopdetails)
            {
                int shopId = shoprow.ShopID;
                overlappingPlanId = Plan_OverlapCheckerForShopDownwards(startdatestring, enddatestring, shopId, ShiftPlannerID);
                if (overlappingPlanId.Count > 0)
                {
                    break;
                }
            }

            if (overlappingPlanId.Count == 0)
            {
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                String sql = null;
              
                if (ShiftPlannerID != 0)
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND PlantID =" + PlantID + "   and IsDeleted = 0 and ShiftPlannerID != " + ShiftPlannerID + " ORDER BY ShiftPlannerID ASC";
                }
                else
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND PlantID =" + PlantID + "   and IsDeleted = 0  ORDER BY ShiftPlannerID ASC";
                }
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();

                for (int i = 0; i < dataHolder.Rows.Count; i++)
                {
                    overlappingPlanId.Add(Convert.ToInt32(dataHolder.Rows[i][0]));
                }
            }
            return overlappingPlanId;
        }
        public List<int> Plan_OverlapCheckerForShopDownwards(string startdatestring, string enddatestring, int FactorID, int ShiftPlannerID = 0)
        {
            List<int> overlappingPlanId = new List<int>(), overlappingPlanId1 = new List<int>(), overlappingPlanId2 = new List<int>();
            int ShopID = FactorID;

            //1st check if its Cells has a Plan.
            //so get its cellid.
            var celldetails = db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == ShopID).ToList();
            foreach (var cellrow in celldetails)
            {
                int cellId = cellrow.CellID;
                overlappingPlanId = Plan_OverlapCheckerForCellDownwards(startdatestring, enddatestring, cellId, ShiftPlannerID);
                if (overlappingPlanId.Count > 0)
                {
                    break;
                }
            }

            var machinedetails = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == ShopID).ToList();
            foreach (var machinerow in machinedetails)
            {
                int machineId = machinerow.MachineID;
                overlappingPlanId1 = Plan_OverlapCheckerForMachineDownwards(startdatestring, enddatestring, machineId, ShiftPlannerID);
                if (overlappingPlanId1.Count > 0)
                {
                    break;
                }
            }

            //move all id's into one list.
            overlappingPlanId2.AddRange(overlappingPlanId);
            overlappingPlanId2.AddRange(overlappingPlanId1);

            if (overlappingPlanId2.Count == 0)
            {
                DataTable dataHolder = new DataTable();
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                String sql = null;
                if (ShiftPlannerID != 0)
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE (( StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND ShopID =" + ShopID + "  and IsDeleted = 0  and ShiftPlannerID != " + ShiftPlannerID + " ORDER BY ShiftPlannerID ASC";
                }
                else
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE (( StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND ShopID =" + ShopID + "  and IsDeleted = 0  ORDER BY ShiftPlannerID ASC";
                }
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();
                for (int i = 0; i < dataHolder.Rows.Count; i++)
                {
                    overlappingPlanId2.Add(Convert.ToInt32(dataHolder.Rows[i][0]));
                }
            }
            return overlappingPlanId2;
        }
        public List<int> Plan_OverlapCheckerForCellDownwards(string startdatestring, string enddatestring, int FactorID, int ShiftPlannerID = 0)
        {
            List<int> overlappingPlanId = new List<int>();
            int CellID = FactorID;
            DataTable dataHolder = new DataTable();
            //1st check if its machines has a Plan.
            //so get its machineids.
            var machinedetails = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.CellID == CellID).ToList();
            foreach (var machinerow in machinedetails)
            {
                int machineId = machinerow.MachineID;
                overlappingPlanId = Plan_OverlapCheckerForMachineDownwards(startdatestring, enddatestring, machineId, ShiftPlannerID);
                if (overlappingPlanId.Count > 0)
                {
                    break;
                }
            }

            if (overlappingPlanId.Count == 0)
            {
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                String sql = null;
                if (ShiftPlannerID != 0)
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND CellID =" + CellID + "  and IsDeleted = 0  and ShiftPlannerID != " + ShiftPlannerID + " ORDER BY ShiftPlannerID ASC";
                }
                else
                {
                    sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND CellID =" + CellID + "  and IsDeleted = 0  ORDER BY ShiftPlannerID ASC";
                }
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();
                for (int i = 0; i < dataHolder.Rows.Count; i++)
                {
                    overlappingPlanId.Add(Convert.ToInt32(dataHolder.Rows[i][0]));
                }

            }
            return overlappingPlanId;
        }
        public List<int> Plan_OverlapCheckerForMachineDownwards(string startdatestring, string enddatestring, int FactorID, int ShiftPlannerID = 0)
        {
            List<int> overlappingPlanId =  new List<int>();
            int MachineID = FactorID;
            DataTable dataHolder = new DataTable();

            MsqlConnection mc = new MsqlConnection();
            mc.open();
             String sql = null;
             if (ShiftPlannerID != 0)
             {
                 sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND MachineID =" + MachineID + "  and IsDeleted = 0  and ShiftPlannerID != " + ShiftPlannerID + " ORDER BY ShiftPlannerID ASC";
             }
             else
             {
                 sql = "SELECT * FROM tblShiftPlanner WHERE ((StartDate <='" + startdatestring + "' AND EndDate >='" + startdatestring + "') OR (StartDate <'" + enddatestring + "' AND EndDate >='" + enddatestring + "')) AND MachineID =" + MachineID + "  and IsDeleted = 0  ORDER BY ShiftPlannerID ASC";
             }
            MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
            da.Fill(dataHolder);
            mc.close();

            for (int i = 0; i < dataHolder.Rows.Count; i++)
            {
                overlappingPlanId.Add(Convert.ToInt32(dataHolder.Rows[i][0]));
            }
            return overlappingPlanId;
        }

        public JsonResult CheckIfThisPlanIsInAction(int id)
        {
            //if nothing == 0 you will let him edit .
            int nothing = 1;
            ShiftDetails sd = new ShiftDetails();
            nothing = sd.IsThisPlanInAction(id) == true ? 1 : 0;
            return Json(nothing, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetShop(int PlantID)
        {
            var ShopData = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == PlantID), "ShopID", "ShopName");
            return Json(ShopData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCell(int ShopID)
        {
            var CellData = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == ShopID), "CellID", "CellName");
            return Json(CellData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetWC_Cell(int CellID)
        {
            var MachineData = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.CellID == CellID && m.IsNormalWC == 0 ), "MachineID", "MachineInvNo");
            return Json(MachineData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetWC_Shop(int ShopID)
        {
            var MachineData = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == ShopID && m.CellID.Equals(null) && m.IsNormalWC == 0), "MachineID", "MachineInvNo");
            return Json(MachineData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetWC_Cell_MWC(int CellID)
        {
            var MachineData = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.CellID == CellID && m.ManualWCID.Equals(null)), "MachineID", "MachineInvNo");
            return Json(MachineData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetWC_Shop_MWC(int ShopID)
        {
            var MachineData = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == ShopID && m.CellID.Equals(null) && m.ManualWCID.Equals(null)), "MachineID", "MachineInvNo");
            return Json(MachineData, JsonRequestBehavior.AllowGet);
        }

    }
}
