using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BIIT_Parent_Appointment.Models
{
    public class HistoryModel
    {
        public int hid { get; set; }
        public string regNo { get; set; }
        public string date { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string status { get; set; }
        public string reason { get; set; }
        public string adminId { get; set; }
        public string parentId { get; set; }
        public string referedTo { get; set; }
        public string suggestion { get; set; }
        public string adminFeedback { get; set; }
        public string parentFullName { get; set; }
        public string studentFullName { get; set; }
        public double parentRating { get; set; }
        public Nullable<double> attentive { get; set; }
        public Nullable<double> polite { get; set; }
        public Nullable<double> rudness { get; set; }
    }
}