using Framework;
using Framework.Attributes;
using System.Text.Json;
using Framework.HttpResponses;


namespace Server.Controllers
{
    [HttpController("auth")]
    public class AuthController  : ControllerBase
    {
        [Get("login")]
        public string Login()
        {
            return "entrance.html";
        }

        [Post("login")]
        public IHttpResponseTypeResult Login([FormData] string login, [FormData] string password)
        {
            if (login == "admin" && password == "admin123")
            {
                return Html("<h1>Вход выполнен</h1><p><a href=\"/admin.html\">Перейти в админ-панель</a></p>");
            }

            return Html("<h1>Неверный логин или пароль</h1><p><a href=\"/auth/login\">Вернуться ко входу</a></p>");
        }
    }
}