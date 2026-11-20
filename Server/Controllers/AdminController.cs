using Framework.Attributes;
using Framework.Controllers;
using Framework.HttpResponses;
using Server.Models;

namespace Server.Controllers
{
    [HttpController("admin")]
    public class AdminController : ControllerBase
    {
        private static readonly List<GameEntity> Entities = new()
        {
            new GameEntity { Id = 1, Name = "Настя", Type = EntityType.Character, Features = "Медсестра" },
            new GameEntity { Id = 2, Name = "Пистолет", Type = EntityType.Weapon, Features = "Огнестрельное" }
        };

        [Get("index")]
        public IHttpResponseTypeResult Index()
        {
            var model = new { Entities };
            return RenderTemplate("static/admin.html", model);
        }

        [Get("edit")]
        public IHttpResponseTypeResult Edit(int id)
        {
            var entity = Entities.FirstOrDefault(e => e.Id == id);
            if (entity == null)
            {
                return Redirect("/admin/index");
            }

            var model = new
            {
                Entities,
                Entity = entity
            };

            return RenderTemplate("static/edit.html", model);
        }

        [Post("create")]
        public IHttpResponseTypeResult Create(
            [FormData] string name,
            [FormData] string type,
            [FormData] string characteristics)
        {
            var entityType = ParseEntityType(type);
            int nextId = Entities.Count > 0 ? Entities.Max(e => e.Id) + 1 : 1;

            Entities.Add(new GameEntity
            {
                Id = nextId,
                Name = name ?? string.Empty,
                Type = entityType,
                Features = characteristics ?? string.Empty
            });

            return Redirect("/admin/index");
        }

        [Post("update")]
        public IHttpResponseTypeResult Update(
            [FormData] int id,
            [FormData] string name,
            [FormData] string type,
            [FormData] string characteristics)
        {
            var entity = Entities.FirstOrDefault(e => e.Id == id);
            if (entity != null)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    entity.Name = name;
                }

                if (!string.IsNullOrWhiteSpace(type))
                {
                    entity.Type = ParseEntityType(type);
                }

                entity.Features = characteristics ?? string.Empty;
            }

            return Redirect("/admin/index");
        }

        [Post("delete")]
        public IHttpResponseTypeResult Delete([FormData] int id)
        {
            var entity = Entities.FirstOrDefault(e => e.Id == id);
            if (entity != null)
            {
                Entities.Remove(entity);
            }

            return Redirect("/admin/index");
        }

        private static EntityType ParseEntityType(string? rawType)
        {
            return rawType?.ToLowerInvariant() switch
            {
                "character" => EntityType.Character,
                "monstr" or "monster" => EntityType.Monster,
                "weapon" => EntityType.Weapon,
                "things" or "thing" => EntityType.Thing,
                _ => EntityType.Thing
            };
        }
    }
}