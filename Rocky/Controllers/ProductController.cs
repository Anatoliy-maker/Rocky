using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Rocky.Data;
using Rocky.Models;
using Rocky.Models.ViewModels;

namespace Rocky.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> objList = _db.Product.Include(u => u.Category).Include(u => u.ApplicationType);
            
            //foreach (var obj in objList) 
            //{ 
            //    obj.Category = _db.Category.FirstOrDefault(u => u.Id == obj.CategoryId);
            //    obj.ApplicationType = _db.ApplicationType.FirstOrDefault(u => u.Id == obj.ApplicationTypeId);
            //}

            return View(objList);
        }

        //GET - UPSERT
        public IActionResult Upsert(int? id)
        {
            //IEnumerable<SelectListItem> CategoryDropDown = _db.Category.Select(i => new SelectListItem
            //{
            //    Text = i.Name,
            //    Value = i.Id.ToString()
            //});

            //ViewBag.CategoryDropDown = CategoryDropDown;

            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategorySelectList = _db.Category.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),

                ApplicationTypeSelectList = _db.ApplicationType.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            
            if (id == null)
            {
                //this for create
                return View(productVM);
            }
            else 
            {
                //this for update
                productVM.Product = _db.Product.Find(id);
                if (productVM.Product == null) 
                {
                    return NotFound();
                }
                return View(productVM);
            }            
        }

        //POST - UPSERT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM productVM)
        {
            
            var files = HttpContext.Request.Form.Files;
            string webRootPath = _webHostEnvironment.WebRootPath;

            if (productVM.Product.Id == 0)
            {
                //creating
                string upload = webRootPath + WC.ImagePath;
                string filename = Guid.NewGuid().ToString();
                string extention = Path.GetExtension(files[0].FileName);

                using (var filestream = new FileStream(Path.Combine(upload, filename + extention), FileMode.Create))
                {
                    files[0].CopyTo(filestream);
                }

                productVM.Product.Image = filename + extention;
                _db.Product.Add(productVM.Product);
            }
            else
            {
                //updating
                var objFromDb = _db.Product.AsNoTracking().FirstOrDefault(u => u.Id == productVM.Product.Id);

                if (files.Count > 0)
                {
                    string upload = webRootPath + WC.ImagePath;
                    string filename = Guid.NewGuid().ToString();
                    string extention = Path.GetExtension(files[0].FileName);

                    var oldFile = Path.Combine(upload, objFromDb.Image);

                    if (System.IO.File.Exists(oldFile)) 
                    {
                        System.IO.File.Delete(oldFile);
                    }

                    using (var fileStream = new FileStream(Path.Combine(upload, filename + extention), FileMode.Create)) 
                    {
                        files[0].CopyTo(fileStream);
                    }

                    productVM.Product.Image = filename + extention;
                }
                else 
                {
                    productVM.Product.Image = objFromDb.Image;
                }

                _db.Product.Update(productVM.Product);
            }

                _db.SaveChanges();
                return RedirectToAction("Index");                   
        }

      

        //GET - DELETE
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //var obj = _db.Product.Find(id);
            //obj.Category = _db.Category.Find(obj.CategoryId);

            Product product = _db.Product.Include(u => u.Category).Include(u => u.ApplicationType).FirstOrDefault(u => u.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        //POST - DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var obj = _db.Product.Find(id);

            string upload = _webHostEnvironment.WebRootPath + WC.ImagePath;
            

            var oldFile = Path.Combine(upload, obj.Image);

            if (System.IO.File.Exists(oldFile))
            {
                System.IO.File.Delete(oldFile);
            }

            _db.Product.Remove(obj);
            _db.SaveChanges();
            return RedirectToAction("Index");            
        }
    }
}

