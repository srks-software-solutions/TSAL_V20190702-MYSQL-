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
    
    public partial class code_master
    {
        public int CodeID { get; set; }
        public Nullable<int> Code { get; set; }
        public string MCode { get; set; }
        public string CodeDescription { get; set; }
        public string CodeType { get; set; }
        public Nullable<System.DateTime> InsertedOn { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<int> IsDeleted { get; set; }
    }
}
