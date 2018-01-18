using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Ryan_BugTracker.Models;
using Microsoft.AspNet.Identity;
using System.IO;
using Microsoft.AspNet.Identity.Owin;
using System.Threading.Tasks;

namespace Ryan_BugTracker.Controllers
{
    [RequireHttps]
    public class TicketsController : UserNames
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private ApplicationUserManager _userManager;

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
          

        // GET: Tickets
        [Authorize(Roles = "Administrator")]
        public ActionResult Index()
        {
            var tickets = db.Tickets.Include(t => t.AssignedToUser).Include(t => t.AuthorUser).Include(t => t.Project).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType);
            return View(tickets.ToList());
        }

        // GET: Tickets/Details/5
        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }

        // GET: Tickets/Create
        [Authorize]
        public ActionResult Create(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var project = db.Projects.Find(id);

            if (project == null)
            {
                return HttpNotFound();
            }


            var user = db.Users.Find(User.Identity.GetUserId());

            ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "FirstName");
            ViewBag.ProjectName = project.Title;
            Ticket ticket = new Ticket();
            ticket.ProjectId = project.Id;
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name");
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name");
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name");
            return View(ticket);
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Create(IEnumerable<HttpPostedFileBase> files, [Bind(Include = "Id,Title,Body,ProjectId,TicketTypeId,TicketPriorityId")] Ticket ticket)
        {            
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
            var userid = user.Id;
            var changed = System.DateTime.Now;

            if (ModelState.IsValid)
            {
               
                ticket.AuthorUserId = User.Identity.GetUserId();
                ticket.Created = System.DateTime.Now;
                ticket.TicketStatusId = 1;
                db.Tickets.Add(ticket);
                db.SaveChanges();

                TicketAttachment attachment = new TicketAttachment();

                var path = Server.MapPath("~/TicketAttachments/" + ticket.Id);
                Directory.CreateDirectory(path);
               
                foreach (var file in files)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        attachment.TicketId = ticket.Id;

                        file.SaveAs(Path.Combine(Server.MapPath("~/TicketAttachments/" + attachment.TicketId), Path.GetFileName(file.FileName)));
                        attachment.FileUrl = file.FileName;
                        
                        attachment.AuthorUserId = User.Identity.GetUserId();
                        attachment.Created = System.DateTime.Now;
                        db.TicketAttachments.Add(attachment);
                        db.SaveChanges();
                    }
                }
                TicketHistory th = new TicketHistory
                {
                    TicketId = ticket.Id,
                    Property = "Ticket Created",
                    Dialogue = "created",
                    OldValue = ticket.Title,
                    NewValue = ticket.Body,
                    Changed = changed, //system date
                    UserId = userid, //current userId
                };
                db.TicketHistories.Add(th);
                db.SaveChanges();

                return RedirectToAction("Index", "Home");
            }

            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "FirstName", ticket.AssignedToUserId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Title", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);           

            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Created,Updated,Title,Body,AuthorUserId,AssignedToUserId,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId")] Ticket ticket)
        {
            Ticket oldTic = db.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == ticket.Id);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
            var userid = user.Id;
            var changed = System.DateTime.Now;
            var changes = "";

            if (ModelState.IsValid)
            {
                ticket.Updated = System.DateTime.Now;
                db.Tickets.Attach(ticket);
                db.Entry(ticket).Property("Title").IsModified = true;
                db.Entry(ticket).Property("Body").IsModified = true;
                db.Entry(ticket).Property("Updated").IsModified = true;
                db.Entry(ticket).Property("AssignedToUserId").IsModified = true;
                db.Entry(ticket).Property("ProjectId").IsModified = true;
                db.Entry(ticket).Property("TicketTypeId").IsModified = true;
                db.Entry(ticket).Property("TicketPriorityId").IsModified = true;
                db.Entry(ticket).Property("TicketStatusId").IsModified = true;               

                db.SaveChanges();

                if (oldTic?.Title != ticket.Title)
                {
                    TicketHistory th1 = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = "Ticket Updated (Name)",
                        Dialogue = "name",
                        OldValue = oldTic?.Title,
                        NewValue = ticket.Title,
                        Changed = changed, //system date
                        UserId = userid, //current userId
                    };
                    changes = changes + "Name<br />";
                    db.TicketHistories.Add(th1);
                }

                if (oldTic?.Body != ticket.Body)
                {
                    TicketHistory th2 = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = "Ticket Updated (Description)",
                        Dialogue = "description",
                        OldValue = oldTic?.Body,
                        NewValue = ticket.Body,
                        Changed = changed, //system date
                        UserId = userid, //current userId
                    };
                    changes = changes + "Description<br />";
                    db.TicketHistories.Add(th2);
                }

                if (oldTic?.ProjectId != ticket.ProjectId)
                {
                    TicketHistory th3 = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = "Ticket Updated (Project)",
                        Dialogue = "project",
                        OldValue = db.Projects.Find(oldTic?.ProjectId).Title,
                        NewValue = db.Projects.Find(ticket.ProjectId).Title,
                        Changed = changed, //system date
                        UserId = userid, //current userId
                    };
                    changes = changes + "Project<br />";
                    db.TicketHistories.Add(th3);
                }

                if (oldTic?.TicketTypeId != ticket.TicketTypeId)
                {
                    TicketHistory th4 = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = "Ticket Updated (Type)",
                        Dialogue = "type",
                        OldValue = db.TicketTypes.Find(oldTic?.TicketTypeId).Name,
                        NewValue = db.TicketTypes.Find(ticket.TicketTypeId).Name,
                        Changed = changed, //system date
                        UserId = userid, //current userId
                    };
                    changes = changes + "Type<br />";
                    db.TicketHistories.Add(th4);
                }

                if (oldTic?.TicketPriorityId != ticket.TicketPriorityId)
                {
                    TicketHistory th5 = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = "Ticket Updated (Priority)",
                        Dialogue = "priority",
                        OldValue = db.TicketPriorities.Find(oldTic?.TicketPriorityId).Name,
                        NewValue = db.TicketPriorities.Find(ticket.TicketPriorityId).Name,
                        Changed = changed, //system date
                        UserId = userid, //current userId
                    };
                    changes = changes + "Priority<br />";
                    db.TicketHistories.Add(th5);
                }

                if (oldTic?.TicketStatusId != ticket.TicketStatusId)
                {
                    TicketHistory th6 = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = "Ticket Updated (Status)",
                        Dialogue = "status",
                        OldValue = db.TicketStatuses.Find(oldTic?.TicketStatusId).Name,
                        NewValue = db.TicketStatuses.Find(ticket.TicketStatusId).Name,
                        Changed = changed, //system date
                        UserId = userid, //current userId
                    };
                    changes = changes + "Status<br />";
                    db.TicketHistories.Add(th6);
                }
                db.SaveChanges();

                if (ticket.AssignedToUser != null)
                {
                    if (oldTic?.Title != ticket.Title || oldTic?.Body != ticket.Body || oldTic?.ProjectId != ticket.ProjectId || oldTic?.TicketPriorityId != ticket.TicketPriorityId || oldTic?.TicketTypeId != ticket.TicketTypeId || oldTic?.TicketStatusId != ticket.TicketStatusId)
                    {
                        var userToNotify = await UserManager.FindByNameAsync(ticket.AssignedToUser.Email);
                        if (userToNotify != null)
                        {
                            // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                            // Send an email with this link       
                            var callbackUrl = Url.Action("Details", "Tickets", new { id = ticket.Id }, protocol: Request.Url.Scheme);
                            await UserManager.SendEmailAsync(userToNotify.Id, "Ticket Updated: " + ticket.Title, "This ticket has been updated with respect to its:<br /><br />" + changes + "<br />" + "Please click <a href=\"" + callbackUrl + "\">here</a> to view the ticket.");
                        }
                    }
               
                    if (oldTic?.Title != ticket.Title || oldTic?.Body != ticket.Body || oldTic?.ProjectId != ticket.ProjectId || oldTic?.TicketPriorityId != ticket.TicketPriorityId || oldTic?.TicketTypeId != ticket.TicketTypeId || oldTic?.TicketStatusId != ticket.TicketStatusId)
                    {
                        Notification n1 = new Notification
                        {
                            TicketId = ticket.Id,
                            Description = ticket.Title + " has been modified.",
                            Type = "Modification",
                            Created = System.DateTimeOffset.Now,
                            NotifyUserId = ticket.AssignedToUserId, //assigned userId
                        };
                        db.Notifications.Add(n1);
                        db.SaveChanges();
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "FirstName", ticket.AssignedToUserId);
            ViewBag.AuthorUserId = new SelectList(db.Users, "Id", "FirstName", ticket.AuthorUserId);
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Title", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // GET: Tickets/Delete/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteConfirmed(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            db.Tickets.Remove(ticket);
            db.SaveChanges();
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // GET: Tickets/EditProjectAssignments
        [Authorize(Roles = "Administrator, Project Manager")]
        public ActionResult EditTicketAssignments(int? id)

        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            ApplicationDbContext context = new ApplicationDbContext();

            Project project = db.Projects.Find(ticket.ProjectId);
            var projectUsers = context.Users.Where(u => u.Projects.Any(p => p.Title == project.Title));

            var role = context.Roles.SingleOrDefault(u => u.Name == "Developer");
            var usersInRole = context.Users.Where(u => u.Roles.Any(r => (r.RoleId == role.Id)));

            var displayUsers = usersInRole.Where(u => u.Projects.Any(p => (p.Title == project.Title)));

            ViewBag.AssignedToUserId = new SelectList(displayUsers, "Id", "DisplayName", ticket.AssignedToUserId);

            return View(ticket);
        }

        // Post: Tickets/EditProjectAssignments
        [HttpPost]
        [Authorize(Roles = "Administrator, Project Manager")]
        public async Task<ActionResult> EditTicketAssignments([Bind(Include = "Id,Created,Updated,Title,Body,AuthorUserId,AssignedToUserId,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId")]Ticket ticket)
        {
            Ticket oldTic = db.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == ticket.Id);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
            var userid = user.Id;
            var changed = System.DateTime.Now;

            if (ModelState.IsValid)
            {
                ticket.Updated = System.DateTime.Now;
                ticket.TicketStatusId = 2;
                db.Tickets.Attach(ticket);
                db.Entry(ticket).Property("Updated").IsModified = true;
                db.Entry(ticket).Property("AssignedToUserId").IsModified = true;
                db.Entry(ticket).Property("TicketStatusId").IsModified = true;

                db.SaveChanges();

                if (oldTic?.AssignedToUserId != null)
                {
                    ApplicationUser olduser = db.Users.FirstOrDefault(u => u.Id.Equals(oldTic.AssignedToUserId));
                    ApplicationUser newuser = db.Users.FirstOrDefault(u => u.Id.Equals(ticket.AssignedToUserId));

                    if (oldTic?.AssignedToUserId != ticket.AssignedToUserId)
                    {
                        TicketHistory th7 = new TicketHistory
                        {
                            TicketId = ticket.Id,
                            Property = "New Assignment",
                            Dialogue = "assigned developer",
                            OldValue = olduser.DisplayName,
                            NewValue = newuser.DisplayName,
                            Changed = changed, //system date
                            UserId = userid, //current userId
                        };
                        db.TicketHistories.Add(th7);
                    }
                }

                else
                {
                    ApplicationUser newuser = db.Users.FirstOrDefault(u => u.Id.Equals(ticket.AssignedToUserId));

                    TicketHistory th7 = new TicketHistory
                        {
                            TicketId = ticket.Id,
                            Property = "New Assignment",
                            Dialogue = "assigned user",
                            NewValue = newuser.DisplayName,
                            Changed = changed, //system date
                            UserId = userid, //current userId
                        };
                        db.TicketHistories.Add(th7);
                }

                if (oldTic?.TicketStatusId != ticket.TicketStatusId)
                {
                    TicketHistory th8 = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = "Ticket Updated (Status)",
                        Dialogue = "status",
                        OldValue = db.TicketStatuses.Find(oldTic?.TicketStatusId).Name,
                        NewValue = db.TicketStatuses.Find(ticket.TicketStatusId).Name,
                        Changed = changed, //system date
                        UserId = userid, //current userId
                    };
                    db.TicketHistories.Add(th8);
                }
                db.SaveChanges();
            }

            if (oldTic?.AssignedToUserId == null || oldTic?.AssignedToUserId != ticket.AssignedToUserId)
            {
                var userToNotify = await UserManager.FindByNameAsync(ticket.AssignedToUser.Email);
                if (userToNotify != null)
                {
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link          
                    var callbackUrl = Url.Action("Details", "Tickets", new { id = ticket.Id }, protocol: Request.Url.Scheme);
                    await UserManager.SendEmailAsync(userToNotify.Id, "NEW Ticket Assignment", "You've been assigned to a new ticket!  Please click <a href=\"" + callbackUrl + "\">here</a> to view it.");
                }
            }

            if (oldTic?.AssignedToUserId == null || oldTic?.AssignedToUserId != ticket.AssignedToUserId)
            {
                Notification n = new Notification
                {
                    TicketId = ticket.Id,
                    Description = "NEW Assignment: " + ticket.Title,
                    Type = "Assignment",
                    Created = System.DateTimeOffset.Now,
                    NotifyUserId = ticket.AssignedToUserId, //assigned userId
                };
                db.Notifications.Add(n);
                db.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> CreateComment(TicketComment comment)
        {            
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
            var userid = user.Id;
            var changed = System.DateTime.Now;

            if (ModelState.IsValid)
            {
                var ticket = db.Tickets.Find(comment.TicketId);
                ticket.Updated = System.DateTime.Now;               
                db.Entry(ticket).Property("Updated").IsModified = true;

                comment.AuthorUserId = User.Identity.GetUserId();
                comment.Created = System.DateTime.Now;
                db.TicketComments.Add(comment);
                db.SaveChanges();

                TicketHistory th9 = new TicketHistory
                {
                    TicketId = ticket.Id,
                    Property = "New Comment",
                    Dialogue = "new comment",           
                    NewValue = comment.Body,
                    Changed = changed, //system date
                    UserId = userid, //current userId
                };
                db.TicketHistories.Add(th9);
                db.SaveChanges();

                if (ticket.AssignedToUser != null)
                {
                    var userToNotify = await UserManager.FindByNameAsync(ticket.AssignedToUser?.Email);
                    if (userToNotify != null)
                    {
                        // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                        // Send an email with this link       
                        var callbackUrl = Url.Action("Details", "Tickets", new { id = ticket.Id }, protocol: Request.Url.Scheme);
                        await UserManager.SendEmailAsync(userToNotify.Id, "NEW Ticket Comment: " + ticket.Title, "A new comment has been added to this ticket:<br /><br /><em>" + '"' + comment.Body + '"' + "<br /><br /></em>" + "Please click <a href=\"" + callbackUrl + "\">here</a> to view the ticket.");
                    }

                    Notification n2 = new Notification
                    {
                        TicketId = ticket.Id,
                        Description = "New comment for " + ticket.Title,
                        Type = "Comment",
                        Created = System.DateTimeOffset.Now,
                        NotifyUserId = ticket.AssignedToUserId, //assigned userId
                    };
                    db.Notifications.Add(n2);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Details", "Tickets", new { id = comment.TicketId });
        }

        // GET: Tickets/EditComment/5
        [Authorize(Roles = "Administrator")]
        public ActionResult EditComment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketComment comment = db.TicketComments.Find(id);
            if (comment == null)
            {
                return HttpNotFound();
            }
            return View(comment);
        }

        // POST: Tickets/EditComment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult EditComment(TicketComment comment, int ticketid)
        {
            TicketComment oldCom = db.TicketComments.AsNoTracking().FirstOrDefault(c => c.Id == comment.Id);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
            var userid = user.Id;
            var changed = System.DateTime.Now;

            if (ModelState.IsValid)
            {
                var ticket = db.Tickets.Find(comment.TicketId);
                ticket.Updated = System.DateTime.Now;
                db.Entry(ticket).Property("Updated").IsModified = true;

                db.TicketComments.Attach(comment);
                db.Entry(comment).Property("Body").IsModified = true;
                db.Entry(comment).Property("Updated").IsModified = true;
                comment.Updated = System.DateTime.Now;
                db.SaveChanges();

                TicketHistory th10 = new TicketHistory
                {
                    TicketId = ticket.Id,
                    Property = "Comment Edited",
                    Dialogue = "comment was edited",
                    OldValue = oldCom?.Body,
                    NewValue = comment.Body,
                    Changed = changed, //system date
                    UserId = userid, //current userId
                };
                db.TicketHistories.Add(th10);
                db.SaveChanges();

                return RedirectToAction("Details", "Tickets", new { id = ticketid });
            }
            return View(comment);
        }

        // GET: Tickets/DeleteComment/5
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteComment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketComment comment = db.TicketComments.Find(id);
            if (comment == null)
            {
                return HttpNotFound();
            }
            return View(comment);
        }

        // POST: Tickets/DeleteComment/5
        [HttpPost, ActionName("DeleteComment")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteCommentConfirmed(int? id, int ticketid)
        {
            var ticket = db.Tickets.Find(ticketid);
            ticket.Updated = System.DateTime.Now;
            db.Entry(ticket).Property("Updated").IsModified = true;

            TicketComment comment = db.TicketComments.Find(id);
            var delCom = comment.Body;
            db.TicketComments.Remove(comment);
            db.SaveChanges();

            TicketComment oldCom = db.TicketComments.AsNoTracking().FirstOrDefault(c => c.Id == comment.Id);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
            var userid = user.Id;
            var changed = System.DateTime.Now;

            TicketHistory th11 = new TicketHistory
            {
                TicketId = ticket.Id,
                Property = "Comment Deleted",
                Dialogue = "A comment was deleted",
                OldValue = delCom,             
                Changed = changed, //system date
                UserId = userid, //current userId
            };
            db.TicketHistories.Add(th11);
            db.SaveChanges();

            return RedirectToAction("Details", "Tickets", new { id = ticketid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> CreateAttachment(IEnumerable<HttpPostedFileBase> files, TicketAttachment attachment)
        {
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
            var userid = user.Id;
            var changed = System.DateTime.Now;
            var attachments = "";

            if (ModelState.IsValid)
            {
                var ticket = db.Tickets.Find(attachment.TicketId);
                ticket.Updated = System.DateTime.Now;
                db.Entry(ticket).Property("Updated").IsModified = true;

                foreach (var file in files)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        file.SaveAs(Path.Combine(Server.MapPath("~/TicketAttachments/" + attachment.TicketId), Path.GetFileName(file.FileName)));
                        attachment.FileUrl = file.FileName;

                        attachment.AuthorUserId = User.Identity.GetUserId();
                        attachment.Created = System.DateTime.Now;                       
                        db.TicketAttachments.Add(attachment);                       
                        db.SaveChanges();

                        TicketHistory th12 = new TicketHistory
                        {
                            TicketId = ticket.Id,
                            Property = "New Attachment",
                            Dialogue = "new attachment",
                            NewValue = attachment.FileUrl,
                            Changed = changed, //system date
                            UserId = userid, //current userId
                        };
                        attachments = attachments + attachment.FileUrl + "<br />";
                        db.TicketHistories.Add(th12);
                        db.SaveChanges();

                    }
                }
                if (ticket.AssignedToUser != null)
                {
                    var userToNotify = await UserManager.FindByNameAsync(ticket.AssignedToUser?.Email);
                    if (userToNotify != null)
                    {
                        // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                        // Send an email with this link       
                        var callbackUrl = Url.Action("Details", "Tickets", new { id = ticket.Id }, protocol: Request.Url.Scheme);
                        await UserManager.SendEmailAsync(userToNotify.Id, "NEW Ticket Attachment(s): " + ticket.Title, "New attachments have been added to this ticket:<br /><br />" + attachments + "<br />" + "Please click <a href=\"" + callbackUrl + "\">here</a> to view the ticket.");
                    }

                    Notification n3 = new Notification
                    {
                        TicketId = ticket.Id,
                        Description = "New attachment for " + ticket.Title,
                        Type = "Attachment",
                        Created = System.DateTimeOffset.Now,
                        NotifyUserId = ticket.AssignedToUserId, //assigned userId
                    };
                    db.Notifications.Add(n3);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Details", "Tickets", new { id = attachment.TicketId });
        }

        // GET: Tickets/DeleteAttachment/5
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteAttachment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketAttachment attachment = db.TicketAttachments.Find(id);
            if (attachment == null)
            {
                return HttpNotFound();
            }
            return View(attachment);
        }

        // POST: Tickets/DeleteAttachment/5
        [HttpPost, ActionName("DeleteAttachment")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteAttachmentConfirmed(int? id, int ticketid)
        {
            var ticket = db.Tickets.Find(ticketid);
            ticket.Updated = System.DateTime.Now;
            db.Entry(ticket).Property("Updated").IsModified = true;

            TicketAttachment attachment = db.TicketAttachments.Find(id);
            var delFile = attachment.FileUrl;
            db.TicketAttachments.Remove(attachment);
            db.SaveChanges();

            TicketAttachment oldAttach = db.TicketAttachments.AsNoTracking().FirstOrDefault(a => a.Id == attachment.Id);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
            var userid = user.Id;
            var changed = System.DateTime.Now;           

            TicketHistory th13 = new TicketHistory
            {
                TicketId = ticket.Id,
                Property = "Attachment Deleted",
                Dialogue = "An attachment was deleted",
                OldValue = delFile,               
                Changed = changed, //system date
                UserId = userid, //current userId
            };
            db.TicketHistories.Add(th13);
            db.SaveChanges();

            return RedirectToAction("Details", "Tickets", new { id = ticketid });
        }
    }
}
