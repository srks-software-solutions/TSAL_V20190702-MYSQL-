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
    public class DownTimeCategoryController : Controller
    {
        private mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "MachineDetails";
        string Action = null;
        //
        // GET: /DownTimeCategory/

        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            return View(db.tbldowntimecategories.Where(m => m.IsDeleted == 0).ToList());
         // return View(a);
        }

        //
        // GET: /DownTimeCategory/Details/5

        public ActionResult Details(int id = 0)
        {
            tbldowntimecategory tbldowntimecategory = db.tbldowntimecategories.Find(id);
            if (tbldowntimecategory == null)
            {
                return HttpNotFound();
            }
            return View(tbldowntimecategory);
        }

        //
        // GET: /DownTimeCategory/Create

        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            return View();
        }

        //
        // POST: /DownTimeCategory/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tbldowntimecategory tbldowntimecategory)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            if (ModelState.IsValid)
            {
                //ActiveLog Code
                int UserID = Convert.ToInt32(Session["UserId"]);
                string CompleteModificationdetail = "New Creation";
                Action = "Create";
                //ActiveLogStorage Obj = new ActiveLogStorage();
                //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
                //End
                DateTime TimeOFCreation=DateTime.Now;
                //db.createDownTimeCategory(tbldowntimecategory.DTCategory, tbldowntimecategory.DTCategoryDesc, 0, TimeOFCreation,1);
                tbldowntimecategory.CreatedBy = UserID;
                tbldowntimecategory.CreatedOn = TimeOFCreation;
               db.tbldowntimecategories.Add(tbldowntimecategory);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tbldowntimecategory);
        }

        //
        // GET: /DownTimeCategory/Edit/5

        public ActionResult Edit(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            tbldowntimecategory tbldowntimecategory = db.tbldowntimecategories.Find(id);
            if (tbldowntimecategory == null)
            {
                return HttpNotFound();
            }
            return View(tbldowntimecategory);
        }

        //
        // POST: /DownTimeCategory/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tbldowntimecategory tbldowntimecategory)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int UserID = Convert.ToInt32(Session["UserID"]);
            String Username = Session["Username"].ToString();
            if (ModelState.IsValid)
            {

                //#region Active Log Code
                //tbldowntimecategory OldData = db.tbldowntimecategories.Find(tbldowntimecategory.DTC_ID);
                //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<tbldowntimecategory>(OldData, tbldowntimecategory);
                //ICollection<tbldowntimecategory> c = FullData as ICollection<tbldowntimecategory>;
                //int Count = c.Count;
                //if (Count != 0)
                //{
                //    string CompleteModificationdetail = null;
                //    for (int i = 0; i < Count; i++)
                //    {
                //        CompleteModificationdetail = CompleteModificationdetail + "-" + FullData.Take(i).ToArray();
                //    }
                //    Action = "Edit";
                //    ActiveLogStorage Obj = new ActiveLogStorage();
                //    Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
                //}
                //#endregion //End Active Log
                DateTime TimeOFCreation = DateTime.Now;
                tbldowntimecategory.ModifiedBy = UserID;
                tbldowntimecategory.ModifiedOn = TimeOFCreation;
                db.Entry(tbldowntimecategory).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                DateTime Date=DateTime.Now;
                //db.updateDownTimeCategory(tbldowntimecategory.DTC_ID, tbldowntimecategory.DTCategory, tbldowntimecategory.DTCategoryDesc, tbldowntimecategory.IsDeleted, tbldowntimecategory.CreatedOn, 1, Date,1);
                return RedirectToAction("Index");
            }
            return View(tbldowntimecategory);
        }

        //
        // GET: /DownTimeCategory/Delete/5

        public ActionResult Delete(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            tbldowntimecategory tbldowntimecategory = db.tbldowntimecategories.Find(id);
            DateTime Date = DateTime.Now;
            //if (tbldowntimecategory == null)
            //{
            //    return HttpNotFound();
            //}
            //return View(tbldowntimecategory);
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "Deleted DownTimeCategory";
            Action = "Delete";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            //db.updateDownTimeCategory(tbldowntimecategory.DTC_ID, tbldowntimecategory.DTCategory, tbldowntimecategory.DTCategoryDesc, 1, tbldowntimecategory.CreatedOn, 1, Date, 1);
            tbldowntimecategory.IsDeleted = 1;
            db.Entry(tbldowntimecategory).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // POST: /DownTimeCategory/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            tbldowntimecategory tbldowntimecategory = db.tbldowntimecategories.Find(id);
            db.tbldowntimecategories.Remove(tbldowntimecategory);
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