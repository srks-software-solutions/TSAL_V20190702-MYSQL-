using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace TATA.Controllers
{
    public class MachineCategoryController : Controller
    {
        //
        // GET: /MachineCategory/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "MachineCategory";
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
            var cat = db.tblmachinecategories.Where(m => m.IsDeleted == 0);
            return View(cat.ToList());
            //return View();
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
        public ActionResult Create(tblmachinecategory tblmc)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
           
          
            //ActiveLog Code
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "New Creation";
            Action = "Create";
            ActiveLogStorage Obj = new ActiveLogStorage();
            Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.tblmachinecategories.Add(tblmc);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult timeline()
        {

            return View();  
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
            tblmachinecategory tblmc = db.tblmachinecategories.Find(id);
            if (tblmc == null)
            {
                return HttpNotFound();
            }
            return View(tblmc);
        }
        [HttpPost]
        public ActionResult Edit(tblmachinecategory tblmc)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"]);
          
            {
                if (ModelState.IsValid)
                {
                    //#region Active Log Code
                    //tblmachinecategory OldData = db.tblmachinecategories.Find(tblmc.ID);
                    //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<tblmachinecategory>(OldData, tblmc);
                    //ICollection<tblmachinecategory> c = FullData as ICollection<tblmachinecategory>;
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
                    db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(tblmc);
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
          
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "Deleted Role";
            Action = "Delete";
            ActiveLogStorage Obj = new ActiveLogStorage();
            Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            tblmachinecategory tblmc = db.tblmachinecategories.Find(id);
            tblmc.IsDeleted = 1;
            db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
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
