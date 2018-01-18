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
using Ryan_BugTracker.Models.Helpers;

namespace Ryan_BugTracker.Controllers
{
    [RequireHttps]
    public class ProjectsController : UserNames
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        //// GET: Projects
        //public ActionResult Index()
        //{
        //    return View(db.Projects.ToList());
        //}

        // GET: Projects
        [Authorize]
        public ActionResult ProjectList()
        {
            ProjectAssignmentsHelper ph = new ProjectAssignmentsHelper(db);

            IList<Project> projects = new List<Project>();

            if (User.IsInRole("Administrator"))
            {
                projects = db.Projects.ToList();
            }
            else
            {
                projects = ph.ListUserProjects(User.Identity.GetUserId());
            }

            return View(projects);
        }

        // GET: Projects/Details/5
        [Authorize(Roles = "Administrator, Project Manager, Developer")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Roles = "Administrator, Project Manager")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Project Manager")]
        public ActionResult Create([Bind(Include = "Id,Created,Updated,Title,Body,AuthorUserId")] Project project)
        {
            if (ModelState.IsValid)
            {
                var id = User.Identity.GetUserId();
                ApplicationUser projectuser = db.Users.FirstOrDefault(u => u.Id.Equals(id));

                if (String.IsNullOrWhiteSpace(project.Title))
                {
                    ModelState.AddModelError("Title", "Invalid title.");
                    return View(project);
                }
                if (db.Projects.Any(p => p.Title == project.Title))
                {
                    ModelState.AddModelError("Title", "The title must be unique.");
                    return View(project);
                }

                project.AuthorUserId = projectuser.DisplayName;
                project.Created = System.DateTime.Now;
                db.Projects.Add(project);
                db.SaveChanges();

                ApplicationUser attachUser = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
                Project attachProject = db.Projects.Find(project.Id);
                ProjectAssignmentsHelper helper = new ProjectAssignmentsHelper(db);
                helper.AddUserToProject(attachUser.Id, attachProject.Id);

                return RedirectToAction("ProjectList");
                
            }

            return View(project);
        }

        // GET: Projects/Edit/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult Edit([Bind(Include = "Id,Created,Updated,Title,Body")] Project project)
        {
            if (ModelState.IsValid)
            {
                project.Updated = System.DateTime.Now;
                db.Projects.Attach(project);
                db.Entry(project).Property("Title").IsModified = true;
                db.Entry(project).Property("Body").IsModified = true;
                db.Entry(project).Property("Updated").IsModified = true;
                //db.Entry(project).State = EntityState.Modified;

                db.SaveChanges();
                return RedirectToAction("ProjectList");
            }
            return View(project);
        }

        // GET: Projects/Delete/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteConfirmed(int id)
        {
            Project project = db.Projects.Find(id);
            db.Projects.Remove(project);
            db.SaveChanges();
            return RedirectToAction("ProjectList");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // GET: Projects/EditProjectAssignments
        [Authorize(Roles = "Administrator, Project Manager")]
        public ActionResult EditProjectAssignments(int id)

        {
            var project = db.Projects.Find(id);
            ProjectAssignmentsHelper helper = new ProjectAssignmentsHelper(db);
            var model = new ProjectUserViewModels();
            model.Project = project;
            model.SelectedUsers = helper.ListUsersOnProject(id).Select(u => u.Id).ToArray();
            model.Users = new MultiSelectList(db.Users, "Id", "DisplayName", model.SelectedUsers);

            return View(model);
        }

        // Post: Projects/EditProjectAssignments
        [HttpPost]
        [Authorize(Roles = "Administrator, Project Manager")]
        public ActionResult EditProjectAssignments(ProjectUserViewModels model)
        {

            var project = db.Projects.Find(model.Project.Id);
            ProjectAssignmentsHelper helper = new ProjectAssignmentsHelper(db);
            project.Updated = System.DateTime.Now;
            db.Entry(project).Property("Updated").IsModified = true;

            foreach (var user in db.Users.Select(u => u.Id).ToList())
            {
                helper.RemoveUserFromProject(user, project.Id);
            }

            if (model.SelectedUsers != null)
            {
                foreach (var user in model.SelectedUsers)
                {
                    helper.AddUserToProject(user, project.Id);
                }

                return RedirectToAction("ProjectList", "Projects");
            }

            else
            {
                return RedirectToAction("ProjectList", "Projects");
            }

        }
    }
}
