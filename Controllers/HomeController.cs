using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using FreeGeoIPCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ShortnerService.Models;
using UAParser;
using MimeKit;
using MailKit.Net.Smtp;


namespace ShortnerService.Controllers
{
    [Authorize(Roles = "admin")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DatabaseContext _context;
        private readonly IActionContextAccessor _accessor;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _hosting;

        public HomeController(ILogger<HomeController> logger, DatabaseContext context, IActionContextAccessor accessor, IWebHostEnvironment hosting)
        {
            _context = context;
            _logger = logger;
            _accessor = accessor;
            _hosting = hosting;
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        [Route("{token}")]
        [HttpGet]
        public async Task<IActionResult> Index(string token)
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                Link l = _context.Links.FirstOrDefault(a => a.Token == token);

                if(l != null)
                {
                    Statistic statistic = new Statistic();
                    statistic.IP = _accessor.ActionContext.HttpContext.Connection.RemoteIpAddress.ToString();
                    statistic.DateTime = DateTime.Now;
                    var userAgent = Request.Headers["User-Agent"];
                    string uaString = Convert.ToString(userAgent[0]);

                    var uaParser = Parser.GetDefault();
                    ClientInfo c = uaParser.Parse(uaString);

                    statistic.Browser = c.UA.ToString();
                    statistic.OS = c.OS.ToString();

                    try
                    {
                        var response = await _httpClient.GetAsync($"http://ip-api.com/json/{statistic.IP}");

                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();

                            GeoInfo f = JsonConvert.DeserializeObject<GeoInfo>(json);

                            statistic.City = f.City;
                            statistic.Country = f.Country;
                        }

                       
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    try
                    {
                        if (l.Notification)
                        {
                            await SendEmailAsync(HttpContext.User.Identity.Name, $"Following a link: Link - {l.Url} | LinkToken - {l.Token} | IP - {statistic.IP} | Country - {statistic.Country} | City - {statistic.City}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    l.Statistics.Add(statistic);
                    await _context.SaveChangesAsync();
                }

                return Redirect(l.Url);
            }
            else
            {
                return RedirectToAction("Login", new { redirect=token });
            }
        }

        public async Task<IActionResult> Links()
        {
            ViewBag.URL = _accessor.ActionContext.HttpContext.Request.Host;
            return View(await _context.Links.ToListAsync());
        }

        [Route("Link/{id}")]
        public async Task<IActionResult> Link(int id)
        {
            Link a = await _context.Links.Include(a => a.Statistics).FirstOrDefaultAsync(a=> a.Id == id);
      
            return View(a);
        }
        [HttpPost]
        public IActionResult DeleteLink(int id)
        {
            Link l = _context.Links.FirstOrDefault(a=> a.Id == id);
            _context.Remove(l);
            _context.SaveChanges();
            return RedirectToAction("Links");
        }
        [HttpPost]
        public IActionResult NotificationSet(int id, bool notific)
        {
            Link l = _context.Links.FirstOrDefault(a => a.Id == id);
            l.Notification = notific;
            _context.Update(l);
            _context.SaveChanges();
            return RedirectToAction("Links");
        }
        [HttpPost]
        public async Task<IActionResult> SaveLink(string url)
        {
            string token = Shortener.GenerateToken();

            List<Link> links = await _context.Links.ToListAsync();

            foreach (var l in links)
            {
                if (l.Url == url)
                {
                    return Content("This link has already been created");
                }
                if (l.Token == token)
                {
                    token = Shortener.GenerateToken();
                }
            }

           

            Link link = new Link() { Url = url, Token = token };
            _context.Links.Add(link);
            _context.SaveChanges();
            return RedirectToAction("Links");
        }
        [HttpGet]
        public IActionResult Settings()
        {
            User a = _context.Users.FirstOrDefault(a => a.Email == HttpContext.User.Identity.Name);
            return View(a);
        }
        [HttpPost]
        public async Task<IActionResult> Settings(User user)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string redirect = null)
        {
            ViewBag.Redirect = redirect;
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(User u, string redirect = null)
        {
            User user = await _context.Users
                   .Include(u => u.Role)
                   .FirstOrDefaultAsync(u => u.Email == u.Email && u.Password == u.Password);
            if (user != null && u.Email == user.Email)
            {
                await Authenticate(user); // аутентификация

                if(redirect != null)
                {
                    Link l = _context.Links.FirstOrDefault(a => a.Token == redirect);
                    if (l != null)
                    {
                        return Redirect(l.Url);
                    }
                    else return Content("Link is not find");
                }
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        [AllowAnonymous]
        public IActionResult AccessDanied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task SendEmailAsync(string email, string message)
        {
            var emailmessage = new MimeMessage();

            emailmessage.From.Add(new MailboxAddress("Shortenet Link", HttpContext.User.Identity.Name));
            emailmessage.To.Add(new MailboxAddress("", email));
            emailmessage.Subject = "Open link";
            emailmessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message };

            using(var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 465);
                await client.AuthenticateAsync("shortenerservice@gmail.com", "mpvsqwerty");
                await client.SendAsync(emailmessage);
                await client.DisconnectAsync(true);
            }
        }

        private async Task Authenticate(User user)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role?.Name)
            };
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }
    }
}
