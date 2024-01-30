using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BIIT_Parent_Appointment.Models
{
    public class DisciplinaryModel
    {
        public int id { get; set; }
        public string regNo { get; set; }
        public string @class { get; set; }
        public int semester { get; set; }
        public string section { get; set; }
        public string actions { get; set; }
    }
}