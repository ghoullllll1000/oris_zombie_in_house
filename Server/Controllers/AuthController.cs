using Framework.Attributes;
using Framework.Controllers;
using Framework.HttpResponses;
using System.Net;

namespace Server.Controllers
{
    [HttpController("auth")]
    public class AuthController : ControllerBase
    {
        [Get("login")]
        public IHttpResponseTypeResult Login() =>
        RenderTemplate("static/entrance.html", new
        {
            IsAdmin = IsAdminAuthenticated(),
            Login = "",
            LoginError = "",
            PasswordError = "",
            AuthError = ""
        });


        [Post("login")]
        public IHttpResponseTypeResult Login([FormData] string login, [FormData] string password)
        {
            login = login?.Trim() ?? string.Empty;
            password ??= string.Empty;

            string loginError = "";
            string passwordError = "";
            string authError = "";

            if (string.IsNullOrEmpty(login))
            {
                loginError = "Введите логин.";
            }

            if (string.IsNullOrEmpty(password))
            {
                passwordError = "Введите пароль.";
            }

            if (loginError != "" || passwordError != "")
            {
                return RenderTemplate("static/entrance.html", new
                {
                    IsAdmin = false,
                    Login = login,
                    LoginError = loginError,
                    PasswordError = passwordError,
                    AuthError = authError
                });
            }

            if (login == "admin" && password == "admin123")
            {
                const int adminUserId = 1;
                string sessionId = SessionService.CreateSession(adminUserId);

                var cookie = new Cookie("sessionId", sessionId, "/")
                {
                    HttpOnly = true,
                    Secure = Context.Request.IsSecureConnection,
                    Expires = DateTime.UtcNow.AddMinutes(30)
                };

                Context.Response.SetCookie(cookie);

                return Redirect("/admin/index");
            }

            authError = "Неверный логин или пароль.";

            return RenderTemplate("static/entrance.html", new
            {
                IsAdmin = false,
                Login = login,
                LoginError = loginError,
                PasswordError = passwordError,
                AuthError = authError
            });
        }

        [Post("logout")]
        public IHttpResponseTypeResult Logout()
        {
            string? sessionId = Context.Request.Cookies["sessionId"]?.Value;
            SessionService.RemoveSession(sessionId);

            var cookie = new Cookie("sessionId", string.Empty, "/")
            {
                HttpOnly = true,
                Secure = Context.Request.IsSecureConnection,
                Expires = DateTime.UtcNow.AddDays(-1)
            };
            Context.Response.SetCookie(cookie);

            return Redirect("/auth/login");
        }
    }
}