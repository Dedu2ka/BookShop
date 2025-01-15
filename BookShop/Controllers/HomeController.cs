using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;


namespace BookShop
{
    public class HomeController : Controller
    {
        private const string ConnectionString = "Server=localhost; Port=5433; Database=users; UserId=postgres; Password=123; CommandTimeout=120;";

       

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Cart()
        {
            return View();
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

        public IActionResult Account()
        {
            return View();
        }
    }
}
