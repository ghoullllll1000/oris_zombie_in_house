
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ORM
{

    public class EntityMetadata
    {
        public string TableName { get; }

        public PropertyInfo[] Properties { get; }

        public PropertyInfo[] NavigationProperties { get; }

        public PropertyInfo PrimaryKey { get; }

        public EntityMetadata(Type entityType)
        {
            TableAttribute? tableAttribute = entityType.GetCustomAttribute<TableAttribute>();

            TableName = tableAttribute?.Name ?? entityType.Name.ToLowerInvariant();

            PropertyInfo[] allProperties = entityType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    p.CanRead &&
                    p.CanWrite &&
                    p.GetMethod?.IsPublic == true &&
                    p.SetMethod?.IsPublic == true)
                .ToArray();

            Properties = allProperties
                .Where(p => IsScalar(p.PropertyType))
                .ToArray();

            NavigationProperties = allProperties
                .Where(p => !IsScalar(p.PropertyType))
                .ToArray();

            PrimaryKey = Properties.FirstOrDefault(p =>
                p.GetCustomAttribute<KeyAttribute>() != null)
                ?? throw new InvalidOperationException(
                    $"У модели {entityType.Name} не найден первичный ключ.");
        }

        public string GetColumnName(PropertyInfo property)
        {
            ColumnAttribute? columnAttribute = property.GetCustomAttribute<ColumnAttribute>();

            return columnAttribute?.Name ?? property.Name.ToLowerInvariant();
        }

        private static bool IsScalar(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(DateOnly) ||
                   type == typeof(TimeOnly) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid) ||
                   type == typeof(byte[]);
        }
    }
}
