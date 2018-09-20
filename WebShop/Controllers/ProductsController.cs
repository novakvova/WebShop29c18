﻿using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebShop.Healpers;
using WebShop.Models;
using WebShop.Models.Entities;
using WebShop.ViewModels;

namespace WebShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;// = new ApplicationDbContext();
        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public ActionResult Index(string category, string search, string sortBy, int? page)
        {
            //instantiate a new view model 
            ProductIndexViewModel viewModel = new ProductIndexViewModel();


            var products = _context.Products.Include(p => p.Category);


            if (!String.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name.Contains(search) ||
                p.Description.Contains(search) ||
                p.Category.Name.Contains(search));
                viewModel.Search = search;
            }
            //group search results into categories and count how many items in each category 
            viewModel.CatsWithCount = from matchingProducts in products
                               where matchingProducts.CategoryId != null
                               group matchingProducts by
                               matchingProducts.Category.Name into
                               catGroup
                               select new CategoryWithCount()
                               {
                                  CategoryName = catGroup.Key,
                                  ProductCount = catGroup.Count()
                               };


            if (!String.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category.Name == category);
                viewModel.Category = category;
            }

            // sort the results
            switch (sortBy)
            {
                case "price_lowest":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_highest":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    products = products.OrderBy(p => p.Name);
                    break;
            }

            int currentPage = (page ?? 1);
            viewModel.Products = products.ToPagedList(currentPage, Constants.PageItems);
            viewModel.SortBy = sortBy;
            viewModel.Sorts = new Dictionary<string, string>
            {
                { "Price low to high", "price_lowest" },
                { "Price high to low", "price_highest" }
            };

            return View(viewModel);
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = _context.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Description,Price,CategoryId")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = _context.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Description,Price,CategoryId")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Entry(product).State = EntityState.Modified;
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = _context.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = _context.Products.Find(id);
            _context.Products.Remove(product);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ContentResult CreateAjax([Bind(Include = "Id,Name,Description,Price,CategoryId")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
            }
            string json = JsonConvert.SerializeObject(new
            {
                product
            });

            return Content(json, "application/json");
        }

        [HttpGet]
        public ContentResult SearchByNameJson(string name)
        {

            var products = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Name.Contains(name) ||
                p.Description.Contains(name) ||
                p.Category.Name.Contains(name))
                .Select(p=>new ProductSearchViewModel
                {
                    Id=p.Id,
                    Name=p.Name,
                    CategoryName=p.Category.Name
                }).ToList();


            string json = JsonConvert.SerializeObject(new
            {
                products
            });

            return Content(json, "application/json");
        }
        [HttpPost]
        public JsonResult UploadImageDecription(HttpPostedFileBase file)
        {
            string link = string.Empty;
            string filename = Guid.NewGuid().ToString() + ".jpg";
            string image = Server.MapPath(Constants.ProductDescriptionPath) + filename;
            try
            {
                // The Complete method commits the transaction. If an exception has been thrown,
                // Complete is not  called and the transaction is rolled back.
                Bitmap imgCropped = new Bitmap(file.InputStream);
                var saveImage = ImageWorker.CreateImage(imgCropped, 450, 450);
                if (saveImage == null)
                    throw new Exception("Error save image");
                saveImage.Save(image, ImageFormat.Jpeg);
                link = Url.Content(Constants.ProductDescriptionPath) + filename;
                ProductImageBasket pImage = new ProductImageBasket()
                {
                    Name = filename
                };
                _context.ProductImageBaskets.Add(pImage);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                if (System.IO.File.Exists(image))
                {
                    System.IO.File.Delete(image);
                }
            }

            return Json(new { link });
        }
    }

}
