using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace Tata.Controllers
{
    public class ShopController : Controller
    {
        //
        // GET: /Shop/

        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "Shop";
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
            var cat = db.tblshops.Where(m => m.IsDeleted == 0);
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

            ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName");
            return View();
        }
        [HttpPost]
        public ActionResult Create(tblshop tblp, int Plant = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            #region//ActiveLog Code
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "New Creation";
            Action = "Create";
            // ActiveLogStorage Obj = new ActiveLogStorage();
            // Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            #endregion

            //shop name validation
            string shopname = tblp.ShopName.ToString();
            var doesThisShopExists = db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == Plant && m.ShopName == shopname).ToList();
            if (doesThisShopExists.Count == 0)
            {
                tblp.CreatedBy = UserID;
                tblp.CreatedOn = DateTime.Now;
                tblp.PlantID = Plant;
                db.tblshops.Add(tblp);
                db.SaveChanges();
            }
            else
            {
                Session["Error"] = "Shop Name already exists for this Plant.";
            }
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
            tblshop tblmc = db.tblshops.Find(id);
            if (tblmc == null)
            {
                return HttpNotFound();
            }
            ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", tblmc.PlantID);
            //ViewBag.unit = new SelectList(db.tblunits.Where(m => m.IsDeleted == 0), "U_ID", "Unit", tblpart.UnitDesc);
            return View(tblmc);
        }
        [HttpPost]
        public ActionResult Edit(tblshop tblmc, int Plant = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"]);
            //shop name validation
            string shopname = tblmc.ShopName.ToString();
            int shopid = tblmc.ShopID;
            var doesThisShopExists = db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == Plant && m.ShopName == shopname && m.ShopID != shopid).ToList();
            if (doesThisShopExists.Count == 0)
            {
                #region Active Log Code
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
                #endregion //End Active Log

                tblmc.ModifiedBy = UserID;
                tblmc.PlantID = Plant;
                tblmc.ModifiedOn = DateTime.Now;
                db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                Session["Error"] = "Shop Name already exists for this Plant.";
                ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", Plant);
                return View(tblmc);
            }
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
            // ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            tblshop tblmc = db.tblshops.Find(id);
            tblmc.IsDeleted = 1;
            tblmc.ModifiedBy = UserID;
            int shopId = tblmc.ShopID;
            tblmc.ModifiedOn = DateTime.Now;
            db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            //delete corresponding cells & machines also.
            var cellsdata = db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == shopId).ToList();
            foreach (var cellrow in cellsdata)
            {
                cellrow.IsDeleted = 1;
                db.Entry(cellrow).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                int cellid = cellrow.CellID;

                var machinedata = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.CellID == cellid).ToList();
                foreach (var machinerow in machinedata)
                {
                    machinerow.IsDeleted = 1;
                    db.Entry(machinerow).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
                
            }

            var machinedata1 = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == shopId).ToList();
            foreach (var machinerow in machinedata1)
            {
                machinerow.IsDeleted = 1;
                db.Entry(machinerow).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }

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
