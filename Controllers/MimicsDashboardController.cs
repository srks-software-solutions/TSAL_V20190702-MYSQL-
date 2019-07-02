using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace Tata.Controllers
{
    public class MimicsDashboardController : Controller
    {
        //
        // GET: /MimicsDashboard/

        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "MimicsDashboard";
        string Action = null;
        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            //get current date
            TimeSpan currentHourMint = new TimeSpan(05, 59, 59);
            TimeSpan RealCurrntHour = System.DateTime.Now.TimeOfDay;
            string CorrectedDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
            if (RealCurrntHour < currentHourMint)
            {
                CorrectedDate = DateTime.Now.AddDays(-1).Date.ToString("yyyy-MM-dd");
            }

            //DateTime Time = DateTime.Now;
            //TimeSpan Tm = new TimeSpan(Time.Hour, Time.Minute, Time.Second);
            //var ShiftDetails = db.tblshift_mstr.Where(m => m.StartTime <= Tm && m.EndTime >= Tm);
            //string Shift = null;
            //foreach (var a in ShiftDetails)
            //{
            //    Shift = a.ShiftName;
            //}

            ViewBag.date = System.DateTime.Now;
            //if (Shift != null)
            //    ViewBag.shift = Shift;
            //else
            //    ViewBag.shift = "C";
            // ViewBag.dateandtime = date + time;

            var machineids = from mid in db.tblmachinedetails where mid.IsDeleted == 0 select mid.MachineID;
            var mimicsdata = from mimics in db.tblmimics where mimics.CorrectedDate == CorrectedDate &&  machineids.Contains(mimics.MachineID) orderby mimics.MachineID orderby mimics.Shift  select mimics ;

           // var data = db.tblmimics.Include(t => t.tblmachinedetail).Where(m => m.CorrectedDate == CorrectedDate ).OrderBy(m=>m.MachineID).ThenBy(m=>m.Shift);
            
            #region old
            //foreach (var a in data)
            //{ }
           
            //For Page Rotation
            //int RotationCount = Convert.ToInt32(Session["Rotation"]);
            //if (RotationCount == 0)
            //{
            //    Session["Rotation"] = 1;
            //}
            //if (RotationCount == 5)
            //{
            //    Session["Rotation"] = 1;
            //    return RedirectToAction("Index", "MachineStatus", null);
            //}
            #endregion

            return View(mimicsdata.ToList());
            //return View(data.OrderBy(m => m.Shift).Where(m => m.CorrectedDate == CorrectedDate).ToList());
        }

    }
}
