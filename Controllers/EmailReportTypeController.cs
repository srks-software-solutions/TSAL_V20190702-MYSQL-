using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace TATA.Controllers
{
    public class EmailReportTypeController : Controller
    {
        //
        // GET: /EmailReportType/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "MachineDetails";
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
            var ert = db.tblemailreporttypes.Where(m => m.IsDeleted == 0);
            return View(ert);
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
            return View();
        }
        [HttpPost]
        public ActionResult Create(tblemailreporttype tblert)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            tblert.CreatedBy = Convert.ToInt32(Session["UserId"]);
            tblert.CreatedOn = System.DateTime.Now;
            tblert.IsDeleted = 0;
            //ActiveLog Code
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "New Creation";
            Action = "Create";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.tblemailreporttypes.Add(tblert);
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
            tblemailreporttype tblert = db.tblemailreporttypes.Find(id);
            if (tblert == null)
            {
                return HttpNotFound();
            }

            return View(tblert);
        }
        [HttpPost]
        public ActionResult Edit(tblemailreporttype tblert)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserId"]);
            tblert.ModifiedBy = UserID;
            tblert.ModifiedOn = System.DateTime.Now;
            {
                if (ModelState.IsValid)
                {
                    //#region Active Log Code
                    //tblemailreporttype OldData = db.tblemailreporttypes.Find(tblert.ERTID);
                    //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<tblemailreporttype>(OldData, tblert);
                    //ICollection<tblemailreporttype> c = FullData as ICollection<tblemailreporttype>;
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
                    db.Entry(tblert).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(tblert);

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
            tblemailreporttype tblert = db.tblemailreporttypes.Find(id);
            tblert.IsDeleted = 1;
            tblert.ModifiedBy = Convert.ToInt32(Session["UserId"]); ;
            tblert.ModifiedOn = System.DateTime.Now;
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "Deleted EmailReportType";
            Action = "Delete";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.Entry(tblert).State = System.Data.Entity.EntityState.Modified;
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
