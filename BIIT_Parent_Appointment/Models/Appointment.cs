using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BIIT_Parent_Appointment.Models
{
    public partial class Appointment
    {
        public int mid { get; set; }
        public int tsid { get; set; }
        public string regNo { get; set; }
        public string reason { get; set; }
        public string status { get; set; }
        public string adminId { get; set; }
        public string parentId { get; set; }
        public string date { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string referedTo { get; set; }
        public Nullable<bool> studentMeeting { get; set; }
    }
}