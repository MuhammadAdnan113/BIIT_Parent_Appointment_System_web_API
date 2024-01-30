using BIIT_Parent_Appointment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BIIT_Parent_Appointment.Controllers
{
    public class ReferController : ApiController
    {
        BIIT_Parent_AppointmentEntities db = new BIIT_Parent_AppointmentEntities();

        [HttpGet]
        public HttpResponseMessage UpdateMeetingStatus(int mid, string status, string remarks,float rating)
        {
            try
            {
                var result = db.Meetings.Where(b => b.mid == mid).FirstOrDefault();
                if (result != null)
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
                    return Request.CreateResponse(HttpStatusCode.OK, "Meeting Status Updated to " + status);
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
        public HttpResponseMessage DeleteMeeting(int mid)
        {
            try
            {
                var result = db.Meetings.Where(b => b.mid == mid).FirstOrDefault();
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
        public HttpResponseMessage GetAdminDetails()
        {
            try
            {       //  /api/refer/GetAdminDetails
                List<AdminDetailModel> adminDetails = new List<AdminDetailModel>();
                var adminlist = db.Admins.Select(h => h).ToList<Admin>();
                if (adminlist != null)
                {
                    foreach (var v in adminlist)
                    {
                        AdminDetailModel a = new AdminDetailModel();
                        var rudness = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.rudness);
                        var attentive = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.attentive);
                        var polite = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.polite);
                        var count = db.Histories.Where(t => t.adminId == v.cnic).ToList().Count();
                        if (rudness == null)
                        {
                            rudness = 0;
                        }
                        if(attentive==null)
                        {
                            attentive = 0;
                        }
                        if(polite==null)
                        {
                            polite = 0;
                        }
                        a.cnic = v.cnic;
                        a.email = v.email;
                        a.firstName = v.firstName;
                        a.lastName = v.lastName;
                        a.role = v.role;
                        a.attentive = (double)attentive;
                        a.polite = (double)polite;
                        a.rudness = (double)rudness;
                        a.noOfAppointments = count;
                        adminDetails.Add(a);

                    }
                    return Request.CreateResponse(HttpStatusCode.OK, adminDetails);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Admin Persons Record Not Exist");
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetAdminDetails

        [HttpGet]
        public HttpResponseMessage GetRudnessAdmin()
        {
            try
            {       //  /api/refer/GetRudnessAdmin
                List<AdminDetailModel> adminDetails = new List<AdminDetailModel>();
                var adminlist = db.Admins.Where(h => h.role=="Admin").ToList<Admin>();
                if (adminlist != null)
                {
                    foreach (var v in adminlist)
                    {
                        AdminDetailModel a = new AdminDetailModel();
                        var rudness = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.rudness);
                        var attentive = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.attentive);
                        var polite = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.polite);
                        var count = db.Histories.Where(t => t.adminId == v.cnic).ToList().Count();
                        if (rudness == null)
                        {
                            rudness = 0;
                        }
                        if (attentive == null)
                        {
                            attentive = 0;
                        }
                        if (polite == null)
                        {
                            polite = 0;
                        }
                        a.cnic = v.cnic;
                        a.email = v.email;
                        a.firstName = v.firstName;
                        a.lastName = v.lastName;
                        a.role = v.role;
                        a.attentive = (double)attentive;
                        a.polite = (double)polite;
                        a.rudness = (double)rudness;
                        a.noOfAppointments = count;
                        adminDetails.Add(a);

                    }
                    var rd = adminDetails[0];
                    for (int i=0;i<adminDetails.Count;i++)
                    {
                        if(adminDetails[0].rudness<adminDetails[i].rudness)
                        {
                            rd = adminDetails[i];
                        }
                        
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, rd);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Admin Persons Record Not Exist");
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetRudnessAdmin

        [HttpGet]
        public HttpResponseMessage GetPoliteAdmin()
        {
            try
            {       //  /api/refer/GetPoliteAdmin
                List<AdminDetailModel> adminDetails = new List<AdminDetailModel>();
                var adminlist = db.Admins.Where(h => h.role == "Admin").ToList<Admin>();
                if (adminlist != null)
                {
                    foreach (var v in adminlist)
                    {
                        AdminDetailModel a = new AdminDetailModel();
                        var rudness = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.rudness);
                        var attentive = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.attentive);
                        var polite = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.polite);
                        var count = db.Histories.Where(t => t.adminId == v.cnic).ToList().Count();
                        if (rudness == null)
                        {
                            rudness = 0;
                        }
                        if (attentive == null)
                        {
                            attentive = 0;
                        }
                        if (polite == null)
                        {
                            polite = 0;
                        }
                        a.cnic = v.cnic;
                        a.email = v.email;
                        a.firstName = v.firstName;
                        a.lastName = v.lastName;
                        a.role = v.role;
                        a.attentive = (double)attentive;
                        a.polite = (double)polite;
                        a.rudness = (double)rudness;
                        a.noOfAppointments = count;
                        adminDetails.Add(a);

                    }
                    var p = adminDetails[0];
                    for (int i = 0; i < adminDetails.Count; i++)
                    {
                        if (adminDetails[0].polite < adminDetails[i].polite)
                        {
                            p = adminDetails[i];
                        }
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, p);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Admin Persons Record Not Exist");
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetPoliteAdmin

        [HttpGet]
        public HttpResponseMessage GetAttentiveAdmin()
        {
            try
            {       //  /api/refer/GetAttentiveAdmin
                List<AdminDetailModel> adminDetails = new List<AdminDetailModel>();
                var adminlist = db.Admins.Where(h => h.role == "Admin").ToList<Admin>();
                if (adminlist != null)
                {
                    foreach (var v in adminlist)
                    {
                        AdminDetailModel a = new AdminDetailModel();
                        var rudness = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.rudness);
                        var attentive = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.attentive);
                        var polite = db.Feedbacks.Where(t => t.adminId == v.cnic).Average(t => t.polite);
                        var count = db.Histories.Where(t => t.adminId == v.cnic).ToList().Count();
                        if (rudness == null)
                        {
                            rudness = 0;
                        }
                        if (attentive == null)
                        {
                            attentive = 0;
                        }
                        if (polite == null)
                        {
                            polite = 0;
                        }
                        a.cnic = v.cnic;
                        a.email = v.email;
                        a.firstName = v.firstName;
                        a.lastName = v.lastName;
                        a.role = v.role;
                        a.attentive = (double)attentive;
                        a.polite = (double)polite;
                        a.rudness = (double)rudness;
                        a.noOfAppointments = count;
                        adminDetails.Add(a);

                    }
                    var p = adminDetails[0];
                    for (int i = 0; i < adminDetails.Count; i++)
                    {
                        if (adminDetails[0].attentive < adminDetails[i].attentive)
                        {
                            p = adminDetails[i];
                        }
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, p);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Admin Persons Record Not Exist");
                }

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end GetAttentiveAdmin


    }
}
