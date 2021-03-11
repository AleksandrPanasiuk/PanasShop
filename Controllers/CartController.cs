using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using PanasShop.Models.Data;
using PanasShop.Models.ViewModels.Cart;

namespace PanasShop.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        public ActionResult Cart()
        {
            //Объявляем лист типа cartVM
            var cart = Session["cart"] as List<CartVM> ?? new List<CartVM>();

            //Проверяем не пустая ли корзина
            if (cart.Count == 0 || Session["cart"] == null)
            {
                ViewBag.Message = "Your cart is empty!";
                return View();
            }

            //Если она не пуста то складывааем сумму и передаем через ViewBag
            decimal total = 0m;

            foreach (var item in cart)
            {
                total += item.Total;
            }

            ViewBag.GrandTotal = total;

            //Возвращзаем лист в представление
            return View(cart);
        }

        public ActionResult CartPartial()
        {
            //Объявляем модеь CartVM
            CartVM model = new CartVM();

            //Объявляем переменную колилчества
            int qty = 0;

            //Объявляем переменную цены
            decimal price = 0m;

            //Проверяем сессию на наличие данных
            if (Session["cart"] != null)
            {
                //Получаем общее колличество и цену, при условии что корзина не пуста
                var list = (List<CartVM>)Session["cart"];

                foreach (var item in list)
                {
                    qty += item.Quantity;
                    price += item.Quantity * item.Price;
                }
            }
            else
            {
                //Или устанавливаем количество и цену 0
                model.Quantity = 0;
                model.Price = 0m;
            }

            model.Quantity = qty;
            model.Price = price;

            //Возвращаем частичное представление с моделью
            return PartialView("_CartPartial",model);
        }

        public ActionResult AddToCartPartial(int id)
        {
            //Объявляем лист типа CartVM
            List<CartVM> cart = Session["cart"] as List<CartVM> ?? new List<CartVM>();

            //Объявляем модель CartVM
            CartVM model = new CartVM();

            using (Db db = new Db())
            {
                //Получаем продукт
                ProductDTO product = db.Products.Find(id);

                //Проверяем, находиться ли товар в корзине
                var productInCart = cart.FirstOrDefault(x => x.ProductId == id);
                
                //Если нет то добавляем новый товар
                if (productInCart == null)
                {
                    cart.Add(new CartVM()
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            Quantity = 1,
                            Price = product.Price,
                            Image = product.ImageName
                        });
                }
                //Если да. то добавляем единицу товара
                else
                {
                    productInCart.Quantity++;
                }
            }

            //Получаем общее количеств, цену и добавляем данные в модель
            int qty = 0;
            decimal price = 0m;

            foreach (var item in cart)
            {
                qty += item.Quantity;
                price += item.Quantity * item.Price;
            }

            model.Quantity = qty;
            model.Price = price;

            //Сохраняем состояние корзины в сессию
            Session["cart"] = cart;

            //Возвращаем частичное представление
            return PartialView("_AddToCartPartial",model);
        }

        public JsonResult IncrementProduct(int productId)
        {
            //Объявляем лист Cart
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                //Получаем CartVm из листа
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                //Добавляем количество
                model.Quantity++;

                //Сохраняем необходимые данные
                var result = new {qty = model.Quantity, price = model.Price};

                //Возвращаем JSON ответ с данными
                return Json(result,JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DecrementProduct(int productId)
        {
            //Объявляем лист Cart
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                //Получаем CartVm из листа
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                //Отнимаем количество
                if (model.Quantity > 1)
                    model.Quantity--;
                else
                {
                    model.Quantity = 0;
                    cart.Remove(model);
                }

                //Сохраняем необходимые данные
                var result = new { qty = model.Quantity, price = model.Price };

                //Возвращаем JSON ответ с данными
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public void RemoveProduct(int productId)
        {
            //Объявляем лист Cart
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                //Получаем CartVm из листа
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                cart.Remove(model);
            }
        }

        public ActionResult PaypalPartial()
        {
            //Получаем список товаров в корзине
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            //Возвращяем частичное представление с листом товаров
            return PartialView(cart);
        }

        [HttpPost ]
        public void PlaceOrder()
        {
            //Получаем лист товаров
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            //Получаем имя пользователя
            string userName = User.Identity.Name;

            //Инициальзируем переменную для OrederId
            int orderId = 0;

            using (Db db = new Db())
            {
                //Объявляем модель OrederDTO
                OrderDTO orderDTO = new OrderDTO();

                //Получаем Id пользователя
                var q = db.Users.FirstOrDefault(x => x.Username == userName);
                int userId = q.Id;

                //Заполняем модель OrderDTO данными и сохраняем
                orderDTO.UserId = userId;
                orderDTO.CreatedAt = DateTime.Now;

                db.Orders.Add(orderDTO);
                db.SaveChanges();

                //Получаем  orderId
                orderId = orderDTO.OrderId;

                //Объявляем модель OrderDetailsDTO
                OrderDetailsDTO orderDetailsDTO = new OrderDetailsDTO();

                //Добавляем данные
                foreach (var item in cart)
                {
                    orderDetailsDTO.OrderId = orderId;
                    orderDetailsDTO.UserId = userId;
                    orderDetailsDTO.ProductId = item.ProductId;
                    orderDetailsDTO.Quantity = item.Quantity;

                    db.OrderDetails.Add(orderDetailsDTO);
                    db.SaveChanges();
                }
            }

            //Отправляем письмо о заказе на почту администратора 
            var client = new SmtpClient("smtp.mailtrap.io", 2525)
            {
                Credentials = new NetworkCredential("9e777deee35012", "25b6366e845c08"),
                EnableSsl = true
            };
            client.Send("shop@example.com", "admin@example.com", "New Order", $"You have a new order. Order number: {orderId}");

            //Обнуляем сессию
            Session["cart"] = null;
        }
    }

}