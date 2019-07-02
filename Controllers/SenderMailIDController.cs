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
    public class SenderMailIDController : Controller
    {
        //
        // GET: /SenderMailID/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "SenderMailID";
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
            var smid = db.tblsendermailids.Include(t => t.tblemailreporttype).Where(m => m.IsDeleted == 0);
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
        public ActionResult Create(tblsendermailid tblsm)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            ViewBag.AutoEmailType = new SelectList(db.tblemailreporttypes.Where(m => m.IsDeleted == 0), "ERTID", "ReportType");
            tblsm.CreatedBy = Convert.ToInt32(Session["UserId"]); 
            tblsm.CreatedOn = System.DateTime.Now;
            tblsm.IsDeleted = 0;
            //ActiveLog Code
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "New Creation";
            Action = "Create";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.tblsendermailids.Add(tblsm);
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
            tblsendermailid tblsm = db.tblsendermailids.Find(id);
            if (tblsm == null)
            {
                return HttpNotFound();
            }
             ViewBag.AutoEmailType = new SelectList(db.tblemailreporttypes.Where(m => m.IsDeleted == 0), "ERTID", "ReportType",tblsm.AutoEmailType);
            return View(tblsm);
        }
        [HttpPost]
        public ActionResult Edit(tblsendermailid tblsm)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID1 = Convert.ToInt32(Session["UserId"]); 
            tblsm.ModifiedBy = UserID1;
            tblsm.ModifiedOn = System.DateTime.Now;
            {
                if (ModelState.IsValid)
                {
                    //Section related to storing data in ActiveLog
                    int UserID = Convert.ToInt32(Session["UserId"]);
                    //#region Active Log Code
                    //tblsendermailid OldData = db.tblsendermailids.Find(tblsm.SE_ID);
                    //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<tblsendermailid>(OldData, tblsm);
                    //ICollection<tblsendermailid> c = FullData as ICollection<tblsendermailid>;
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
                    db.Entry(tblsm).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            ViewBag.AutoEmailType = new SelectList(db.tblemailreporttypes.Where(m => m.IsDeleted == 0), "ERTID", "ReportType");
            return View(tblsm);

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
            tblsendermailid tblsm = db.tblsendermailids.Find(id);
            tblsm.IsDeleted = 1;
            tblsm.ModifiedBy = UserID1;
            tblsm.ModifiedOn = System.DateTime.Now;
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "Deleted SenderMailID";
            Action = "Delete";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.Entry(tblsm).State = System.Data.Entity.EntityState.Modified;
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
