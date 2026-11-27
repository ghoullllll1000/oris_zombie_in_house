using Npgsql;
using ORM;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ORM
{
    public class OrmContext : IOrmContext
    {

        private readonly string _connectionString;

        public OrmContext(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
            _connectionString = connectionString;
        }

        public async Task<IReadOnlyList<T>> GetAllAsync<T>(
            string tableName,
            CancellationToken cancellationToken = default)
            where T : class, new()
        {
            EntityMetadata metadata = new(typeof(T));
            ValidateTableName(tableName, metadata);
            string safeTableName = EscapeIdentifier(metadata.TableName);
            string sql = $"SELECT * FROM {safeTableName}";
            var result = new List<T>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columns = GetColumns(reader);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(MapRow<T>(reader, metadata, columns));
            }
            return result;
        }

        public async Task<T?> GetByIdAsync<T>(
            string tableName,
            int id,
            CancellationToken cancellationToken = default)
            where T : class, new()
        {
            EntityMetadata metadata = new(typeof(T));
            ValidateTableName(tableName, metadata);

            string safeTableName = EscapeIdentifier(metadata.TableName);
            string primaryKey = EscapeIdentifier(metadata.GetColumnName(metadata.PrimaryKey));

            string sql = $"SELECT * FROM {safeTableName} " + $"WHERE {primaryKey} = @id LIMIT 1";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var columns = GetColumns(reader);

            return MapRow<T>(reader, metadata, columns);
        }

        public async Task<int> InsertAsync<T>(
            string tableName,
            T entity,
            CancellationToken cancellationToken = default)
            where T : class
        {
            EntityMetadata metadata = new(typeof(T));
            ValidateTableName(tableName, metadata);
            OrmEntityValidator.Validate(entity);

            PropertyInfo[] properties = metadata.Properties.Where(p => p != metadata.PrimaryKey).ToArray();

            string columns = string.Join(", ", properties.Select(p => EscapeIdentifier(metadata.GetColumnName(p))));

            string parameters = string.Join(", ", properties.Select((p, i) => $"@p{i}"));

            string sql = $"INSERT INTO {EscapeIdentifier(metadata.TableName)} " + $"({columns}) VALUES ({parameters})";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);

            AddParameters(command, properties, entity);

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<int> UpdateAsync<T>(
            string tableName,
            int id,
            T entity,
            CancellationToken cancellationToken = default)
            where T : class
        {
            EntityMetadata metadata = new(typeof(T));
            ValidateTableName(tableName, metadata);
            OrmEntityValidator.Validate(entity);

            PropertyInfo[] properties = metadata.Properties.Where(p => p != metadata.PrimaryKey).ToArray();

            string setClause = string.Join(", ", properties.Select((p, i) => $"{EscapeIdentifier(metadata.GetColumnName(p))} = @p{i}"));
            string primaryKey = EscapeIdentifier(metadata.GetColumnName(metadata.PrimaryKey));
            string sql = $"UPDATE {EscapeIdentifier(metadata.TableName)} " + $"SET {setClause} WHERE {primaryKey} = @id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);

            AddParameters(command, properties, entity);
            command.Parameters.AddWithValue("id", id);

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<int> DeleteAsync(
            string tableName,
            int id,
            CancellationToken cancellationToken = default)
        {
            string safeTableName = EscapeIdentifier(tableName);

            string sql =
                $"DELETE FROM {safeTableName} WHERE \"id\" = @id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static Dictionary<string, int> GetColumns(
            NpgsqlDataReader reader)
        {
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns[reader.GetName(i)] = i;
            }
            return columns;
        }

        private static T MapRow<T>(
            NpgsqlDataReader reader,
            EntityMetadata metadata,
            Dictionary<string, int> columns)
            where T : class, new()
        {
            T entity = new();

            foreach (PropertyInfo property in metadata.Properties)
            {
                string columnName = metadata.GetColumnName(property);

                if (!columns.TryGetValue(columnName, out int index))
                    continue;

                object? value = reader.IsDBNull(index) ? null : reader.GetValue(index);

                if (value == null)
                {
                    if (!property.PropertyType.IsValueType ||
                        Nullable.GetUnderlyingType(property.PropertyType) != null)
                    {
                        property.SetValue(entity, null);
                    }
                    continue;
                }

                property.SetValue(entity, ConvertValue(value, property.PropertyType));
            }
            return entity;
        }

        private static object ConvertValue(
            object value,
            Type propertyType)
        {
            Type targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (targetType.IsInstanceOfType(value))
                return value;

            if (targetType.IsEnum)
            {
                return value is string text ? Enum.Parse(targetType, text, true) : Enum.ToObject(targetType, value);
            }

            if (targetType == typeof(Guid))
                return Guid.Parse(value.ToString()!);

            return Convert.ChangeType(
                value,
                targetType,
                CultureInfo.InvariantCulture);
        }

        private static void AddParameters<T>(
            NpgsqlCommand command,
            PropertyInfo[] properties,
            T entity)
            where T : class
        {
            for (int i = 0; i < properties.Length; i++)
            {
                object value = properties[i].GetValue(entity) ?? DBNull.Value;
                command.Parameters.AddWithValue($"p{i}", value);
            }
        }

        private static string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || !Regex.IsMatch(identifier, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                throw new ArgumentException("Недопустимое имя SQL-идентификатора.", nameof(identifier));
            }
            return $"\"{identifier}\"";
        }

        private static void ValidateTableName(string tableName, EntityMetadata metadata)
        {
            if (!string.Equals(
                tableName,
                metadata.TableName,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Таблица {tableName} не соответствует модели {metadata.TableName}.");
            }

        }

        private static void ValidateColumnName(
            string columnName,
            EntityMetadata metadata)
        {
            bool exists = metadata.Properties.Any(p =>
                string.Equals(
                    metadata.GetColumnName(p),
                    columnName,
                    StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                throw new ArgumentException(
                    $"Столбец {columnName} не найден в модели {metadata.TableName}.");
            }
        }

        public async Task<IReadOnlyList<T>> GetRelatedAsync<T>(
            string tableName,
            string foreignKeyColumn,
            int parentId,
            CancellationToken cancellationToken = default)
            where T : class, new()
        {
            EntityMetadata metadata = new(typeof(T));
            ValidateTableName(tableName, metadata);
            ValidateColumnName(foreignKeyColumn, metadata);

            string sql =
                $"SELECT * FROM {EscapeIdentifier(metadata.TableName)} " +
                $"WHERE {EscapeIdentifier(foreignKeyColumn)} = @id";

            var result = new List<T>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", parentId);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            var columns = GetColumns(reader);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(MapRow<T>(reader, metadata, columns));
            }

            return result;
        }

        public async Task<T?> GetRelatedOneAsync<T>(
            string tableName,
            string foreignKeyColumn,
            int parentId,
            CancellationToken cancellationToken = default)
            where T : class, new()
        {
            EntityMetadata metadata = new(typeof(T));
            ValidateTableName(tableName, metadata);
            ValidateColumnName(foreignKeyColumn, metadata);

            string sql =
                $"SELECT * FROM {EscapeIdentifier(metadata.TableName)} " +
                $"WHERE {EscapeIdentifier(foreignKeyColumn)} = @id LIMIT 1";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", parentId);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var columns = GetColumns(reader);

            return MapRow<T>(reader, metadata, columns);
        }

        public async Task<IReadOnlyList<T>> GetManyToManyAsync<T>(
            string tableName,
            string joinTable,
            string parentForeignKey,
            string relatedForeignKey,
            int parentId,
            CancellationToken cancellationToken = default)
            where T : class, new()
        {
            EntityMetadata metadata = new(typeof(T));
            ValidateTableName(tableName, metadata);

            string primaryKey =
                EscapeIdentifier(metadata.GetColumnName(metadata.PrimaryKey));

            string sql =
                $"SELECT related.* FROM {EscapeIdentifier(metadata.TableName)} related " +
                $"INNER JOIN {EscapeIdentifier(joinTable)} relation " +
                $"ON related.{primaryKey} = relation.{EscapeIdentifier(relatedForeignKey)} " +
                $"WHERE relation.{EscapeIdentifier(parentForeignKey)} = @id";

            var result = new List<T>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", parentId);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            var columns = GetColumns(reader);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(MapRow<T>(reader, metadata, columns));
            }

            return result;
        }
    }
}