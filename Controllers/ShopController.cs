using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PanasShop.Models.Data;
using PanasShop.Models.ViewModels.Shop;

namespace PanasShop.Controllers
{
    public class ShopController : Controller
    {
        // GET: Shop
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Pages");
        }

        public ActionResult CategoryMenuPartial()
        {
            //Объявляем модель типа list CategoryVM
            List<CategoryVM> categoryVMList;

            //Инициализируем модель данными
            using (Db db = new Db())
            {
                categoryVMList = db.Categories.ToArray().OrderBy(x => x.Sorting).Select(x => new CategoryVM(x))
                    .ToList();
            }

            //Возвращаем чистичное представление с моделью
            return PartialView("_CategoryMenuPartial", categoryVMList);

        }

        public ActionResult Category(string name)
        {
            //Объявляем список типи list
            List<ProductVM> productVMList;

            using (Db db = new Db())
            {
                //Получаем Id категории
                CategoryDTO categoryDTO = db.Categories.Where(x => x.Slug == name).FirstOrDefault();

                int catId = categoryDTO.Id;

                //Инициализируем список данными
                productVMList = db.Products.ToArray().Where(x => x.CategoryId == catId).Select(x => new ProductVM(x))
                    .ToList();

                //Получаем имя категории
                var productCat = db.Products.Where(x => x.CategoryId == catId).FirstOrDefault();

                //Проверяем не пустая ли
                if (productCat == null)
                {
                    var catName = db.Categories.Where(x => x.Slug == name).Select(x => x.Name).FirstOrDefault();
                    ViewBag.CategoryName = catName;
                }
                else
                {
                    ViewBag.CategoryName = productCat.CategoryName;
                }
            }

            //Возвращяем представление с моделью
            return View(productVMList);
        }

        [ActionName("product-details")]
        public ActionResult ProductDetails(string name)
        {
            //Объявляем модели DTO и VM
            ProductDTO dto;
            ProductVM model;

            //Инициализируем Id продукта
            int id = 0;

            using (Db db = new Db())
            {
                //Проверяем доступен ли продукт
                if (!db.Products.Any(x => x.Slug.Equals(name)))
                {
                    return RedirectToAction("Index", "Shop");
                }

                //Инициализируем модель DTO данными
                dto = db.Products.Where(x => x.Slug == name).FirstOrDefault();

                //Получаем Id 
                id = dto.Id;

                //Инициализируем модель VM данными
                model = new ProductVM(dto); 
            }

        //Получаем изображение из галереи
        model.GalleryImages = Directory
            .EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
            .Select(fn => Path.GetFileName(fn));

        //Возвращаем модель в представление
        return View("ProductDetails",model);
        }
    }
}