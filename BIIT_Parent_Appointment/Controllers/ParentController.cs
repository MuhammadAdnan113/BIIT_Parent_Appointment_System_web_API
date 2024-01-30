using BIIT_Parent_Appointment.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web.Http;

namespace BIIT_Parent_Appointment.Controllers
{
    public class ParentController : ApiController
    {
        BIIT_Parent_AppointmentEntities db = new BIIT_Parent_AppointmentEntities();

        // CREATE MEETING ===============================================

        [HttpPost]
        public HttpResponseMessage CreateMeeting(Meeting m)
        {
            try
            {
                var meetings = db.Meetings.Where(a=>a.adminId==m.adminId && a.tsid==m.tsid && a.status=="Pending"&& a.date==m.date).ToList<Meeting>();
                if (meetings.Count > 1)
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Slot already Filled");
                }
                else
                {
                    if(meetings.Count>=1)
                    {
                        TimeSlot freeslot = null;
                        var admins=db.UserLogins.Where(a=>a.role=="Admin" && a.cnic!=m.adminId).ToList<UserLogin>();
                        var ts = db.TimeSlots.Where(t => t.adminId == m.adminId).FirstOrDefault();
                        foreach (var v in admins)
                        {
                            var ts1 = db.TimeSlots.Where(t => t.adminId == v.cnic && t.availability == true && t.startTime == ts.startTime).FirstOrDefault();
                            var leave=db.Leaves.Where(l=>l.tsid==ts1.tsid&&l.date==m.date).FirstOrDefault();
                            var meeting = db.Meetings.Where(x => x.date == m.date && x.tsid == ts1.tsid).FirstOrDefault();
                            if(leave==null&&meeting==null)
                            {
                                freeslot = ts1;
                                break;
                            }
                        }
                        if(freeslot!=null)
                        {
                            string admincnic = m.adminId;
                            var parent = db.Students.Where(s => s.regNo == m.regNo).FirstOrDefault();
                            var timeslot = db.TimeSlots.Where(t => t.tsid == freeslot.tsid).FirstOrDefault<TimeSlot>();
                            var admin = db.Admins.Where(ad => ad.cnic == admincnic).FirstOrDefault<Admin>();
                            m.parentId = parent.parentCNIC;
                            m.adminId = admincnic;
                            db.Meetings.Add(m);
                            db.SaveChanges();
                            return Request.CreateResponse(HttpStatusCode.OK, "Meeting Created on Date " + m.date);
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.NotModified, "Slot Already Filled Do You want to Wait?");
                        }
                        //WaitingList waitingList = new WaitingList
                        //{
                        //    tsId=m.tsid,
                        //    regNo=m.regNo,
                        //    reason=m.reason,
                        //    status="Waiting",
                        //    parentId=m.parentId,
                        //    adminId=m.adminId,
                        //    date=m.date,
                        //};
                        //db.WaitingLists.Add(waitingList);
                        //db.SaveChanges();
                        //return Request.CreateResponse(HttpStatusCode.NotModified, "Slot Already Filled Do You want to Wait?");
                    }
                    else
                    {
                        string admincnic = m.adminId;
                        var parent = db.Students.Where(s => s.regNo == m.regNo).FirstOrDefault();
                        var timeslot = db.TimeSlots.Where(ts => ts.tsid == m.tsid).FirstOrDefault<TimeSlot>();
                        var admin = db.Admins.Where(ad => ad.cnic == admincnic).FirstOrDefault<Admin>();
                        m.parentId = parent.parentCNIC;
                        m.adminId = admincnic;
                        db.Meetings.Add(m);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Meeting Created on Date " + m.date);
                    }
                    
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end CreateMeeting

        // Insert from Waiting into MEETING =================================================

        [HttpGet]
        public HttpResponseMessage AcceptMeeting(int id)
        {// Inserting From Waiting List into Meeting after Admin Call and Parent Accept
            try
            {   //  /api/Parent/AcceptMeeting?id=13
                var result = db.WaitingLists.Where(l => l.id == id).FirstOrDefault();

                string nowdate = DateTime.Now.ToString("d/M/yyyy");
                DateTime newdate = DateTime.ParseExact(nowdate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = newdate.DayOfWeek.ToString();

                if (result!=null)
                {
                    var ts = db.TimeSlots.Where(t => t.tsid == result.tsId).FirstOrDefault();
                    var newts=db.TimeSlots.Where(n => n.adminId==result.adminId&&n.startTime==ts.startTime&&n.availability==true).FirstOrDefault();
                    Meeting m = new Meeting
                    {
                        tsid = newts.tsid,
                        date = result.date,
                        regNo = result.regNo,
                        status="Pending",
                        reason=result.reason,
                        adminId=result.adminId,
                        parentId=result.parentId,
                        referedTo="N/A",
                    };
                    db.Meetings.Add(m);
                    db.WaitingLists.Remove(result); // Removing Record from Waiting List
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Now You can Visit Admin for Appointment");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Waiting Record Found");
                }
                
                

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end requestmeeting




        // GetHistory ===============================================

        //[HttpGet]
        //public HttpResponseMessage GetHistory(string id)
        //{
        //    try
        //    {
        //        var history = db.Histories.Where(h => h.parentId == id).ToList<History>();
        //        if (history != null && history.Count != 0)
        //        {
        //            history.Reverse();
        //            return Request.CreateResponse(HttpStatusCode.OK, history);
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "No Meeting Exist");
        //        }
        //    }
        //    catch (Exception exp)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
        //    }
        //}// end GetHistory

        [HttpGet]
        public HttpResponseMessage GetHistory(string id)
        {
            try
            {   //  /api/Parent/GetHistory?id=3240277931065
                List<HistoryModel> historylist = new List<HistoryModel>();
                var history = db.Histories.Where(h => h.parentId == id).ToList<History>();
                if (history != null && history.Count != 0)
                {

                    foreach (var v in history)
                    {
                        Feedback feedback = new Feedback();
                        var rating = db.Feedbacks.Where(r => r.hid == v.hid).FirstOrDefault();
                        if (rating == null)
                        {
                            feedback.attentive = -1;
                            feedback.polite = -1;
                            feedback.rudness = -1;
                        }
                        else
                        {
                            feedback = rating;
                        }
                        var parent = db.Parents.Where(p => p.cnic == v.parentId).FirstOrDefault();
                        var student = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                        HistoryModel h = new HistoryModel
                        {
                            hid = v.hid,
                            regNo = v.regNo,
                            date = v.date,
                            startTime = v.startTime,
                            endTime = v.endTime,
                            status = v.status,
                            reason = v.reason,
                            adminId = v.adminId,
                            parentId = v.parentId,
                            referedTo = v.referedTo,
                            suggestion = v.suggestion,
                            adminFeedback = v.adminFeedback,
                            parentFullName = parent.firstName + " " + parent.lastName,
                            studentFullName = student.firstName + " " + parent.lastName,
                            parentRating = (double)v.parentRating,
                            attentive = (double)feedback.attentive,
                            polite = (double)feedback.polite,
                            rudness = (double)feedback.rudness,
                        };
                        historylist.Add(h);
                    }
                    historylist.Reverse();
                    return Request.CreateResponse(HttpStatusCode.OK, historylist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Record Not Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end GetHistory

        // RESHEDULE MEETING =======================================================


        // UPDATE MEETING STATUS ===============================================

        [HttpGet]
        public HttpResponseMessage UpdateMeetingStatus(int mid, string status)
        {
            try
            {   // status will be Accept
                var result = db.Meetings.Where(b => b.mid == mid).FirstOrDefault();
                if (result != null)
                {
                    result.status = status;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Meeting Accepted");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "Meeting Not Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end UpdateMeetingStatus


        //== PARENT FEEDBACK when Appointment is Done ========

        [HttpGet]
        public HttpResponseMessage Feedback(int hid, float attentive, float polite, float rudness, string suggestion)
        {
            try
            {   //  /api/Parent/Feedback?hid=6&attentive=4.0&polite=4.0&rudness=1.0&suggestion=good
                var result = db.Histories.Where(b => b.hid == hid).FirstOrDefault();
                if (result != null)
                {
                    var chkFeedback = db.Feedbacks.Where(f => f.hid == hid).FirstOrDefault();
                    if(chkFeedback==null)
                    {
                        Feedback f = new Feedback
                        {
                            hid = hid,
                            adminId = result.adminId,
                            attentive = attentive,
                            polite = polite,
                            rudness = rudness,
                        };
                        result.suggestion = suggestion;
                        db.Feedbacks.Add(f);

                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Feedback saved");
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Feedback feedback already saved");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Record Not Exist");
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end Feedback

        //====== Displaying Rating (Feedback) After Parent Rating =============
        [HttpGet]
        public HttpResponseMessage GetRating(int hid)
        {
            try
            {   //  /api/Parent/GetRating?hid=6
                var result = db.Feedbacks.Where(b => b.hid == hid).FirstOrDefault();
                if (result != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Rating Record not found");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetRating


        // COUNT PENDING NOTIFICATION ==================================================

        [HttpGet]
        public HttpResponseMessage CountNotification(string id)
        {
            try
            {
                var notification = db.Meetings.Where(b => b.parentId == id);
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


        [HttpGet]
        public HttpResponseMessage GetNotification(string pcnic)
        {//  /api/Parent/GetNotification?pcnic=3240277931065
            try
            {
                var meeting = db.Meetings.Where(m => m.parentId== pcnic).ToList();
                if (meeting != null && meeting.Count !=0)
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


        // ==============================================================


        [HttpGet]
        public HttpResponseMessage GetTimeSlot(string date)
        {
            try
            {
               // /api/Parent/GetTimeSlot?date=1/2/2023
                var admins = db.UserLogins.Where(d => d.role == "Admin").ToArray<UserLogin>();
                List<TimeSlot> timeSlots = new List<TimeSlot>();

                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(date, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();

                foreach (var v in admins)
                {
                    //var schedule1=db.TimeSlots.SqlQuery("select * from TimeSlot where date='"+ date + "' and availability=true").ToArray<TimeSlot>();
                    var schedule1 = db.TimeSlots.Where(t=>t.availability==true && t.day== dayofweek && t.adminId==v.cnic).ToList<TimeSlot>();
                    foreach (var s in schedule1)
                    {
                        var leave = db.Leaves.Where(l => l.tsid == s.tsid && l.date == date && l.adminId == v.cnic).FirstOrDefault();
                        var meeting=db.Meetings.Where(m => m.date == date && m.tsid == s.tsid).ToList<Meeting>();
                        var waiting = db.WaitingLists.Where(w => w.date == date && w.tsId == s.tsid).ToList<WaitingList>();
                        if (leave == null&& meeting.Count ==0 || leave == null&&waiting.Count==0&& meeting.Count <= 1)
                        {

                            timeSlots.Add(s);
                        }
                    }

                }
                if (timeSlots != null && timeSlots.Count != 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, timeSlots);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Time Slot available for appointment");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end GetTimeSlot

        //==== GetTimeSlot old method

        //[HttpGet]
        //public HttpResponseMessage GetTimeSlot(string date)
        //{
        //    try
        //    {
        //        // /api/Parent/GetTimeSlot?date=23/9/2022
        //        var admins = db.UserLogins.Where(d => d.role == "Admin").ToArray<UserLogin>();
        //        List<TimeSlot> timeSlots = new List<TimeSlot>();

        //        // converting string Date into DateTime data type
        //        DateTime todaydate = DateTime.ParseExact(date, "d/M/yyyy", CultureInfo.CurrentCulture);
        //        // Checking Day of week Like  Saturday
        //        string dayofweek = todaydate.DayOfWeek.ToString();

        //        foreach (var v in admins)
        //        {
        //            //var schedule1=db.TimeSlots.SqlQuery("select * from TimeSlot where date='"+ date + "' and availability=true").ToArray<TimeSlot>();
        //            var schedule1 = db.TimeSlots.Where(t => t.availability == true && t.day == dayofweek && t.adminId == v.cnic).ToList<TimeSlot>();
        //            foreach (var s in schedule1)
        //            {
        //                var leave = db.Leaves.Where(l => l.tsid == s.tsid && l.date == date && l.adminId == v.cnic).FirstOrDefault();
        //                var meeting = db.Meetings.Where(m => m.date == date && m.tsid == s.tsid).FirstOrDefault();
        //                if (meeting == null && leave == null)
        //                {

        //                    timeSlots.Add(s);
        //                }
        //            }

        //        }
        //        if (timeSlots != null && timeSlots.Count != 0)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.OK, timeSlots);
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "No Time Slot available for appointment");
        //        }
        //    }
        //    catch (Exception exp)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
        //    }
        //}// end GetTimeSlot
        //==================

        [HttpGet]
        public HttpResponseMessage GetStudent(String pid)
        {
            try
            {
                var students = db.Students.Where(s=>s.parentCNIC==pid).ToArray<Student>();
                if (students != null && students.Length !=0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, students);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Student Exist for Meeting");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end GetSchedule

        [HttpGet]
        public HttpResponseMessage GetWaitingList(String id)
        {
            try
            {
                var requests = db.WaitingLists.Where(r => r.parentId == id).ToList();
                if (requests != null && requests.Count != 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, requests);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Waiting List is Empty");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetWaitingList

        [HttpPost]
        public HttpResponseMessage InsertWaitingList(WaitingList waiting)
        {
            try
            {
                var result = db.WaitingLists.Select(r => r).ToList<WaitingList>();
                if (result.Count == 5)
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Waiting List Limit Reached");
                }
                else
                {
                    var meeting = db.Meetings.Where(m => m.tsid == waiting.tsId).FirstOrDefault();
                    var timeslot = db.TimeSlots.Where(ts => ts.tsid == meeting.tsid).FirstOrDefault();
                    WaitingList wait = new WaitingList
                    {
                        tsId = meeting.tsid,
                        regNo = meeting.regNo,
                        reason = meeting.reason,
                        date = meeting.date,
                        adminId = meeting.adminId,
                        parentId = meeting.parentId,
                        status = "Waiting"
                    };
                    db.WaitingLists.Add(wait);
                    db.Meetings.Remove(meeting);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Appointment is in Waiting List");
                }


            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end InsertWaitingList

        //== Put into Waiting List in case Parent Want to wait ====
        [HttpPost]
        public HttpResponseMessage PutToWaitingList(WaitingList waiting)
        {
            try
            {
                var result = db.WaitingLists.Select(r => r).ToList<WaitingList>();
                if (result.Count == 5)
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Waiting List Limit Reached");
                }
                else
                {
                    waiting.status = "Waiting";
                    db.WaitingLists.Add(waiting);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Appointment is in Waiting List");
                }


            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end InsertWaitingList

        [HttpGet]
        public HttpResponseMessage UpdateMeeting(int mid,int tsid,string date)
        {
            try
            {
                var meeting = db.Meetings.Where(i => i.mid == mid).FirstOrDefault();
                if (meeting != null)
                {
                    var ts = db.TimeSlots.Where(t => t.tsid == tsid).FirstOrDefault();
                    meeting.tsid = tsid;
                    meeting.date = date;
                    meeting.adminId = ts.adminId;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Appointment Updated");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Appointment Found");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end DeleteMeeting

        [HttpGet]
        public HttpResponseMessage DeleteMeeting(int mid)
        {
            try
            {
                var meeting = db.Meetings.Where(i => i.mid==mid).FirstOrDefault();
                if (meeting!=null)
                {
                    db.Meetings.Remove(meeting);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Appointment Deleted");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Appointment Found");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end DeleteMeeting

        [HttpGet]
        public HttpResponseMessage GetIssueList()
        {
            try
            {
                var issueslist = db.IssuesLists.Select(i=>i.issue).ToList<string>();
                if (issueslist.Count != 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, issueslist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Issues List is Empty");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetIssueList



    }// end Controller
}
