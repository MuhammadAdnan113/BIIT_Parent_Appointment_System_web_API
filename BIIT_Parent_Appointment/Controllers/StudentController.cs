using BIIT_Parent_Appointment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BIIT_Parent_Appointment.Controllers
{
    public class StudentController : ApiController
    {
        BIIT_Parent_AppointmentEntities db = new BIIT_Parent_AppointmentEntities();

        [HttpGet]
        public HttpResponseMessage CGPA(string regno)
        {
            try
            {
                var std = db.CGPAs.Where(a=>a.regNo==regno).FirstOrDefault();
                if (std != null)
                {
                    var std1 = db.Students.Where(s => s.regNo == std.regNo).FirstOrDefault();
                    CGPAModel cgpa = new CGPAModel();
                    cgpa.id = std.id;
                    cgpa.regNo = std.regNo;
                    cgpa.@class = std1.@class;
                    cgpa.semester = (int)std1.semester;
                    cgpa.section = std1.section;
                    cgpa.cgpa1 = std.cgpa1;
                    return Request.CreateResponse(HttpStatusCode.OK, cgpa);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "CGPA Not Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end CGPA

        [HttpGet]
        public HttpResponseMessage getAttendance(string regno)
        {
            try
            {
                List<AttendanceModel> attlist = new List<AttendanceModel>();
                List<Attendance> slist = db.Attendances.Where(s => s.regNo == regno).ToList<Attendance>();
                if (slist != null)
                {
                    foreach (var v in slist)
                    {
                        var std = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                        AttendanceModel a = new AttendanceModel
                        {
                            sid = v.sid,
                            regNo = v.regNo,
                            @class = std.@class,
                            semester = (int)std.semester,
                            section = std.section,
                            subject = v.subject,
                            percentage = v.percentage,
                        };
                        attlist.Add(a);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, attlist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Attendance Record Not Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end getAttendance

        //=== Task Method=======
        [HttpGet]
        public HttpResponseMessage GetStudentNotification(string regno)
        {//  /api/Student/GetStudentNotification?regno=2021-ARID-1761
            try
            {
                var meeting = db.Meetings.Where(m => m.regNo == regno && m.studentMeeting==true).ToList();
                if (meeting != null && meeting.Count != 0)
                {
                    List<Appointment> applist = new List<Appointment>();
                    for (int i = 0; i < meeting.Count; i++)
                    {
                        int tsid1 = (int)meeting[i].tsid;
                        var timeslot = db.TimeSlots.Where(t => t.tsid == tsid1).FirstOrDefault();

                        Appointment app = new Appointment();
                        app.mid = meeting[i].mid;
                        app.tsid = (int)meeting[i].tsid;
                        app.regNo = meeting[i].regNo;
                        app.reason = meeting[i].reason;
                        app.date = meeting[i].date;
                        app.status = meeting[i].status;
                        app.startTime = timeslot.startTime;
                        app.endTime = timeslot.endTime;
                        app.adminId = meeting[i].adminId;
                        app.parentId = meeting[i].parentId;
                        app.referedTo = meeting[i].referedTo;
                        app.studentMeeting = meeting[i].studentMeeting;
                        applist.Add(app);
                    }
                    applist.Reverse();
                    return Request.CreateResponse(HttpStatusCode.OK, applist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Meeting Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetNotification

        [HttpGet]
        public HttpResponseMessage CountNotification(string id)
        {
            try
            {
                var notification = db.Meetings.Where(b => b.regNo == id && b.studentMeeting==true);
                if (notification != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, notification.Count());
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Meeting Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end CountNotification
    }
}
