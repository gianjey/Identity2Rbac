using Identity2_Rbac.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Configuration;
using Identity2_Rbac.Utility;

namespace Identity2_Rbac.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersAdminController : Controller
    {
        public UsersAdminController()
        {
        }

        public UsersAdminController(ApplicationUserManager userManager, 
            ApplicationRoleManager roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
        }

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // Add the Group Manager (NOTE: only access through the public
        // Property, not by the instance variable!)
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
            ViewBag.UserNameSortParm = sortOrder == "username" ? "username_desc" : "username";
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.SurNameSortParam = sortOrder == "surname" ? "surname_desc" : "surname";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var users = from u in UserManager.Users
                        select u;
            if (!String.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.UserName.Contains(searchString)
                                       || u.Name.Contains(searchString)
                                       || u.SurName.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "username_desc":
                    users = users.OrderByDescending(u => u.UserName);
                    break;
                case "username":
                    users = users.OrderBy(u => u.UserName);
                    break;
                case "name_desc":
                    users = users.OrderByDescending(u => u.Name);
                    break;
                case "name":
                    users = users.OrderBy(u => u.Name);
                    break;
                case "surname_desc":
                    users = users.OrderByDescending(u => u.SurName);
                    break;
                case "surname":
                    users = users.OrderBy(u => u.SurName);
                    break;
                case "Id_desc":
                    users = users.OrderByDescending(u => u.Id);
                    break;
                default:
                    users = users.OrderBy(u => u.Id);
                    break;
            }
            int pageSize;
            int.TryParse(WebConfigurationManager.AppSettings["pageSize"], out pageSize);
            int pageNumber = (page ?? 1);
            return View(await users.ToPagedListAsync(pageNumber, pageSize));
        }


        public async Task<ActionResult> Details(int id)
        {
            if (id > 0)
            {
                var user = await UserManager.FindByIdAsync(id);

                // Show the groups the user belongs to:
                var userGroups = await this.GroupManager.GetUserGroupsAsync(id);
                ViewBag.GroupNames = userGroups.Select(u => u.Name).ToList();
                return View(user);
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }


        public ActionResult Create()
        {
            // Show a list of available groups:
            ViewBag.GroupsList = 
                new SelectList(this.GroupManager.Groups, "Id", "Name");
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(RegisterViewModel userViewModel,
            params int[] selectedGroups)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser 
                { 
                    UserName = userViewModel.Email, 
                    Email = userViewModel.Email,
                    Name = userViewModel.Name,
                    SurName = userViewModel.SurName
                };
                var adminresult = await UserManager
                    .CreateAsync(user, userViewModel.Password);

                //Add User to the selected Groups 
                if (adminresult.Succeeded)
                {
                    if (selectedGroups != null)
                    {
                        selectedGroups = selectedGroups ?? new int[] { };
                        await this.GroupManager
                            .SetUserGroupsAsync(user.Id, selectedGroups);
                    }
                    return RedirectToAction("Index");
                }
            }
            ViewBag.Groups = new SelectList(
                await RoleManager.Roles.ToListAsync(), "Id", "Name");
            return View();
        }


        public async Task<ActionResult> Edit(int id)
        {
            if (id > 0)
            {
                var user = await UserManager.FindByIdAsync(id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                // Display a list of available Groups:
                var allGroups = this.GroupManager.Groups;
                var userGroups = await this.GroupManager.GetUserGroupsAsync(id);

                var model = new EditUserViewModel()
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    SurName = user.SurName
                };

                foreach (var group in allGroups)
                {
                    var listItem = new SelectListItem()
                    {
                        Text = group.Name,
                        Value = group.Id.ToString(),
                        Selected = userGroups.Any(g => g.Id == group.Id)
                    };
                    model.GroupsList.Add(listItem);
                }
                return View(model);
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            [Bind(Include = "Email,Id,Name,SurName")] EditUserViewModel editUser, 
            params int[] selectedGroups)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(editUser.Id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                // Update the User:
                user.UserName = editUser.Email;
                user.Email = editUser.Email;
                user.Name = editUser.Name;
                user.SurName = editUser.SurName;
                await this.UserManager.UpdateAsync(user);

                // Update the Groups:
                selectedGroups = selectedGroups ?? new int[] { };
                await this.GroupManager.SetUserGroupsAsync(user.Id, selectedGroups);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Something failed.");
            return View();
        }

        public async Task<ActionResult> Delete(int id)
        {
            if (id > 0)
            {
                var user = await UserManager.FindByIdAsync(id);
                if (user == null)
                {
                    return HttpNotFound();
                }
                return View(user);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            if (ModelState.IsValid)
            {
                if (id > 0)
                {
                    var user = await UserManager.FindByIdAsync(id);
                    if (user == null)
                    {
                        return HttpNotFound();
                    }

                    // Remove all the User Group references:
                    await this.GroupManager.ClearUserGroupsAsync(id);

                    // Then Delete the User:
                    var result = await UserManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        ModelState.AddModelError("", result.Errors.First());
                        return View();
                    }
                    return RedirectToAction("Index");
                }

                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            return View();
        }
    }
}
