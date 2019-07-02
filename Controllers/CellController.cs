using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace Tata.Controllers
{
    public class CellController : Controller
    {
        //
        // GET: /Cell/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "Cell";
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
            var cat = db.tblcells.Where(m => m.IsDeleted == 0);
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
            ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName");
            return View();
        }
        [HttpPost]
        public ActionResult Create(tblcell tblp, int Shop = 0, int Plant = 0)
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

            //Cell name validation
            string cellname = tblp.CellName.ToString();
            var doesThisShopExists = db.tblcells.Where(m => m.IsDeleted == 0 && m.PlantID == Plant && m.ShopID == Shop && m.CellName == cellname).ToList();
            if (doesThisShopExists.Count == 0)
            {
                tblp.CreatedBy = UserID;
                tblp.CreatedOn = DateTime.Now;
                tblp.ShopID = Shop;
                tblp.PlantID = Plant;
                db.tblcells.Add(tblp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName",Plant);
                ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName",Shop);
                Session["Error"] = "Cell Name already exists for this Plant/Shop.";
                return View(tblp);
            }
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
            tblcell tblmc = db.tblcells.Find(id);
            if (tblmc == null)
            {
                return HttpNotFound();
            }
            int plantid = Convert.ToInt32( tblmc.PlantID );
            ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", tblmc.PlantID);
            ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == plantid), "ShopID", "ShopName", tblmc.ShopID);
            return View(tblmc);
        }
        [HttpPost]
        public ActionResult Edit(tblcell tblmc, int Shop = 0, int Plant = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"]);
            //Cell name validation
            string cellname = tblmc.CellName.ToString();
            int cellid = tblmc.CellID;
            var doesThisShopExists = db.tblcells.Where(m => m.IsDeleted == 0 && m.PlantID == Plant && m.ShopID == Shop && m.CellName == cellname && m.CellID != cellid).ToList();
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

                tblmc.PlantID = Plant;
                tblmc.ShopID = Shop;
                tblmc.ModifiedBy = UserID;
                tblmc.ModifiedOn = DateTime.Now;
                db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", tblmc.PlantID);
                ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName", tblmc.ShopID);
                Session["Error"] = "Cell Name already exists for this Shop.";
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
            tblcell tblmc = db.tblcells.Find(id);
            int cellid = tblmc.CellID;
            tblmc.IsDeleted = 1;
            tblmc.ModifiedBy = UserID;
            tblmc.ModifiedOn = DateTime.Now;
            db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            //delete corresponding machines
            var machinedata = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.CellID == cellid).ToList();
            foreach (var machinerow in machinedata)
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

        public JsonResult GetShop(int PlantID)
        {
            var selectedRow = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0).Where(m => m.PlantID == PlantID), "ShopID", "ShopName");
            return Json(selectedRow, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCell(int ShopID)
        {
            var selectedRow = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == ShopID), "CellID", "CellName");
            return Json(selectedRow, JsonRequestBehavior.AllowGet);
        }

    }
}
