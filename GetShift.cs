using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Tata.Models;
using TataMySqlConnection;

namespace Tata
{
    public class GetShift
    {

        public string GetShiftForMachine(int machineID, DateTime datetimevariable)
        {
            string shift = null;
            string datestring = Convert.ToDateTime(datetimevariable).ToString("yyyy-MM-dd");
            string timestring = Convert.ToDateTime(datetimevariable).Hour + " : " + Convert.ToDateTime(datetimevariable).Minute + ":" + Convert.ToDateTime(datetimevariable).Second;

            //shift for today or any other day.(Even for future if plan is set).
            if (datestring == DateTime.Now.Date.ToString("yyyy-MM-dd"))
            {
                string datetimevariablestring = Convert.ToDateTime(datetimevariable).ToString("yyyy-MM-dd HH:mm:ss");
                String sql = "SELECT * FROM tblshiftdetails_machinewise WHERE MachineID = " + machineID + " AND StartTime <='" + datetimevariablestring + "' AND EndTime >= '" + datetimevariablestring + "' ORDER BY StartTime ASC";
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                DataTable dataHolder = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();
                if (dataHolder.Rows.Count != 0)
                {
                    shift = dataHolder.Rows[0][2].ToString();
                }
            }
            else
            {
                //1. Get shiftMethod from shiftPlanner for that date & machine.
                //2. Get Factor and Based on that find if any of them had a plan .

                String sql = "SELECT * FROM tblshiftplanner WHERE   StartDate <='" + datestring + "' AND EndTime >= '" + datestring + "' ";
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                DataTable dataHolder = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();

                int ShiftMethodsID = 0;
                for (int i = 0; i < dataHolder.Rows.Count; i++)
                {
                    string FactorName = null;
                    int FactorID = 0;
                    bool tick = false;
                    if (!DBNull.Value.Equals(dataHolder.Rows[i][4]))
                    {
                        ShiftMethodsID = Convert.ToInt32(dataHolder.Rows[i][3]);
                        FactorID = Convert.ToInt32(dataHolder.Rows[i][4]);
                        FactorName = "Plant";
                        tick = DoesThisMachineBelongToThisFactor(FactorName, FactorID, machineID);
                    }
                    else if (!DBNull.Value.Equals(dataHolder.Rows[i][5]))
                    {
                        ShiftMethodsID = Convert.ToInt32(dataHolder.Rows[i][3]);
                        FactorID = Convert.ToInt32(dataHolder.Rows[i][5]);
                        FactorName = "Shop";
                        tick = DoesThisMachineBelongToThisFactor(FactorName, FactorID, machineID);
                    }
                    else if (!DBNull.Value.Equals(dataHolder.Rows[i][6]))
                    {
                        ShiftMethodsID = Convert.ToInt32(dataHolder.Rows[i][3]);
                        FactorID = Convert.ToInt32(dataHolder.Rows[i][6]);
                        FactorName = "Cell";
                        tick = DoesThisMachineBelongToThisFactor(FactorName, FactorID, machineID);
                    }
                    else if (!DBNull.Value.Equals(dataHolder.Rows[i][7]))
                    {
                        ShiftMethodsID = Convert.ToInt32(dataHolder.Rows[i][3]);
                        FactorID = Convert.ToInt32(dataHolder.Rows[i][7]);
                        FactorName = "Machine";
                        tick = DoesThisMachineBelongToThisFactor(FactorName, FactorID, machineID);
                    }
                    if (tick)
                    {
                        break;
                    }
                }

                //now get shift based on shiftmethod and time from tblshiftdetails

                String sql1 = "SELECT * FROM tblshiftdetails WHERE ShiftMethodID = " + ShiftMethodsID + "AND ShiftStartTime <='" + timestring + "' AND ShiftEndTime >= '" + timestring + "' ORDER BY ShiftStartTime ASC";
                MsqlConnection mc1 = new MsqlConnection();
                mc1.open();
                DataTable dataHolder1 = new DataTable();
                MySqlDataAdapter da1 = new MySqlDataAdapter(sql1, mc1.msqlConnection);
                da1.Fill(dataHolder1);
                mc1.close();
                if (dataHolder1.Rows.Count > 0)
                {
                    shift = dataHolder1.Rows[0][1].ToString();
                }
            }

            return shift;
        }

