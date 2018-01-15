using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using Identity2_Rbac.Utility;
using System.Web.Configuration;
using Identity2_Rbac.Models;

namespace Identity2_Rbac.Controllers
{
    [Authorize]
    public class ArticoloController : Controller
    {
        private EntitiesRbac db = new EntitiesRbac();

        // GET: Articolo
        [Authorize(Roles = "Admin,Articolo_Read")]
        public async Task<ActionResult> Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.idSortParm = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewBag.TitoloSortParm = sortOrder == "titolo" ? "titolo_desc" : "titolo";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;


            var articoli = from s in db.Articolo
                           select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                articoli = articoli.Where(s => s.Titolo.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "id_desc":
                    articoli = articoli.OrderByDescending(s => s.id);
                    break;
                case "titolo_desc":
                    articoli = articoli.OrderByDescending(s => s.Titolo);
                    break;
                case "titolo":
                    articoli = articoli.OrderBy(s => s.Titolo);
                    break;
                default:
                    articoli = articoli.OrderBy(s => s.id);
                    break;
            }

            int pageSize;
            int.TryParse(WebConfigurationManager.AppSettings["pageSize"], out pageSize);
            int pageNumber = (page ?? 1);
            return View(await articoli.ToPagedListAsync(pageNumber, pageSize));
        }

        // GET: Articolo/Details/5
        [Authorize(Roles = "Admin,Articolo_Read")]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Articolo articolo = await db.Articolo.FindAsync(id);
            if (articolo == null)
            {
                return HttpNotFound();
            }
            return View(articolo);
        }

        // GET: Articolo/Create
        [Authorize(Roles = "Articolo_Create")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Articolo/Create
        // Per proteggere da attacchi di overposting, abilitare le proprietà a cui eseguire il binding. 
        // Per ulteriori dettagli, vedere http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Articolo_Create")]
        public async Task<ActionResult> Create([Bind(Include = "id,Titolo,Testo")] Articolo articolo)
        {
            if (ModelState.IsValid)
            {
                db.Articolo.Add(articolo);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(articolo);
        }

        // GET: Articolo/Edit/5
        [Authorize(Roles = "Articolo_Edit")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Articolo articolo = await db.Articolo.FindAsync(id);
            if (articolo == null)
            {
                return HttpNotFound();
            }
            return View(articolo);
        }

        // POST: Articolo/Edit/5
        // Per proteggere da attacchi di overposting, abilitare le proprietà a cui eseguire il binding. 
        // Per ulteriori dettagli, vedere http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Articolo_Edit")]
        public async Task<ActionResult> Edit([Bind(Include = "id,Titolo,Testo")] Articolo articolo)
        {
            if (ModelState.IsValid)
            {
                db.Entry(articolo).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(articolo);
        }

        // GET: Articolo/Delete/5
        [Authorize(Roles = "Articolo_Delete")]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Articolo articolo = await db.Articolo.FindAsync(id);
            if (articolo == null)
            {
                return HttpNotFound();
            }
            return View(articolo);
        }

        // POST: Articolo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Articolo_Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Articolo articolo = await db.Articolo.FindAsync(id);
            db.Articolo.Remove(articolo);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
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
