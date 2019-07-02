using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace Tata.Controllers
{
    public class MachineAllocationController : Controller
    {
        private mazakdaqEntities db = new mazakdaqEntities();

        //
        // GET: /ShiftMaster/

        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            //return View(db.tblmachineallocations.Include(t => t.tblmachinedetail).Include(t => t.tblshift_mstr).Include(t => t.tbluser).Where(m => m.IsDeleted == 0).ToList());
            return View(db.tblmachineallocations.Include(t => t.tblmachinedetail).Include(t => t.tbluser).Where(m => m.IsDeleted == 0).ToList());
        }

        //
        // GET: /ShiftMaster/Details/5

        public ActionResult Details(int id = 0)
        {
            tblshift_mstr tblshift_mstr = db.tblshift_mstr.Find(id);
            if (tblshift_mstr == null)
            {
                return HttpNotFound();
            }
            return View(tblshift_mstr);
        }

        //
        // GET: /ShiftMaster/Create

        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            ViewBag.UserID = new SelectList(db.tblusers.Where(m => m.IsDeleted == 0 && m.PrimaryRole==3), "UserID", "DisplayName");
            ViewBag.ShiftID = new SelectList(db.tblshift_mstr.Where(m => m.IsDeleted == 0), "ShiftID", "ShiftName");
            ViewBag.MachineID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName");
            return View();
        }

        //
        // POST: /ShiftMaster/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblmachineallocation tblmachine)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).SingleOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            tblmachine.CreatedBy = Convert.ToInt32(Session["UserId"]);
            tblmachine.CreatedOn = DateTime.Now;
            tblmachine.CorrectedDate = CorrectedDate;
            tblmachine.IsDeleted = 0;

            if (ModelState.IsValid)
            {
                db.tblmachineallocations.Add(tblmachine);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserID = new SelectList(db.tblusers.Where(m => m.IsDeleted == 0 && m.PrimaryRole == 3), "UserID", "DisplayName");
            ViewBag.ShiftID = new SelectList(db.tblshift_mstr.Where(m => m.IsDeleted == 0), "ShiftID", "ShiftName");
            ViewBag.MachineID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName");
            return View(tblmachine);
        }

        //
        // GET: /ShiftMaster/Edit/5

        public ActionResult Edit(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            tblmachineallocation tblmachineallocatin = db.tblmachineallocations.Find(id);
            if (tblmachineallocatin == null)
            {
                return HttpNotFound();
            }
            ViewBag.UserID = new SelectList(db.tblusers.Where(m => m.IsDeleted == 0 && m.PrimaryRole == 3), "UserID", "DisplayName", tblmachineallocatin.UserID);
            ViewBag.ShiftID = new SelectList(db.tblshift_mstr.Where(m => m.IsDeleted == 0), "ShiftID", "ShiftName", tblmachineallocatin.ShiftID);
            ViewBag.MachineID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName", tblmachineallocatin.MachineID);
            return View(tblmachineallocatin);
        }

        //
        // POST: /ShiftMaster/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tblmachineallocation tblmachine)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            string CorrectedDate = null;
            tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).SingleOrDefault();
            TimeSpan Start = StartTime.StartTime;
            if (Start <= DateTime.Now.TimeOfDay)
            {
                CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            }
            tblmachine.CorrectedDate = CorrectedDate;
            tblmachine.ModifiedBy = Convert.ToInt32(Session["UserId"]);
            tblmachine.ModifiedOn = DateTime.Now;
            tblmachine.IsDeleted = 0;

            if (ModelState.IsValid)
            {
                db.Entry(tblmachine).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserID = new SelectList(db.tblusers.Where(m => m.IsDeleted == 0 && m.PrimaryRole == 3), "UserID", "DisplayName", tblmachine.UserID);
          //  ViewBag.ShiftID = new SelectList(db.tblshiftdetails_machinewise.Where(m => m.IsDeleted == 0).GroupBy(m => m.ShiftDetailsID), "ShiftDetailsID", "ShiftName");
            ViewBag.MachineID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName", tblmachine.MachineID);
            
            return View(tblmachine);
        }

        //
        // GET: /ShiftMaster/Delete/5

        public ActionResult Delete(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            tblmachineallocation tblmachine = db.tblmachineallocations.Find(id);
            if (tblmachine == null)
            {
                return HttpNotFound();
            }
            tblmachine.IsDeleted = 1;
            tblmachine.ModifiedBy = 1;
            tblmachine.ModifiedOn = System.DateTime.Now;
            db.Entry(tblmachine).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // POST: /ShiftMaster/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            tblshift_mstr tblshift_mstr = db.tblshift_mstr.Find(id);
            db.tblshift_mstr.Remove(tblshift_mstr);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