        public int GetShiftDuration(int machineID, string shift = null)
        {
            int totalminutes = 0;

            if (shift != null)
            {
                String sql = "SELECT * FROM tblshiftdetails_machinewise WHERE MachineID = " + machineID + " AND ShiftName = '" + shift + "'";
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                DataTable dataHolder = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();
                if (dataHolder.Rows.Count != 0)
                {
                    for (int i = 0; i < dataHolder.Rows.Count; i++)
                    {
                        TimeSpan tss = (System.TimeSpan)dataHolder.Rows[i][3];
                        TimeSpan tse = (System.TimeSpan)dataHolder.Rows[i][4];
                        TimeSpan finalTS1 = new TimeSpan(0, 0, 0, 0);
                        int sHour = tss.Hours;
                        int eHour = tse.Hours;

                        if (eHour < sHour)
                        {
                            finalTS1 = tse.Subtract(tss);
                        }
                        else
                        {
                            finalTS1 = tss.Subtract(tse);
                        }
                        totalminutes += Convert.ToInt32(finalTS1.TotalMinutes);
                    }
                }
            }
            else
            {
                String sql = "SELECT * FROM tblshiftdetails_machinewise WHERE MachineID = " + machineID;
                MsqlConnection mc = new MsqlConnection();
                mc.open();
                DataTable dataHolder = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
                da.Fill(dataHolder);
                mc.close();
                if (dataHolder.Rows.Count != 0)
                {
                    for (int i = 0; i < dataHolder.Rows.Count; i++)
                    {
                        TimeSpan tss = (System.TimeSpan)dataHolder.Rows[i][3];
                        TimeSpan tse = (System.TimeSpan)dataHolder.Rows[i][4];
                        TimeSpan finalTS1 = new TimeSpan(0, 0, 0, 0);
                        int sHour = tss.Hours;
                        int eHour = tse.Hours;

                        if (eHour < sHour)
                        {
                            finalTS1 = tse.Subtract(tss);
                        }
                        else
                        {
                            finalTS1 = tss.Subtract(tse);
                        }
                        totalminutes += Convert.ToInt32(finalTS1.TotalMinutes);
                    }
                }
            }

            return totalminutes;
        }

        //not using
        protected string GetShiftForMachine_Date(int machineId, DateTime datetimevariable)
        {
            string shift = null;
            string datestring = datetimevariable.Date.ToString("yyyy-MM-dd");
            string timestring = datetimevariable.Hour + " : " + datetimevariable.Minute + ":" + datetimevariable.Second;

            //1. Get shiftMethod from shiftPlanner for that date & machine.
            //2. Get Factor and Based on that find if any of them had a plan .

            String sql = "SELECT * FROM tblshiftplanner WHERE   StartDate <='" + datestring + "' AND EndTime >= '" + datestring + "' ";
            MsqlConnection mc = new MsqlConnection();
            mc.open();
            DataTable dataHolder = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
            da.Fill(dataHolder);
            mc.close();

            string factor = null;
            int factorID = 0;

            for (int i = 0; i < dataHolder.Rows.Count; i++)
            {
                string ShiftMethodName = null;
                int FactorID = 0, ShiftMethodsID = 0;
                bool tick = false;
                if (!DBNull.Value.Equals(dataHolder.Rows[i][4]))
                {
                    ShiftMethodsID = Convert.ToInt32(dataHolder.Rows[i][3]);
                    FactorID = Convert.ToInt32(dataHolder.Rows[i][4]);
                    ShiftMethodName = "Plant";
                    tick = DoesThisMachineBelongToThisFactor(ShiftMethodName, FactorID, machineId);
                }
                else if (!DBNull.Value.Equals(dataHolder.Rows[i][5]))
                {
                    ShiftMethodsID = Convert.ToInt32(dataHolder.Rows[i][3]);
                    FactorID = Convert.ToInt32(dataHolder.Rows[i][5]);
                    ShiftMethodName = "Shop";
                    tick = DoesThisMachineBelongToThisFactor(ShiftMethodName, FactorID, machineId);
                }
                else if (!DBNull.Value.Equals(dataHolder.Rows[i][6]))
                {
                    ShiftMethodsID = Convert.ToInt32(dataHolder.Rows[i][3]);
                    FactorID = Convert.ToInt32(dataHolder.Rows[i][6]);
                    ShiftMethodName = "Cell";
                    tick = DoesThisMachineBelongToThisFactor(ShiftMethodName, FactorID, machineId);
                }
                else if (!DBNull.Value.Equals(dataHolder.Rows[i][7]))
                {
                    ShiftMethodsID = Convert.ToInt32(dataHolder.Rows[i][3]);
                    FactorID = Convert.ToInt32(dataHolder.Rows[i][7]);
                    ShiftMethodName = "Machine";
                    tick = DoesThisMachineBelongToThisFactor(ShiftMethodName, FactorID, machineId);
                }
            }

            return shift;
        }

