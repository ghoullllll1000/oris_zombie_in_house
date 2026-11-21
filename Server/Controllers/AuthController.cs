using Framework.Attributes;
using System.Text.Json;
using Framework.HttpResponses;
using Framework.Controllers;


namespace Server.Controllers
{
    [HttpController("auth")]
    public class AuthController  : ControllerBase
    {
        [Get("login")]
        public IHttpResponseTypeResult Login() => RenderTemplate("static/entrance.html", new { IsAdmin = IsAdminAuthenticated() });

        [Post("login")]
        public IHttpResponseTypeResult Login([FormData] string login, [FormData] string password)
        {
            if (login == "admin" && password == "admin123")
            {
                var cookie = new System.Net.Cookie("admin_auth", "admin_logged_in")
                {
                    Path = "/",
                    HttpOnly = true
                };
                Context.Response.Cookies.Add(cookie);

                return Html("<h1>Вход выполнен</h1><p><a href=\"/admin/index\">Перейти в админ-панель</a></p>");
            }

            return Html("<h1>Неверный логин или пароль</h1><p><a href=\"/auth/login\">Вернуться ко входу</a></p>");
        }
    }
}