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
    
    public partial class tblmachinedetail
    {
        public tblmachinedetail()
        {
            this.tbl_autoreportsetting = new HashSet<tbl_autoreportsetting>();
            this.tbldailyprodstatus = new HashSet<tbldailyprodstatu>();
            this.tblemailescalations = new HashSet<tblemailescalation>();
            this.tblhmiscreens = new HashSet<tblhmiscreen>();
            this.tbllivedailyprodstatus = new HashSet<tbllivedailyprodstatu>();
            this.tbllivehmiscreens = new HashSet<tbllivehmiscreen>();
            this.tbllivehmiscreenreps = new HashSet<tbllivehmiscreenrep>();
            this.tbllivemodes = new HashSet<tbllivemode>();
            this.tbllivemodedbs = new HashSet<tbllivemodedb>();
            this.tblmachineallocations = new HashSet<tblmachineallocation>();
            this.tblmimics = new HashSet<tblmimic>();
            this.tblshiftplanners = new HashSet<tblshiftplanner>();
            this.tblshiftdetails_machinewise = new HashSet<tblshiftdetails_machinewise>();
            this.tblmodes = new HashSet<tblmode>();
            this.tblusers = new HashSet<tbluser>();
            this.tblmultipleworkorders = new HashSet<tblmultipleworkorder>();
            this.tblpartwiseworkcenters = new HashSet<tblpartwiseworkcenter>();
        }
    
        public int MachineID { get; set; }
        public string MachineInvNo { get; set; }
        public string IPAddress { get; set; }
        public string MachineType { get; set; }
        public string ControllerType { get; set; }
        public string InsertedOn { get; set; }
        public int InsertedBy { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public Nullable<int> ModifiedBy { get; set; }
        public Nullable<int> IsDeleted { get; set; }
        public string MachineModel { get; set; }
        public string MachineMake { get; set; }
        public string ModelType { get; set; }
        public string MachineDispName { get; set; }
        public int IsParameters { get; set; }
        public string ShopNo { get; set; }
        public Nullable<int> IsPCB { get; set; }
        public Nullable<int> IsLevel { get; set; }
        public Nullable<int> PlantID { get; set; }
        public Nullable<int> ShopID { get; set; }
        public Nullable<int> CellID { get; set; }
        public string DeletedDate { get; set; }
        public int IsNormalWC { get; set; }
        public Nullable<int> ManualWCID { get; set; }
    
        public virtual ICollection<tbl_autoreportsetting> tbl_autoreportsetting { get; set; }
        public virtual tblcell tblcell { get; set; }
        public virtual ICollection<tbldailyprodstatu> tbldailyprodstatus { get; set; }
        public virtual ICollection<tblemailescalation> tblemailescalations { get; set; }
        public virtual ICollection<tblhmiscreen> tblhmiscreens { get; set; }
        public virtual ICollection<tbllivedailyprodstatu> tbllivedailyprodstatus { get; set; }
        public virtual ICollection<tbllivehmiscreen> tbllivehmiscreens { get; set; }
        public virtual ICollection<tbllivehmiscreenrep> tbllivehmiscreenreps { get; set; }
        public virtual ICollection<tbllivemode> tbllivemodes { get; set; }
        public virtual ICollection<tbllivemodedb> tbllivemodedbs { get; set; }
        public virtual ICollection<tblmachineallocation> tblmachineallocations { get; set; }
        public virtual ICollection<tblmimic> tblmimics { get; set; }
        public virtual ICollection<tblshiftplanner> tblshiftplanners { get; set; }
        public virtual ICollection<tblshiftdetails_machinewise> tblshiftdetails_machinewise { get; set; }
        public virtual ICollection<tblmode> tblmodes { get; set; }
        public virtual ICollection<tbluser> tblusers { get; set; }
        public virtual tblplant tblplant { get; set; }
        public virtual tblshop tblshop { get; set; }
        public virtual ICollection<tblmultipleworkorder> tblmultipleworkorders { get; set; }
        public virtual ICollection<tblpartwiseworkcenter> tblpartwiseworkcenters { get; set; }
    }
}
