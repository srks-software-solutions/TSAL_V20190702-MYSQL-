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
    public class MachineStatusController : Controller
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
            var tblMainDT = db.tbllivedailyprodstatus.Include(t => t.tblmachinedetail).Where(m => m.CorrectedDate == CorrectedDate).OrderBy(m => m.StartTime);
            return View(tblMainDT.ToList());
        }

    }
}
