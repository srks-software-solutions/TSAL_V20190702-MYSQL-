using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Tata.Models;
using TataMySqlConnection;

namespace TATA.Controllers
{
    public class MachineDetailsController : Controller
    {
        //
        // GET: /MachineDetails/
        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "MachineDetails";
        string Action = null;

        public ActionResult Index()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            var machinedetails = db.tblmachinedetails.Where(m => m.IsDeleted == 0);
            return View(machinedetails.ToList());
        }

        public ActionResult Create()
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName");
            ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0), "ShopID", "ShopName");
            ViewBag.Cell = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0), "CellID", "CellName");

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(tblmachinedetail tblmachine, int Shop = 0, int Plant = 0, int Cell = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];

            string machineinv = tblmachine.MachineInvNo;
            var duplicateEntry = db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.PlantID == Plant && m.ShopID == Shop && m.CellID == Cell && m.MachineInvNo == machineinv).ToList();
            if (duplicateEntry.Count == 0)
            {
                tblmachine.InsertedBy = Convert.ToInt32(Session["UserId"]);
                tblmachine.InsertedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                tblmachine.IsDeleted = 0;
                tblmachine.IsParameters = 1;
                tblmachine.MachineType = "2";
                tblmachine.PlantID = Plant;
                tblmachine.ShopID = Shop;
                var shopname = db.tblshops.Where(m => m.IsDeleted == 0 && m.ShopID == Shop).Select(m => m.ShopName).FirstOrDefault();
                tblmachine.ShopNo = shopname.ToString();

                if (Cell != 0)
                {
                    tblmachine.CellID = Cell;
                }
                //ActiveLog Code
                int UserID = Convert.ToInt32(Session["UserId"]);
                String Username = Session["Username"].ToString();
                string CompleteModificationdetail = "New Creation";
                Action = "Create";
                //ActiveLogStorage Obj = new ActiveLogStorage();
                //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
                //End
                db.tblmachinedetails.Add(tblmachine);
                db.SaveChanges();
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
            tblmachinedetail tblmachine = db.tblmachinedetails.Find(id);
            if (tblmachine == null)
            {
                return HttpNotFound();
            }

            ViewData["ispcb"] = tblmachine.IsPCB;
            ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", tblmachine.PlantID);
            ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == tblmachine.PlantID), "ShopID", "ShopName", tblmachine.ShopID);
            ViewBag.Cell = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == tblmachine.ShopID), "CellID", "CellName", tblmachine.CellID);
            ViewBag.WC = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == tblmachine.ShopID && m.CellID == tblmachine.CellID), "MachineInvNo", "MachineInvNo", tblmachine.MachineInvNo);
           
            return View(tblmachine);
        }
        [HttpPost]
        public ActionResult Edit(tblmachinedetail tblmachine, int Shop = 0, int Plant = 0, int Cell = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            int UserID = Convert.ToInt32(Session["UserID"]);
            tblmachine.ModifiedBy = UserID;
            tblmachine.ModifiedOn = System.DateTime.Now;
            {
                if (ModelState.IsValid)
                {
                    #region Active Log Code
                    //tblmachinedetail OldData = db.tblmachinedetails.Find(tblmachine.MachineID);
                    //String Username = Session["Username"].ToString();
                    //IEnumerable<string> FullData = ActiveLog.EnumeratePropertyDifferences<tblmachinedetail>(OldData, tblmachine);
                    //ICollection<tblmachinedetail> c = FullData as ICollection<tblmachinedetail>;
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

                    var shopname = db.tblshops.Where(m => m.IsDeleted == 0 && m.ShopID == tblmachine.ShopID).Select(m => m.ShopName).FirstOrDefault();
                    tblmachine.ShopNo = shopname.ToString();

                    //tblmachine.PlantID = Plant;
                    //tblmachine.ShopID = Shop;
                    //if (Cell != 0)
                    //{
                    //    tblmachine.CellID = Cell;
                    //}
                    db.Entry(tblmachine).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.Plant = new SelectList(db.tblplants.Where(m => m.IsDeleted == 0), "PlantID", "PlantName", tblmachine.PlantID);
            ViewBag.Shop = new SelectList(db.tblshops.Where(m => m.IsDeleted == 0 && m.PlantID == tblmachine.PlantID), "ShopID", "ShopName", tblmachine.ShopID);
            ViewBag.Cell = new SelectList(db.tblcells.Where(m => m.IsDeleted == 0 && m.ShopID == tblmachine.ShopID), "CellID", "CellName", tblmachine.CellID);
            ViewBag.WC = new SelectList(db.tblmachinedetails.Where(m => m.IsDeleted == 0 && m.ShopID == tblmachine.ShopID && m.CellID == tblmachine.CellID), "MachineInvNo", "MachineInvNo", tblmachine.MachineInvNo);
            
            return View(tblmachine);
        }

        public ActionResult Delete(int id)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            //ViewBag.IsConfigMenu = 0;
            
            //start Logging
            int UserID = Convert.ToInt32(Session["UserId"]);
            //String Username = Session["Username"].ToString();
            //string CompleteModificationdetail = "Deleted MachineDetails";
            //Action = "Delete";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            //End
            tblmachinedetail tblmachine = db.tblmachinedetails.Find(id);
            tblmachine.IsDeleted = 1;
            tblmachine.ModifiedBy = UserID;
            tblmachine.DeletedDate = DateTime.Now.ToString("yyyy-MM-dd");
            db.Entry(tblmachine).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {

            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
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

        public ActionResult ImportMachineDetails()
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
        public ActionResult ImportMachineDetails(HttpPostedFileBase file)
        {

            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }

            ////start logging
            //String Username = Session["Username"].ToString();
            //int UserID = Convert.ToInt32(Session["UserId"]);
            //string CompleteModificationdetail = "Import PriorityAlarm";
            //Action = "ImportPriorityAlarm";
            //ActiveLogStorage Obj = new ActiveLogStorage();
            //Obj.SaveActiveLog(Action, Controller, Username, UserID, CompleteModificationdetail);
            ////End

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
                        //excelConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileLocation + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";
                        //excelConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileLocation + ";Extended Properties=Excel 8.0;HDR=YES;";

                        //excelConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;" +
                        //"Data Source=" + fileLocation + ";Extended Properties=\"Excel 8.0;HDR=NO;IMEX=1\"" ;

                        excelConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" +
                        fileLocation + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";

                        excelConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                        fileLocation + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=1\"";

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

                string Errors = null;
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    string a = ds.Tables[0].Rows[i][0].ToString();

                    MsqlConnection mc1 = new MsqlConnection();
                    mc1.open();
                    string dat = DateTime.Now.ToString();
                    dat = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    MySqlCommand cmd2 = null;
                    try
                    {

                        //cmd2 = new MySqlCommand("INSERT INTO tblmachinedetails(InsertedBy,InsertedOn," +
                        //    "IsDeleted, MachineInvNo, IPAddress, ControllerType,MachineModel,MachineMake,ModelType" +
                        //",MachineDispName,IsParameters,ShopNo,IsPCB) VALUES" +
                        //    "(" + Convert.ToInt32(Session["UserId"]) + ",'" + dat + "',0,'" + ds.Tables[0].Rows[i][0].ToString() + "','" + ds.Tables[0].Rows[i][1].ToString() + "','" +
                        //    "" + ds.Tables[0].Rows[i][2].ToString() + "','" + ds.Tables[0].Rows[i][3].ToString() + "','" + ds.Tables[0].Rows[i][4].ToString() + "','" +
                        //"" + ds.Tables[0].Rows[i][5].ToString() + "','" + ds.Tables[0].Rows[i][6].ToString() + "','" + ds.Tables[0].Rows[i][7].ToString() + "'," +
                        //"" + Convert.ToInt32(ds.Tables[0].Rows[i][8]) + ",'" + ds.Tables[0].Rows[i][9].ToString() + "')", mc1.msqlConnection);

                        cmd2 = new MySqlCommand("INSERT INTO tblmachinedetails (InsertedBy,InsertedOn,IsDeleted,MachineType, MachineInvNo, IPAddress, ControllerType,MachineModel,MachineMake,ModelType,MachineDispName,IsParameters,ShopNo,IsPCB) VALUES( '" + Convert.ToInt32(Session["UserId"]) + "','" + dat + "'," + 0 + "," + 2 + ",'" + ds.Tables[0].Rows[i][0].ToString() + "','" + ds.Tables[0].Rows[i][1].ToString() + "','" + ds.Tables[0].Rows[i][2].ToString() + "','" + ds.Tables[0].Rows[i][3].ToString() + "','" + ds.Tables[0].Rows[i][4].ToString() + "','" + ds.Tables[0].Rows[i][5].ToString() + "','" + ds.Tables[0].Rows[i][6].ToString() + "','" + ds.Tables[0].Rows[i][7].ToString() + "','" + ds.Tables[0].Rows[i][8].ToString() + "','" + ds.Tables[0].Rows[i][9].ToString() + "')", mc1.msqlConnection);

                        cmd2.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Errors = e.ToString();
                    }

                    mc1.close();

                }
                Session["MachErrDetails"] = Errors;
            }


            return RedirectToAction("Index");
        }

    }
}
