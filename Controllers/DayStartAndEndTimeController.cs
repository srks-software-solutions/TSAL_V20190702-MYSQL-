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
    public class DayStartAndEndTimeController : Controller
    {
        private mazakdaqEntities db = new mazakdaqEntities();

        //
        // GET: /DayStartAndEndTime/

        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            return View(db.tbldaytimings.Where(m => m.IsDeleted == 0).ToList());
        }

        //
        // GET: /DayStartAndEndTime/Details/5

        public ActionResult Details(int id = 0)
        {
            tbldaytiming tbldaytiming = db.tbldaytimings.Find(id);
            if (tbldaytiming == null)
            {
                return HttpNotFound();
            }
            return View(tbldaytiming);
        }

        //
        // GET: /DayStartAndEndTime/Create

        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            return View();
        }

        //
        // POST: /DayStartAndEndTime/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tbldaytiming tbldaytiming)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            tbldaytiming.CreatedBy = 1;
            tbldaytiming.CreatedOn = DateTime.Now;
            tbldaytiming.IsDeleted = 0;

            //if (ModelState.IsValid)
            {
                db.tbldaytimings.Add(tbldaytiming);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tbldaytiming);
        }

        //
        // GET: /DayStartAndEndTime/Edit/5

        public ActionResult Edit(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            tbldaytiming tbldaytiming = db.tbldaytimings.Find(id);
            if (tbldaytiming == null)
            {
                return HttpNotFound();
            }
            return View(tbldaytiming);
        }

        //
        // POST: /DayStartAndEndTime/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tbldaytiming tbldaytiming)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            tbldaytiming.ModifiedBy = 1;
            tbldaytiming.ModifiedOn = DateTime.Now;
            tbldaytiming.IsDeleted = 0;

            if (ModelState.IsValid)
            {
                db.Entry(tbldaytiming).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tbldaytiming);
        }

        //
        // GET: /DayStartAndEndTime/Delete/5

        public ActionResult Delete(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            tbldaytiming tbldaytiming = db.tbldaytimings.Find(id);
            if (tbldaytiming == null)
            {
                return HttpNotFound();
            }
            tbldaytiming.IsDeleted = 1;
            tbldaytiming.ModifiedBy = 1;
            tbldaytiming.ModifiedOn = System.DateTime.Now;
            db.Entry(tbldaytiming).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // POST: /DayStartAndEndTime/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            tbldaytiming tbldaytiming = db.tbldaytimings.Find(id);
            db.tbldaytimings.Remove(tbldaytiming);
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