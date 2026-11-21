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
                IsAuthenticated = IsAdminAuthenticated(),
                AuthError = false
            });

        [Post("login")]
        public IHttpResponseTypeResult Login([FormData] string login, [FormData] string password)
        {
            if (login?.Trim() == "admin" && password == "admin123")
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

                return RenderTemplate("static/entrance.html", new
                {
                    IsAuthenticated = true,
                    AuthError = false
                });
            }

            return RenderTemplate("static/entrance.html", new
            {
                IsAuthenticated = false,
                AuthError = true
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