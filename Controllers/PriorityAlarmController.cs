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
    public class PriorityAlarmController : Controller
    {
        private mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "ProrityAlarms";
        string Action = null;
        //
        // GET: /ProrityAlarms/

        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            return View(db.tblpriorityalarms.Where(m=>m.IsDeleted==0).ToList());
        }

        //
        // GET: /ProrityAlarms/Details/5

        public ActionResult Details(int id = 0)
        {
            tblpriorityalarm tblpriorityalarm = db.tblpriorityalarms.Find(id);
            if (tblpriorityalarm == null)
            {
                return HttpNotFound();
            }
            return View(tblpriorityalarm);
        }

        //
        // GET: /ProrityAlarms/Create

        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            ViewBag.MachineID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName");
            return View();
        }

        //
        // POST: /ProrityAlarms/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblpriorityalarm tblpriorityalarm)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            string CreatedON = DateTime.Now.ToString("yyyyMMddHHmmss"); 
            if (ModelState.IsValid)
            {
                string CorrectedDate = null;
                tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).SingleOrDefault();
                TimeSpan Start = StartTime.StartTime;
                if (Start <= DateTime.Now.TimeOfDay)
                {
                    CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                }
                else
                {
                    CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                }

                //ActiveLog Code
                int UserID = Convert.ToInt32(Session["UserId"]);
                string CompleteModificationdetail = "New Creation";                
                Action = "Create";
                //ActiveLogStorage Obj = new ActiveLogStorage();
                //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
                //End
                tblpriorityalarm.CreatedBy = UserID;
                tblpriorityalarm.CreatedOn = CreatedON;
                tblpriorityalarm.CorrectedDate = CorrectedDate;
                tblpriorityalarm.MachineID = 1;
                db.tblpriorityalarms.Add(tblpriorityalarm);
                db.SaveChanges();
                //db.createAlarmsPrority(tblpriorityalarm.AlarmNumber, tblpriorityalarm.AlarmDesc, tblpriorityalarm.AxisNo, tblpriorityalarm.AlarmGroup, tblpriorityalarm.PriorityNumber, 0, CreatedON, 1);
                return RedirectToAction("Index");
            }
            ViewBag.MachineID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName");
            return View(tblpriorityalarm);
        }

        //
        // GET: /ProrityAlarms/Edit/5

        public ActionResult Edit(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            tblpriorityalarm tblpriorityalarm = db.tblpriorityalarms.Find(id);
            if (tblpriorityalarm == null)
            {
                return HttpNotFound();
            }
            ViewData["MachineID"] = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName",tblpriorityalarm.MachineID);
            return View(tblpriorityalarm);
        }

        //
        // POST: /ProrityAlarms/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(tblpriorityalarm tblpriorityalarm)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
           

            //Section related to storing data in ActiveLog
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserId"]);
            //#region Active Log Code
            //tblpriorityalarm OldData = db.tblpriorityalarms.Find(tblpriorityalarm.AlarmID);
            //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<tblpriorityalarm>(OldData, tblpriorityalarm);
            //ICollection<tblpriorityalarm> c = FullData as ICollection<tblpriorityalarm>;
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
            //#endregion End Active Log

            if (ModelState.IsValid)
            {
                DateTime modifiedOn = DateTime.Now;
                tblpriorityalarm.ModifiedBy = UserID;
                tblpriorityalarm.ModifiedOn = modifiedOn;
                db.Entry(tblpriorityalarm).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                //db.updateAlarmsPrority(tblpriorityalarm.AlarmID, tblpriorityalarm.AlarmNumber, tblpriorityalarm.AlarmDesc, tblpriorityalarm.AxisNo, tblpriorityalarm.AlarmGroup, tblpriorityalarm.PriorityNumber, 0, tblpriorityalarm.CreatedOn, tblpriorityalarm.CreatedBy, modifiedOn, 1);
                return RedirectToAction("Index");
            }
            ViewBag.MachineID = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0), "MachineID", "MachineDispName");
            return View(tblpriorityalarm);
        }

        //
        // GET: /ProrityAlarms/Delete/5

        public ActionResult Delete(int id = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            tblpriorityalarm tblpriorityalarm = db.tblpriorityalarms.Find(id);
            if (tblpriorityalarm == null)
            {
                return HttpNotFound();
            }
            DateTime modifiedOn = DateTime.Now;
            db.Entry(tblpriorityalarm).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            string CompleteModificationdetail = "Deleted PriorityAlarm";
            Action = "Delete";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            //db.updateAlarmsPrority(tblpriorityalarm.AlarmID, tblpriorityalarm.AlarmNumber, tblpriorityalarm.AlarmDesc, tblpriorityalarm.AxisNo, tblpriorityalarm.AlarmGroup, tblpriorityalarm.PriorityNumber, 1, tblpriorityalarm.CreatedOn, tblpriorityalarm.CreatedBy, modifiedOn, 1);
            tblpriorityalarm.IsDeleted = 1;
            db.Entry(tblpriorityalarm).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
            
        }

        //
        // POST: /ProrityAlarms/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
           
            tblpriorityalarm tblpriorityalarm = db.tblpriorityalarms.Find(id);
            
            db.tblpriorityalarms.Remove(tblpriorityalarm);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
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
            ////start logging
            //String Username = Session["Username"].ToString();
            //int UserID = Convert.ToInt32(Session["UserId"]);
            //string CompleteModificationdetail = "Import PriorityAlarm";
            //Action = "ImportPriorityAlarm";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            ////End

            //Deleting Excel file
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


            DataSet ds = new DataSet();
            if (Request.Files["file"].ContentLength > 0)
            {
                string fileExtension =
                                     System.IO.Path.GetExtension(Request.Files["file"].FileName);
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
                    excelConnection1.Close();
                    excelConnection.Close();

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
                string msg = null;
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    string CorrectedDate = null;
                    tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).SingleOrDefault();
                    TimeSpan Start = StartTime.StartTime;
                    if (Start <= DateTime.Now.TimeOfDay)
                    {
                        CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                    }

                    string dat = DateTime.Now.ToString("yyyyMMddHHmmss");

                    string a = ds.Tables[0].Rows[i][0].ToString();
                    if (string.IsNullOrEmpty(a) == false)
                    {

                        //Checking value in db
                        int AlarmNumber = 0;
                        try
                        {
                            AlarmNumber = Convert.ToInt32(ds.Tables[0].Rows[i][0]);
                        }
                        catch
                        {
                            msg = msg + " AlarmNumber should be a Number.\n";
                            continue;
                        }
                        //
                        tblpriorityalarm tblprio = new tblpriorityalarm();
                        String Username = Session["Username"].ToString();
                        tblprio.CreatedBy = Convert.ToInt32(Session["UserId"]);
                        try
                        {
                            tblprio.AlarmDesc = ds.Tables[0].Rows[i][1].ToString();
                        }
                        catch
                        {
                            msg = msg + " Please enter AlarmDescription of AlarmNumber " + AlarmNumber + ".\n";
                            continue;
                        }
                        tblprio.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        tblprio.IsDeleted = 0;
                        tblprio.AlarmNumber = AlarmNumber;
                        try
                        {
                            tblprio.AxisNo = Convert.ToInt32(ds.Tables[0].Rows[i][2]);
                        }
                        catch
                        {
                            msg = msg + " AxisNo of AlarmNumber " + AlarmNumber + " should be a Number.\n";
                            continue;
                        }
                        try
                        {
                            tblprio.AlarmGroup = ds.Tables[0].Rows[i][3].ToString();
                        }
                        catch
                        {
                            msg = msg + " Please enter  AlarmGroup of AlarmNumber " + AlarmNumber + " .\n";
                            continue;
                        }

                        try
                        {
                            tblprio.PriorityNumber = Convert.ToInt32(ds.Tables[0].Rows[i][4]);
                        }
                        catch
                        {
                            msg = msg + " Priority Number of AlarmNumber" + AlarmNumber + " should be a Number.\n";
                            continue;
                        }
                        int macid = 0;
                        try
                        {
                            macid = Convert.ToInt32(ds.Tables[0].Rows[i][5]);
                        }
                        catch
                        {
                            msg = msg + " MachineID of AlarmNumber " + AlarmNumber + " should be a Number.\n";
                            continue;
                        }
                        var machid = db.tblmachinedetails.Where(m => m.MachineID == macid).SingleOrDefault();
                        if (machid == null)
                        {
                            msg = msg + " MachineID of AlarmNumber " + AlarmNumber + " doesnot match in Database.\n";
                            continue;
                        }
                        else
                        {
                            tblprio.MachineID = Convert.ToInt32(ds.Tables[0].Rows[i][5]);
                        }

                        //ActiveLog Code
                        int UserID = Convert.ToInt32(Session["UserId"]);
                        string CompleteModificationdetail = "New Creation";
                        Action = "Create";

                        var msgcode = db.tblpriorityalarms.Where(m => m.AlarmNumber == AlarmNumber && m.IsDeleted == 0).SingleOrDefault();
                        //
                        if (msgcode == null)
                        {
                            db.tblpriorityalarms.Add(tblprio);
                            db.SaveChanges();
                        }
                        else
                        {
                            msg = msg + "Alarm Number " + AlarmNumber + " exists in Database.\n";
                            continue;
                        }


                        //MsqlConnection mc1 = new MsqlConnection();
                        //mc1.open();
                        //MySqlCommand cmd2 = new MySqlCommand("INSERT INTO tblpriorityalarms(CreatedOn,CreatedBy," +
                        //    "IsDeleted,isMailSent,CorrectedDate, AlarmNumber, AlarmDesc,AxisNo, AlarmGroup,PriorityNumber,MachineID) VALUES" +
                        //    "('" + dat + "'," + Convert.ToInt32(Session["UserId"]) + ",0,0,'" + CorrectedDate + "'," + Convert.ToInt32(ds.Tables[0].Rows[i][0]) + ",'" +
                        //    "" + ds.Tables[0].Rows[i][1].ToString() + "'," + ds.Tables[0].Rows[i][2].ToString() + "," + ds.Tables[0].Rows[i][3].ToString() + "," +
                        //"" + ds.Tables[0].Rows[i][4].ToString() + "," + Convert.ToInt32(ds.Tables[0].Rows[i][5]) + ")", mc1.msqlConnection);
                        //cmd2.ExecuteNonQuery();
                        //mc1.close();
                    }
                }
                Session["AlarmNumber"] = msg;
            }
            return RedirectToAction("Index");
        }
    }
}