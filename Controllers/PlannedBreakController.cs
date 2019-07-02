using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace TATA.Controllers
{
    public class PlannedBreakController : Controller
    {
        private mazakdaqEntities db = new mazakdaqEntities();

        //
        // GET: /PlannedBreak/

        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            return View(db.tblplannedbreaks.Where(m=>m.IsDeleted==0).Include(m =>m.tblshift_mstr).ToList());
        }

        //
        // GET: /PlannedBreak/Details/5

        public ActionResult Details(int id = 0)
        {
            tblplannedbreak tblplannedbreak = db.tblplannedbreaks.Find(id);
            if (tblplannedbreak == null)
            {
                return HttpNotFound();
            }
            return View(tblplannedbreak);
        }

        //
        // GET: /PlannedBreak/Create

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

        //
        // POST: /PlannedBreak/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblplannedbreak tblplannedbreak)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            tblplannedbreak.CreatedBy = 1;
            tblplannedbreak.CreatedOn = DateTime.Now;
            tblplannedbreak.IsDeleted = 0;

            //if (ModelState.IsValid)
            {
                db.tblplannedbreaks.Add(tblplannedbreak);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tblplannedbreak);
        }

        //
        // GET: /PlannedBreak/Edit/5

        public ActionResult Edit(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            tblplannedbreak tblplannedbreak = db.tblplannedbreaks.Find(id);
            if (tblplannedbreak == null)
            {
                return HttpNotFound();
            }
            return View(tblplannedbreak);
        }

        //
        // POST: /PlannedBreak/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tblplannedbreak tblplannedbreak)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            tblplannedbreak.ModifiedBy = 1;
            tblplannedbreak.ModifiedOn = DateTime.Now;
            tblplannedbreak.IsDeleted = 0;

            //if (ModelState.IsValid)
            {
                db.Entry(tblplannedbreak).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tblplannedbreak);
        }

        //
        // GET: /PlannedBreak/Delete/5

        public ActionResult Delete(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            tblplannedbreak tblplannedbreak = db.tblplannedbreaks.Find(id);
            if (tblplannedbreak == null)
            {
                return HttpNotFound();
            }
            tblplannedbreak.IsDeleted = 1;
            tblplannedbreak.ModifiedBy = 1;
            tblplannedbreak.ModifiedOn = System.DateTime.Now;
            db.Entry(tblplannedbreak).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // POST: /PlannedBreak/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            tblplannedbreak tblplannedbreak = db.tblplannedbreaks.Find(id);
            db.tblplannedbreaks.Remove(tblplannedbreak);
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