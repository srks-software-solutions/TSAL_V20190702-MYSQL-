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
    public class ShiftMasterController : Controller
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
            return View(db.tblshift_mstr.Where(m => m.IsDeleted == 0).ToList());
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
            return View();
        }

        //
        // POST: /ShiftMaster/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblshift_mstr tblshift_mstr)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            tblshift_mstr.CreatedBy = Convert.ToInt32(Session["UserId"]);
            tblshift_mstr.CreatedOn = DateTime.Now;
            tblshift_mstr.IsDeleted =0;

            //if (ModelState.IsValid)
            {
                db.tblshift_mstr.Add(tblshift_mstr);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tblshift_mstr);
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
            tblshift_mstr tblshift_mstr = db.tblshift_mstr.Find(id);
            if (tblshift_mstr == null)
            {
                return HttpNotFound();
            }
            return View(tblshift_mstr);
        }

        //
        // POST: /ShiftMaster/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tblshift_mstr tblshift_mstr)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            tblshift_mstr.ModifiedBy = 1;
            tblshift_mstr.ModifiedOn = DateTime.Now;
            tblshift_mstr.IsDeleted = 0;

            if (ModelState.IsValid)
            {
                db.Entry(tblshift_mstr).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tblshift_mstr);
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
            tblshift_mstr tblshift_mstr = db.tblshift_mstr.Find(id);
            if (tblshift_mstr == null)
            {
                return HttpNotFound();
            }
            tblshift_mstr.IsDeleted = 1;
            tblshift_mstr.ModifiedBy = 1;
            tblshift_mstr.ModifiedOn = System.DateTime.Now;
            db.Entry(tblshift_mstr).State = System.Data.Entity.EntityState.Modified;
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