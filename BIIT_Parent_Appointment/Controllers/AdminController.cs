using BIIT_Parent_Appointment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web.Http;
using System.Runtime.Serialization;
using System.Globalization;

namespace BIIT_Parent_Appointment.Controllers
{
    public class AdminController : ApiController
    {
        BIIT_Parent_AppointmentEntities db = new BIIT_Parent_AppointmentEntities();

        List<TimeSlot> addedtimeslot = new List<TimeSlot>();

        [HttpGet]
        public HttpResponseMessage CustomMeeting(string regno,string date,int tsid,string reason)
        {
            try
            {   //  /api/admin/CustomMeeting?regno=2019-Arid-0082&adminid=3240277921986&date=31/1/2023&tsid=1&reason=fee
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(date, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                if(dayofweek=="Saturday" || dayofweek== "Sunday")
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Possible due to Holiday");
                }
                var ts = db.TimeSlots.Where(a => a.tsid == tsid).FirstOrDefault();
                var std = db.Students.Where(s => s.regNo == regno).FirstOrDefault();
                if(std==null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Student Not Exist");
                }
                var parent = db.Students.Where(s => s.regNo == regno).FirstOrDefault();
                if (ts != null)
                {
                    if (ts.availability == true)
                    {
                        Meeting m = new Meeting
                        {
                            tsid = ts.tsid,
                            date=date,
                            regNo = regno,
                            reason = reason,
                            status = "Pending",
                            adminId = ts.adminId,
                            parentId = parent.parentCNIC,
                            referedTo = "N/A",
                        };
                        db.Meetings.Add(m);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Appointment Created on Date = " + date);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Accepted due to busy Slot");
                    }
                    
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Free TimeSlot Not Found");
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end CustomMeeting

        [HttpPost]
        public HttpResponseMessage DeleteMeeting(int meetingid)
        {
            try
            {
                var result = db.Meetings.Where(b => b.mid == meetingid).FirstOrDefault();
                if (result != null)
                {
                    db.Meetings.Remove(result);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Meeting Deleted");
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
        } // end DeleteMeeting

        [HttpGet]
        public HttpResponseMessage GetPendingMeeting(string adminid)
        {
            try
            {   //  /api/admin/GetPendingMeeting?adminid=3240277921986
                //var user=from l in db.Logins select l where user_name=username and password = pass;
                var timeslot = db.TimeSlots.Where(t=>t.adminId== adminid).FirstOrDefault();
                List<Meeting> m = db.Meetings.Where(mt=>mt.adminId== adminid && mt.status=="Pending").ToList<Meeting>();
                if (m != null && timeslot != null)
                {
                    List<Appointment> appointment= new List<Appointment>();
                    foreach(var v in m)
                    {
                        Appointment a = new Appointment();
                        a.mid =v.mid;
                        a.regNo =v.regNo;
                        a.reason =v.reason;
                        a.date = v.date;
                        a.startTime = timeslot.startTime;
                        a.endTime = timeslot.endTime;
                        a.parentId = v.parentId;
                        a.adminId = v.adminId;
                        a.status = v.status;
                        a.referedTo = v.referedTo;

                        appointment.Add(a);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, appointment);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Meeting Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end GetPendingMeeting


        //[HttpGet]
        //public HttpResponseMessage GetHistory(string adminid)
        //{
        //    try
        //    {   //    /api/admin/GetHistory?adminid=3240277921986
        //        //var user=from l in db.Logins select l where user_name=username and password = pass;
        //        var result = db.Histories.Where(h => h.adminId == adminid).ToList<History>();
        //        if (result != null && result.Count != 0)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.OK, result);
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
        public HttpResponseMessage GetHistory(string adminid)
        {
            try
            {   //  /api/Admin/GetHistory?adminid=3240277921986
                List<HistoryModel> historylist = new List<HistoryModel>();
                var history = db.Histories.Where(h => h.adminId == adminid).ToList<History>();
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

        //=== Parent History on basis of Parent ID ======
        [HttpGet]
        public HttpResponseMessage GetParentHistory(string parentid)
        {
            try
            {   //  /api/Admin/GetParentHistory?parentid=3240277931065
                var history = db.Histories.Where(h => h.parentId == parentid).ToList<History>();
                if (history.Count != 0)
                {
                    history.Reverse();
                    return Request.CreateResponse(HttpStatusCode.OK, history);
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
        }// end GetParentHistory

        // RESHEDULE MEETING =======================================================

        [HttpGet]
        public HttpResponseMessage ReSheduleMeeting(int mid, string date)
        {
            try
            {   //  /api/admin/ReSheduleMeeting?mid=1&date=20/1/2023
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(date, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                var meeting = db.Meetings.Where(w => w.mid == mid).FirstOrDefault();
                if (meeting != null)
                {
                    var timeslot = db.TimeSlots.Where(t => t.day == dayofweek&&t.availability==true).FirstOrDefault();
                    meeting.tsid = timeslot.tsid;
                    meeting.date = date;
                    meeting.status = "Reschedualed";
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Meeting Resheduled on Date: " + date);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Meeting Not Exist");
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end ReSheduleMeeting

        //=== Readjust Appointments with other admin OR date in case of admin Leave =====
        [HttpGet]
        public HttpResponseMessage ReAdjustMeeting(string adminid,string date)
        {
            try
            {
                // /api/Admin/ReAdjustMeeting?adminid=3240277921986&date=5/2/2023

                List<TimeSlot> newtslist = new List<TimeSlot>(); // TimeSlot for new Adjustments
                var admin = db.UserLogins.Where(b => b.role == "Admin" && b.cnic != adminid).ToArray<UserLogin>();
                if (admin != null)
                {
                    string newdate = date;
                    string[] datearray = newdate.Split('/');
                    int day = int.Parse(datearray[0]);
                    int month = int.Parse(datearray[1]);
                    int year = int.Parse(datearray[2]);
                    //======= Date Split End
                    // converting string Date into DateTime data type
                    DateTime todaydate = DateTime.ParseExact(newdate, "d/M/yyyy", CultureInfo.CurrentCulture);
                    // Checking Day of week Like  Saturday
                    string dayofweek = todaydate.DayOfWeek.ToString();
                    string today = dayofweek;
                    // Appointments to Adjust
                    var meetinglist = db.Meetings.Where(w => w.adminId == adminid && w.date == date).ToList<Meeting>();
                    if (meetinglist.Count != 0)
                    {
                        int adjustcount = 0;
                        //how many days to get timeslot, if day is saturday or sunday or No timeslots availablethen it will increase
                        int dayscount = 1;
                        int index = 0;
                    nextDate://repeating code if it is holiday or higher no of appointments then timeslot of day
                        for (int i = index; index <= dayscount; index++)
                        {

                            newdate = day + "/" + month + "/" + year;//getting everyday date
                            todaydate = DateTime.ParseExact(newdate, "d/M/yyyy", CultureInfo.CurrentCulture);
                            dayofweek = todaydate.DayOfWeek.ToString();//getting day of week Like Sunday
                            day += 1;
                            if (day > 30)
                            {
                                day = 1;
                                month += 1;
                            }
                            if (month > 12)
                            {
                                month = 1;
                                year += 1;
                            }
                            if (index != 0)
                            {
                                admin = db.UserLogins.Where(b => b.role == "Admin").ToArray<UserLogin>();
                            }
                            if (dayofweek == "Saturday" || dayofweek == "Sunday")
                            {
                                dayscount += 1;
                                index += 1;
                                goto nextDate;
                            }
                            foreach (var a in admin)
                            {   // loop for admins and get their timeslot
                                // getting list of Available TimeSlot
                                var timeSlot = db.TimeSlots.Where(ts => ts.day == dayofweek && ts.availability == true && ts.adminId == a.cnic).ToList<TimeSlot>();
                                if (timeSlot.Count > 0)//TimeSlot of each admin is adding in list
                                {
                                    newtslist.AddRange(timeSlot);
                                }
                            }//end foreach for admin
                            if (newtslist.Count == 0)//true if no timeslot available like holidays (Saturday or Sunday)
                            {   // will go for next day timelots
                                dayscount += 1;
                                index += 1;
                                goto nextDate;
                            }
                            //Now Rearanging Appointments with other admin at same date and next date
                            int endloop = 0;
                            if (meetinglist.Count < newtslist.Count)
                            {
                                endloop = meetinglist.Count;
                            }
                            else
                            {
                                endloop = newtslist.Count;
                            }
                            int t = 0;
                        nextTimeSlot:
                            for (int j = 0; j < endloop; j++)
                            {
                                int id = (int)newtslist[t].tsid;
                                string adminID = newtslist[t].adminId.ToString();
                                var chkmeeting = db.Meetings.Where(m => m.tsid == id && m.date == newdate).FirstOrDefault();
                                var leave = db.Leaves.Where(l => l.tsid == id && l.adminId == adminID && l.date == newdate).FirstOrDefault();
                                if (chkmeeting == null && leave == null)
                                {
                                    meetinglist[j].tsid = newtslist[j].tsid;
                                    meetinglist[j].date = newdate;
                                    meetinglist[j].adminId = newtslist[j].adminId;
                                    meetinglist[j].status = "Reschedualed";
                                    db.SaveChanges();
                                    adjustcount++;
                                    t += 1;
                                }
                                else if (newtslist[t] == null)
                                {
                                    goto nextDate;
                                }
                                else
                                {
                                    t += 1;
                                    goto nextTimeSlot;
                                }

                            }//end for loop for appointments adjustment
                             //if all appointments are adjusted then will return
                            if (adjustcount >= meetinglist.Count)
                            {
                                WholeDayLeave(adminid, today, date);
                                return Request.CreateResponse(HttpStatusCode.OK, adjustcount + " Appointments are Adjusted with Other Admin");
                            }

                        }//end for loop
                         //if all appointments are adjusted then will return
                        WholeDayLeave(adminid, today, date);
                        return Request.CreateResponse(HttpStatusCode.OK, adjustcount + " Appointments are Adjusted with Other Admin");
                    }
                    else
                    {
                        WholeDayLeave(adminid, today, date);
                        return Request.CreateResponse(HttpStatusCode.NotFound, " No Appointments to Adjust with Other Admin");
                    }
                }// end if admin not null
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Admin found for appointment");
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end ReAdjustMeeting

        public void WholeDayLeave(string adminid,string day,string date)
        {
            var timeslots = db.TimeSlots.Where(t => t.day == day && t.adminId == adminid&&t.availability==true).ToList<TimeSlot>();
            if(timeslots.Count!=0)
            {
                foreach(var ts in timeslots)
                {
                    var chkleave = db.Leaves.Where(l => l.date == date && l.tsid == ts.tsid && l.adminId == ts.adminId).FirstOrDefault();
                    if(chkleave==null)
                    {
                        Leave leave = new Leave
                        {
                            tsid=ts.tsid,
                            adminId=ts.adminId,
                            date=date,
                        };
                        db.Leaves.Add(leave);
                        db.SaveChanges();
                    }
                }// end for loop
            }
            var log = db.LeaveLogs.Where(l => l.date == date && l.adminId == adminid).FirstOrDefault();
            if(log==null)
            {
                LeaveLog leaveLog = new LeaveLog
                {
                    adminId = adminid,
                    date = date,
                    reason = "N/A",
                };
                db.LeaveLogs.Add(leaveLog);
                db.SaveChanges();
            }
        }


        [HttpGet]
        public HttpResponseMessage UpdateMeetingStatus(int mid, string status,string remarks,float rating)
        {
            try
            {
                var result = db.Meetings.Where(b => b.mid == mid).FirstOrDefault();
                if (result != null)
                {
                    if(status=="Held" || (status.StartsWith("H"))|| status == "Not Held" || (status.StartsWith("N")))
                    {
                        var timeslot = db.TimeSlots.Where(ts => ts.tsid == result.tsid).FirstOrDefault();
                        History history = new History
                        {
                            regNo = result.regNo,
                            date = result.date,
                            startTime = timeslot.startTime,
                            endTime = timeslot.endTime,
                            status = status,
                            reason = result.reason,
                            adminId = result.adminId,
                            parentId = result.parentId,
                            referedTo = result.referedTo,
                            suggestion = "N/A",
                            parentRating=rating,
                            adminFeedback = remarks
                        };
                        db.Histories.Add(history);
                        db.Meetings.Remove(result);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Meeting Status Updated");
                    }
                    else
                    {
                        result.status = status;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Meeting Status Updated to " + status);
                    }
                    
                    
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Meeting Not Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end UpdateMeetingStatus

        [HttpGet]
        public HttpResponseMessage GetRating(int hid)
        {
            try
            {   //  /api/Admin/GetRating?hid=6
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

        [HttpGet]
        public HttpResponseMessage UpdateWaitingStatus(int id, string adminid)
        {
            try
            {
                var wait = db.WaitingLists.Where(r => r.id == id).FirstOrDefault();
                if (wait != null)
                {
                    var ts= db.TimeSlots.Where(t => t.adminId == adminid && t.availability==true).FirstOrDefault();
                    wait.tsId = ts.tsid;
                    wait.status = "Calling";
                    wait.adminId = adminid;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Requested Parent to meet with You");
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
        } // end UpdateWaitingStatus

        [HttpGet]
        public HttpResponseMessage UpdateAvailability(int tsid,bool avb)
        {
            try
            {   //  /api/Admin/UpdateAvailability?tsid=4&avb=true
                string NowDate = DateTime.Now.ToString("d/M/yyyy");
                DateTime todaydate = DateTime.ParseExact(NowDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                //============================
                var timeslot = db.TimeSlots.Where(t => t.tsid == tsid).FirstOrDefault();
                if (dayofweek != timeslot.day)
                {
                    for (int i = 1; i <= 7; i++)// Geting Same day to process next
                    {
                        NowDate = DateTime.Now.AddDays(i).ToString("d/M/yyyy");// adding 1 Day in date
                        todaydate = DateTime.ParseExact(NowDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                        // Checking Day of week Like  Saturday
                        dayofweek = todaydate.DayOfWeek.ToString();
                        if (dayofweek == timeslot.day)
                            break;
                    }
                }
                
                var chkMeeting = db.Meetings.Where(m => m.tsid == tsid && m.date == NowDate).FirstOrDefault();
                var leave = db.Leaves.Where(l => l.tsid == tsid &&l.date== NowDate).FirstOrDefault();
                if (leave == null && avb==false)
                {
                    if (chkMeeting == null)
                    {
                        Leave l = new Leave
                        {
                            tsid = tsid,
                            date = NowDate,
                            adminId = timeslot.adminId,
                        };
                        db.Leaves.Add(l);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Your Availibility is Updated to " + avb+" on "+dayofweek);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotModified, "Can't Update Availability to " + avb+" Due to Appointment");
                    }
                    
                }
                else if (leave != null && avb == true)
                {
                    db.Leaves.Remove(leave);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Your Availibility is Updated to " + avb+" on "+dayofweek);
                }// end else if
                else if(leave==null && avb==true)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "You are already Available");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Already Availability is "+avb+" on "+dayofweek);
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end UpdateAvailability

        [HttpGet]
        public HttpResponseMessage AdjustOneAppointment(int tsid)
        {
            try
            {   //  /api/Admin/AdjustOneAppointment?tsid=3
                // converting string Date into DateTime data type
                string date = DateTime.Now.ToString("d/M/yyyy");
                DateTime todaydate = DateTime.ParseExact(date, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();

                var meeting = db.Meetings.Where(r => r.tsid == tsid).FirstOrDefault();
                if (meeting != null)
                {
                    var ts = db.TimeSlots.Where(t => t.adminId == meeting.adminId && t.tsid==tsid && t.availability == true).FirstOrDefault();
                    var admins = db.UserLogins.Where(u => u.role == "Admin").ToList<UserLogin>();
                    int j = 0;
                    bool changes = false;
                    for(int i= 0; i<= 6; i++)
                    {
                        string aid = admins[j].cnic;
                        j++;
                        var tslist = db.TimeSlots.Where(a => a.adminId == aid && a.day==dayofweek && a.availability == true).FirstOrDefault();
                        if (tslist != null)
                        {
                            var leave = db.Leaves.Where(l => l.adminId == tslist.adminId && l.tsid == tslist.tsid && l.date == date).FirstOrDefault();
                            var app = db.Meetings.Where(m => m.tsid == tslist.tsid && m.date == date).FirstOrDefault();
                            if (leave == null && app == null && tslist.tsid!=tsid)
                            {
                                meeting.date = date;
                                meeting.tsid = tslist.tsid;
                                meeting.status = "Reschedualed";
                                meeting.adminId = tslist.adminId;
                                db.SaveChanges();
                                changes = true;
                                break;
                            }
                        }
                        if(j>2)
                        {
                            j = 0;
                        }
                    }
                    if(changes)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, "Appointment Readjusted with Admin");
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Readjusted");
                    }
                    
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment not Found");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end UpdateWaitingStatus

        [HttpGet]
        public HttpResponseMessage ReferTo(int mid,string refer,int tsid)
        {
            try
            {
                var result = db.Meetings.Where(b => b.mid == mid).FirstOrDefault();
                if (result != null)
                {
                    var ts = db.TimeSlots.Where(t => t.tsid == tsid).FirstOrDefault();
                    result.adminId = ts.adminId;
                    result.referedTo = refer;
                    result.tsid = tsid;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Meeting Refered to "+ refer);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Meeting Not Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end ReferTo



        [HttpPost]
        public HttpResponseMessage InsertWaitingList(WaitingList waiting)
        {
            try
            {
                var result = db.WaitingLists.Select(r => r).ToList<WaitingList>();
                if (result.Count != 0 && result != null)
                {
                    if(result.Count==5)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Waiting List Limit Reached");
                    }
                    else
                    {
                        var meeting = db.Meetings.Where(m => m.tsid == waiting.tsId).FirstOrDefault();
                        db.WaitingLists.Add(waiting);
                        db.Meetings.Remove(meeting);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Appointment is in Waiting List");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Request Found");
                }


            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end InsertWaitingList

        [HttpGet]
        public HttpResponseMessage GetWaitingList()
        {
            try
            {   //  /api/Admin/GetWaitingList
                var requests = db.WaitingLists.Where(s=>s.status=="Waiting").ToList<WaitingList>();
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

        [HttpGet]
        public HttpResponseMessage CallWaitingParent(int id,string adminid)
        {
            try
            {   //  /api/Admin/CallWaitingParent?id=1&adminid=3240277921986
                var wait = db.WaitingLists.Where(r => r.id == id).FirstOrDefault();
                if (wait != null)
                {
                    wait.status = "Calling";
                    wait.adminId = adminid;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Requested Parent to meet with You");
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

        [HttpGet]
        public HttpResponseMessage CountNotification(string id)
        {
            try
            {   //  /api/Admin/CountNotification?id=3240277921986
                var notification = db.Meetings.Where(a=>a.adminId== id);
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
        public HttpResponseMessage GetNotification(string adminid)
        {
            try
            {   //  /api/Admin/GetNotification?adminid=3240277921986
                var meeting = db.Meetings.Where(n=>n.adminId== adminid).ToArray();
                if (meeting != null && meeting.Length!=0)
                {
                    List<Appointment> appointment = new List<Appointment>();
                    foreach (var v in meeting)
                    {
                        Appointment a = new Appointment();
                        var timeslot = db.TimeSlots.Where(t=>t.tsid==v.tsid).FirstOrDefault();
                        a.mid = v.mid;
                        a.tsid = (int)v.tsid;
                        a.regNo = v.regNo;
                        a.reason = v.reason;
                        a.date = v.date;
                        a.startTime = timeslot.startTime;
                        a.endTime = timeslot.endTime;
                        a.parentId = v.parentId;
                        a.adminId = v.adminId;
                        a.status = v.status;
                        a.referedTo = v.referedTo;
                        a.studentMeeting = v.studentMeeting;

                        appointment.Add(a);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, appointment);
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
        public HttpResponseMessage GetNotificationByDate(string adminid,string date)
        {
            try
            {   //  /api/Admin/GetNotificationByDate?adminid=3240277921986&date=23/1/2023
                var meeting = db.Meetings.Where(n => n.adminId == adminid && n.status=="Pending" && n.date==date).ToArray();
                if (meeting != null && meeting.Length != 0)
                {
                    List<Appointment> appointment = new List<Appointment>();
                    foreach (var v in meeting)
                    {
                        Appointment a = new Appointment();
                        var timeslot = db.TimeSlots.Where(t => t.tsid == v.tsid).FirstOrDefault();
                        if (timeslot != null)
                        {
                            a.mid = v.mid;
                            a.tsid = (int)v.tsid;
                            a.date = v.date;
                            a.regNo = v.regNo;
                            a.reason = v.reason;
                            a.date = v.date;
                            a.startTime = timeslot.startTime;
                            a.endTime = timeslot.endTime;
                            a.parentId = v.parentId;
                            a.adminId = v.adminId;
                            a.status = v.status;
                            a.referedTo = v.referedTo;

                            appointment.Add(a);
                        }
                    }
                    if(appointment.Count!=0)
                    {
                        // if appointments in given date OK
                        return Request.CreateResponse(HttpStatusCode.OK, appointment);
                    }
                    else
                    {
                        // if No appointment in given date Not Ok
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No Appointments in Date "+date);
                    }
                    
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
        public HttpResponseMessage GetParent(string reg)
        {
            try
            {
                var result = db.Students.Where(b => b.regNo == reg).FirstOrDefault();
                if (result != null)
                {
                    var parent = db.Parents.Where(b => b.cnic == result.parentCNIC).FirstOrDefault();
                    return Request.CreateResponse(HttpStatusCode.OK, parent);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Parent Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetParent

        [HttpGet]
        public HttpResponseMessage LowCGPAAppointment(string date)
        {
            try
            {
                //  /api/Admin/LowCGPAAppointment?date=20/1/2023

                var student = db.CGPAs.Where(a=>a.cgpa1<2.5).ToArray<CGPA>();
                // spliting date for customization
                string[] datelist = date.Split('/');
                int day = int.Parse(datelist[0]);
                int month = int.Parse(datelist[1]);
                int year = int.Parse(datelist[2]);
                string customeDate = day + "/" + month + "/" + year;
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                if (dayofweek == "Saturday" || dayofweek == "Sunday")
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Possible due to Holiday");
                }
                //================
                int count = 0; // How much Appointments are fixed
                List<TimeSlot> schedule = new List<TimeSlot>();
                var admins = db.UserLogins.Where(d => d.role == "Admin").ToArray<UserLogin>();
                foreach (var a in admins)
                {
                    var schedule2 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1 and adminId='" + a.cnic + "'").ToList<TimeSlot>();
                    foreach (var ts in schedule2)
                    {
                        var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                        if (leave == null)
                        {
                            schedule.Add(ts);
                        }
                    }
                }
                if (schedule.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Slot Found on given Date");
                }
                if (schedule != null && student.Length != 0)
                {
                    int length = 0;
                    if (schedule.Count < student.Length)
                    {
                        day += 1;
                        if (day > 30)
                        {
                            day = 1;
                            month += 1;
                            if (month > 12)
                            {
                                month = 1;
                                year += 1;
                            }
                        }
                        customeDate = day + "/" + month + "/" + year;
                        // converting string Date into DateTime data type
                        todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                        // Checking Day of week Like  Saturday
                        dayofweek = todaydate.DayOfWeek.ToString();
                        var schedule1 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1").ToList<TimeSlot>();
                        foreach (var ts in schedule1)
                        {
                            var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                            if (leave == null)
                            {
                                schedule.Add(ts);
                            }
                        }
                        length = schedule.Count;
                    }
                    else
                    {
                        length = student.Length;
                    }
                    int j = 0;
                    repeat:
                    for(int i=0;i<length;i++)
                    {

                        string reg = student[i].regNo;
                        int tsid = schedule[j].tsid;
                        var meeting = db.Meetings.Where(mt=>mt.regNo==reg && mt.tsid==tsid || mt.tsid == tsid && mt.date==date).FirstOrDefault();
                        if (meeting == null)
                        {
                            var parent = db.Students.Where(p => p.regNo == reg).FirstOrDefault();
                            Meeting m = new Meeting();
                            m.tsid = schedule[j].tsid;
                            m.date = date;
                            m.regNo = student[i].regNo;
                            m.parentId = parent.parentCNIC;
                            m.adminId = schedule[j].adminId;
                            m.reason = "Low CGPA";
                            m.status = "Request";
                            m.referedTo = "N/A";
                            m.studentMeeting = false;
                            db.Meetings.Add(m);
                            db.SaveChanges();
                            count++;
                            j++;
                        }
                        else
                        {
                            j++;
                            goto repeat;
                        }
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, count+" Low CGPA Appointments Fixed");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Student or TimeSlot Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end LowCGPAAppointment

        [HttpGet]
        public HttpResponseMessage GetLowCGPAStudents()
        {
            try
            {
                //  /api/Admin/GetLowCGPAStudents
                List<CGPAModel> cgpalist = new List<CGPAModel>();
                var result = db.CGPAs.Where(b => b.cgpa1 < 2.5).ToList<CGPA>();
                if (result != null)
                {
                    foreach(var v in result)
                    {
                        var std = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                        CGPAModel c = new CGPAModel
                        {
                            id=v.id,
                            regNo=v.regNo,
                            @class=std.@class,
                            semester= (int)std.semester,
                            section=std.section,
                            cgpa1=v.cgpa1,
                        };
                        cgpalist.Add(c);
                    }
                    
                    return Request.CreateResponse(HttpStatusCode.OK, cgpalist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Student Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetLowCGPAStudents



        //==========================================================================




        [HttpGet]
        public HttpResponseMessage ShortAttendanceAppointment(string date)
        {
            try
            {   //  /api/Admin/ShortAttendanceAppointment?date=20/2/2023

                var student = db.Attendances.Where(a=>a.percentage<75).ToList<Attendance>();

                // spliting date for customization
                string[] datelist = date.Split('/');
                int day = int.Parse(datelist[0]);
                int month = int.Parse(datelist[1]);
                int year = int.Parse(datelist[2]);
                string customeDate = day + "/" + month + "/" + year;
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                if (dayofweek == "Saturday" || dayofweek == "Sunday")
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Possible due to Holiday");
                }
                //================
                int count = 0; // How much Appointments are fixed
                List<TimeSlot> schedule = new List<TimeSlot>();
                var admins = db.UserLogins.Where(d => d.role == "Admin").ToArray<UserLogin>();
                foreach (var a in admins)
                {
                    var schedule2 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1 and adminId='"+a.cnic+"'").ToList<TimeSlot>();
                    foreach (var ts in schedule2)
                    {
                        var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                        if (leave == null)
                        {
                            schedule.Add(ts);
                        }
                    }
                }
                if (schedule.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Slot Found on given Date");
                }

                if (schedule != null && student.Count != 0)
                {
                    int length = 0;
                    if (schedule.Count < student.Count)
                    {
                        day += 1;
                        if (day > 30)
                        {
                            day = 1;
                            month += 1;
                            if (month > 12)
                            {
                                month = 1;
                                year += 1;
                            }
                        }
                        customeDate = day + "/" + month + "/" + year;
                        // converting string Date into DateTime data type
                        todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                        // Checking Day of week Like  Saturday
                        dayofweek = todaydate.DayOfWeek.ToString();
                        var schedule1 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1").ToList<TimeSlot>();
                        foreach (var ts in schedule1)
                        {
                            var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                            if (leave == null)
                            {
                                schedule.Add(ts);
                            }
                        }
                        length = student.Count;
                    }
                    else
                    {
                        length = student.Count;
                    }
                    int j = 0;
                    if(schedule.Count==0)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound,  "No Slot Found for Appointment");
                    }
                repeat:
                    for (int i = 0; i < length; i++)
                    {

                        string reg = student[i].regNo;
                        int tsid = schedule[j].tsid;
                        var meeting = db.Meetings.Where(mt=>mt.tsid == tsid&& mt.date==date).FirstOrDefault();
                        if (meeting == null)
                        {
                            var parent = db.Students.Where(p => p.regNo == reg).FirstOrDefault();
                            Meeting m = new Meeting();
                            m.tsid = schedule[j].tsid;
                            m.date = date;
                            m.regNo = student[i].regNo;
                            m.parentId = parent.parentCNIC;
                            m.adminId = schedule[j].adminId;
                            m.reason = "Short Attendance";
                            m.status = "Request";
                            m.referedTo = "N/A";
                            m.studentMeeting = false;
                            db.Meetings.Add(m);
                            db.SaveChanges();
                            count++;
                            j++;
                        }
                        else
                        {
                            j++;
                            goto repeat;
                        }

                    }
                    return Request.CreateResponse(HttpStatusCode.OK, count+" Short Attendance Appointment Fixed");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Student or TimeSlot Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end ShortAttendanceAppointment

        //=========================

        [HttpGet]
        public HttpResponseMessage GetShortAttendance()
        {
            try
            {
                List<AttendanceModel> attlist = new List<AttendanceModel>();
                var result = db.Attendances.Where(b => b.percentage < 75).ToList<Attendance>();
                if (result != null)
                {
                    foreach(var v in result)
                    {
                        var std = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                        AttendanceModel a = new AttendanceModel
                        {
                            sid=v.sid,
                            regNo=v.regNo,
                            @class=std.@class,
                            semester= (int)std.semester,
                            section=std.section,
                            subject=v.subject,
                            percentage=v.percentage,
                        };
                        attlist.Add(a);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, attlist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Student Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetShortAttendance

        [HttpGet]
        public HttpResponseMessage DisciplenaryAppointment(string date)
        {
            try
            {
                //  /api/admin/DisciplenaryAppointment?date=20/1/2023

                var student = db.Disciplinaries.SqlQuery("Select * from Disciplinary").ToArray<Disciplinary>();
                // spliting date for customization
                string[] datelist = date.Split('/');
                int day = int.Parse(datelist[0]);
                int month = int.Parse(datelist[1]);
                int year = int.Parse(datelist[2]);
                string customeDate = day + "/" + month + "/" + year;
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                if (dayofweek == "Saturday" || dayofweek == "Sunday")
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Possible due to Holiday");
                }
                //================
                int count = 0; // How much Appointments are fixed
                List<TimeSlot> schedule = new List<TimeSlot>();
                var admins = db.UserLogins.Where(d => d.role == "Admin").ToArray<UserLogin>();
                foreach (var a in admins)
                {
                    var schedule2 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1 and adminId='" + a.cnic + "'").ToList<TimeSlot>();
                    foreach (var ts in schedule2)
                    {
                        var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                        if (leave == null)
                        {
                            schedule.Add(ts);
                        }
                    }
                }
                if (schedule.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Slot Found on given Date");
                }
                if (schedule != null && student.Length != 0)
                {
                    int length = 0;
                    if (schedule.Count < student.Length)
                    {
                        day += 1;
                        if (day > 30)
                        {
                            day = 1;
                            month += 1;
                            if (month > 12)
                            {
                                month = 1;
                                year += 1;
                            }
                        }
                        customeDate = day + "/" + month + "/" + year;
                        // converting string Date into DateTime data type
                        todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                        // Checking Day of week Like  Saturday
                        dayofweek = todaydate.DayOfWeek.ToString();
                        var schedule1 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1").ToList<TimeSlot>();
                        foreach (var ts in schedule1)
                        {
                            var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                            if (leave == null)
                            {
                                schedule.Add(ts);
                            }
                        }
                        length = schedule.Count;
                    }
                    else
                    {
                        length = student.Length;
                    }
                    if(schedule.Count==0)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No Slot Found on given Date");
                    }
                    int j = 0;
                    repeat:
                    for (int i = 0; i < length; i++)
                    {
                        string reg = student[i].regNo;
                        int tsid = schedule[j].tsid;
                        var meeting = db.Meetings.Where(mt => mt.tsid == tsid && mt.date == date).FirstOrDefault();
                        if (meeting == null)
                        {
                            var parent = db.Students.Where(p => p.regNo == reg).FirstOrDefault();
                            Meeting m = new Meeting();
                            m.tsid = schedule[j].tsid;
                            m.date = date;
                            m.regNo = student[i].regNo;
                            m.parentId = parent.parentCNIC;
                            m.adminId = schedule[j].adminId;
                            m.reason = student[i].actions;
                            m.status = "Request";
                            m.referedTo = "N/A";
                            m.studentMeeting = false;
                            db.Meetings.Add(m);
                            db.SaveChanges();
                            count++;
                            j++;
                        }
                        else
                        {
                            j++;
                            goto repeat;
                        }

                    }
                    return Request.CreateResponse(HttpStatusCode.OK, count+" Disciplenary Appointments Fixed");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Student or TimeSlot Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end DisciplinaryAppointment

        [HttpGet]
        public HttpResponseMessage GetDisciplenary()
        {
            try
            {
                List<DisciplinaryModel> dlist = new List<DisciplinaryModel>();
                var result = db.Disciplinaries.Select(b => b).ToList<Disciplinary>();
                if (result != null)
                {
                    foreach (var v in result)
                    {
                        var std = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                        DisciplinaryModel a = new DisciplinaryModel
                        {
                            id=v.id,
                            regNo = v.regNo,
                            @class = std.@class,
                            semester = (int)std.semester,
                            section = std.section,
                            actions=v.actions,
                        };
                        dlist.Add(a);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, dlist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Student Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetDisciplenary

        [HttpGet]
        public HttpResponseMessage FailedAppointment(string date)
        {
            try
            {
                //  /api/admin/FailedAppointment?date=20/1/2023

                var student = db.Disciplinaries.Select(b=>b).ToArray<Disciplinary>();

                // spliting date for customization
                string[] datelist = date.Split('/');
                int day = int.Parse(datelist[0]);
                int month = int.Parse(datelist[1]);
                int year = int.Parse(datelist[2]);
                string customeDate = day + "/" + month + "/" + year;
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                if (dayofweek == "Saturday" || dayofweek == "Sunday")
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Possible due to Holiday");
                }
                //================

                List<TimeSlot> schedule = new List<TimeSlot>();
                var admins = db.UserLogins.Where(d => d.role == "Admin").ToArray<UserLogin>();
                foreach (var a in admins)
                {
                    var schedule2 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1 and adminId='" + a.cnic + "'").ToList<TimeSlot>();
                    foreach (var ts in schedule2)
                    {
                        var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                        if (leave == null)
                        {
                            schedule.Add(ts);
                        }
                    }
                }
                if (schedule.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Slot Found on given Date");
                }
                if (schedule != null && student.Length != 0)
                {
                    int length = 0;
                    if (schedule.Count < student.Length)
                    {
                        day += 1;
                        if (day > 30)
                        {
                            day = 1;
                            month += 1;
                            if (month > 12)
                            {
                                month = 1;
                                year += 1;
                            }
                        }
                        customeDate = day + "/" + month + "/" + year;
                        // converting string Date into DateTime data type
                        todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                        // Checking Day of week Like  Saturday
                        dayofweek = todaydate.DayOfWeek.ToString();
                        var schedule1 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1").ToList<TimeSlot>();
                        foreach (var ts in schedule1)
                        {
                            var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                            if (leave==null)
                            {
                                schedule.Add(ts);
                            }
                        }
                    }
                    else
                    {
                        length = student.Length;
                    }
                    int j = 0;
                    repeat:
                    for (int i = 0; i < length; i++)
                    {

                        string reg = student[i].regNo;
                        int tsid = schedule[j].tsid;
                        var meeting = db.Meetings.Where(mt=>mt.date==date && mt.tsid==tsid).FirstOrDefault();
                        if (meeting == null)
                        {
                            var parent = db.Students.Where(p => p.regNo == reg).FirstOrDefault();
                            Meeting m = new Meeting();
                            m.tsid = schedule[j].tsid;
                            m.date = date;
                            m.regNo = student[i].regNo;
                            m.parentId = parent.parentCNIC;
                            m.adminId = schedule[j].adminId;
                            m.reason = "Failed Subject";
                            m.status = "Request";
                            m.referedTo = "N/A";
                            m.studentMeeting = false;
                            db.Meetings.Add(m);
                            db.SaveChanges();
                            j++;
                        }
                        else
                        {
                            j++;
                            goto repeat;
                        }


                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "Failed Subject Appointments Fixed");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Student or TimeSlot Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end FailedAppointment

        [HttpGet]
        public HttpResponseMessage GetFailedSubjects()
        {
            try
            {
                var result = db.FailedSubjects.Select(b => b).ToList<FailedSubject>();
                if (result != null && result.Count!=0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Student Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetFailedSubjects

        //=======================================

        [HttpGet]
        public HttpResponseMessage NearToDropAppointment(string date)
        {
            try
            {
                //  /api/admin/NearToDropAppointment?date=19/1/2023

                // spliting date for customization
                string[] datelist = date.Split('/');
                int day = int.Parse(datelist[0]);
                int month = int.Parse(datelist[1]);
                int year = int.Parse(datelist[2]);
                string customeDate = day + "/" + month + "/" + year;
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                //================
                List<CGPAModel> stdlist = new List<CGPAModel>();
                var cgpalist = db.CGPAs.ToList<CGPA>();

                List<string> reglist = new List<string>(); // Near to Drop Students RegNo will be added here
                // Checking Students near to drop 
                foreach (var v in cgpalist)
                {
                    var std = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                    string regno = v.regNo;
                    if (std.semester == 1 && v.cgpa1 <= 0.75)
                    {

                        reglist.Add(regno);
                    }
                    else if (std.semester == 2 && v.cgpa1 <= 1.0)
                    {
                        reglist.Add(regno);
                    }
                    else if (std.semester == 3 && v.cgpa1 <= 1.25)
                    {
                        reglist.Add(regno);
                    }
                    else if (std.semester == 4 && v.cgpa1 <= 1.5)
                    {
                        reglist.Add(regno);
                    }
                    else if (std.semester == 5 && v.cgpa1 <= 1.75)
                    {
                        reglist.Add(regno);
                    }
                    else if (std.semester == 6 && v.cgpa1 <= 2.0)
                    {
                        reglist.Add(regno);
                    }
                    else if (std.semester == 7 && v.cgpa1 <= 2.25)
                    {
                        reglist.Add(regno);
                    }
                    else if (std.semester == 8 && v.cgpa1 <= 2.5)
                    {
                        reglist.Add(regno);
                    }
                }// end for loop

                int count = 0;
                 //==============
                if (reglist.Count != 0)
                {
                    List<TimeSlot> schedule;
                    schedule = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1").ToList<TimeSlot>();

                    if (schedule != null && reglist != null && reglist.Count != 0)
                    {
                        int length = 0;
                        if (schedule.Count < reglist.Count)
                        {
                            day += 1;
                            if(day>30)
                            {
                                day = 1;
                                month += 1;
                                if(month>12)
                                {
                                    month = 1;
                                    year += 1;
                                }
                            }
                            customeDate = day + "/" + month + "/" + year;
                            // converting string Date into DateTime data type
                            todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                            // Checking Day of week Like  Saturday
                            dayofweek = todaydate.DayOfWeek.ToString();
                            var schedule1 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1").ToList<TimeSlot>();
                            if(schedule1.Count!=0)
                            {
                                schedule.AddRange(schedule1);
                            }
                        }
                        else
                        {
                            length = reglist.Count;
                        }
                        int j = 0;
                        repeat:
                        for (int i = 0; i < length; i++)
                        {

                            string reg = reglist[i].ToString();
                            int tsid = (int)schedule[j].tsid;
                            var meeting = db.Meetings.Where(mt => mt.regNo == reg && mt.tsid == tsid&&mt.date==date).FirstOrDefault();
                            if (meeting == null)
                            {
                                var parent = db.Students.Where(p => p.regNo == reg).FirstOrDefault();
                                Meeting m = new Meeting();
                                m.tsid = schedule[j].tsid;
                                m.date = date;
                                m.regNo = reglist[i];
                                m.parentId = parent.parentCNIC;
                                m.adminId = schedule[j].adminId;
                                m.reason = "Near To Drop";
                                m.status = "Request";
                                m.referedTo = "N/A";
                                db.Meetings.Add(m);
                                db.SaveChanges();

                                count++; // for checking how much Appointments are fixed
                                j++;
                            }
                            else
                            {
                                j++;
                                goto repeat;
                            }


                        }
                        return Request.CreateResponse(HttpStatusCode.OK, count+" Near to Drop Appointments Fixed");
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Student or TimeSlot Exist");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Student Near to Drop");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end NearToDropAppointment

        //======= Getting Near to Drop Students on the Basis of CGPA and Semester ====

        [HttpGet]
        public HttpResponseMessage GetNearToDropStudent()
        {
            try
            {   //  /api/Admin/GetNearToDropStudent

                List<CGPAModel> stdlist = new List<CGPAModel>();
                var cgpalist = db.CGPAs.ToList<CGPA>();
                if (cgpalist != null)
                {
                    foreach (var v in cgpalist)
                    {
                        var std = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                        CGPAModel c = new CGPAModel
                        {
                            id = v.id,
                            regNo = v.regNo,
                            @class = std.@class,
                            semester = (int)std.semester,
                            section = std.section,
                            cgpa1 = v.cgpa1
                        };
                        if (std.semester == 1 && v.cgpa1 <= 0.75)
                        {

                            stdlist.Add(c);
                        }
                        else if (std.semester == 2 && v.cgpa1 <= 1.0)
                        {
                            stdlist.Add(c);
                        }
                        else if (std.semester == 3 && v.cgpa1 <= 1.25)
                        {
                            stdlist.Add(c);
                        }
                        else if (std.semester == 4 && v.cgpa1 <= 1.5)
                        {
                            stdlist.Add(c);
                        }
                        else if (std.semester == 5 && v.cgpa1 <= 1.75)
                        {
                            stdlist.Add(c);
                        }
                        else if (std.semester == 6 && v.cgpa1 <= 2.0)
                        {
                            stdlist.Add(c);
                        }
                        else if (std.semester == 7 && v.cgpa1 <= 2.25)
                        {
                            stdlist.Add(c);
                        }
                        else if (std.semester == 8 && v.cgpa1 <= 2.5)
                        {
                            stdlist.Add(c);
                        }
                    }
                    if (stdlist.Count == 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No Student Near to Drop");
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, stdlist);
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
        } // end GetNearToDropStudent

        //==============================================================

        [HttpGet]
        public HttpResponseMessage GetTimeSlotRole(string role)
        {
            try
            {   //  /api/Admin/GetTimeSlotRole?role=Datacell

                var datacell = db.UserLogins.Where(d => d.role == role).ToArray<UserLogin>();

                List<TimeSlot> timeSlots = new List<TimeSlot>();

                string nowdate = DateTime.Now.ToString("d/M/yyyy");
                DateTime newdate = DateTime.ParseExact(nowdate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = newdate.DayOfWeek.ToString();
                //var toDate = DateTime.ParseExact(nowdate, "d/M/yyyy", CultureInfo.CurrentCulture); //11/10/2022
                foreach (var v in datacell)
                {
                    var schedule1 = db.TimeSlots.Where(t=>t.adminId==v.cnic && t.availability==true && t.day== dayofweek).ToArray<TimeSlot>();
                    foreach (var s in schedule1)
                    {
                        var leave = db.Leaves.Where(l => l.tsid == s.tsid && l.date == nowdate && l.adminId == v.cnic).FirstOrDefault();
                        var meeting = db.Meetings.Where(m => m.date == nowdate && m.tsid == s.tsid).FirstOrDefault();
                        if (meeting == null&& leave==null)
                        {
                            TimeSlot ts = new TimeSlot
                            {
                                tsid = s.tsid,
                                day = s.day,
                                startTime = s.startTime,
                                endTime = s.endTime,
                                adminId = s.adminId,
                                availability = s.availability,

                            };
                            timeSlots.Add(ts);
                        }

                    }

                }
                if (timeSlots != null && timeSlots.Count != 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, timeSlots);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Time Slot Exist for Meeting");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end GetTimeSlot

        [HttpGet]
        public HttpResponseMessage GetTimeSlot(string adminid)
        {
            try
            {   // /api/Admin/GetTimeSlot?adminid=3240277921986
                List<TimeSlot> tslist = new List<TimeSlot>();
                string nowdate = DateTime.Now.ToString("d/M/yyyy");
                DateTime NowDate = DateTime.ParseExact(nowdate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = NowDate.DayOfWeek.ToString();
                var timeslot = db.TimeSlots.Where(d => d.adminId == adminid).ToArray<TimeSlot>();
                if (timeslot.Length != 0)
                {
                    foreach(var ts in timeslot)
                    {
                        if (dayofweek != ts.day)
                        {   // if not today then next day with next Date Leave will be checked
                            string newDate = nowdate;
                            string dayofweek1 = dayofweek;
                            for (int i = 1; i <= 6; i++)// Geting Same day to process next
                            {
                                newDate = DateTime.Now.AddDays(i).ToString("d/M/yyyy");
                                NowDate = DateTime.ParseExact(newDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                                // Checking Day of week Like  Saturday
                                dayofweek1 = NowDate.DayOfWeek.ToString();
                                if (dayofweek1 == ts.day)
                                    break;
                            }
                            bool? avb;
                            var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == newDate && l.adminId == adminid).FirstOrDefault();
                            if (leave != null || ts.availability == false)
                            {
                                avb = false;
                            }
                            else if (ts.availability == null)
                            {
                                avb = null;
                            }
                            else
                            {
                                avb = true;
                            }
                            TimeSlot slot = new TimeSlot
                            {
                                tsid = ts.tsid,
                                day = ts.day,
                                startTime = ts.startTime,
                                endTime = ts.endTime,
                                adminId = ts.adminId,
                                availability = avb,
                            };
                            tslist.Add(slot);
                        }
                        else
                        {
                            // if today then Same day with same Date Leave will be checked
                            bool? avb;
                            var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == nowdate && l.adminId == adminid).FirstOrDefault();
                            if (leave != null || ts.availability == false)
                            {
                                avb = false;
                            }
                            else if (ts.availability == null)
                            {
                                avb = null;
                            }
                            else
                            {
                                avb = true;
                            }
                            TimeSlot slot = new TimeSlot
                            {
                                tsid = ts.tsid,
                                day = ts.day,
                                startTime = ts.startTime,
                                endTime = ts.endTime,
                                adminId = ts.adminId,
                                availability = avb,
                            };
                            tslist.Add(slot);
                        }

                    }
                    
                    return Request.CreateResponse(HttpStatusCode.OK, tslist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Time Slot Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end GetTimeSlot

        //=========================================================
        //=== Task Functions =================================

        //==== Task Function ========================

        [HttpGet]
        public HttpResponseMessage GetLowCGPAStudentsParentCall()
        {
            try
            {
                //  /api/Admin/GetLowCGPAStudentsParentCall
                List<CGPAModel> cgpalist = new List<CGPAModel>();
                var result = db.CGPAs.Where(b => b.cgpa1 < 1.0).ToList<CGPA>();
                if (result != null)
                {
                    foreach (var v in result)
                    {
                        var std = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                        CGPAModel c = new CGPAModel
                        {
                            id = v.id,
                            regNo = v.regNo,
                            @class = std.@class,
                            semester = (int)std.semester,
                            section = std.section,
                            cgpa1 = v.cgpa1,
                        };
                        cgpalist.Add(c);
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, cgpalist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Student Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetLowCGPAStudentsParentCall

        //==== Task Function =========


        [HttpGet]
        public HttpResponseMessage GetLowCGPAStudentsStudentCall()
        {
            try
            {
                //  /api/Admin/GetLowCGPAStudentsStudentCall
                List<CGPAModel> cgpalist = new List<CGPAModel>();
                var result = db.CGPAs.Where(b => b.cgpa1 < 2.0 && b.cgpa1 > 1.0).ToList<CGPA>();
                string NowDate = DateTime.Now.ToString("d/M/yyyy");
                if (result != null)
                {
                    foreach (var v in result)
                    {
                        var std = db.Students.Where(s => s.regNo == v.regNo).FirstOrDefault();
                        var mt = db.Meetings.Where(m => m.regNo == v.regNo && m.date == NowDate).FirstOrDefault();
                        if(mt==null)
                        {
                            CGPAModel c = new CGPAModel
                            {
                                id = v.id,
                                regNo = v.regNo,
                                @class = std.@class,
                                semester = (int)std.semester,
                                section = std.section,
                                cgpa1 = v.cgpa1,
                            };
                            cgpalist.Add(c);
                        }
                        
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, cgpalist);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Student Exist");
                }// end else

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetLowCGPAStudentsStudentCall
        //========================================

        [HttpGet]
        public HttpResponseMessage LowCGPAAppointmentParentCall(string date)
        {
            try
            {
                //  /api/Admin/LowCGPAAppointmentParentCall?date=10/2/2023

                var student = db.CGPAs.Where(a => a.cgpa1 < 1.0).ToArray<CGPA>();
                // spliting date for customization
                string[] datelist = date.Split('/');
                int day = int.Parse(datelist[0]);
                int month = int.Parse(datelist[1]);
                int year = int.Parse(datelist[2]);
                string customeDate = day + "/" + month + "/" + year;
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                if (dayofweek == "Saturday" || dayofweek == "Sunday")
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Possible due to Holiday");
                }
                //================
                int count = 0; // How much Appointments are fixed
                List<TimeSlot> schedule = new List<TimeSlot>();
                var admins = db.UserLogins.Where(d => d.role == "Admin").ToArray<UserLogin>();
                foreach (var a in admins)
                {
                    var schedule2 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1 and adminId='" + a.cnic + "'").ToList<TimeSlot>();
                    foreach (var ts in schedule2)
                    {
                        var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                        if (leave == null)
                        {
                            schedule.Add(ts);
                        }
                    }
                }
                if (schedule.Count==0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Slot Found on given Date");
                }
                if (schedule != null && student.Length != 0)
                {
                    int length = 0;
                    if (schedule.Count < student.Length)
                    {
                        day += 1;
                        if (day > 30)
                        {
                            day = 1;
                            month += 1;
                            if (month > 12)
                            {
                                month = 1;
                                year += 1;
                            }
                        }
                        customeDate = day + "/" + month + "/" + year;
                        // converting string Date into DateTime data type
                        todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                        // Checking Day of week Like  Saturday
                        dayofweek = todaydate.DayOfWeek.ToString();
                        var schedule1 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1").ToList<TimeSlot>();
                        foreach (var ts in schedule1)
                        {
                            var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                            if (leave == null)
                            {
                                schedule.Add(ts);
                            }
                        }
                        length = schedule.Count;
                    }
                    else
                    {
                        length = student.Length;
                    }
                    int j = 0;
                repeat:
                    for (int i = 0; i < length; i++)
                    {

                        string reg = student[i].regNo;
                        int tsid = schedule[j].tsid;
                        var meeting = db.Meetings.Where(mt => mt.regNo == reg && mt.tsid == tsid || mt.tsid == tsid && mt.date == date).FirstOrDefault();
                        if (meeting == null)
                        {
                            var parent = db.Students.Where(p => p.regNo == reg).FirstOrDefault();
                            Meeting m = new Meeting();
                            m.tsid = schedule[j].tsid;
                            m.date = date;
                            m.regNo = student[i].regNo;
                            m.parentId = parent.parentCNIC;
                            m.adminId = schedule[j].adminId;
                            m.reason = "Low CGPA";
                            m.status = "Pending";
                            m.referedTo = "N/A";
                            m.studentMeeting = false;
                            db.Meetings.Add(m);
                            db.SaveChanges();
                            count++;
                            j++;
                        }
                        else
                        {
                            j++;
                            goto repeat;
                        }
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, count + " Low CGPA Appointments Fixed");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Student or TimeSlot Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end LowCGPAAppointmentParentCall


        //=======================================================

        [HttpGet]
        public HttpResponseMessage LowCGPAAppointmentStudentCall(string date)
        {
            try
            {
                //  /api/Admin/LowCGPAAppointmentStudentCall?date=10/2/2023

                var student = db.CGPAs.Where(a => a.cgpa1 < 2.0 && a.cgpa1 > 1.0).ToArray<CGPA>();
                // spliting date for customization
                string[] datelist = date.Split('/');
                int day = int.Parse(datelist[0]);
                int month = int.Parse(datelist[1]);
                int year = int.Parse(datelist[2]);
                string customeDate = day + "/" + month + "/" + year;
                // converting string Date into DateTime data type
                DateTime todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                // Checking Day of week Like  Saturday
                string dayofweek = todaydate.DayOfWeek.ToString();
                if (dayofweek == "Saturday" || dayofweek == "Sunday")
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Appointment Not Possible due to Holiday");
                }
                //================
                int count = 0; // How much Appointments are fixed
                List<TimeSlot> schedule = new List<TimeSlot>();
                var admins = db.UserLogins.Where(d => d.role == "Admin").ToArray<UserLogin>();
                foreach (var a in admins)
                {
                    var schedule2 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1 and adminId='" + a.cnic + "'").ToList<TimeSlot>();
                    foreach (var ts in schedule2)
                    {
                        var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                        if (leave == null)
                        {
                            schedule.Add(ts);
                        }
                    }
                }
                if (schedule.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Slot Found on given Date");
                }
                if (schedule != null && student.Length != 0)
                {
                    int length = 0;
                    if (schedule.Count < student.Length)
                    {
                        day += 1;
                        if (day > 30)
                        {
                            day = 1;
                            month += 1;
                            if (month > 12)
                            {
                                month = 1;
                                year += 1;
                            }
                        }
                        customeDate = day + "/" + month + "/" + year;
                        // converting string Date into DateTime data type
                        todaydate = DateTime.ParseExact(customeDate, "d/M/yyyy", CultureInfo.CurrentCulture);
                        // Checking Day of week Like  Saturday
                        dayofweek = todaydate.DayOfWeek.ToString();
                        var schedule1 = db.TimeSlots.SqlQuery("Select * from TimeSlot where day='" + dayofweek + "' and availability=1").ToList<TimeSlot>();
                        foreach (var ts in schedule1)
                        {
                            var leave = db.Leaves.Where(l => l.tsid == ts.tsid && l.date == customeDate).FirstOrDefault();
                            if (leave == null)
                            {
                                schedule.Add(ts);
                            }
                        }
                        length = schedule.Count;
                    }
                    else
                    {
                        length = student.Length;
                    }
                    if (schedule.Count == 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No Slot Found on given Date");
                    }
                    int j = 0;
                repeat:
                    for (int i = 0; i < length; i++)
                    {

                        string reg = student[i].regNo;
                        int tsid = schedule[j].tsid;
                        var meeting = db.Meetings.Where(mt => mt.regNo == reg && mt.tsid == tsid || mt.tsid == tsid && mt.date == date).FirstOrDefault();
                        if (meeting == null)
                        {
                            var parent = db.Students.Where(p => p.regNo == reg).FirstOrDefault();
                            Meeting m = new Meeting();
                            m.tsid = schedule[j].tsid;
                            m.date = date;
                            m.regNo = student[i].regNo;
                            m.parentId = parent.parentCNIC;
                            m.adminId = schedule[j].adminId;
                            m.reason = "Low CGPA";
                            m.status = "Pending";
                            m.referedTo = "N/A";
                            m.studentMeeting = true;
                            db.Meetings.Add(m);
                            db.SaveChanges();
                            count++;
                            j++;
                        }
                        else
                        {
                            j++;
                            goto repeat;
                        }
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, count + " Low CGPA Appointments Fixed");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Student or TimeSlot Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end LowCGPAAppointmentStudentCall


    }// end Class
}
