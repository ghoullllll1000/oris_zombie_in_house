using Framework;
using Framework.Attributes;
using Framework.HttpResponses;

namespace Server.Controllers
{
    [HttpController("home")]
    public class GameController : ControllerBase
    {
        [Get("index")]
        public IHttpResponseTypeResult Index() => RenderStaticPage("index.html");

        [Get("about")]
        public IHttpResponseTypeResult About() => RenderStaticPage("about.html");

        [Get("characters")]
        public IHttpResponseTypeResult Characters() => RenderStaticPage("characters.html");

        [Get("items")]
        public IHttpResponseTypeResult Items() => RenderStaticPage("items.html");
    }
}