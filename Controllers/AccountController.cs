using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using PanasShop.Models.Data;
using PanasShop.Models.ViewModels.Account;
using PanasShop.Models.ViewModels.Shop;

namespace PanasShop.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        // GET: account/create-account
        [ActionName("create-account")]
        [HttpGet]
        public ActionResult CreateAccount()
        {
            return View("CreateAccount");
        }

        // POST: account/create-account
        [ActionName("create-account")]
        [HttpPost]
        public ActionResult CreateAccount(UserVM model)
        {
            //Проверяем модель на валидность
            if (!ModelState.IsValid)
                return View("CreateAccount", model);

            //Проверяем соотвецтвие пароля
            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("","Password does not match!");
                return View("CreateAccount", model);
            }

            using (Db db = new Db())
            {
                //Проверяем имя на уникальность
                if (db.Users.Any(x => x.Username.Equals(model.Username)))
                {
                    ModelState.AddModelError("",$"Username{model.Username} is taken!");
                    model.Username = "";
                    return View("CreateAccount", model);
                }

                //Создаем экземпрял класса UserDTO
                UserDTO userDTO = new UserDTO()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAdress = model.EmailAdress,
                    PhoneNumber = model.PhoneNumber,
                    Username = model.Username,
                    Password = model.ConfirmPassword
                };

                //Добавляем данные в экземпляр класса (в модель)
                db.Users.Add(userDTO);

                //Сохраняем данные
                db.SaveChanges();

                //Добавляем роль пользователю
                int id = userDTO.Id;

                UserRoleDTO userRoleDTO = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = 2
                };

                db.UserRoles.Add(userRoleDTO);
                db.SaveChanges();
            } 
            //Выводим cообщение в TempData
            TempData["SM"] = "You are now registered and can login";

            //Переадрессовываем пользователя
            return RedirectToAction("Login");
        }

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            //Подтверждаем что пользователь не авторизован
            string userName = User.Identity.Name;

            if (!string.IsNullOrEmpty(userName))
                return RedirectToAction("user-profile");
            
            //Возвращаем представление
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(LoginUserVM model)
        {
            //Проверяем модель на валидность
            if (!ModelState.IsValid)
                return View(model);

            //Проверяем пользователя на валидность
            bool isValid = false;

            using (Db db = new Db())
            {
                if (db.Users.Any(x => x.Username.Equals(model.Username) && x.Password.Equals(model.Password)))
                    isValid = true;

                if (!isValid)
                {
                    ModelState.AddModelError("", "Invalid Username or Password!");
                    return View(model);
                }
                else
                {
                    FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                    return Redirect(FormsAuthentication.GetRedirectUrl(model.Username, model.RememberMe));
                }
            }
        }

        [Authorize]
        // GET: Account/Logout
        [HttpGet]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [Authorize]
        public ActionResult UserNavPartial()
        {
            //Получаем имя пользователя
            string userName = User.Identity.Name;

            //Объявляем модель 
            UserNavPartialVM model;

            using (Db db = new Db())
            {
                //Получаем пользователя
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == userName);

                //Заполняем модель данными из DTO
                model = new UserNavPartialVM()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                };
            }
            //Возвращаем частичное представление с моделью
            return PartialView(model);
        }

        // GET: Account/user-profile
        [Authorize]
        [HttpGet]
        [ActionName("user-profile")]
        public ActionResult UserProfile()
        {
            //Получаем имя пользователя
            string userName = User.Identity.Name;

            //Объявляем модель
            UserProfileVM model;

            using (Db db = new Db())
            {
                //Получаем пользователя
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == userName);

                //Инициализируем модель данными
                model = new UserProfileVM(dto);
            }

            //Возвращяем модель в представление
            return View("UserProfile", model);
        }

        // POST: Account/user-profile
        [Authorize]
        [HttpPost]
        [ActionName("user-profile")]
        public ActionResult UserProfile(UserProfileVM model)
        {
            bool userNameIsChanged = false;
            //Проверяем модель на валидность
            if (!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            //Проверяем пароль (если пользователь его меняем)
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("","Password does not match!");
                    return View("UserProfile", model);  
                }
            }

            using (Db db = new Db())
            {
                //Получаем имя пользователя
                string userName = User.Identity.Name;
                
                //Проверяем изменено ли имя пользователя
                if (userName != model.Username)
                {
                    userName = model.Username;
                    userNameIsChanged = true;
                }

                //Проверяем имя на уникальность
                if (db.Users.Where(x => x.Id != model.Id).Any(x=>x.Username == userName))
                {
                    ModelState.AddModelError("", $"Username {model.Username} already exist!");
                    model.Username = "";
                    return View("UserProfile", model);
                }
                //Изменяем модель контекста данных
                UserDTO dto = db.Users.Find(model.Id);

                dto.FirstName = model.FirstName;
                dto.LastName = model.LastName;
                dto.EmailAdress = model.EmailAdress;
                dto.PhoneNumber = model.PhoneNumber;
                dto.Username = model.Username;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    dto.Password = model.Password;
                }

                //Сохраняем изменения
                db.SaveChanges();
            }
            //Устанавливаем сообщение в TempData
            TempData["SM"] = "Your have edited your profile!";

            if (!userNameIsChanged)
                //Возвращаем представление с моделью
                return View("UserProfile", model);
            else
                return RedirectToAction("Logout");
        }

        [Authorize(Roles = "User")]
        public ActionResult Orders()
        {
            //Инициализируем модель OrdersForUserVM
            List<OrdersForUserVM> ordersForUser = new List<OrdersForUserVM>();

            using (Db db = new Db())
            {
                //Получаем ID пользователя
                UserDTO user = db.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
                int userId = user.Id;

                //Инициализируем модель OrderVM
                List<OrderVM> orders = db.Orders.Where(x => x.UserId == userId).ToArray().Select(x => new OrderVM(x))
                    .ToList();

                //Перебираем список товаров
                foreach (var order in orders)
                {
                    //Инициализируем словарь товаров
                    Dictionary<string, int> productAndQty = new Dictionary<string, int>();

                    //Создаём переменную для общей суммы заказа
                    decimal total = 0m;

                    //Инициализируем лист OrderDetailsDTO
                    List<OrderDetailsDTO> orderDetailsDto =
                        db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                     foreach (var orderDetails in orderDetailsDto)
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

                    //Добавляем полученые данные в модель OrdersForUserVM
                    ordersForUser.Add(new OrdersForUserVM()
                    {
                        OrderNumber = order.OrderId,
                        Total = total,
                        ProductsAndQty = productAndQty,
                        CreatedAt = order.CreatedAt
                    });
                }
            }

            //Возвращаем представление с моделью
            return View(ordersForUser);
        }
    }
}