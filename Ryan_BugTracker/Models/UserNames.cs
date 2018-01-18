using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Ryan_BugTracker.Models
{
    public class UserNames : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (User.Identity.IsAuthenticated)
            {
                ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
                //var user = db.Users.Find(User.Identity.GetUserId());
                ViewBag.Notifications = user.Notifications.OrderByDescending(c => c.Created).ToList();
                ViewBag.ProfilePic = user.ProfilePic;
                ViewBag.DisplayName = user.DisplayName;
                ViewBag.FirstName = user.FirstName;
                ViewBag.LastName = user.LastName;
                base.OnActionExecuting(filterContext);
            }
        }
    }
}