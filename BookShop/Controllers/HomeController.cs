using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;


namespace BookShop
{

    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }
    }

    public class HomeController : Controller
    {
        private const string ConnectionString = "Server=localhost; Port=5433; Database=users; UserId=postgres; Password=123; CommandTimeout=120;";

        public IActionResult PaymentSuccess()
        {
            // Очищаем корзину после оплаты
            HttpContext.Session.Remove("Cart");

            return View();
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            var itemToRemove = cart.Find(item => item.Id == id);
            if (itemToRemove != null)
            {
                if(itemToRemove.Quantity == 1)
                {
                    cart.Remove(itemToRemove);
                }
                else
                {
                    itemToRemove.Quantity -= 1;
                }
            }
            HttpContext.Session.Set("Cart", cart);
            return Json(new { success = true });
        }


        public IActionResult Index()
        {
            List<Book> books = new List<Book>();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                string query = "SELECT id, name, price, author, imgurl FROM books;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Price = reader.GetInt32(2),
                                Author = reader.GetString(3),
                                ImgUrl = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            return View(books);
        }


        public IActionResult AddToCart(int id, string name, int price)
        {
            List<CartItem> cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

            var existingItem = cart.Find(item => item.Id == id);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    Id = id,
                    Name = name,
                    Price = price,
                    Quantity = 1
                });
            }

            HttpContext.Session.Set("Cart", cart);
            return RedirectToAction("Index");
        }

        public IActionResult Cart()
        {
            List<CartItem> cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registration(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Message = "Username and password cannot be empty.";
                return View();
            }

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    await conn.OpenAsync();
                    string insertSql = "INSERT INTO users (login, password) VALUES (@login, @password)";
                    using (var cmd = new NpgsqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("login", username);
                        cmd.Parameters.AddWithValue("password", password);

                        await cmd.ExecuteNonQueryAsync();
                        ViewBag.Message = "User registered successfully.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Error: {ex.Message}";
                }
            }

            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("ApplicationCookie");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Message = "Username and password cannot be empty.";
                return View();
            }

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    await conn.OpenAsync();
                    string selectSql = "SELECT password FROM users WHERE login = @login";
                    using (var cmd = new NpgsqlCommand(selectSql, conn))
                    {
                        cmd.Parameters.AddWithValue("login", username);
                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null)
                        {
                            string storedPassword = result.ToString();
                            if (storedPassword == password)
                            {
                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, username)
                                };

                                var identity = new ClaimsIdentity(claims, "ApplicationCookie");
                                var principal = new ClaimsPrincipal(identity);

                                await HttpContext.SignInAsync(principal);

                                return RedirectToAction("Index");
                            }
                            else
                            {
                                ViewBag.Message = "Invalid password. Please try again.";
                            }
                        }
                        else
                        {
                            ViewBag.Message = "User not found.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Error: {ex.Message}";
                }
            }

            return View();
        }
    }
}
