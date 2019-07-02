//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Tata.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class tblplant
    {
        public tblplant()
        {
            this.tbl_autoreportsetting = new HashSet<tbl_autoreportsetting>();
            this.tblcells = new HashSet<tblcell>();
            this.tblemailescalations = new HashSet<tblemailescalation>();
            this.tblmachinedetails = new HashSet<tblmachinedetail>();
            this.tblmachinedetailsnews = new HashSet<tblmachinedetailsnew>();
            this.tblmultipleworkorders = new HashSet<tblmultipleworkorder>();
            this.tblshops = new HashSet<tblshop>();
            this.tblshiftplanners = new HashSet<tblshiftplanner>();
        }
    
        public int PlantID { get; set; }
        public string PlantName { get; set; }
        public string PlantDesc { get; set; }
        public int IsDeleted { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public Nullable<int> ModifiedBy { get; set; }
    
        public virtual ICollection<tbl_autoreportsetting> tbl_autoreportsetting { get; set; }
        public virtual ICollection<tblcell> tblcells { get; set; }
        public virtual ICollection<tblemailescalation> tblemailescalations { get; set; }
        public virtual ICollection<tblmachinedetail> tblmachinedetails { get; set; }
        public virtual ICollection<tblmachinedetailsnew> tblmachinedetailsnews { get; set; }
        public virtual ICollection<tblmultipleworkorder> tblmultipleworkorders { get; set; }
        public virtual ICollection<tblshop> tblshops { get; set; }
        public virtual ICollection<tblshiftplanner> tblshiftplanners { get; set; }
    }
}
