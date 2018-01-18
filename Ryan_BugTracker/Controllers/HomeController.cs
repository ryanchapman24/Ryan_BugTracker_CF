using Microsoft.AspNet.Identity;
using Ryan_BugTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Ryan_BugTracker.Controllers
{
    [RequireHttps]
    public class HomeController : UserNames
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Error()
        {
            return View();
        }

        public ActionResult Landing()
        {

            return View();
        }

        [Authorize]
        public ActionResult DismissNotification (int? tId, int? nId, bool dA)
        {
            var user = User.Identity.GetUserId();
            var notification = db.Notifications.Find(nId);
            var ticket = db.Tickets.Find(tId);

            if (dA && nId == null && tId == null)
            {
                var notes = db.Notifications.Where(u => u.NotifyUserId == user);
                foreach (var note in notes)
                {
                    db.Notifications.Remove(note);
                }
                db.SaveChanges();
                return Redirect(HttpContext.Request.UrlReferrer.AbsoluteUri);            
            }

            if (ticket != null)
            {
                db.Notifications.Remove(notification);
                db.SaveChanges();
                return RedirectToAction("Details", "Tickets", new { id = ticket.Id });
            }
            else
            {
                db.Notifications.Remove(notification);
                db.SaveChanges();
                return Redirect(HttpContext.Request.UrlReferrer.AbsoluteUri);
            }
        }

        [Authorize]
        public ActionResult Index(int? id)
        {
            //if (id != null)
            //{
            //    //var project = db.Projects.Find(id);
            //    //var tuser = db.Users.Find(User.Identity.GetUserId());
            //    //var plist = project.Tickets.ToList();
            //    ViewBag.Tickets = db.Tickets.Where(u => u.ProjectId == id).ToList();
            //    var pList = db.Projects.Where(u => u.Id == id).ToList();

            //    return View(pList);
            //}

            var uId = User.Identity.GetUserId();
            ApplicationUser user = new ApplicationUser();
            user = db.Users.Find(uId);

            if (User.IsInRole("Administrator"))
            {
                ViewBag.tickets = db.Tickets.ToList();
            }
            else if(User.IsInRole("Project Manager") || User.IsInRole("Developer"))
            {
                var userTickets = db.Tickets.Where(t => t.AssignedToUserId.Equals(uId) || t.AuthorUserId.Equals(uId)).ToList();
                var projectTickets = user.Projects.SelectMany(p => p.Tickets).ToList();
                ViewBag.tickets = userTickets.Union(projectTickets).ToList();
            }
            else
            { 
                ViewBag.tickets = db.Tickets.Where(t => t.AuthorUserId.Equals(uId)).ToList();
            }

            return View(db.Projects.ToList());
        }

        [Authorize]
        public ActionResult ProfilePage(string id)
        {         
            if (!string.IsNullOrWhiteSpace(id))
            {
                var userCheck = db.Users.Find(id);
                if (userCheck != null)
                {
                    var roleListCheck = new List<string>();
                    foreach (var role in userCheck.Roles)
                    {
                        var roleName = db.Roles.First(u => u.Id == role.RoleId);
                        roleListCheck.Add(roleName.Name);
                    }
                    ViewBag.RoleList = roleListCheck.ToList();

                    var userA = userCheck.Id;                
                    var activitiesA = db.TicketHistories.Where(a => a.UserId == userA).OrderByDescending(b=>b.Changed);
                    ViewBag.Activities = activitiesA.ToList();

                    var ticketsA = db.Tickets.Where(u => u.AssignedToUserId == userA);
                    ViewBag.Tickets = ticketsA.ToList();
                
                    return View(userCheck);
                }

            }           

            var user = db.Users.Find(User.Identity.GetUserId());
            var roleList = new List<string>();
            foreach (var role in user.Roles)
            {
                var roleName = db.Roles.First(u => u.Id == role.RoleId);
                roleList.Add(roleName.Name);
            }
            ViewBag.RoleList = roleList.ToList();

            var userAc = User.Identity.GetUserId();
            var activitiesAc = db.TicketHistories.Where(a => a.UserId == userAc).OrderByDescending(b => b.Changed);
            ViewBag.Activities = activitiesAc.ToList();

            var ticketsAc = db.Tickets.Where(u => u.AssignedToUserId == userAc);
            ViewBag.Tickets = ticketsAc.ToList();

            return View(user);
        }

        [Authorize]
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        [Authorize]
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}