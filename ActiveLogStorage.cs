using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tata.Models;

namespace TATA
{
    public class ActiveLogStorage
    {
        private mazakdaqEntities db = new mazakdaqEntities();
        public void SaveActiveLog(string View, string Controller, String UserName, int UserID, string CompleteModificationDetails)
        {
            TimeSpan Optime = DateTime.Now.TimeOfDay;
            DateTime Opdate = DateTime.Now.Date;
            DateTime Opdatetime = DateTime.Now;
            string IP_Address = null;
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    IP_Address = addresses[0];
                }
            }
            //Use this for client IP Address
            IP_Address = context.Request.ServerVariables["REMOTE_ADDR"];
            string clientMachineName;
            //Taking Client System Name
            clientMachineName = (System.Net.Dns.GetHostEntry(context.Request.ServerVariables["remote_addr"]).HostName);
            //db.createActiveLog(UserName, UserID, IP_Address, clientMachineName, CompleteModificationDetails, Controller, View, Optime, Opdate, Opdatetime);
        }
    }
}