using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace Tata.Controllers.MachineConfiguration
{
    public class PlantController : Controller
    {
        //
        // GET: /Plant/

        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "Plant";
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
            var cat = db.tblplants.Where(m => m.IsDeleted == 0);
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
        public ActionResult Create(tblplant tblp)
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

            string plantName = tblp.PlantName.ToString();
            var doesThisPlantExist = db.tblplants.Where(m => m.IsDeleted == 0 && m.PlantName == plantName).ToList();
            if (doesThisPlantExist.Count == 0)
            {
                tblp.CreatedBy = UserID;
                tblp.CreatedOn = DateTime.Now;
                db.tblplants.Add(tblp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                Session["Error"] = "Plant Name already Exists.";
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
            tblplant tblmc = db.tblplants.Find(id);
            if (tblmc == null)
            {
                return HttpNotFound();
            }
            return View(tblmc);
        }
        [HttpPost]
        public ActionResult Edit(tblplant tblmc)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"]);

            string plantName = tblmc.PlantName.ToString();
            int plantid = tblmc.PlantID;
            var doesThisPlantExist = db.tblplants.Where(m => m.IsDeleted == 0 && m.PlantName == plantName && m.PlantID != plantid).ToList();
            if (doesThisPlantExist.Count == 0)
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
                tblmc.ModifiedOn = DateTime.Now;
                db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                Session["Error"] = "Plant Name already Exists.";
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
            tblplant tblmc = db.tblplants.Find(id);
            int plantid = tblmc.PlantID;
            tblmc.IsDeleted = 1;
            db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            //Delete corresponding shops cells & machines.

            var shopdata = db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == plantid).ToList();
            foreach (var shoprow in shopdata)
            {
                shoprow.IsDeleted = 1;
                db.Entry(shoprow).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                int shopid = shoprow.ShopID;

                var cellsdata = db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == shopid).ToList();
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

                var machinedata1 = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == shopid).ToList();
                foreach (var machinerow in machinedata1)
                {
                    machinerow.IsDeleted = 1;
                    db.Entry(machinerow).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
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
