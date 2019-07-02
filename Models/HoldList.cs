using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tata.Models
{
    public class HoldList
    {
        public List<tblmanuallossofentry> HoldListDetailsWO { get; set; }
        //public List<tbllossofentry> HoldListDetailsWC { get; set; }
        public List<tbllivelossofentry> HoldListDetailsWC { get; set; }
    }
}