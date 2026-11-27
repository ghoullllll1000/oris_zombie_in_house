using Microsoft.VisualStudio.TestTools.UnitTesting;
using TemplateEngine;

namespace TemplateEngine.UnitTests
{
    [TestClass]
    public class HtmlTemplateRendererTests
    {
        private HtmlTemplateRenderer _renderer = null!;

        [TestInitialize]
        public void Setup()
        {
            _renderer = new HtmlTemplateRenderer();
        }

        [TestMethod]
        public void RenderFromString_OneProperty_ReplacesValue()
        {
            var model = new { Name = "Пистолет" };
            string template = "<p>${Name}</p>";

            string result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("<p>Пистолет</p>", result);
        }

        [TestMethod]
        public void RenderFromString_TwoProperties_ReplacesBoth()
        {
            var model = new { Name = "Ключ", Role = "Открывает дверь" };
            string template = "<h1>${Name}</h1><span>${Role}</span>";

            string result = _renderer.RenderFromString(template, model);

            Assert.AreEqual("<h1>Ключ</h1><span>Открывает дверь</span>", result);
        }

        [TestMethod]
        public void RenderFromString_EmptyTemplate_ReturnsEmpty()
        {
            var model = new { Name = "Тест" };

            string result = _renderer.RenderFromString("", model);

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_IfCondition_TrueAndFalse()
        {
            string template = "$if Show\n<b>Видно</b>\n$endif";

            string whenTrue = _renderer.RenderFromString(template, new { Show = true });
            string whenFalse = _renderer.RenderFromString(template, new { Show = false });

            StringAssert.Contains(whenTrue, "Видно");
            Assert.IsFalse(whenFalse.Contains("Видно"));
        }

        [TestMethod]
        public void RenderFromString_Foreach_RendersItems()
        {
            var model = new
            {
                Items = new[]
                {
                    new { Name = "Аптечка" },
                    new { Name = "Бинт" }
                }
            };
            string template = "$foreach item in Items\n<li>${Name}</li>\n$endfor";

            string result = _renderer.RenderFromString(template, model);

            StringAssert.Contains(result, "<li>Аптечка</li>");
            StringAssert.Contains(result, "<li>Бинт</li>");
        }
    }
}