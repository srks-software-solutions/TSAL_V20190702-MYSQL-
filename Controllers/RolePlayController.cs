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
    public class RolePlayController : Controller
    {
        private mazakdaqEntities db = new mazakdaqEntities();
        int RlID = 0;

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

            string Controller = "RolePlay";
            string Action = "Index";
            int ModuleID = 18;
            var CreateEditPermission = db.tblroleplaymasters.Where(m => m.ModuleID == ModuleID).FirstOrDefault();
            if (CreateEditPermission != null)
            {
                if (CreateEditPermission.IsAll)
                {
                    ViewBag.Create = true;
                    ViewBag.Modify = true;
                }
                else if (CreateEditPermission.IsReadOnly)
                {
                    ViewBag.Create = false;
                    ViewBag.Modify = false;
                }

            }

            ViewBag.RoleID1 = new SelectList(db.tblroles.Where(m => m.IsDeleted == 0), "Role_ID", "RoleType");
            var tblRolPlay = db.tblroleplaymasters.Include(t => t.tblmodulemaster).Include(t => t.tblrole).Where(m => m.IsDeleted == 0).OrderBy(m => m.RolePlayID);
            return View(tblRolPlay.ToList());
        }

        public Dictionary<string, bool> Permissions(int ModuleID)
        {
            Dictionary<string, bool> retVals = new Dictionary<string, bool>();
            var CreateEditPermission = db.tblroleplaymasters.Where(m => m.ModuleID == ModuleID).FirstOrDefault();
            if (CreateEditPermission != null)
            {
                if (CreateEditPermission.IsAll)
                {
                    retVals.Add("View", true);
                    retVals.Add("Create", true);
                    retVals.Add("Modify", true);
                    retVals.Add("Delete", true);
                }
                else if (CreateEditPermission.IsReadOnly) //Index page Only
                {
                    retVals.Add("View", true);
                    retVals.Add("Create", false);
                    retVals.Add("Modify", false);
                    retVals.Add("Delete", false);
                }
                else if (CreateEditPermission.IsAdded) //Index and Create Only
                {
                    retVals.Add("View", true);
                    retVals.Add("Create", true);
                    retVals.Add("Modify", false);
                    retVals.Add("Delete", false);
                }
                else if (CreateEditPermission.IsEdited) // Index and Edit Only
                {
                    retVals.Add("View", true);
                    retVals.Add("Create", false);
                    retVals.Add("Modify", true);
                    retVals.Add("Delete", false);
                }
                else if (CreateEditPermission.IsRemoved) //Index and Delete Only
                {
                    retVals.Add("View", true);
                    retVals.Add("Create", false);
                    retVals.Add("Modify", false);
                    retVals.Add("Delete", true);
                }
            }
            return retVals;
        }

        public ActionResult Details(int id)
        {
            return View();
        }

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
            //ViewBag.RoleID = new SelectList(db.tblroles.Where(m => m.IsDeleted == 0), "Role_ID", "RoleName");

            var tblRolePlayHelper = db.tblmodulehelpers.Where(m => m.IsDeleted == 0);

            ViewBag.RoleID1 = new SelectList(db.tblroles.Where(m => m.IsDeleted == 0), "Role_ID", "RoleType");
            return View(tblRolePlayHelper.ToList());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(List<tblmodulehelper> tblRolePlayHolder, int RoleID1)
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
            int Role = 0;
            int count = 0;
            string[,] sss = new string[2, 11];
            if (tblRolePlayHolder != null)
            {
                int i = 0;
                foreach (var plan in tblRolePlayHolder)
                {
                    tblroleplaymaster rl = new tblroleplaymaster();
                    sss[i, 0] = plan.IsAdded.ToString();
                    sss[i, 1] = plan.IsAll.ToString();
                    sss[i, 2] = plan.IsDeleted.ToString();
                    sss[i, 3] = plan.IsEdited.ToString();
                    sss[i, 4] = plan.IsHidden.ToString();
                    sss[i, 5] = plan.IsReadonly.ToString();
                    sss[i, 6] = plan.IsRemoved.ToString();
                    sss[i, 7] = RoleID1.ToString();
                    sss[i, 8] = plan.ModuleID.ToString();
                    sss[i, 9] = Convert.ToInt32(Session["UserID"]).ToString();
                    sss[i, 10] = System.DateTime.Now.ToString();
                    setvalue(sss);
                    count++;
                }
                //db.Entry(rl).State = EntityState.Modified;
                //db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ErrorMsg = "No daily plan details";
            ViewBag.PrimaryRole = new SelectList(db.tblroles.Where(m => m.IsDeleted == 0), "Role_ID", "RoleName");
            var tblRolPlayMaster1 = db.tblroleplaymasters.Include(t => t.tblmodulemaster).Where(m => m.IsDeleted == 0);
            return View(tblRolPlayMaster1.ToList());
        }

        public void setvalue(string[,] set)
        {
            int i = 0;
            int ModuleID = 0;
            string Modulename = set[0, 8];
            int RlId = Convert.ToInt32(set[0, 7]);
            var note = (from n in db.tblmodulemasters where n.ModuleName == Modulename select n).First();
            if (note != null)
            {
                ModuleID = note.ModuleID;
            }

            //Checking Role and dodule In DB
            int Count = 0;
            try
            {
                var RoleModule = (from n in db.tblroleplaymasters where (n.ModuleID == ModuleID && n.RolePlayID == RlId) select n).First();
                if (RoleModule != null)
                {
                    Count = 1;
                }
            }
            catch { }
            if (Count == 0)
            {
                //Taking moduleId From Table                

                for (int j = 0; j < 1; j++)
                {
                    tblroleplaymaster rl = new tblroleplaymaster();
                    rl.IsAdded = Convert.ToBoolean(set[j, 0]);
                    rl.IsAll = Convert.ToBoolean(set[j, 1]);
                    rl.IsDeleted = Convert.ToInt32(set[j, 2]);
                    rl.IsEdited = Convert.ToBoolean(set[j, 3]);
                    rl.IsHidden = Convert.ToBoolean(set[j, 4]);
                    rl.IsReadOnly = Convert.ToBoolean(set[j, 5]);
                    rl.IsRemoved = Convert.ToBoolean(set[j, 6]);
                    rl.RoleID = Convert.ToInt32(set[j, 7]);
                    rl.ModuleID = ModuleID;
                    rl.CreatedBy = Convert.ToInt32(Session["UserID"]);
                    rl.CreatedOn = System.DateTime.Now;
                    db.tblroleplaymasters.Add(rl);
                    db.SaveChanges();
                }
            }
        }

        [HttpGet]
        public ActionResult Edit(int RoleID1 = 0)
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
            RlID = Convert.ToInt32(Session["RLID"]);
            var tblRolPlay = db.tblroleplaymasters.Where(m => m.IsDeleted == 0 && m.RoleID == RoleID1).OrderBy(m => m.RolePlayID);
            if (tblRolPlay == null)
            {
                return HttpNotFound();
            }
            return View(tblRolPlay.ToList());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(IEnumerable<tblroleplaymaster> tblrolePlay)
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
            if (tblrolePlay != null)
            {
                if (ModelState.IsValid)
                {
                    foreach (var plan in tblrolePlay)
                    {
                        plan.tblmodulemaster = null;
                        //plan.tblrole = null;
                        plan.ModifiedBy = Convert.ToInt32(Session["UserID"]);
                        plan.ModifiedOn = System.DateTime.Now;
                        db.Entry(plan).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                    return RedirectToAction("Index");
                }
            }
            return View(tblrolePlay);
        }

        public ActionResult Delete(int id)
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
            tblcustomer tblCustomer = db.tblcustomers.Find(id);
            tblCustomer.IsDeleted = 1;
            tblCustomer.ModifiedBy = UserID1;
            tblCustomer.ModifiedOn = System.DateTime.Now;
            db.Entry(tblCustomer).State = System.Data.Entity.EntityState.Modified;
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

        public JsonResult ModuleData(int roleID)
        {
            var ModelData = "blah";


            return Json(ModelData, JsonRequestBehavior.AllowGet);
        }
    }
}
