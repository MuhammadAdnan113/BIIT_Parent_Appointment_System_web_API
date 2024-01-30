using BIIT_Parent_Appointment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web.Http;

namespace BIIT_Parent_Appointment.Controllers
{
    public class UserController : ApiController
    {
        BIIT_Parent_AppointmentEntities db = new BIIT_Parent_AppointmentEntities();
        // SIGNUP============================
        [HttpPost]
        public HttpResponseMessage signUp(UserLogin l)
        {
            try
            {
                var user = db.UserLogins.Where(u=>u.userName==l.userName || u.email==l.email).FirstOrDefault<UserLogin>();
                if(user !=null)
                {
                    return Request.CreateResponse(HttpStatusCode.Ambiguous, "User Name or Email already Exist");
                }
                else
                {

                    var result = db.UserLogins.Where(b => b.cnic == l.cnic).FirstOrDefault();

                    if (result != null)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Parent with same CNIC already Exist");
                        
                    }
                    else
                    {
                        db.UserLogins.Add(l);
                        db.SaveChanges();
                        string code = sendOTP(l);
                        return Request.CreateResponse(HttpStatusCode.OK, "Parent account created and "+ code);
                    }
                    

                }// end Elseif

            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        } // end signUp

        // LOGIN===========================================

        [HttpGet]
        public HttpResponseMessage logIn(string username, string pass)
        {
            //  /api/user/login?username=Admin1&pass=12345
            try
            {
                var user = db.UserLogins.Where(a=>a.userName==username && a.password==pass).FirstOrDefault<UserLogin>();
                if (user != null)
                {
                    bool verify = checkVerify(username, pass, user.role);
                    if (verify)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, user);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Account not Verified");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User Not Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end login

        // CHECK VERIFY=====================================

        [HttpGet]
        public bool checkVerify(string username, string pass, string role)
        {
            try
            {
                var user = db.UserLogins.SqlQuery("Select * from UserLogin where userName='" + username + "' and password='" + pass + "' and role='"+role+"' and verify=1").FirstOrDefault<UserLogin>();
                if(user !=null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }// end login

        // SEND OTP =======================================

        [HttpPost]
        public string sendOTP(UserLogin l)
        {
            try
            {
                Random rnd = new Random();
                int otp = rnd.Next(1000, 9999);

                    String from, to, pass, messageBody;

                    MailMessage message = new MailMessage();
                    to = l.email;
                    from = "jampur5035@gmail.com";
                    pass = "vmdkoaqauwfqrhfw";
                    messageBody = "Your OTP is: "+otp;
                    message.To.Add(to);
                    message.From = new MailAddress(from);
                    message.Body = messageBody;
                    message.Subject = "BIIT Parent Appointment";
                    SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                
                smtp.UseDefaultCredentials = true;
                smtp.EnableSsl = true;
                smtp.Port = 587;
                    
                smtp.Credentials = new NetworkCredential(from, pass);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);

                OTP result = new OTP();
                  DateTime date = DateTime.Now;

                    string time = date.ToString("hh:mm:ss");

                    string[] t1 = time.Split(':');
                    int h = int.Parse(t1[0]);
                    int m = int.Parse(t1[1]);
                    int s = int.Parse(t1[2]);
                    m += 01;

                if (m > 60)
                {
                    m = 01;
                    m += 01;
                }
                    
                    string final = h + ":" + m + ":" + s;

                    result.code = otp;
                    result.email = l.email;
                    result.startTime = date.ToString("hh:mm:ss");
                    result.endTime = final;

                    db.OTPs.Add(result);
                    db.SaveChanges();
                    return "OTP Sent";
                }
                catch (Exception ex)
                {
                return ex.Message+" Check Network";
                }

            }// end sendOTP

        // VERIFY OTP ==============================================
        [HttpGet]
        public HttpResponseMessage verifyOTP(int code)
        {//  /api/user/verifyOTP?code=5331
            try
            {

                var result = db.OTPs.SingleOrDefault(b => b.code == code);
                if (result != null)
                {
                   string email= result.email;
                    
                    string start = result.startTime;
                    string end= result.endTime;
                    string[] st = start.Split(':');
                    string[] et = end.Split(':');

                    DateTime date = DateTime.Now;

                    string time = date.ToString("hh:mm:ss");
                    string[] t = time.Split(':');
                    int now =int.Parse(t[1]);

                    int em =int.Parse(et[1]);

                    
                    if (now<= em)
                    {
                        var result1 = db.UserLogins.Where(b => b.email == email && b.verify==0).FirstOrDefault();
                        if(result1!=null)
                        {
                            result1.verify = 1;

                            db.OTPs.Remove(result);
                            db.SaveChanges();
                            return Request.CreateResponse(HttpStatusCode.OK, "Account Verified");
                        }
                        db.OTPs.Remove(result);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Account already Verified");
                        
                        
                    }
                    else
                    {
                        db.OTPs.Remove(result);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "OTP Expired");
                    }

                   
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid OTP");
                }
                
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end verifyOTP

        // SEND OTP =======================================

        [HttpGet]
        public HttpResponseMessage resendOTP(String email)
        { //   /api/user/resendOTP?email=ma1422906@gmail.com
            try
            {
                var res = db.UserLogins.Where(b => b.email == email).FirstOrDefault();
                if(res !=null)
                {
                    if (res.verify == 0)
                    {
                        Random rnd = new Random();
                        int otp = rnd.Next(1000, 9999);

                        String from, to, pass, messageBody;

                        MailMessage message = new MailMessage();
                        to = email;
                        from = "jampur5035@gmail.com";
                        pass = "vmdkoaqauwfqrhfw";
                        messageBody = "Your OTP is: " + otp;
                        message.To.Add(to);
                        message.From = new MailAddress(from);
                        message.Body = messageBody;
                        message.Subject = "BIIT Parent Appointment OTP";
                        SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                        smtp.UseDefaultCredentials = true;
                        smtp.EnableSsl = true;
                        smtp.Port = 587;
                        smtp.Credentials = new NetworkCredential(from, pass);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Send(message);

                        OTP result = new OTP();
                        DateTime date = DateTime.Now;

                        string time = date.ToString("hh:mm:ss");

                        string[] t1 = time.Split(':');
                        int h = int.Parse(t1[0]);
                        int m = int.Parse(t1[1]);
                        int s = int.Parse(t1[2]);
                        m += 01;

                        if (m > 60)
                        {
                            m = 01;
                            m += 01;
                        }

                        string final = h + ":" + m + ":" + s;

                        result.code = otp;
                        result.userName = "";
                        result.email = email;
                        result.startTime = date.ToString("hh:mm:ss");
                        result.endTime = final;

                        db.OTPs.Add(result);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "OTP Sent on Email");
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "Account already Verified no need to send OTP");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Account Not Created on this Email");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

        }// end sendOTP

        // FORGET PASSWORD ==================================================

        [HttpGet]
        public HttpResponseMessage ForgetPassword(string email)
        {
            try
            {
                var user = db.UserLogins.SingleOrDefault(b => b.email == email);
                if (user != null)
                {
                    String from, to, pass, messageBody;

                    MailMessage message = new MailMessage();
                    to = email;
                    from = "jampur5035@gmail.com";
                    pass = "vmdkoaqauwfqrhfw";
                    messageBody = "BIIT Parent Appointment\nUser Name: " + user.userName+"\nPassword: "+user.password;
                    message.To.Add(to);
                    message.From = new MailAddress(from);
                    message.Body = messageBody;
                    message.Subject = "BIIT Parent Appointment";
                    SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                    smtp.UseDefaultCredentials = true;
                    smtp.EnableSsl = true;
                    smtp.Port = 587;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Credentials = new NetworkCredential(from, pass);
                    smtp.Send(message);


                    return Request.CreateResponse(HttpStatusCode.OK, "Email Sent with UserName and Password");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Account Not Exist");
                }
            }
            catch (Exception exp)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, exp.Message);
            }
        }// end ForgetPassword


    }// end controler
}
