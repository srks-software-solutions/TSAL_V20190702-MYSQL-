using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Tata.Models;

namespace TATA.Controllers
{
    public class recipientmailidController : Controller
    {
        //
        // GET: /recipientmailid/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "recipientmailid";
        string Action = null;
        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            var smid = db.recipientmailids.Include(t => t.tblemailreporttype).Where(m => m.IsDeleted == 0);
            return View(smid.ToList());
        }
        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            ViewBag.AutoEmailType = new SelectList(db.tblemailreporttypes.Where(m => m.IsDeleted == 0), "ERTID", "ReportType");
            return View();
        }
        [HttpPost]
        public ActionResult Create(recipientmailid tblrm)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            ViewBag.AutoEmailType = new SelectList(db.tblemailreporttypes.Where(m => m.IsDeleted == 0), "ERTID", "ReportType");
            tblrm.CreatedBy = Convert.ToInt32(Session["UserId"]); 
            tblrm.CreatedOn = System.DateTime.Now;
            tblrm.IsDeleted = 0;
            //ActiveLog Code
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "New Creation";
            Action = "Create";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.recipientmailids.Add(tblrm);
            db.SaveChanges();

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
            recipientmailid tblrm = db.recipientmailids.Find(id);
            if (tblrm == null)
            {
                return HttpNotFound();
            }
            ViewBag.AutoEmailType = new SelectList(db.tblemailreporttypes.Where(m => m.IsDeleted == 0), "ERTID", "ReportType", tblrm.AutoEmailType);
            return View(tblrm);
        }

        [HttpPost]
        public ActionResult Edit(recipientmailid tblrm)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserId"]); 
            tblrm.ModifiedBy = UserID;
            tblrm.ModifiedOn = System.DateTime.Now;
            ViewBag.AutoEmailType = new SelectList(db.tblemailreporttypes.Where(m => m.IsDeleted == 0), "ERTID", "ReportType");
            //#region Active Log Code
            //recipientmailid OldData = db.recipientmailids.Find(tblrm.RE_ID);
            //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<recipientmailid>(OldData, tblrm);
            //ICollection<recipientmailid> c = FullData as ICollection<recipientmailid>;
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
            db.Entry(tblrm).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
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
            recipientmailid tblrm = db.recipientmailids.Find(id);
            tblrm.IsDeleted = 1;
            tblrm.ModifiedBy = UserID1;
            tblrm.ModifiedOn = System.DateTime.Now;
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "Deleted recipientmailid";
            Action = "Delete";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.Entry(tblrm).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

    }
}
