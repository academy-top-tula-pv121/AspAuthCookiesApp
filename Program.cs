using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace AspAuthCookiesApp
{
    class User
    {
        public string Login { set; get; }
        public string Password { set; get; }
    };
    public class Program
    {
        public static void Main(string[] args)
        {
            var users = new List<User>
            {
                new(){ Login = "bob", Password = "12345"},
                new(){ Login = "joe", Password = "54321"},
            };

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                            .AddCookie(options => options.LoginPath = "/login");
            builder.Services.AddAuthorization();
            var app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/login", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                string loginForm = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Auth Page</title>
</head>
<body>
    <form method='post'>
        <h3>Sign in to site</h3>
        <p>
            <label>Input login:</label><br>
            <input type='text' name='login'>
        </p>
        <p>
            <label>Input password:</label><br>
            <input type='password' name='password'>
        </p>
        <input type='submit' value='Log in'>
    </form>
</body>
</html>";
                await context.Response.WriteAsync(loginForm);
            });

            app.MapPost("/login", async (string? returnUrl, HttpContext context) =>
            {
                var form = context.Request.Form;
                if (!form.ContainsKey("login") || !form.ContainsKey("password"))
                    return Results.BadRequest("Login or/and password undefined");

                string login = form["login"];
                string password = form["password"];

                User? user = users.FirstOrDefault(u => u.Login == login && u.Password == password);
                if (user is null)
                    return Results.Unauthorized();

                var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.Login) };
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");

                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                return Results.Redirect(returnUrl ?? "/");

            });

            app.MapGet("/", () => "Hello World!");
            
            app.MapGet("/admin", [Authorize] () => "Admin page");

            app.Run();
        }
    }
}