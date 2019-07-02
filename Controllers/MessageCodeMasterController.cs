using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace Tata.Controllers
{
    public class MessageCodeMasterController : Controller
    {
        //
        // GET: /MachineCodeMaster/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "MimicsDashboard";
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
            var data = db.message_code_master.Where(m => m.IsDeleted == 0 && m.MessageType == "IDLE");
            return View(data.ToList());
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
            ViewBag.MessageType = new SelectList(db.tbldowntimecategories.Where(m => m.IsDeleted == 0), "DTCategory", "DTCategory");
            return View();
        }

        [HttpPost]
        public ActionResult Create(message_code_master msgcm)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            int UserID = Convert.ToInt32(Session["UserId"]);
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            // Save was Start Now

            //msgcm.InsertedBy = UserID.ToString();
            //msgcm.InsertedOn = System.DateTime.Now;
            //msgcm.IsDeleted = 0;
            //msgcm.MessageMCode = "M" + msgcm.MessageCode;
            //db.message_code_master.Add(msgcm);
            //db.SaveChanges();
            //return RedirectToAction("Index");

            var msgcode = db.message_code_master.Where(m => m.MessageCode == msgcm.MessageCode).SingleOrDefault();
            if (msgcode == null)
            {
                msgcm.InsertedBy = UserID.ToString();
                msgcm.InsertedOn = System.DateTime.Now;
                msgcm.IsDeleted = 0;
                msgcm.MessageType = "IDLE";
                msgcm.MessageMCode = "M" + msgcm.MessageCode;
                db.message_code_master.Add(msgcm);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.BDCode = "This Code is in Use";
            ViewBag.MessageType = new SelectList(db.tbldowntimecategories.Where(m => m.IsDeleted == 0), "DTCategory", "DTCategory");
            return View(msgcm);
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
            message_code_master msgcm = db.message_code_master.Find(id);
            if (msgcm == null)
            {
                return HttpNotFound();
            }
            ViewBag.MessageType = new SelectList(db.tbldowntimecategories.Where(m => m.IsDeleted == 0), "DTCategory", "DTCategory", msgcm.MessageType);
            return View(msgcm);
        }
        [HttpPost]
        public ActionResult Edit(message_code_master msgcm)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"]);
            msgcm.ModifiedBy = UserID.ToString();
            msgcm.ModifiedOn = System.DateTime.Now;
            msgcm.MessageType = "IDLE";
            if (ModelState.IsValid)
            {
                //#region Active Log Code
                //tblrole OldData = db.tblroles.Find(tblrole.Role_ID);
                //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<tblrole>(OldData, tblrole);
                //ICollection<tblrole> c = FullData as ICollection<tblrole>;
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
                var msgcode = db.message_code_master.Where(m => m.MessageCode == msgcm.MessageCode && m.MessageCode != msgcm.MessageCode).SingleOrDefault();
                if (msgcode == null)
                {
                    db.Entry(msgcm).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.BDCode = "This Code is in Use";
                ViewBag.MessageType = new SelectList(db.tbldowntimecategories.Where(m => m.IsDeleted == 0), "DTCategory", "DTCategory");
                return View(msgcm);
            }
            return View(msgcm);
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

            message_code_master msgcm = db.message_code_master.Find(id);
            msgcm.IsDeleted = 1;
            msgcm.ModifiedBy = UserID1.ToString();
            msgcm.ModifiedOn = System.DateTime.Now;
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "Deleted Role";
            Action = "Delete";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.Entry(msgcm).State = System.Data.Entity.EntityState.Modified;
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
