using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BIIT_Parent_Appointment.Models
{
    public class Times
    {
        public String start { get; set; }
        public String end { get; set; }
        public Times(string start,string end)
        {
            this.start = start;
            this.end = end;
        }

    }
}