using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BIIT_Parent_Appointment.Models
{
    public class AdminDetailModel
    {
        public string cnic { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string role { get; set; }
        public double rudness { get; set; }
        public double attentive { get; set; }
        public double polite { get; set; }
        public int noOfAppointments { get; set; }
    }
}