using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;
using TataMySqlConnection;

namespace Tata.Controllers
{
    public class ShiftMethodController : Controller
    {
        //
        // GET: /ShiftMethod/

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
            var cat = db.tblshiftmethods.Where(m => m.IsDeleted == 0);
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
        public ActionResult Create(tblshiftmethod tblp)
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

            string shiftmethodname = tblp.ShiftMethodName;
            var doesthisExist = db.tblshiftmethods.Where(m => m.IsDeleted == 0 && m.ShiftMethodName == shiftmethodname).ToList();
            if (doesthisExist.Count == 0)
            {
                tblp.CreatedBy = UserID;
                tblp.CreatedOn = DateTime.Now;
                db.tblshiftmethods.Add(tblp);
                db.SaveChanges();
            }
            else
            {
                TempData["Error"] = "Shift Method Exists.";
                return View(tblp);
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
            tblshiftmethod tblmc = db.tblshiftmethods.Find(id);
            if (tblmc == null)
            {
                return HttpNotFound();
            }
            return View(tblmc);
        }
        [HttpPost]
        public ActionResult Edit(tblshiftmethod tblmc)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"]);

            string shiftmethodname = tblmc.ShiftMethodName;
            int shiftmethodId = tblmc.ShiftMethodID;
            var doesthisExist = db.tblshiftmethods.Where(m => m.IsDeleted == 0 && m.ShiftMethodName == shiftmethodname && m.ShiftMethodID != shiftmethodId).ToList();
            if (doesthisExist.Count == 0)
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

                //check if shift method is in use or was used and now its being modified.
                ShiftDetails sd = new ShiftDetails();
                int shiftmethodid = Convert.ToInt32(tblmc.ShiftMethodID);
                bool tick = sd.IsThisShiftMethodIsInActionOrEnded(shiftmethodid);
                if (tick)
                {
                    tblshiftmethod tsm = new tblshiftmethod();
                    tsm.CreatedBy = UserID;
                    tsm.CreatedOn = DateTime.Now;
                    tsm.IsDeleted = 0;
                    tsm.NoOfShifts = tblmc.NoOfShifts;
                    tsm.ShiftMethodDesc = tblmc.ShiftMethodDesc;
                    tsm.ShiftMethodName = tblmc.ShiftMethodName;
                    db.tblshiftmethods.Add(tsm);
                    db.SaveChanges();

                    tblmc.ModifiedBy = UserID;
                    tblmc.ModifiedOn = DateTime.Now;
                    db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    tblmc.ModifiedBy = UserID;
                    tblmc.ModifiedOn = DateTime.Now;
                    db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            else
            {
                TempData["Error"] = "Shift Method Exists.";
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
            tblshiftmethod tblmc = db.tblshiftmethods.Find(id);

            var shiftdetailsList = db.tblshiftdetails.Where(m => m.IsDeleted == 0 && m.ShiftMethodID == id).ToList();
            foreach (var shiftdetailsrow in shiftdetailsList)
            {
                shiftdetailsrow.IsDeleted = 1;
                db.Entry(shiftdetailsrow).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }

            tblmc.IsDeleted = 1;
            db.Entry(tblmc).State = System.Data.Entity.EntityState.Modified;
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

        #region
        //public bool IsThisShiftMethodIsInActionOrEnded(int id)
        //{
        //    bool status = true;
        //    DataTable dataHolder = new DataTable();

        //    string CorrectedDate = null;
        //    tbldaytiming StartTime = db.tbldaytimings.Where(m => m.IsDeleted == 0).SingleOrDefault();
        //    TimeSpan Start = StartTime.StartTime;
        //    if (Start <= DateTime.Now.TimeOfDay)
        //    {
        //        CorrectedDate = DateTime.Now.ToString("yyyy-MM-dd");
        //    }
        //    else
        //    {
        //        CorrectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        //    }

        //    MsqlConnection mc = new MsqlConnection();
        //    mc.open();
        //    String sql = "SELECT * FROM tblShiftPlanner WHERE (( StartDate <='" + CorrectedDate + "' AND EndDate >='" + CorrectedDate + "') OR ( EndDate <'" + CorrectedDate + "' )) AND ShiftMethodID = " + id + " ORDER BY ShiftPlannerID ASC";
        //    MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
        //    da.Fill(dataHolder);
        //    mc.close();

        //    if (dataHolder.Rows.Count == 0)
        //    {
        //        status = false;
        //    }
        //    return status;
        //}
        #endregion
    }
}
