using Framework.Attributes;
using Framework.Controllers;
using Framework.HttpResponses;
using ORM;
using Server.Models;
using System.Net;
using System.Text;
using System.Web;

namespace Server.Controllers
{
    [HttpController("admin")]
    public class AdminController : ControllerBase
    {
        private IHttpResponseTypeResult RedirectToLogin()
        {
            return Redirect("/auth/login");
        }

        [Get("index")]
        [Authorize]
        public async Task<IHttpResponseTypeResult> Index()
        {
            if (!IsAdminAuthenticated())
            {
                return RedirectToLogin();
            }

            IOrmContext orm = new OrmContext(ConnectionString);
            var entities = await LoadAllEntitiesAsync(orm);

            var model = new
            {
                IsAdmin = true,
                Entities = entities
            };

            return RenderTemplate("static/admin.html", model);
        }

        [Get("edit")]
        [Authorize]
        public async Task<IHttpResponseTypeResult> Edit([QueryData] string type, [QueryData] int id)
        {
            string normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            if (id <= 0)
            {
                return Redirect("/admin/index");
            }

            IOrmContext orm = new OrmContext(ConnectionString);

            object? entityModel = normalizedType switch
            {
                "character" => await BuildCharacterEditModel(orm, id),
                "monster" => await BuildMonsterEditModel(orm, id),
                "weapon" => await BuildWeaponEditModel(orm, id),
                "item" => await BuildItemEditModel(orm, id),
                _ => null
            };

            if (entityModel == null)
            {
                return Redirect("/admin/index");
            }

            return RenderTemplate("static/edit.html", entityModel);
        }

        [Post("create")]
        [Authorize]
        public async Task<IHttpResponseTypeResult> Create(
            [FormData] string type,
            [FormData] string name,
            [FormData] string category,
            [FormData] string description,
            [FormData] int? health)
        {
            string normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            string cleanName = (name ?? string.Empty).Trim();
            string cleanCategory = (category ?? string.Empty).Trim();
            string cleanDescription = (description ?? string.Empty).Trim();
            int hp = health.HasValue && health.Value > 0 ? health.Value : 3;

            if (string.IsNullOrWhiteSpace(cleanName))
            {
                return Redirect("/admin/index");
            }

            IOrmContext orm = new OrmContext(ConnectionString);

            switch (normalizedType)
            {
                case "character":
                    await orm.InsertAsync("characters", new Character
                    {
                        Name = cleanName,
                        Health = hp,
                        Feature = cleanDescription
                    });
                    break;

                case "monster":
                    await orm.InsertAsync("monsters", new Monster
                    {
                        Name = cleanName,
                        Type = string.IsNullOrWhiteSpace(cleanCategory) ? "Обычный" : cleanCategory,
                        Behavior = cleanDescription
                    });
                    break;

                case "weapon":
                    await orm.InsertAsync("weapons", new Weapon
                    {
                        Name = cleanName,
                        Category = string.IsNullOrWhiteSpace(cleanCategory) ? "Холодное оружие" : cleanCategory,
                        Effect = cleanDescription
                    });
                    break;

                case "item":
                    await orm.InsertAsync("items", new Item
                    {
                        Name = cleanName,
                        Usage = string.IsNullOrWhiteSpace(cleanCategory) ? "Здоровье" : cleanCategory,
                        Effect = cleanDescription
                    });
                    break;
            }

            return Redirect("/admin/index");
        }

        [Post("update")]
        [Authorize]
        public async Task<IHttpResponseTypeResult> Update(
            [FormData] int id,
            [FormData] string type,
            [FormData] string name,
            [FormData] string category,
            [FormData] string description,
            [FormData] int? health)
        {
            if (id <= 0)
            {
                return Redirect("/admin/index");
            }

            string normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            string cleanName = (name ?? string.Empty).Trim();
            string cleanCategory = (category ?? string.Empty).Trim();
            string cleanDescription = (description ?? string.Empty).Trim();
            int hp = health.HasValue && health.Value > 0 ? health.Value : 3;

            IOrmContext orm = new OrmContext(ConnectionString);

            switch (normalizedType)
            {
                case "character":
                    await orm.UpdateAsync("characters", id, new Character
                    {
                        Id = id,
                        Name = cleanName,
                        Health = hp,
                        Feature = cleanDescription
                    });
                    break;

                case "monster":
                    await orm.UpdateAsync("monsters", id, new Monster
                    {
                        Id = id,
                        Name = cleanName,
                        Type = cleanCategory,
                        Behavior = cleanDescription
                    });
                    break;

                case "weapon":
                    await orm.UpdateAsync("weapons", id, new Weapon
                    {
                        Id = id,
                        Name = cleanName,
                        Category = cleanCategory,
                        Effect = cleanDescription
                    });
                    break;

                case "item":
                    await orm.UpdateAsync("items", id, new Item
                    {
                        Id = id,
                        Name = cleanName,
                        Usage = cleanCategory,
                        Effect = cleanDescription
                    });
                    break;
            }

            return Redirect("/admin/index");
        }

