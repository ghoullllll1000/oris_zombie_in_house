using Framework.HttpResponses;
using System.Net;
using TemplateEngine;

namespace Framework
{
    public class ControllerBase
    {
        protected HttpListenerContext Context { get; private set; }
        private static readonly IHtmlTemplateRenderer TemplateRenderer = new HtmlTemplateRenderer();

        internal void SetContext(HttpListenerContext context)
        {
            Context = context;
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
