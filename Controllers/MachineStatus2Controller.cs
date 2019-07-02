using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;
using Tata.Models;
using System.Data.Entity;
using TataMySqlConnection;
using MySql.Data.MySqlClient;

namespace TATA.Controllers
{
    public class MachineStatus2Controller : Controller
    {
        // GET: /AllMachineStatus/
        private mazakdaqEntities db = new mazakdaqEntities();
        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            Session["colordata"] = null;
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            //calculating Corrected Date
            TimeSpan currentHourMint = new TimeSpan(05, 59, 59);
            TimeSpan RealCurrntHour = System.DateTime.Now.TimeOfDay;
            string CorrectedDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
            if (RealCurrntHour < currentHourMint)
            {
                CorrectedDate = DateTime.Now.AddDays(-1).Date.ToString("yyyy-MM-dd");
            }

            // getting all machine details and their count.
            var macData = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.IsNormalWC == 0);
            int mc = macData.Count();
            ViewBag.macCount = mc;

            int[] macid = new int[mc];
            int macidlooper = 0;
            foreach (var v in macData)
            {
                macid[macidlooper++] = v.MachineID;
            }
            Session["macid"] = macid;
            ViewBag.macCount = mc;

            int[,] maindata = new int[mc, 5];
            //int[,] maindata = new int[mc, 6];
            // write a raw query to get sum of powerOff, Operating, Idle, BreakDown, PlannedMaintenance. 

            using (MsqlConnection mc1 = new MsqlConnection())
            {
                mc1.open();
                MySqlCommand cmd1 = new MySqlCommand("SELECT MachineID,sum(MachineOffTime) as op,sum(OperatingTime)as o,sum(IdleTime) as it,sum(BreakdownTime)as bt FROM mazakdaq.tblmimics where CorrectedDate='" + CorrectedDate + "'and MachineID IN (select distinct(MachineID) from tblmachinedetails where IsDeleted = 0 and IsNormalWC = 0) group by MachineID", mc1.msqlConnection);
                MySqlDataReader datareader = cmd1.ExecuteReader();
                int maindatalooper1 = 0;

                while (datareader.Read())
                {
                    int maindatalooper2 = 0;
                    maindata[maindatalooper1, maindatalooper2++] = datareader.GetInt32(0);
                    maindata[maindatalooper1, maindatalooper2++] = datareader.GetInt32(1);
                    maindata[maindatalooper1, maindatalooper2++] = datareader.GetInt32(2);
                    maindata[maindatalooper1, maindatalooper2++] = datareader.GetInt32(3);
                    maindata[maindatalooper1, maindatalooper2++] = datareader.GetInt32(4);
                    maindatalooper1++;
                }
                mc1.close();
            }
            Session["colordata"] = maindata;
            //var tblMainDT = db.tbllivedailyprodstatus.Include(t => t.tblmachinedetail).Where(m => m.CorrectedDate == CorrectedDate).OrderBy(m => m.StartTime);
            //return View(tblMainDT.ToList());

            //Get Modes for All Machines for Today
            //List<tbllivemodedb> tblModeDT = db.tblmodes.Where(m => m.CorrectedDate == CorrectedDate && m.tblmachinedetail.IsDeleted == 0 && m.tblmachinedetail.IsNormalWC == 0).OrderBy(m => m.MachineID).ThenBy(m => m.StartTime).ToList();

            List<tbllivemodedb> tblModeDT = db.tbllivemodedbs.Where(m => m.CorrectedDate == CorrectedDate && m.tblmachinedetail.IsDeleted == 0 && m.tblmachinedetail.IsNormalWC == 0 && m.IsCompleted == 1).OrderBy(m => m.MachineID).ThenBy(m => m.StartTime).ToList();
            List<tbllivemodedb> tblModeDTCurr = db.tbllivemodedbs.Where(m => m.CorrectedDate == CorrectedDate && m.tblmachinedetail.IsDeleted == 0 && m.tblmachinedetail.IsNormalWC == 0 && m.IsCompleted == 0).OrderBy(m => m.MachineID).ThenByDescending(m => m.ModeID).ToList();

            //Get Latest Mode for each machine and Update the DurationInSec Column
            List<tbllivemodedb> CurrentModesOfAllMachines = (from row in tblModeDT
                                                       where row.IsCompleted == 0
                                                       select row).ToList();
            int PrvMachineID = 0;
            foreach (var row in tblModeDTCurr)
            {
               // DateTime startDateTime = Convert.ToDateTime( row.StartTime);
               // int DurInSec = Convert.ToInt32( DateTime.Now.Subtract(startDateTime).TotalSeconds );
               // //row.DurationInSec = Convert.ToInt32( DateTime.Now.Subtract(startDateTime).TotalSeconds );
               // int ModeID = row.ModeID;
               //foreach ( var tom in tblModeDT.Where(w => w.ModeID == ModeID)) {
               //             tom.DurationInSec = DurInSec;
               //         }

                if (PrvMachineID != row.MachineID)
                {
                    DateTime startDateTime = Convert.ToDateTime(row.StartTime);
                    int DurInSec = Convert.ToInt32(DateTime.Now.Subtract(startDateTime).TotalSeconds);
                    //row.DurationInSec = Convert.ToInt32( DateTime.Now.Subtract(startDateTime).TotalSeconds );
                    int ModeID = row.ModeID;
                    row.DurationInSec = DurInSec;
                    tblModeDT.Add(row);
                    //foreach (var tom in tblModeDT.Where(w => w.ModeID == ModeID))
                    //{

                    //}
                    PrvMachineID = row.MachineID;
                }
            }
            List<DBMode> ShowMode = new List<DBMode>();
            //Update DurationInSec to Minutes
            foreach(var MainRow in tblModeDT.Where(m=>m.DurationInSec > 0)){
                DBMode ShowModeItem = new DBMode();
                ShowModeItem.ColorCode = MainRow.ColorCode;
                ShowModeItem.CorrectedDate = MainRow.CorrectedDate;
                ShowModeItem.DurationInSec = MainRow.DurationInSec / 60.00;
                ShowModeItem.EndTime = MainRow.EndTime;
                ShowModeItem.InsertedBy = MainRow.InsertedBy;
                ShowModeItem.InsertedOn = MainRow.InsertedOn;
                ShowModeItem.IsCompleted = MainRow.IsCompleted;
                ShowModeItem.IsDeleted = MainRow.IsDeleted;
                ShowModeItem.MachineID = MainRow.MachineID;
                ShowModeItem.Mode = MainRow.Mode;
                ShowModeItem.ModeID = MainRow.ModeID;
                ShowModeItem.ModifiedBy = MainRow.ModifiedBy;
                ShowModeItem.ModifiedOn = MainRow.ModifiedOn;
                ShowModeItem.StartTime = MainRow.StartTime;
                ShowModeItem.tblmachinedetail = MainRow.tblmachinedetail;
                ShowMode.Add(ShowModeItem);
                MainRow.DurationInSec = Convert.ToInt32(MainRow.DurationInSec / 60);
            };

            List<string> ShopNames = db.tbllivemodedbs.Where(m => m.CorrectedDate == CorrectedDate && m.tblmachinedetail.IsDeleted == 0 && m.tblmachinedetail.IsNormalWC == 0).Select(m=>m.tblmachinedetail.ShopNo).Distinct().ToList();
            ViewBag.DistinctShops = ShopNames;

            //return View(tblModeDT);
            return View(ShowMode);
        }

    }
}
