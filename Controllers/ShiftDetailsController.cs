using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace Tata.Controllers
{
    public class ShiftDetailsController : Controller
    {
        //
        // GET: /ShiftDetails/

        mazakdaqEntities db = new mazakdaqEntities();
        string Controller = "shiftdetails";
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
            var cat = db.tblshiftdetails.Where(m => m.IsDeleted == 0).OrderBy(m => m.ShiftMethodID).ToList();
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

            ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName");
            return View();
        }
        [HttpPost]
        public ActionResult Create(IEnumerable<tblshiftdetail> tblp, int ShiftMethod = 0)
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

            //shop name validation
            //string shopname = tblp.ShopName.ToString();
            //var doesThisShiftDetailsExists = db.tblshiftdetails.Where(m => m.IsDeleted == 0 && m.ShiftDetailsName == ).ToList();
            //if (doesThisShopExists.Count == 0)
            //{

            //check if there's a entry of this shiftMethod in tblshiftdetails
            var shiftmethodCheck = db.tblshiftdetails.Where(m => m.IsDeleted == 0 && m.ShiftMethodID == ShiftMethod ).ToList();
            if (shiftmethodCheck.Count > 0)
            {
                Session["Error"] = "ShiftDetails for this ShiftMethod Exists.";
                ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName");
                return RedirectToAction("Index");
            }

            var shiftmethodiddata = db.tblshiftmethods.Where(m => m.IsDeleted == 0 && m.ShiftMethodID == ShiftMethod).SingleOrDefault();
            int noofshifts = shiftmethodiddata.NoOfShifts;
            int rowscount = 0;

            //to check if names are duplicate
            List<string> shiftdetailsnames = new List<string>();
            foreach (var shift in tblp)
            {
                if (shift.ShiftDetailsName != null)
                {
                    shiftdetailsnames.Add(shift.ShiftDetailsName);
                }
            }
            // for current shiftdetails.
            if (shiftdetailsnames.Distinct().Count() != shiftdetailsnames.Count())
            {
                //Console.WriteLine("List contains duplicate values.");
                TempData["Error"] = "Shift Names Cannot be Same.";
                ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName");
                return RedirectToAction("Index");
            }

            try
            {
                foreach (var shift in tblp)
                {
                    if (rowscount < noofshifts)
                    {
                        // calculate duration
                        int duration = 0;
                        string starttimestring = "2016-06-02" + " " + shift.ShiftStartTime;
                        DateTime starttimedatetime = Convert.ToDateTime(starttimestring);
                        string endtimestring = null;

                        TimeSpan tsStart = (System.TimeSpan)shift.ShiftStartTime;
                        TimeSpan tsEnd = (System.TimeSpan)shift.ShiftEndTime;

                        int result = TimeSpan.Compare(tsStart, tsEnd);
                        if (result < 0)
                        {
                            endtimestring = "2016-06-02" + " " + shift.ShiftEndTime;
                        }
                        else if (result > 0)
                        {
                            endtimestring = "2016-06-03" + " " + shift.ShiftEndTime;
                            shift.NextDay = 1;
                        }
                        DateTime endtimedatetime = Convert.ToDateTime(endtimestring);
                        TimeSpan ts = endtimedatetime.Subtract(starttimedatetime);
                        duration = Convert.ToInt32(ts.TotalMinutes);

                        //create new object/row
                        tblshiftdetail tsd = new tblshiftdetail();
                        tsd.CreatedBy = UserID;
                        tsd.CreatedOn = DateTime.Now;
                        tsd.Duration = duration;
                        tsd.IsDeleted = 0;
                        tsd.NextDay = shift.NextDay;
                        tsd.ShiftMethodID = ShiftMethod;
                        tsd.ShiftDetailsDesc = shift.ShiftDetailsDesc;
                        tsd.ShiftDetailsName = shift.ShiftDetailsName;
                        tsd.ShiftEndTime = shift.ShiftEndTime;
                        tsd.ShiftStartTime = shift.ShiftStartTime;

                        db.tblshiftdetails.Add(tsd);
                        db.SaveChanges();
                    }
                    rowscount++;
                }
            }
            catch (Exception e)
            {
                Session["Error"] = "Shift Name already exists for this ShiftMethod.";
                using (mazakdaqEntities db1 = new mazakdaqEntities())
                {
                    var todeletedata = db1.tblshiftdetails.Where(m => m.IsDeleted == 0 && m.ShiftMethodID == ShiftMethod).ToList();
                    foreach (var row in todeletedata)
                    {
                        row.IsDeleted = 1;
                        db.Entry(row).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            //ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName");
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

            //get all the shiftsdetails in the shift method
            tblshiftdetail tblmc = db.tblshiftdetails.Find(id);
            List<tblshiftdetail> tsd = null;
            if (tblmc == null)
            {
                return HttpNotFound();
            }
            else
            {
                int shiftmethodid = Convert.ToInt32(tblmc.ShiftMethodID);
                tsd = db.tblshiftdetails.Where(m => m.IsDeleted == 0 && m.ShiftMethodID == shiftmethodid).ToList();
            }
            ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName", tblmc.ShiftMethodID);
            ViewBag.NextDay = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "NextDay", "NextDay", tblmc.NextDay);
            //ViewBag.unit = new SelectList(db.tblunits.Where(m => m.IsDeleted == 0), "U_ID", "Unit", tblpart.UnitDesc);
            return View(tsd);
        }
        [HttpPost]
        public ActionResult Edit(IEnumerable<tblshiftdetail> tblp, int ShiftMethod = 0)
        {
            if ((Session["UserId"] == null) || (Session["UserId"].ToString() == String.Empty))
            {
                return RedirectToAction("Login", "Login", null);
            }
            ViewBag.Logout = Session["Username"];
            ViewBag.roleid = Session["RoleID"];
            String Username = Session["Username"].ToString();
            int UserID = Convert.ToInt32(Session["UserID"]);

            var shiftmethodiddata = db.tblshiftmethods.Where(m => m.IsDeleted == 0 && m.ShiftMethodID == ShiftMethod).SingleOrDefault();
            int noofshifts = shiftmethodiddata.NoOfShifts;
            int rowscount = 0;

            //insert isedited and other details into old rows and insert the new rows.
            var shiftDetailsData = db.tblshiftdetails.Where(m => m.IsDeleted == 0 && m.ShiftMethodID == ShiftMethod).ToList();
            //check if shift method is in use or was used and now its being modified.
            ShiftDetails sd = new ShiftDetails();
            int shiftmethodid = Convert.ToInt32(ShiftMethod);
            bool tick = sd.IsThisShiftMethodIsInActionOrEnded(shiftmethodid);
            try
            {
                foreach (var shift in tblp)
                {
                    if (rowscount < noofshifts)
                    {
                        using (mazakdaqEntities db3 = new mazakdaqEntities())
                        {
                            // calculate duration
                            int duration = 0;
                            string starttimestring = "2016-06-02" + " " + shift.ShiftStartTime;
                            DateTime starttimedatetime = Convert.ToDateTime(starttimestring);
                            string endtimestring = null;

                            TimeSpan tsStart = (System.TimeSpan)shift.ShiftStartTime;
                            TimeSpan tsEnd = (System.TimeSpan)shift.ShiftEndTime;

                            int result = TimeSpan.Compare(tsStart, tsEnd);
                            if (result < 0)
                            {
                                endtimestring = "2016-06-02" + " " + shift.ShiftEndTime;
                            }
                            else if (result > 0)
                            {
                                endtimestring = "2016-06-03" + " " + shift.ShiftEndTime;
                                shift.NextDay = 1;
                            }
                            DateTime endtimedatetime = Convert.ToDateTime(endtimestring);
                            TimeSpan ts = endtimedatetime.Subtract(starttimedatetime);
                            duration = Convert.ToInt32(ts.TotalMinutes);

                            if (tick)
                            {
                                //create new object/row
                                int shiftid = shift.ShiftDetailsID;
                                int oldcreatedby = 0;
                                DateTime oldcreatedon = DateTime.Now;
                                using (mazakdaqEntities db1 = new mazakdaqEntities())
                                {
                                    var getShiftId = db1.tblshiftdetails.Where(m => m.IsDeleted == 0 && m.ShiftDetailsID == shiftid).SingleOrDefault();
                                    getShiftId.IsShiftDetailsEdited = 1;
                                    getShiftId.IsDeleted = 1;
                                    getShiftId.ShiftMethodID = ShiftMethod;
                                    getShiftId.ShiftDetailsEditedDate = DateTime.Now;

                                    oldcreatedon = Convert.ToDateTime(getShiftId.CreatedOn);
                                    oldcreatedby = Convert.ToInt32(getShiftId.CreatedBy);
                                    ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName", shift.ShiftMethodID);

                                    db1.Entry(getShiftId).State = System.Data.Entity.EntityState.Modified;
                                    db1.SaveChanges();
                                }
                                tblshiftdetail tsd = new tblshiftdetail();
                                tsd.Duration = duration;
                                tsd.IsDeleted = 0;
                                tsd.CreatedBy = oldcreatedby;
                                tsd.CreatedOn = oldcreatedon;
                                tsd.ModifiedBy = UserID;
                                tsd.ModifiedOn = DateTime.Now;
                                tsd.IsDeleted = 0;
                                tsd.NextDay = shift.NextDay;
                                tsd.ShiftMethodID = ShiftMethod;
                                tsd.ShiftDetailsDesc = shift.ShiftDetailsName;
                                tsd.ShiftDetailsName = shift.ShiftDetailsDesc;
                                tsd.ShiftEndTime = shift.ShiftEndTime;
                                tsd.ShiftStartTime = shift.ShiftStartTime;
                                db.tblshiftdetails.Add(tsd);
                                db.SaveChanges();
                            }
                            else
                            {
                                //create new object/row
                                shift.ModifiedBy = UserID;
                                shift.ModifiedOn = DateTime.Now;
                                shift.Duration = duration;
                                shift.IsDeleted = 0;
                                shift.ShiftMethodID = ShiftMethod;

                                db3.Entry(shift).State = System.Data.Entity.EntityState.Modified;
                                db3.SaveChanges();
                            }
                        }
                    }
                    rowscount++;
                }

            }
            catch (Exception e)
            {
                ViewBag.ShiftMethod = new SelectList(db.tblshiftmethods.Where(m => m.IsDeleted == 0), "ShiftMethodID", "ShiftMethodName", ShiftMethod);
                return View(tblp);
            }
            return RedirectToAction("Index");
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
            tblshiftdetail tblmc = db.tblshiftdetails.Find(id);
            tblmc.IsDeleted = 1;
            tblmc.ModifiedBy = UserID;
            tblmc.ModifiedOn = DateTime.Now;
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

        public JsonResult GetShifts(int shiftsCount)
        {
            int shifts = 0;
            var NumberOfShifts = db.tblshiftmethods.Where(m => m.IsDeleted == 0 && m.ShiftMethodID == shiftsCount).Take(1).ToList();
            shifts = Convert.ToInt32(NumberOfShifts[0].NoOfShifts);
            return Json(shifts, JsonRequestBehavior.AllowGet);
        }

    }
}
