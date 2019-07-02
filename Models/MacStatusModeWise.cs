using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tata.Models
{
    public class MacStatusModeAndStartTimeWise
    {
        public int MacID { get; set; }
        public string Color { get; set; }
        public int Duration { get; set; }
    }
    public class MacStatusModeWiseOverAll
    {
        public int MacID { get; set; }
        public string Color { get; set; }
        public int Duration { get; set; }
    }

    public class MacStatus
    {
        public string MacInvNo { get; set; }
        public string DisplayName { get; set; }
        public string ShopName { get; set; }
        List<MacStatusModeAndStartTimeWise> MSMST = new List<MacStatusModeAndStartTimeWise>();
    }

}