        [Post("delete")]
        [Authorize]
        public async Task<IHttpResponseTypeResult> Delete([FormData] string type, [FormData] int id)
        {
            if (id <= 0)
            {
                return Redirect("/admin/index");
            }

            string normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            string? tableName = normalizedType switch
            {
                "character" => "characters",
                "monster" => "monsters",
                "weapon" => "weapons",
                "item" => "items",
                _ => null
            };

            if (tableName != null)
            {
                IOrmContext orm = new OrmContext(ConnectionString);
                await orm.DeleteAsync(tableName, id);
            }

            return Redirect("/admin/index");
        }

        private static async Task<object?> BuildCharacterEditModel(IOrmContext orm, int id)
        {
            var entity = (await orm.GetAllAsync<Character>("characters")).FirstOrDefault(x => x.Id == id);
            if (entity == null) return null;

            return new
            {
                IsAdmin = true,
                EntityType = "character",
                entity.Id,
                entity.Name,
                Category = entity.Health.ToString(),
                Description = entity.Feature
            };
        }

        private static async Task<object?> BuildMonsterEditModel(IOrmContext orm, int id)
        {
            var entity = (await orm.GetAllAsync<Monster>("monsters")).FirstOrDefault(x => x.Id == id);
            if (entity == null) return null;

            return new
            {
                IsAdmin = true,
                EntityType = "monster",
                entity.Id,
                entity.Name,
                Category = entity.Type,
                Description = entity.Behavior
            };
        }

        private static async Task<object?> BuildWeaponEditModel(IOrmContext orm, int id)
        {
            var entity = (await orm.GetAllAsync<Weapon>("weapons")).FirstOrDefault(x => x.Id == id);
            if (entity == null) return null;

            return new
            {
                IsAdmin = true,
                EntityType = "weapon",
                entity.Id,
                entity.Name,
                Category = entity.Category,
                Description = entity.Effect
            };
        }

        private static async Task<object?> BuildItemEditModel(IOrmContext orm, int id)
        {
            var entity = (await orm.GetAllAsync<Item>("items")).FirstOrDefault(x => x.Id == id);
            if (entity == null) return null;

            return new
            {
                IsAdmin = true,
                EntityType = "item",
                entity.Id,
                entity.Name,
                Category = entity.Usage,
                Description = entity.Effect
            };
        }

        private static async Task<List<object>> LoadAllEntitiesAsync(IOrmContext orm)
        {
            var characters = (await orm.GetAllAsync<Character>("characters")).OrderBy(x => x.Id);
            var monsters = (await orm.GetAllAsync<Monster>("monsters")).OrderBy(x => x.Id);
            var weapons = (await orm.GetAllAsync<Weapon>("weapons")).OrderBy(x => x.Id);
            var items = (await orm.GetAllAsync<Item>("items")).OrderBy(x => x.Id);

            var result = new List<object>();

            result.AddRange(characters.Select(c => new
            {
                c.Id,
                c.Name,
                Type = "Персонаж",
                EntityType = "character",
                Features = c.Feature
            }));

            result.AddRange(monsters.Select(m => new
            {
                m.Id,
                m.Name,
                Type = "Монстр",
                EntityType = "monster",
                Features = m.Behavior
            }));

            result.AddRange(weapons.Select(w => new
            {
                w.Id,
                w.Name,
                Type = "Оружие",
                EntityType = "weapon",
                Features = w.Effect
            }));

            result.AddRange(items.Select(i => new
            {
                i.Id,
                i.Name,
                Type = "Предмет",
                EntityType = "item",
                Features = i.Effect
            }));

            return result;
        }

        private static async Task<Dictionary<string, string>> ReadFormAsync(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            string body = await reader.ReadToEndAsync();
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split('=', 2);
                string key = HttpUtility.UrlDecode(parts[0]) ?? string.Empty;
                string value = parts.Length > 1 ? (HttpUtility.UrlDecode(parts[1]) ?? string.Empty) : string.Empty;
                dict[key] = value.Trim();
            }

            return dict;
        }

        private static string GetValue(Dictionary<string, string> form, string key)
        {
            return form.TryGetValue(key, out string? val) ? val : string.Empty;
        }
    }
}