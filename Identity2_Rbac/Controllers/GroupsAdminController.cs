using Identity2_Rbac.Models;
using Identity2_Rbac.Utility;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Identity2_Rbac.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GroupsAdminController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private ApplicationGroupManager _groupManager;
        public ApplicationGroupManager GroupManager
        {
            get
            {
                return _groupManager ?? new ApplicationGroupManager();
            }
            private set
            {
                _groupManager = value;
            }
        }

        private ApplicationRoleManager _roleManager;
        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext()
                    .Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }


        public async Task<ActionResult> Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.idSortParm = String.IsNullOrEmpty(sortOrder) ? "Id_desc" : "";
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.DescriptionSortParm = sortOrder == "description" ? "description_desc" : "description";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var groups = from g in this.GroupManager.Groups
                         select g;

            if (!String.IsNullOrEmpty(searchString))
            {
                groups = groups.Where(g => g.Name.Contains(searchString)
                                       || g.Description.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    groups = groups.OrderByDescending(g => g.Name);
                    break;
                case "name":
                    groups = groups.OrderBy(g => g.Name);
                    break;
                case "description_desc":
                    groups = groups.OrderByDescending(g => g.Description);
                    break;
                case "description":
                    groups = groups.OrderBy(g => g.Description);
                    break;
                case "Id_desc":
                    groups = groups.OrderByDescending(g => g.Id);
                    break;
                default:
                    groups = groups.OrderBy(g => g.Id);
                    break;
            }

            int pageSize;
            int.TryParse(WebConfigurationManager.AppSettings["pageSize"], out pageSize);
            int pageNumber = (page ?? 1);
            return View(await groups.ToPagedListAsync(pageNumber, pageSize));
        }


        public async Task<ActionResult> Details(int id)
        {
            if (id > 0)
            {
                ApplicationGroup applicationgroup =
                await this.GroupManager.Groups.FirstOrDefaultAsync(g => g.Id == id);
                if (applicationgroup == null)
                {
                    return HttpNotFound();
                }
                var groupRoles = this.GroupManager.GetGroupRoles(applicationgroup.Id);
                string[] RoleNames = groupRoles.Select(p => p.Name).ToArray();
                ViewBag.RolesList = RoleNames;
                ViewBag.RolesCount = RoleNames.Count();
                return View(applicationgroup);
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }


        public ActionResult Create()
        {
            //Get a SelectList of Roles to choose from in the View:
            ViewBag.RolesList = new SelectList(
                this.RoleManager.Roles.ToList(), "Id", "Name");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            [Bind(Include = "Name,Description")] ApplicationGroup applicationgroup, params string[] selectedRoles)
        {
            if (ModelState.IsValid)
            {
                // Create the new Group:
                var result = await this.GroupManager.CreateGroupAsync(applicationgroup);
                if (result.Succeeded)
                {
                    selectedRoles = selectedRoles ?? new string[] { };

                    // Add the roles selected:
                    await this.GroupManager.SetGroupRolesAsync(applicationgroup.Id, selectedRoles);
                }
                return RedirectToAction("Index");
            }

            // Otherwise, start over:
            ViewBag.RoleId = new SelectList(
                this.RoleManager.Roles.ToList(), "Id", "Name");
            return View(applicationgroup);
        }


        //public async Task<ActionResult> Edit(string id)
        public async Task<ActionResult> Edit(int id)
        {
            if (id > 0)
            {
                ApplicationGroup applicationgroup = await this.GroupManager.FindByIdAsync(id);
                if (applicationgroup == null)
                {
                    return HttpNotFound();
                }

                // Get a list, not a DbSet or queryable:
                var allRoles = await this.RoleManager.Roles.ToListAsync();
                var groupRoles = await this.GroupManager.GetGroupRolesAsync(id);

                var model = new GroupViewModel()
                {
                    Id = applicationgroup.Id,
                    Name = applicationgroup.Name,
                    Description = applicationgroup.Description
                };

                // load the roles/Roles for selection in the form:
                foreach (var Role in allRoles)
                {
                    var listItem = new SelectListItem()
                    {
                        Text = Role.Name,
                        Value = Role.Id.ToString(),
                        Selected = groupRoles.Any(g => g.Id == Role.Id)
                    };
                    model.RolesList.Add(listItem);
                }
                return View(model);    
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            [Bind(Include = "Id,Name,Description")] GroupViewModel model, params string[] selectedRoles)
        {
            var group = await this.GroupManager.FindByIdAsync(model.Id);
            if (group == null)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                group.Name = model.Name;
                group.Description = model.Description;
                await this.GroupManager.UpdateGroupAsync(group);

                selectedRoles = selectedRoles ?? new string[] { };
                await this.GroupManager.SetGroupRolesAsync(group.Id, selectedRoles);
                return RedirectToAction("Index");
            }
            return View(model);
        }


        //public async Task<ActionResult> Delete(string id)
        public async Task<ActionResult> Delete(int id)
        {
            if (id > 0)
            {
                ApplicationGroup applicationgroup = await this.GroupManager.FindByIdAsync(id);
                if (applicationgroup == null)
                {
                    return HttpNotFound();
                }
                return View(applicationgroup);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            if (id > 0)
            {
                ApplicationGroup applicationgroup = await this.GroupManager.FindByIdAsync(id);
                await this.GroupManager.DeleteGroupAsync(id);
                return RedirectToAction("Index");    
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
