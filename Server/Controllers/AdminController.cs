using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Framework.Attributes;
using Framework.Controllers;
using Framework.HttpResponses;
using ORM;
using Server.Models;

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
        public async Task<IHttpResponseTypeResult> Edit()
        {
            if (!IsAdminAuthenticated())
            {
                return RedirectToLogin();
            }

            string? type = Context.Request.QueryString["type"]?.Trim().ToLowerInvariant();
            if (!int.TryParse(Context.Request.QueryString["id"], out int id))
            {
                return Redirect("/admin/index");
            }

            IOrmContext orm = new OrmContext(ConnectionString);

            object? entityModel = type switch
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
        public async Task<IHttpResponseTypeResult> Create()
        {
            if (!IsAdminAuthenticated())
            {
                return RedirectToLogin();
            }

            var form = await ReadFormAsync(Context.Request);
            string type = GetValue(form, "type").ToLowerInvariant();
            string name = GetValue(form, "name");
            string categoryOrType = GetValue(form, "category");
            string description = GetValue(form, "description");
            int health = int.TryParse(GetValue(form, "health"), out int hp) ? hp : 3;

            if (string.IsNullOrWhiteSpace(name))
            {
                return Redirect("/admin/index");
            }

            IOrmContext orm = new OrmContext(ConnectionString);

            switch (type)
            {
                case "character":
                    await orm.InsertAsync("characters", new Character
                    {
                        Name = name,
                        Health = health,
                        Feature = description
                    });
                    break;

                case "monster":
                    await orm.InsertAsync("monsters", new Monster
                    {
                        Name = name,
                        Type = string.IsNullOrWhiteSpace(categoryOrType) ? "Обычный" : categoryOrType,
                        Behavior = description
                    });
                    break;

                case "weapon":
                    await orm.InsertAsync("weapons", new Weapon
                    {
                        Name = name,
                        Category = string.IsNullOrWhiteSpace(categoryOrType) ? "Холодное оружие" : categoryOrType,
                        Effect = description
                    });
                    break;

                case "item":
                    await orm.InsertAsync("items", new Item
                    {
                        Name = name,
                        Usage = string.IsNullOrWhiteSpace(categoryOrType) ? "Здоровье" : categoryOrType,
                        Effect = description
                    });
                    break;
            }

            return Redirect("/admin/index");
        }

        [Post("update")]
        [Authorize]
        public async Task<IHttpResponseTypeResult> Update()
        {
            if (!IsAdminAuthenticated())
            {
                return RedirectToLogin();
            }

            var form = await ReadFormAsync(Context.Request);
            string type = GetValue(form, "type").ToLowerInvariant();
            if (!int.TryParse(GetValue(form, "id"), out int id))
            {
                return Redirect("/admin/index");
            }

            string name = GetValue(form, "name");
            string categoryOrType = GetValue(form, "category");
            string description = GetValue(form, "description");
            int health = int.TryParse(GetValue(form, "health"), out int hp) ? hp : 3;

            IOrmContext orm = new OrmContext(ConnectionString);

            switch (type)
            {
                case "character":
                    await orm.UpdateAsync("characters", id, new Character
                    {
                        Id = id,
                        Name = name,
                        Health = health,
                        Feature = description
                    });
                    break;

                case "monster":
                    await orm.UpdateAsync("monsters", id, new Monster
                    {
                        Id = id,
                        Name = name,
                        Type = categoryOrType,
                        Behavior = description
                    });
                    break;

                case "weapon":
                    await orm.UpdateAsync("weapons", id, new Weapon
                    {
                        Id = id,
                        Name = name,
                        Category = categoryOrType,
                        Effect = description
                    });
                    break;

                case "item":
                    await orm.UpdateAsync("items", id, new Item
                    {
                        Id = id,
                        Name = name,
                        Usage = categoryOrType,
                        Effect = description
                    });
                    break;
            }

            return Redirect("/admin/index");
        }

        [Post("delete")]
        public async Task<IHttpResponseTypeResult> Delete()
        {
            if (!IsAdminAuthenticated())
            {
                return RedirectToLogin();
            }

            var form = await ReadFormAsync(Context.Request);
            string type = GetValue(form, "type").ToLowerInvariant();
            if (!int.TryParse(GetValue(form, "id"), out int id))
            {
                return Redirect("/admin/index");
            }

            string? tableName = type switch
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
                Category = entity.Effect,
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