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
using TataMySqlConnection;
using MySql.Data.MySqlClient;
using System.IO;

namespace TATA.Controllers
{
    public class PartsController : Controller
    {
        //
        // GET: /Parts/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "Parts";
        string Action = null;
        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            var tblparts = db.tblparts.Where(m => m.IsDeleted == 0);
            return View(tblparts.ToList());
        }
        // GET: /Parts/Create

        public ActionResult Create()
        {

            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            ViewBag.Unit = new SelectList(db.tblunits.Where(m => m.IsDeleted == 0), "U_ID", "Unit");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblpart tblpart, int Unit = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            String Username = Session["Username"].ToString();
            tblpart.CreatedBy = Convert.ToInt32(Session["UserId"]);
            tblpart.UnitDesc = Unit;
            tblpart.CreatedOn = DateTime.Now.ToString("yyyyMMddHHmmss");
            tblpart.IsDeleted = 0;
            //ActiveLog Code
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "New Creation";
            Action = "Create";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.tblparts.Add(tblpart);
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


            tblpart tblpart = db.tblparts.Find(id);
            if (tblpart == null)
            {
                return HttpNotFound();
            }
            ViewBag.unit = new SelectList(db.tblunits.Where(m => m.IsDeleted == 0), "U_ID", "Unit", tblpart.UnitDesc);
            return View(tblpart);
        }
        [HttpPost]
        public ActionResult Edit(tblpart tblpart, int Unit = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            int UserID = Convert.ToInt32(Session["UserId"]);
            tblpart.UnitDesc = Unit;
            tblpart.ModifiedBy = UserID;
            tblpart.ModifiedOn = System.DateTime.Now;
            {
                if (ModelState.IsValid)
                {
                    //#region Active Log Code
                    //tblpart OldData = db.tblparts.Find(tblpart.PartID);
                    //String Username = Session["Username"].ToString();
                    //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<tblpart>(OldData, tblpart);
                    //ICollection<tblpart> c = FullData as ICollection<tblpart>;
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
                    db.Entry(tblpart).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            ViewBag.unit = new SelectList(db.tblunits.Where(m => m.IsDeleted == 0), "U_ID", "Unit");
            return View(tblpart);
        }
        public ActionResult Delete(int id)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            int UserID1 = id;
            //ViewBag.IsConfigMenu = 0;
            tblpart tblpart = db.tblparts.Find(id);
            tblpart.IsDeleted = 1;
            tblpart.ModifiedBy = UserID1;
            tblpart.ModifiedOn = System.DateTime.Now;
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            String Username = Session["Username"].ToString();
            string CompleteModificationdetail = "Deleted Parts/Item";
            Action = "Delete";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            db.Entry(tblpart).State = System.Data.Entity.EntityState.Modified;
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

        public ActionResult ImportPriorityAlarm()
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
        public ActionResult ImportPriorityAlarm(HttpPostedFileBase file)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            //Deleting Excel file
            #region
            string fileLocation1 = Server.MapPath("~/Content/");
            DirectoryInfo di = new DirectoryInfo(fileLocation1);
            FileInfo[] files = di.GetFiles("*.xlsx").Where(p => p.Extension == ".xlsx").ToArray();
            foreach (FileInfo file1 in files)
                try
                {
                    file1.Attributes = FileAttributes.Normal;
                    System.IO.File.Delete(file1.FullName);
                }
                catch { }
            #endregion

            ////start logging
            //String Username = Session["Username"].ToString();
            //int UserID = Convert.ToInt32(Session["UserId"]);
            //string CompleteModificationdetail = "Import PriorityAlarm";
            //Action = "ImportPriorityAlarm";
            ////ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            ////End
            DataSet ds = new DataSet();
            if (Request.Files["file"].ContentLength > 0)
            {

                string fileExtension = System.IO.Path.GetExtension(Request.Files["file"].FileName);
                if (fileExtension == ".xls" || fileExtension == ".xlsx")
                {
                    string fileLocation = Server.MapPath("~/Content/") + Request.Files["file"].FileName;
                    if (System.IO.File.Exists(fileLocation))
                    {
                        System.IO.File.Delete(fileLocation);
                    }
                    Request.Files["file"].SaveAs(fileLocation);
                    string excelConnectionString = string.Empty;
                    excelConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                    fileLocation + ";Extended Properties=\"Excel 12.0;HDR=Yes;IMEX=2\"";
                    //connection String for xls file format.
                    if (fileExtension == ".xls")
                    {
                        excelConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" +
                        fileLocation + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";
                    }
                    //connection String for xlsx file format.
                    else if (fileExtension == ".xlsx")
                    {
                        excelConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                        fileLocation + ";Extended Properties=\"Excel 12.0;HDR=Yes;IMEX=2\"";
                    }
                    //Create Connection to Excel work book and add oledb namespace
                    OleDbConnection excelConnection = new OleDbConnection(excelConnectionString);
                    excelConnection.Open();
                    DataTable dt = new DataTable();
                    dt = excelConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    if (dt == null)
                    {
                        return null;
                    }
                    String[] excelSheets = new String[dt.Rows.Count];
                    int t = 0;
                    //excel data saves in temp file here.
                    foreach (DataRow row in dt.Rows)
                    {
                        excelSheets[t] = row["TABLE_NAME"].ToString();
                        t++;
                    }
                    OleDbConnection excelConnection1 = new OleDbConnection(excelConnectionString);
                    string query = string.Format("Select * from [{0}]", excelSheets[0]);
                    using (OleDbDataAdapter dataAdapter = new OleDbDataAdapter(query, excelConnection1))
                    {
                        dataAdapter.Fill(ds);
                    }
                    excelConnection.Close();
                    excelConnection1.Close();
                }
                if (fileExtension.ToString().ToLower().Equals(".xml"))
                {
                    string fileLocation = Server.MapPath("~/Content/") + Request.Files["FileUpload"].FileName;
                    if (System.IO.File.Exists(fileLocation))
                    {
                        System.IO.File.Delete(fileLocation);
                    }
                    Request.Files["FileUpload"].SaveAs(fileLocation);
                    XmlTextReader xmlreader = new XmlTextReader(fileLocation);
                    // DataSet ds = new DataSet();
                    ds.ReadXml(xmlreader);
                    xmlreader.Close();
                }
                if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
                {
                    return RedirectToAction("Login", "Login", null);
                }
                ViewBag.Logout = Session["Username"];
                ViewBag.roleid = Session["RoleID"];

                string Parts = null;
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {

                    string a = ds.Tables[0].Rows[i][0].ToString();
                    if (string.IsNullOrEmpty(a) == false)
                    {
                        int PartNo = 0;
                        //Checking value in db
                        try
                        {
                            PartNo = Convert.ToInt32(ds.Tables[0].Rows[i][0]);
                        }
                        catch
                        {
                            Parts = Parts + " PartNo should be Number.\n";
                            continue;
                        }
                        //
                        tblpart tblpart = new tblpart();
                        String Username = Session["Username"].ToString();
                        tblpart.CreatedBy = Convert.ToInt32(Session["UserId"]);

                        //check if unit is integer(U_ID from tblunits).
                        int unit = 0;
                        try
                        {
                            unit = Convert.ToInt32(ds.Tables[0].Rows[i][4]);
                        }
                        catch
                        {
                            Parts = Parts + "Unit of Part number" + PartNo + " should be Number.\n";
                            continue;
                        }

                        var msgcode = db.tblunits.Where(m => m.U_ID == unit).SingleOrDefault();
                        if (msgcode == null)
                        {
                            Parts = Parts + "Unit of Part number " + PartNo + " is not present in DataBase.\n";
                            continue;
                        }
                        else
                        {
                            tblpart.UnitDesc = Convert.ToInt32(ds.Tables[0].Rows[i][4]);

                        }
                        tblpart.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        tblpart.IsDeleted = 0;
                        tblpart.PartNo = PartNo;
                        try
                        {
                            tblpart.IdleCycleTime = Convert.ToInt32(ds.Tables[0].Rows[i][3]);
                        }
                        catch
                        {
                            Parts = Parts + "IdleCycleTime of Part Number" + PartNo + " should be Number.\n";
                            continue;
                        }
                        try
                        {
                            tblpart.PartName = ds.Tables[0].Rows[i][2].ToString();
                            tblpart.PartDesc = ds.Tables[0].Rows[i][1].ToString();
                        }
                        catch
                        {
                            Parts = Parts + "PartName or PartDesc " + PartNo + " should not be empty.\n";
                            continue;
                        }
                        //ActiveLog Code
                        int UserID = Convert.ToInt32(Session["UserId"]);
                        string CompleteModificationdetail = "New Creation";
                        Action = "Create";

                        var msgcode1 = db.tblparts.Where(m => m.PartNo == PartNo && m.IsDeleted == 0).SingleOrDefault();
                        if (msgcode1 == null)
                        {
                            db.tblparts.Add(tblpart);
                            db.SaveChanges();
                        }
                        else
                        {
                            Parts = Parts + "Part Number " + PartNo + " exists in Database.\n";
                            continue;
                        }


                        //string dat = DateTime.Now.ToString();
                        //MsqlConnection mc1 = new MsqlConnection();
                        //mc1.open();
                        //MySqlCommand cmd2 = new MySqlCommand("INSERT INTO tblparts(CreatedOn,CreatedBy,IsDeleted, PartDesc, PartName,IdleCycleTime, UnitDesc,PartNo) VALUES" +
                        //   "('" + dat + "', " + Convert.ToInt32(Session["UserId"]) + ",0,'" + ds.Tables[0].Rows[i][1].ToString() + "','" + ds.Tables[0].Rows[i][2].ToString() + "'," +
                        //    "" + Convert.ToInt32(ds.Tables[0].Rows[i][3]) + ",'" + Convert.ToInt32(ds.Tables[0].Rows[i][4]) + "'," + Convert.ToInt32(ds.Tables[0].Rows[i][0]) + ")", mc1.msqlConnection);
                        //cmd2.ExecuteNonQuery();
                        //mc1.close();
                    }
                }
                Session["PartNo"] = Parts;
            }

            return RedirectToAction("Index", "Parts");
            //return View(); 
        }
    }
}
