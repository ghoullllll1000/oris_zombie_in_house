
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;

namespace Server.Validation
{
    public class EntityValidator
    {
        public List<string> Validate(
            string type,
            string name,
            string category,
            string description,
            int? health = null)
        {
            List<string> errors = new List<string>();

            name = (name ?? "").Trim();
            category = (category ?? "").Trim();
            description = (description ?? "").Trim();
            type = (type ?? "").Trim().ToLower();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Введите имя.");
            }
            else if (name.Length < 2 || name.Length > 100)
            {
                errors.Add("Имя должно содержать от 2 до 100 символов.");
            }
            else if (!Regex.IsMatch(name, @"^[а-яА-ЯёЁa-zA-Z0-9 -]+$"))
            {
                errors.Add("Имя содержит недопустимые символы.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                errors.Add("Введите описание.");
            }
            else if (description.Length < 3 || description.Length > 1000)
            {
                errors.Add("Описание должно содержать от 3 до 1000 символов.");
            }
            else if (!Regex.IsMatch(description, @"^[а-яА-ЯёЁa-zA-Z0-9\s.,!?():;'«»\-+/]+$"))
            {
                errors.Add("Описание содержит недопустимые символы.");
            }

            switch (type)
            {
                case "character":
                    string characterHealth = health.HasValue
                        ? health.Value.ToString()
                        : category;

                    if (characterHealth != "3" && characterHealth != "5")
                    {
                        errors.Add("Здоровье персонажа должно быть 3 или 5.");
                    }
                    break;

                case "monster":
                    if (category != "Обычный" && category != "Босс")
                    {
                        errors.Add("Выберите правильный тип монстра.");
                    }
                    break;

                case "weapon":
                    if (category != "Холодное оружие" &&
                        category != "Огнестрельное оружие" &&
                        category != "Взрывчатка" &&
                        category != "Специальное оружие")
                    {
                        errors.Add("Выберите правильную категорию оружия.");
                    }
                    break;

                case "item":
                    if (category != "Здоровье" &&
                        category != "Защита проёмов" &&
                        category != "Машина")
                    {
                        errors.Add("Выберите правильный тип предмета.");
                    }
                    break;

                default:
                    errors.Add("Неизвестный тип сущности.");
                    break;
            }

            return errors;
        }
    }
}
