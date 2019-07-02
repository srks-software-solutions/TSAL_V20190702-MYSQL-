using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Web.Configuration;
using System.Data.SqlClient;
using Tata.Models;

namespace Product1.Controllers
{
    public class ModuleMasterController : Controller
    {
        //
        // GET: /ModuleMaster/
        private mazakdaqEntities db = new mazakdaqEntities();

        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            if (Request.Cookies["YourApLogin&"] != null)
            {
                string username = Request.Cookies["YourAppLogin"].Values["username"];
                string RoleID = Request.Cookies["YourAppLogin"].Values["RoleID"];
                string UserID = Request.Cookies["YourAppLogin"].Values["UserId"];
                Session["Username"] = username;
                Session["RoleID"] = RoleID;
                Session["UserId"] = UserID;
            }
            else if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            else
            {
                ViewBag.Logout = Session["Username"];
                ViewBag.roleid = Session["RoleID"];
                String Username = Session["Username"].ToString();
            }
            var tblModule = db.tblmodulemasters.Where(m => m.IsDeleted == 0);
            return View(tblModule.ToList());
        }

        //
        // GET: /ModuleMaster/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /ModuleMaster/Create

        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            if (Request.Cookies["YourApLogin&"] != null)
            {
                string username = Request.Cookies["YourAppLogin"].Values["username"];
                string RoleID = Request.Cookies["YourAppLogin"].Values["RoleID"];
                string UserID = Request.Cookies["YourAppLogin"].Values["UserId"];
                Session["Username"] = username;
                Session["RoleID"] = RoleID;
                Session["UserId"] = UserID;
            }
            else if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            else
            {
                ViewBag.Logout = Session["Username"];
                ViewBag.roleid = Session["RoleID"];
                String Username = Session["Username"].ToString();
            }
            return View();
        }

        //
        // POST: /ModuleMaster/Create

        [HttpPost]
        public ActionResult Create(tblmodulemaster tblModule)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            if (Request.Cookies["YourApLogin&"] != null)
            {
                string username = Request.Cookies["YourAppLogin"].Values["username"];
                string RoleID = Request.Cookies["YourAppLogin"].Values["RoleID"];
                string UserID = Request.Cookies["YourAppLogin"].Values["UserId"];
                Session["Username"] = username;
                Session["RoleID"] = RoleID;
                Session["UserId"] = UserID;
            }
            else if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            else
            {
                ViewBag.Logout = Session["Username"];
                ViewBag.roleid = Session["RoleID"];
                String Username = Session["Username"].ToString();
            }
            
                int UserID1 = Convert.ToInt32(Session["UserID"].ToString());
                // Save was Start Now
                string modulename = tblModule.ModuleName;
                tblModule.CreatedBy = UserID1;
                tblModule.CreatedOn = System.DateTime.Now;
                tblModule.IsDeleted = 0;
                db.tblmodulemasters.Add(tblModule);
                db.SaveChanges();

                //Saving in module helper
                tblmodulehelper rl = new tblmodulehelper();
                rl.IsAdded = false;
                rl.IsAll = false;
                rl.IsDeleted = 0;
                rl.IsEdited = false;
                rl.IsHidden = false;
                rl.IsReadonly = false;
                rl.IsRemoved = false;
                rl.RoleID = 1;
                rl.ModuleID = modulename;
                db.tblmodulehelpers.Add(rl);
                db.SaveChanges();

                return RedirectToAction("Index");
        }

        //
        // GET: /ModuleMaster/Edit/5

        public ActionResult Edit(int id)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            if (Request.Cookies["YourApLogin&"] != null)
            {
                string username = Request.Cookies["YourAppLogin"].Values["username"];
                string RoleID = Request.Cookies["YourAppLogin"].Values["RoleID"];
                string UserID = Request.Cookies["YourAppLogin"].Values["UserId"];
                Session["Username"] = username;
                Session["RoleID"] = RoleID;
                Session["UserId"] = UserID;
            }
            else if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            else
            {
                ViewBag.Logout = Session["Username"];
                ViewBag.roleid = Session["RoleID"];
                String Username = Session["Username"].ToString();
            }
            tblmodulemaster tblModule = db.tblmodulemasters.Find(id);
            if (tblModule == null)
            {
                return HttpNotFound();
            }
            Session["ID"] = id;
            return View(tblModule);
        }

        //
        // POST: /ModuleMaster/Edit/5

        [HttpPost]
        public ActionResult Edit(tblmodulemaster tblModule)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            if (Request.Cookies["YourApLogin&"] != null)
            {
                string username = Request.Cookies["YourAppLogin"].Values["username"];
                string RoleID = Request.Cookies["YourAppLogin"].Values["RoleID"];
                string UserID = Request.Cookies["YourAppLogin"].Values["UserId"];
                Session["Username"] = username;
                Session["RoleID"] = RoleID;
                Session["UserId"] = UserID;
            }
            else if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            else
            {
                ViewBag.Logout = Session["Username"];
                ViewBag.roleid = Session["RoleID"];
                String Username = Session["Username"].ToString();
            }
            int UserID1 = Convert.ToInt32(Session["UserID"].ToString());
            ViewBag.IsEmailEscalation = 0;
            tblModule.ModifiedBy = UserID1;
            string moduleName = tblModule.ModuleName;
            tblModule.ModifiedOn = System.DateTime.Now;
            {
                if (ModelState.IsValid)
                {
                    db.Entry(tblModule).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //Updating in module helper
                    int ID = Convert.ToInt32(Session["ID"]);
                    tblmodulehelper module = db.tblmodulehelpers.Find(ID);
                    module.ModuleID = moduleName;
                    db.Entry(tblModule).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
            }
            return View(tblModule);
        }

        //
        // GET: /ModuleMaster/Delete/5

        public ActionResult Delete(int id)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            if (Request.Cookies["YourApLogin&"] != null)
            {
                string username = Request.Cookies["YourAppLogin"].Values["username"];
                string RoleID = Request.Cookies["YourAppLogin"].Values["RoleID"];
                string UserID = Request.Cookies["YourAppLogin"].Values["UserId"];
                Session["Username"] = username;
                Session["RoleID"] = RoleID;
                Session["UserId"] = UserID;
            }
            else if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            else
            {
                ViewBag.Logout = Session["Username"];
                ViewBag.roleid = Session["RoleID"];
                String Username = Session["Username"].ToString();
            }
            int UserID1 = Convert.ToInt32(Session["UserID"].ToString());
            tblmodulemaster tblModule = db.tblmodulemasters.Find(id);
            tblModule.IsDeleted = 1;
            tblModule.ModifiedBy = UserID1;
            tblModule.ModifiedOn = System.DateTime.Now;
            db.Entry(tblModule).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            //deleting from module helper
            tblmodulehelper tblModulehelper = db.tblmodulehelpers.Find(id);
            tblModulehelper.IsDeleted = 1;
            db.Entry(tblModulehelper).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        //
        // POST: /ModuleMaster/Delete/5

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
