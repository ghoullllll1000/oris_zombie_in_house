using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

namespace ORM
{
    public class EntityMetadata
    {
        public string TableName { get; }

        public PropertyInfo[] Properties { get; }

        public PropertyInfo PrimaryKey { get; }

        public EntityMetadata(Type entityType)
        {
            TableAttribute? tableAttribute =
                entityType.GetCustomAttribute<TableAttribute>();

            TableName = tableAttribute?.Name
                ?? entityType.Name.ToLowerInvariant();

            Properties = entityType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    p.CanRead &&
                    p.CanWrite &&
                    p.GetMethod?.IsPublic == true &&
                    p.SetMethod?.IsPublic == true)
                .ToArray();

            PrimaryKey = Properties.FirstOrDefault(p =>
                p.GetCustomAttribute<KeyAttribute>() != null)
                ?? throw new InvalidOperationException(
                    $"У модели {entityType.Name} не найден первичный ключ.");
        }

        public string GetColumnName(PropertyInfo property)
        {
            ColumnAttribute? columnAttribute =
                property.GetCustomAttribute<ColumnAttribute>();

            return columnAttribute?.Name
                ?? property.Name.ToLowerInvariant();
        }
    }
}
