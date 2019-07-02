using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.OleDb;
using System.Xml;
using Tata.Models;
using System.IO;

namespace Tata.Controllers
{
    public class CustomerController : Controller
    {
        //
        // GET: /Customer/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "Customer";
        string Action = null;
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
            var tblCust = db.tblcustomers.Where(m => m.IsDeleted == 0);
            return View(tblCust.ToList());
        }
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
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblcustomer tblcust, HttpPostedFileBase inputimg)
        {
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
            {//
                string filename = "";
                byte[] bytes;
                int BytestoRead;
                int numBytesRead;
                if (inputimg != null)
                {

                    filename = Path.GetFileName(inputimg.FileName);
                    bytes = new byte[inputimg.ContentLength];
                    BytestoRead = (int)inputimg.ContentLength;
                    numBytesRead = 0;
                    while (BytestoRead > 0)
                    {
                        int n = inputimg.InputStream.Read(bytes, numBytesRead, BytestoRead);
                        if (n == 0) break;
                        numBytesRead += n;
                        BytestoRead -= n;
                    }
                    tblcust.Logo = bytes;
                }
                //
                int UserID1 = Convert.ToInt32(Session["UserID"].ToString());
                // Save was Start Now
                tblcust.CreatedBy = UserID1;
                tblcust.CreatedOn = System.DateTime.Now;
                tblcust.IsDeleted = 0;
                db.tblcustomers.Add(tblcust);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tblcust);
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
            tblcustomer tblcust = db.tblcustomers.Find(id);
            Session["logo"] = tblcust.Logo;
            if (tblcust == null)
            {
                return HttpNotFound();
            }
            return View(tblcust);
        }

        //
        // POST: /ModuleMaster/Edit/5

        [HttpPost]
        public ActionResult Edit(tblcustomer tblCustomer, HttpPostedFileBase inputimg)
        {
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
            //tblCustomer.Logo = inputimg;
            tblCustomer.ModifiedBy = UserID1;
            tblCustomer.ModifiedOn = System.DateTime.Now;
            {
               
                if (ModelState.IsValid)
                {
                    if(inputimg==null)
                    {
                            //byte b =Convert.ToByte(Session["logo"]);// img src
                            //byte[] imgarray = new byte[b];
                            //= (Session["logo"]).ToString();
                            //tblCustomer.Logo = Encoding.ASCII.GetBytes(a);
                        var a = Session["logo"] as byte[];
                        tblCustomer.Logo = a;
                    }
                    tblCustomer.IsDeleted = 0;
                    db.Entry(tblCustomer).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(tblCustomer);
        }

        //
        // GET: /ModuleMaster/Delete/5

        public ActionResult Delete(int id)
        {
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
        public ActionResult MyImage(int id, int img)
        {
            ViewBag.Pic = img;
            tblcustomer insp = db.tblcustomers.Find(id);
            return View(insp);
        }
    }
}