        bool DoesThisMachineBelongToThisFactor(string FactorName, int FactorID, int machineID)
        {
            bool tick = false;
            String sql = null;
            if (FactorName == "Plant")
            {
                sql = "SELECT * FROM tblmachinedetails WHERE MachineID = " + machineID + " AND PlantID = " + FactorID;
            }
            else if (FactorName == "Shop")
            {
                sql = "SELECT * FROM tblmachinedetails WHERE MachineID = " + machineID + " AND ShopID = " + FactorID;
            }
            else if (FactorName == "Cell")
            {
                sql = "SELECT * FROM tblmachinedetails WHERE MachineID = " + machineID + " AND CellID = " + FactorID;
            }
            else if (FactorName == "Machine")
            {
                sql = "SELECT * FROM tblmachinedetails WHERE MachineID = " + machineID + " AND MachineID = " + FactorID;
            }
            MsqlConnection mc = new MsqlConnection();
            mc.open();
            DataTable dataHolder = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
            da.Fill(dataHolder);
            mc.close();
            if (dataHolder.Rows.Count > 0)
            {
                tick = true;
            }
            return tick;
        }

    }

    public class ShiftDetails
    {
        mazakdaqEntities db = new mazakdaqEntities();

        public bool IsThisPlanInAction(int id)
        {
            bool status = false;
            DataTable dataHolder = new DataTable();

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

            MsqlConnection mc = new MsqlConnection();
            mc.open();
            String sql = "SELECT * FROM tblShiftPlanner WHERE StartDate <='" + CorrectedDate + "' AND EndDate >='" + CorrectedDate + "'AND ShiftPlannerID = " + id + " ORDER BY ShiftPlannerID ASC";
            MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
            da.Fill(dataHolder);
            mc.close();

            if (dataHolder.Rows.Count > 0)
            {
                status = true;
            }
            return status;
        }

        public bool IsThisShiftMethodIsInActionOrEnded(int id)
        {
            bool status = true;
            DataTable dataHolder = new DataTable();

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

            MsqlConnection mc = new MsqlConnection();
            mc.open();
            String sql = "SELECT * FROM tblShiftPlanner WHERE (( StartDate <='" + CorrectedDate + "' AND EndDate >='" + CorrectedDate + "') OR ( EndDate <'" + CorrectedDate + "' )) AND ShiftMethodID = " + id + " ORDER BY ShiftPlannerID ASC";
            MySqlDataAdapter da = new MySqlDataAdapter(sql, mc.msqlConnection);
            da.Fill(dataHolder);
            mc.close();

            if (dataHolder.Rows.Count == 0)
            {
                status = false;
            }
            return status;
        }

    }
}