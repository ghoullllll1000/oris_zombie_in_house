using Framework.Attributes;
using Framework.Controllers;

namespace Framework.UnitTests
{

    [HttpController("test")]
    public class TestController : ControllerBase
    {
        [Get("hello")]
        public string Hello()
        {
            return "Hello!";
        }

        [Get("query")]
        public string Query([QueryData] string name)
        {
            return name;
        }

        [Post("form")]
        public string Form([FormData] string name)
        {
            return name;
        }

        [Authorize]
        [Get("secret")]
        public string Secret()
        {
            return "Secret!";
        }
    }
}