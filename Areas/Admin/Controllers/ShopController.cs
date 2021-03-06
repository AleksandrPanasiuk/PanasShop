using PanasShop.Models.Data;
using PanasShop.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using PagedList;
using PanasShop.Areas.Admin.Models.ViewModels.Shop;

namespace PanasShop.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShopController : Controller
    {
        // GET: Admin/Shop
        [HttpGet]
        public ActionResult Categories()
        {
            //Объявляем модель типа List
            List<CategoryVM> categoryVMlist;

            using (Db db = new Db())
            {
                //Инициализируем модель данными
                categoryVMlist = db.Categories
                    .ToArray()
                    .OrderBy(x => x.Sorting)
                    .Select(x => new CategoryVM(x))
                    .ToList();
            }
                //Возвращаем list в представление

                return View(categoryVMlist);
        }

        // POST: Admin/Shop/AddNewCategory
        [HttpPost]
        public string AddNewCategory(string catName)
        {
            //Объявляем строковую переменную ID
            string id;

            using (Db db = new Db())
            {
                //Проверяем имя категории на уникальность
                if (db.Categories.Any(x => x.Name == catName))
                    return "titletaken";

                //Инициализируем модель DTO
                CategoryDTO dto = new CategoryDTO();

                //Заполняем модель данными
                dto.Name = catName;
                dto.Slug = catName.Replace(" ", "-").ToLower();
                dto.Sorting = 100;

                //Сохряняем модель
                db.Categories.Add(dto);
                db.SaveChanges();

                //Получаем ID для возврата в представление
                id = dto.Id.ToString();
            }
            //Возвращаем ID в представление
            return id;
        }

        // POST: Admin/Shop/ReorederPages
        [HttpPost]
        public void ReorederCategories(int[] id)
        {
            using (Db db = new Db())
            {
                //Реализуем счетчик 
                int count = 1;

                //Инициализируем модели данных
                CategoryDTO dto;

                //Устанавливаем сортировку для каждой страницы
                foreach (var catId in id)
                {
                    dto = db.Categories.Find(catId);
                    dto.Sorting = count;

                    db.SaveChanges();
                    count++;
                }

            }
        }

        // GET: Admin/Shop/DeleteCategory/Id
        [HttpGet]
        public ActionResult DeleteCategory(int id)
        {
            using (Db db = new Db())
            {
                //Получаем категорию
                CategoryDTO dto = db.Categories.Find(id);

                //Удаляем категорию
                db.Categories.Remove(dto);

                //Сохранение изменений в базу
                db.SaveChanges();
            }


            //Оповещаем пользователя в TempData
            TempData["SM"] = "You have deleted the category!";

            //Переадресация пользователя на страницу pages
            return RedirectToAction("Categories");
        }

        // POST: Admin/Shop/RenameCategory/Id
        [HttpPost]
        public string RenameCategory(string newCatName, int id)
        {

            using (Db db = new Db())
            {
                //Проверяем имя категории на уникальность
                if (db.Categories.Any(x => x.Name == newCatName))
                    return "titletaken";

                //Инициализируем модель DTO
                CategoryDTO dto = db.Categories.Find(id);

                //Редактируем модельDTO
                dto.Name = newCatName;
                dto.Slug = newCatName.Replace(" ", "-").ToLower();

                //Сохраняем изменения
                db.SaveChanges();
            }
            //Возвращаем слово
            return "ok";
        }

        // GET: Admin/Shop/AddProduct
        [HttpGet]
        public ActionResult AddProduct()
        {
            //Объявляем модель данных
            ProductVM model = new ProductVM();
            
            //Добавляем в модель категории
            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "id", "Name");
            }

            //Вернуть модель в представление
            return View(model);
        }

        // POST: Admin/Shop/AddProduct
        [HttpPost]
        public ActionResult AddProduct(ProductVM model, HttpPostedFileBase file)
        {
            //Проверяем модель на валидность
            if (!ModelState.IsValid)
            {
                using (Db db = new Db())
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    return View(model);
                }
            }

            //Проверяем имя продукта на уникальность
            using (Db db = new Db())
            {
                if(db.Products.Any(x => x.Name == model.Name))
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
            }

            //Объявляем переменную productID
            int id;

            //Инициализируем и сохраяняем модель в базу
            using (Db db = new Db())
            {
                ProductDTO product = new ProductDTO();

                product.Name = model.Name;
                product.Slug = model.Name.Replace(" ", "-").ToLower();
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);

                product.CategoryName = catDTO.Name;
                product.Manufacturer = model.Manufacturer;

                db.Products.Add(product);
                db.SaveChanges();

                id = product.Id;
            }

            //Дабавляем сообщение в TempData
            TempData["SM"] = "You have added a new product!";

                #region Upload Image

                //Создаём необходимые ссылки на дериктории
                var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));

                var pathString1 = Path.Combine(originalDirectory.ToString(), "Products");
                var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
                var pathString3 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");
                var pathString4 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
                var pathString5 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

            //Проверяем, имеется ли дериктория (если нет, то создаем)
            if (!Directory.Exists(pathString1))
                Directory.CreateDirectory(pathString1);

            if (!Directory.Exists(pathString2))
                Directory.CreateDirectory(pathString2);

            if (!Directory.Exists(pathString3))
                Directory.CreateDirectory(pathString3);

            if (!Directory.Exists(pathString4))
                Directory.CreateDirectory(pathString4);

            if (!Directory.Exists(pathString5))
                Directory.CreateDirectory(pathString5);

            //Проверяем был ли файл загружен
            if (file != null && file.ContentLength > 0)
            {
                //Получаем расширение файла
                string ext = file.ContentType.ToLower();

                //Проверяем расширения загружаемого файла
                if (ext != "image/jpg" &&
                    ext != "image/jpeg" &&
                    ext != "image/pjpeg" &&
                    ext != "image/gif" &&
                    ext != "image/x-png" &&
                    ext != "image/png")
                {
                    using (Db db = new Db())
                    {
                        model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "The image is not uploaded - wrong image extension!");
                        return View(model);
                    }
                }

            
                //Объявляем переменную с именем изображения
                string imageName = file.FileName;

                //Сохраняем имя изображения в модель DTO
                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;

                    db.SaveChanges();
                }

                //Назначаем пути к оригинальному и уменьшеному изображению
                var path = string.Format($"{pathString2}\\{imageName}");
                var path2 = string.Format($"{pathString3}\\{imageName}");

                //Сохраняем оригинальное изображение
                file.SaveAs(path);

                //Создаём и сохраняем уменьшенную копию
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200).Crop(1,1);
                img.Save(path2);
            }
            #endregion

            //Переадресовываем пользователя
            return RedirectToAction("AddProduct");
        }

        // GET: Admin/Shop/Products
        [HttpGet]
        public ActionResult Products(int? page, int? catId)
        {
            //Объявляем ProductVM типа List
            List<ProductVM> listOfProductVM;

            //Устанавливаем номер страницы
            var pageNumber = page ?? 1;
            using (Db db = new Db())
            {
                //Инициализируем наш лист и заполняем данными
                listOfProductVM = db.Products.ToArray()
                    .Where(x => catId == null || catId == 0 || x.CategoryId == catId)
                    .Select(x => new ProductVM(x)).ToList();

                //Заполняем категории для соритивки
                ViewBag.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                //Устанавливаем выбранную категорию
                ViewBag.SelectedCat = catId.ToString();

            }
            //Устанавливаем постраничную навигацию
            var onePageOfProducts = listOfProductVM.ToPagedList(pageNumber, 5);
            ViewBag.onePageOfProducts = onePageOfProducts;

            //Возвращаем представление
            return View(listOfProductVM);
        }

        // GET: Admin/Shop/EditProduct/id
        [HttpGet]
        public ActionResult EditProduct(int id)
        {
            //Объявляем модель ProductVM
            ProductVM model;
            using (Db db = new Db())
            {
                //Получаем все поля продукта
                ProductDTO dto = db.Products.Find(id);

                //Проверяем доступен ли продукт
                if (dto == null)
                {
                    return Content("That product does not exist!");
                }

                //Инициализируем модель данными
                model = new ProductVM(dto);

                //Создаем список категорий
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                //Получаем все изображения из галереи
                model.GalleryImages = Directory
                    .EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                    .Select(fn => Path.GetFileName(fn));
            }
            //Возвращаем модель в представление
            return View(model);
        }

        // POST: Admin/Shop/EditProduct
        [HttpPost]
        public ActionResult EditProduct(ProductVM model, HttpPostedFileBase file)
        {
            //Получаем Id продукта
            int id = model.Id;

            //Заполняем список категориями и изображениями
            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

            model.GalleryImages = Directory
                .EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                .Select(fn => Path.GetFileName(fn));

            //Проверяем модель на валидность
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //Проверяем имя продукта на уникальность
            using (Db db = new Db())
            {
                if (db.Products.Where(x => x.Id != id).Any(x => x.Name == model.Name))
                {
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
            }

            //Обновляем продукт в базе данных
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);

                dto.Name = model.Name;
                dto.Slug = model.Name.Replace(" ", "-").ToLower();
                dto.Description = model.Description;
                dto.Price = model.Price;
                dto.CategoryId = model.CategoryId;
                dto.Manufacturer = model.Manufacturer;
                dto.ImageName = model.ImageName;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                dto.CategoryName = catDTO.Name;

                db.SaveChanges();
            }

            //Устанавливаем сообщение в TempData
            TempData["SM"] = "You have edited that product!";

            //Логика обработки изображений

            #region Image Upload

            // Проверяем загружен ли файл
            if (file != null && file.ContentLength > 0)
            {
                //Получаем расширение файла
                string ext = file.ContentType.ToLower();

                //Проверяем расширение файла
                if (ext != "image/jpg" &&
                    ext != "image/jpeg" &&
                    ext != "image/pjpeg" &&
                    ext != "image/gif" &&
                    ext != "image/x-png" &&
                    ext != "image/png")
                {
                    using (Db db = new Db())
                    {
                        ModelState.AddModelError("", "The image is not uploaded - wrong image extension!");
                        return View(model);
                    }
                }

                //Устанавливаем пути для загрузки
                var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));

                var pathString1 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
                var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");

                //Удаляем существующие файлы и директориях

                DirectoryInfo di1 = new DirectoryInfo(pathString1);
                DirectoryInfo di2 = new DirectoryInfo(pathString2);

                foreach (var file2 in di1.GetFiles())
                {
                    file2.Delete();
                }

                foreach (var file3 in di2.GetFiles())
                {
                    file3.Delete();
                }

                //Сохраняем имя изображение
                string imageName = file.FileName;
                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;

                    db.SaveChanges();
                }

                var path = string.Format($"{pathString1}\\{imageName}");
                var path2 = string.Format($"{pathString2}\\{imageName}");

                //Сохраняем оригинальное изображение
                file.SaveAs(path);

                //Создаём и сохраняем уменьшенную копию
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200).Crop(1, 1);
                img.Save(path2);
            }

            #endregion

                //Переадресовываем пользователя
                return RedirectToAction("EditProduct");
        }

        // GET: Admin/Shop/DeleteProduct/id
        [HttpGet]
        public ActionResult DeleteProduct(int id)
        {
            //Удаляем товар из базы данных
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);
                db.Products.Remove(dto);

                db.SaveChanges();
            }

            //Удаляем дериктории товара (изображения)
            var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));
            var pathString = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());

            if(Directory.Exists(pathString))
                Directory.Delete(pathString, true);

            //переадресовываем пользователя
            return RedirectToAction("Products");
        }

        // POST: Admin/Shop/SaveGalleryImages/id
        public void SaveGalleryImages(int id)
        {
            //Перебираем все полученые из представления файлы
            foreach (string fileName in Request.Files)
            {
                //Инициализируем файлы
                HttpPostedFileBase file = Request.Files[fileName];

                //Проверяем не пустые ли файлы
                if (file != null && file.ContentLength > 0)
                {
                    //Назаначаем все пути к деректориям
                    var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));
                    string pathString1 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
                    string pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

                    //Назаначаем пути самих изображений
                    var path = string.Format($"{pathString1}\\{file.FileName}");
                    var path2 = string.Format($"{pathString2}\\{file.FileName}");

                    //Сохраняем оригинальные и уменьшенные копии
                    file.SaveAs(path);

                    WebImage img = new WebImage(file.InputStream);
                    img.Resize(200, 200).Crop(1, 1);
                    img.Save(path2);
                }
            }
        }

        // GET: Admin/Shop/DeleteImage/id/imageName
        public void DeleteImage(int id, string imageName)
        {
            string fullPath1 = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/" + imageName);
            string fullPath2 = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/Thumbs/" + imageName);

            if(System.IO.File.Exists(fullPath1))
                System.IO.File.Delete(fullPath1);

            if (System.IO.File.Exists(fullPath2))
                System.IO.File.Delete(fullPath2);
        }

        public ActionResult Orders()
        {
            //Инициализируем модель OrdersForAdmin
            List<OrdersForAdminVM> ordersForAdmin = new List<OrdersForAdminVM>();

            using (Db db = new Db())
            {
                //Инициализируем модель OrderVM
                List<OrderVM> orders = db.Orders.ToArray().Select(x => new OrderVM(x)).ToList();

                //Перебераем данные модели OrderVM
                foreach (var order in orders)
                {
                    //Инициализируем словарь товаров
                    Dictionary<string, int> productAndQty = new Dictionary<string, int>();

                    //Создаём переменную для общей суммы заказа
                    decimal total = 0m;

                    //Инициализируем лист OrderDetailsDTO
                    List<OrderDetailsDTO> orderDetailsList =
                        db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    //Получаем имя пользователя
                    UserDTO user = db.Users.FirstOrDefault(x => x.Id == order.UserId);
                    string userName = user.Username;

                    //Перебезаем заказы конкретного пользователя из OrderDetailsDTO
                    foreach (var orderDetails in orderDetailsList)
                    {
                        //Получаем товар
                        ProductDTO product = db.Products.FirstOrDefault(x => x.Id == orderDetails.ProductId);

                        //Получаем цену
                        decimal price = product.Price;

                        //Получаем название товара
                        string productName = product.Name;

                        //Добавляем товары в словарь
                        productAndQty.Add(productName, orderDetails.Quantity);

                        //Получаем полную стоимость всех товаров пользоватея
                        total += orderDetails.Quantity * price;

                    }
                    //Добавляем список товаров в OrdersForAdminVM
                    ordersForAdmin.Add(new OrdersForAdminVM()
                        {
                            OrderNumber = order.OrderId,
                            UserName = userName,
                            Total = total,
                            ProductsAndQty = productAndQty,
                            CreatedAt = order.CreatedAt
                        });
                }
            }

            //Возвращаем представление с моделью
            return View(ordersForAdmin);
        }
    }
}