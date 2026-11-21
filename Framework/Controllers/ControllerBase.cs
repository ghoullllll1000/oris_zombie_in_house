using Framework.Authentication;
using Framework.HttpResponses;
using System.Net;
using TemplateEngine;

namespace Framework.Controllers
{
    public class ControllerBase
    {
        protected HttpListenerContext Context { get; private set; }
        protected string ConnectionString { get; private set; } = string.Empty;
        private static readonly IHtmlTemplateRenderer TemplateRenderer = new HtmlTemplateRenderer();
        protected IUserSessionService SessionService { get; private set; } = null!;

        public void SetSessionService(IUserSessionService sessionService)
        {
            SessionService = sessionService;
        }

        protected bool IsAdminAuthenticated()
        {
            string? sessionId = Context.Request.Cookies["sessionId"]?.Value;
            return SessionService != null && SessionService.GetUserId(sessionId).HasValue;
        }

        public void SetContext(HttpListenerContext context)
        {
            Context = context;
        }

        public void SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString ?? string.Empty;
        }

        protected JsonResult Json(object obj) => new JsonResult(obj);

        protected HtmlResult Html(string html) => new HtmlResult(html);

        protected IHttpResponseTypeResult RenderStaticPage(string fileName)
        {
            string staticRoot = Path.Combine(Directory.GetCurrentDirectory(), "static");
            string filePath = Path.Combine(staticRoot, fileName);

            if (File.Exists(filePath))
            {
                return Html(File.ReadAllText(filePath));
            }

            Context.Response.StatusCode = 404;
            string notFoundPath = Path.Combine(staticRoot, "404.html");
            return Html(File.ReadAllText(notFoundPath));
        }

        protected IHttpResponseTypeResult Redirect(string url)
        {
            return new RedirectResult(url);
        }
        protected IHttpResponseTypeResult RenderTemplate(string templatePath, object model)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);
            string html = TemplateRenderer.RenderFromFile(fullPath, model);
            return Html(html);
        }
    }
}
