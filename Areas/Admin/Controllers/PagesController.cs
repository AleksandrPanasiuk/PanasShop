using PanasShop.Models.Data;
using PanasShop.Models.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PanasShop.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PagesController : Controller
    {
        // GET: Admin/Pages
        public ActionResult Index()
        {
            //Объявляем список для представления (PageVM)
            List<PageVM> pageList;

            //Инициализировать список (DB)
            using (Db db = new Db())
            {
                pageList = db.Pages.ToArray()
                    .OrderBy(x => x.Sorting)
                    .Select(x => new PageVM(x))
                    .ToList();
            }
            //Возвращаем список в представление
            return View(pageList);
        }


        // GET: Admin/Pages/AddPage
        [HttpGet]
        public ActionResult AddPage()
        {
            return View();
        }

        // POST: Admin/Pages/AddPage
        [HttpPost]
        public ActionResult AddPage(PageVM model)
        {
            //Проверка модели на валидность
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (Db db = new Db())
            {

                //Объявляем переменную для краткого описания(Slug)
                string slug;

                //Инициализируем класс PageDTO
                PagesDTO dto = new PagesDTO();

                //Присваиваем зоголовок модели
                dto.Title = model.Title.ToUpper();

                //Проверяем, есть ли крткое описание, если нет, присваеваем его
                if (string.IsNullOrWhiteSpace(model.Slug))
                {
                    slug = model.Title.Replace(" ", "-").ToLower();
                }
                else
                {
                    slug = model.Slug.Replace(" ", "-").ToLower();
                }

                //Убеждаемся что зоголовок и краткое описсание уникальны
                if (db.Pages.Any(x => x.Title == model.Title))
                {
                    ModelState.AddModelError("", "That title already exist");
                    return View(model);
                }
                else if(db.Pages.Any(x => x.Slug == model.Slug))
                {
                    ModelState.AddModelError("", "That slug already exist");
                    return View(model);
                }

                //Присваиваем оставшееся значения модели
                dto.Slug = slug;
                dto.Body = model.Body;
                dto.HasSidebar = model.HasSidebar;
                dto.Sorting = 100;

                //Сохраняем модель в БД
                db.Pages.Add(dto);
                db.SaveChanges();

            }
            //Передаем сообщение через TempData
            TempData["SM"] = "You have added a new page";

            //Переадрисовываем пользователя на метод Index
            return RedirectToAction("Index");
        }

        // GET: Admin/Pages/EditPage
        [HttpGet]
        public ActionResult EditPage(int id)
        {
            //Объявим модель PageVM
            PageVM model;

            using (Db db= new Db()) 
            {
                //Получаем страницу по Id
                PagesDTO dto = db.Pages.Find(id);

                //Проверяем, доступна ли страница
                if(dto == null)
                {
                    return Content("The Page does not exist");
                }

                //Инициализируем модели данными
                model = new PageVM(dto);

            }

            //Возвращаем представление с моделью
            return View(model);

        }

        // POST: Admin/Pages/EditPage
        [HttpPost]
        public ActionResult EditPage(PageVM model)
        {
            
            //Проверяем модель на валидность
            if (!ModelState.IsValid)
            {
                return View(model); 
            }

            using (Db db = new Db())
            {
                //Получаем id страницы
                int id = model.Id;

                //Объявляем переменную для заголовка
                string slug = "home";

                //Получаем страницу по id
                PagesDTO dto = db.Pages.Find(id);

                //Присваиваем название из полученой модели в DTO
                dto.Title = model.Title;

                //Проверяем краткий заголовок и присваим его если это необходимо
                if(model.Slug != "home")
                {
                    if (string.IsNullOrWhiteSpace(model.Slug))
                    {
                        slug = model.Title.Replace(" ", "-").ToLower();
                    }
                    else
                    {
                        slug = model.Slug.Replace(" ", "-").ToLower();
                    }
                }

                //Проверяем краткий заголовок заголовок на уникальность
                if(db.Pages.Where(x => x.Id !=id).Any(x => x.Title == model.Title))
                {
                    ModelState.AddModelError("", "That title already exist");
                    return View(model);
                }
                else if (db.Pages.Where(x => x.Id != id).Any(x => x.Slug == slug))
                {
                    ModelState.AddModelError("", "That slug already exist");
                    return View(model);
                }

                //Записываем остальные значение в класс DTO
                dto.Slug = slug;
                dto.Body = model.Body;
                dto.HasSidebar = model.HasSidebar;

                //Сохраняем изменения в базу данных
                db.SaveChanges();

            }

            //Оповещаем пользователя в TempData
            TempData["SM"] = "You have edited the page";

            //Переадресовываем на страницу Page
            return RedirectToAction("EditPage");

        }

        // GET: Admin/Pages/PageDetailes
        [HttpGet]
        public ActionResult PageDetails(int id)
        {
            //Объявляем модель PageVM
            PageVM model;

            using (Db db = new Db())
            {
                //Получаем страницу
                PagesDTO dto = db.Pages.Find(id);

                //Проверяем доступна ли страница
                if(dto== null)
                {
                    return Content("This page does not exist");
                }

                //Присваиваем модели информацию из базы
                model = new PageVM(dto);
            }
                //Возвращяем модель представления
                return View(model);
        }

        // GET: Admin/Pages/DeletePage
        [HttpGet]
        public ActionResult DeletePage(int id)
        {
            using (Db db = new Db())
            {
                //Получаем страницу
                PagesDTO dto = db.Pages.Find(id);

                //Удаляем страницу
                db.Pages.Remove(dto);

                //Сохранение изменений в базу
                db.SaveChanges();
            }


         //Оповещаем пользователя в TempData
         TempData["SM"] = "You have deleted the page!";

         //Переадресация пользователя на страницу pages
         return RedirectToAction("Index");
        }

        // GET: Admin/Pages/ReorederPages
        [HttpPost]
        public void ReorederPages(int[] id)
        {
            using (Db db = new Db())
            {
                //Реализуем счетчик 
                int count = 1;

                //Инициализируем модели данных
                PagesDTO dto;

                //Устанавливаем сортировку для каждой страницы
                foreach (var pageId in id)
                {
                    dto = db.Pages.Find(pageId);
                    dto.Sorting = count;

                    db.SaveChanges();
                    count++;
                }

            }
        }

        // GET: Admin/Pages/EditSidebar
        [HttpGet]
        public ActionResult EditSidebar()
        {
            //Объявляем модель
            SidebarVM model;

            using (Db db = new Db())
            {
                //Получаем данные из бд
                SidebarDTO dto = db.Sidebars.Find(1);

                //Заполняем модели
                model = new SidebarVM(dto);

            }

            //Возвращаем представление с моделью
            return View(model);

        }

        // POST: Admin/Pages/EditSidebar
        [HttpPost]
        public ActionResult EditSidebar(SidebarVM model)
        {
            using (Db db = new Db())
            {
                //Получаем данные из бд
                SidebarDTO dto = db.Sidebars.Find(1);

                //Присваимаем данные в body
                dto.Body = model.Body;

                //Сохраняем изменения
                db.SaveChanges();
            }
            //Присваиваем сообщение tempdata
            TempData["SM"] = "You have edited the sidebar";

            //Переадресация пользователя 
            return RedirectToAction("EditSidebar");

        }
        

        
    }

}