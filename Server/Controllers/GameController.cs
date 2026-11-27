using System;
using System.Linq;
using System.Threading.Tasks;
using Framework.Attributes;
using Framework.Controllers;
using Framework.HttpResponses;
using ORM;
using Server.Models;

namespace Server.Controllers
{
    [HttpController("home")]
    public class GameController : ControllerBase
    {
        [Get("index")]
        public IHttpResponseTypeResult Index() => RenderTemplate("static/index.html", new { IsAdmin = IsAdminAuthenticated() });

        [Get("about")]
        public IHttpResponseTypeResult About() => RenderTemplate("static/about.html", new { IsAdmin = IsAdminAuthenticated() });

        [Get("characters")]
        public async Task<IHttpResponseTypeResult> Characters() => RenderTemplate("static/characters.html", new { IsAdmin = IsAdminAuthenticated()});

        [Get("characters-filter")]
        public async Task<IHttpResponseTypeResult> CharactersFilter()
        {
            string? charType = Context.Request.QueryString["char-type"]?.Trim().ToLowerInvariant();
            string? ability = Context.Request.QueryString["ability"]?.Trim().ToLowerInvariant();

            IOrmContext orm = new OrmContext(ConnectionString);

            var characters = (await orm.GetAllAsync<Character>("characters"))
                .OrderBy(x => x.Id)
                .AsEnumerable();

            var monsters = (await orm.GetAllAsync<Monster>("monsters"))
                .OrderBy(x => x.Id)
                .AsEnumerable();

            if (!string.IsNullOrEmpty(charType))
            {
                if (charType == "survivor")
                    monsters = Enumerable.Empty<Monster>();
                else if (charType == "zombie")
                    characters = Enumerable.Empty<Character>();
            }

            if (!string.IsNullOrEmpty(ability))
            {
                characters = ability switch
                {
                    "hp-3" => characters.Where(c => c.Health == 3),
                    "hp-5" => characters.Where(c => c.Health == 5),
                    "normal" or "boss" => Enumerable.Empty<Character>(),
                    _ => characters
                };

                monsters = ability switch
                {
                    "normal" => monsters.Where(m => MatchesCategory(m.Type, "Обычный")),
                    "boss" => monsters.Where(m => MatchesCategory(m.Type, "Босс")),
                    "hp-3" or "hp-5" => Enumerable.Empty<Monster>(),
                    _ => monsters
                };
            }

            var characterItems = characters.Select(c => new Dictionary<string, object?>
            {
                ["id"] = c.Id,
                ["name"] = c.Name,
                ["description"] = c.Feature,
                ["image"] = ResolveCharacterImage(c.Name)
            }).ToList();

            var monsterItems = monsters.Select(m => new Dictionary<string, object?>
            {
                ["id"] = m.Id,
                ["name"] = m.Name,
                ["description"] = m.Behavior,
                ["image"] = ResolveMonsterImage(m.Name)
            }).ToList();

            return Json(new Dictionary<string, object?>
            {
                ["characters"] = characterItems,
                ["monsters"] = monsterItems
            });
        }

        [Get("items")]
        public async Task<IHttpResponseTypeResult> Items()
        {
            string? itemType = Context.Request.QueryString["item-type"]?.Trim().ToLowerInvariant();
            string? itemAbility = Context.Request.QueryString["item-ability"]?.Trim().ToLowerInvariant();

            IOrmContext orm = new OrmContext(ConnectionString);

            var weapons = (await orm.GetAllAsync<Weapon>("weapons"))
                .OrderBy(x => x.Id)
                .AsEnumerable();

            var items = (await orm.GetAllAsync<Item>("items"))
                .OrderBy(x => x.Id)
                .AsEnumerable();

            if (!string.IsNullOrEmpty(itemType))
            {
                if (itemType == "weapon")
                {
                    items = Enumerable.Empty<Item>();
                }
                else if (itemType == "things" || itemType == "thing")
                {
                    weapons = Enumerable.Empty<Weapon>();
                }
            }

            if (!string.IsNullOrEmpty(itemAbility))
            {
                weapons = itemAbility switch
                {
                    "melee" => weapons.Where(w => MatchesCategory(w.Category, "Холодное оружие")),
                    "firearm" => weapons.Where(w => MatchesCategory(w.Category, "Огнестрельное оружие")),
                    "explosive" => weapons.Where(w => MatchesCategory(w.Category, "Взрывчатка")),
                    "special" => weapons.Where(w => MatchesCategory(w.Category, "Специальное оружие")),
                    "health" or "defense" or "car" => Enumerable.Empty<Weapon>(),
                    _ => weapons
                };

                items = itemAbility switch
                {
                    "health" => items.Where(i => MatchesCategory(i.Usage, "Здоровье")),
                    "defense" => items.Where(i => MatchesCategory(i.Usage, "Защита проёмов")),
                    "car" => items.Where(i => MatchesCategory(i.Usage, "Машина")),
                    "melee" or "firearm" or "explosive" or "special" => Enumerable.Empty<Item>(),
                    _ => items
                };
            }

            var weaponList = weapons.Select(w => new
            {
                w.Id,
                w.Name,
                w.Category,
                w.Effect,
                Image = ResolveWeaponImage(w.Name)
            }).ToList();

            var itemList = items.Select(i => new
            {
                i.Id,
                i.Name,
                i.Usage,
                i.Effect,
                Image = ResolveItemImage(i.Name)
            }).ToList();

            var model = new
            {
                IsAdmin = IsAdminAuthenticated(),
                IsEmpty = (weaponList.Count + itemList.Count) == 0,
                Weapons = weaponList,
                Items = itemList
            };

            return RenderTemplate("static/items.html", model);
        }

        private static bool MatchesCategory(string? actual, string expected)
        {
            return string.Equals(
                actual?.Trim().Replace('ё', 'е'),
                expected.Trim().Replace('ё', 'е'),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveCharacterImage(string? name) => name?.Trim().ToLowerInvariant() switch
        {
            "саша" => "images/sasha.png",
            "настя" => "images/nastya.png",
            "надя" => "images/nadya.png",
            "макс" => "images/max.png",
            "борис" or "боря" => "images/boris.png",
            _ => "images/person.svg"
        };

        private static string ResolveMonsterImage(string? name)
        {
            var key = name?.Trim().ToLowerInvariant().Replace('ё', 'е');
            return key switch
            {
                "зомби" => "images/zombie.png",
                "дьявольские псы" or "дьявольский пес" => "images/dog.png",
                "пауки-мутанты" or "паук-мутант" => "images/spider.png",
                "болотный ужас" => "images/boss.png",
                _ => "images/monster.png"
            };
        }

        private static string ResolveWeaponImage(string? name)
        {
            var key = name?.Trim().ToLowerInvariant().Replace('ё', 'е');
            return key switch
            {
                "нож" => "images/knife.png",
                "арбалет" => "images/crossbow.png",
                "топор" => "images/axe.png",
                "пистолет" => "images/gun.png",
                "автомат" => "images/machine.png",
                "дробовик" => "images/shotgun.png",
                "граната" => "images/grenade.png",
                "гранатомет" => "images/flamethrower.png",
                _ => "images/weapon.png"
            };
        }

        private static string ResolveItemImage(string? name)
        {
            var key = name?.Trim().ToLowerInvariant().Replace('ё', 'е');
            return key switch
            {
                "аптечка" => "images/pharmacy.png",
                "доски" => "images/boards.png",
                "канистра с бензином" or "канистра" => "images/canister.png",
                "ключ" => "images/key.png",
                _ => "images/item.png"
            };
        }
    }
}