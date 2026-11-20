
using Npgsql;
using ORM;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ORM;

public class OrmContext : IOrmContext
{
    private readonly string _connectionString;

    public OrmContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(
        string tableName,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        string safeTableName = EscapeIdentifier(tableName);

        string sql = $"SELECT * FROM {safeTableName}";

        var result = new List<T>();

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        PropertyInfo[] properties = GetProperties(typeof(T));

        var columns = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns[reader.GetName(i)] = i;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapRow<T>(reader, properties, columns));
        }

        return result;
    }

    public async Task<T?> GetByIdAsync<T>(
        string tableName,
        int id,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        string safeTableName = EscapeIdentifier(tableName);

        string sql =
            $"SELECT * FROM {safeTableName} WHERE \"id\" = @id LIMIT 1";

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", id);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            PropertyInfo[] properties = GetProperties(typeof(T));

            var columns = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns[reader.GetName(i)] = i;
            }

            return MapRow<T>(reader, properties, columns);
        }

        return null;
    }

    public async Task<int> InsertAsync<T>(
        string tableName,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class
    {
        PropertyInfo[] properties = GetProperties(typeof(T))
            .Where(p => !p.Name.Equals(
                "id", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        string columns = string.Join(", ",
            properties.Select(p =>
                EscapeIdentifier(p.Name.ToLowerInvariant())));

        string parameters = string.Join(", ",
            properties.Select((p, i) => $"@p{i}"));

        string sql =
            $"INSERT INTO {EscapeIdentifier(tableName)} " +
            $"({columns}) VALUES ({parameters})";

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(sql, connection);

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
        PropertyInfo[] properties = GetProperties(typeof(T))
            .Where(p => !p.Name.Equals(
                "id", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        string setClause = string.Join(", ",
            properties.Select((p, i) =>
                $"{EscapeIdentifier(p.Name.ToLowerInvariant())} = @p{i}"));

        string sql =
            $"UPDATE {EscapeIdentifier(tableName)} " +
            $"SET {setClause} WHERE \"id\" = @id";

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(sql, connection);

        AddParameters(command, properties, entity);

        command.Parameters.AddWithValue("id", id);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default)
    {
        string sql =
            $"DELETE FROM {EscapeIdentifier(tableName)} " +
            "WHERE \"id\" = @id";

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", id);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static PropertyInfo[] GetProperties(Type type)
    {
        return type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance)
            .Where(p =>
                p.CanRead &&
                p.CanWrite &&
                p.GetMethod?.IsPublic == true &&
                p.SetMethod?.IsPublic == true)
            .ToArray();
    }

    private static T MapRow<T>(
        NpgsqlDataReader reader,
        PropertyInfo[] properties,
        Dictionary<string, int> columns)
        where T : class, new()
    {
        T entity = new T();

        foreach (PropertyInfo property in properties)
        {
            if (!columns.TryGetValue(
                property.Name, out int index))
            {
                continue;
            }

            object? value = reader.IsDBNull(index)
                ? null
                : reader.GetValue(index);

            if (value == null)
            {
                if (!property.PropertyType.IsValueType ||
                    Nullable.GetUnderlyingType(
                        property.PropertyType) != null)
                {
                    property.SetValue(entity, null);
                }

                continue;
            }

            property.SetValue(
                entity,
                ConvertValue(value, property.PropertyType));
        }

        return entity;
    }

    private static object ConvertValue(
        object value,
        Type propertyType)
    {
        Type targetType =
            Nullable.GetUnderlyingType(propertyType)
            ?? propertyType;

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType.IsEnum)
        {
            return value is string text
                ? Enum.Parse(targetType, text, true)
                : Enum.ToObject(targetType, value);
        }

        if (targetType == typeof(Guid))
        {
            return Guid.Parse(value.ToString()!);
        }

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
            object value =
                properties[i].GetValue(entity) ?? DBNull.Value;

            command.Parameters.AddWithValue($"p{i}", value);
        }
    }

    private static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            !Regex.IsMatch(
                identifier,
                @"^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            throw new ArgumentException(
                "Недопустимое имя SQL-идентификатора.",
                nameof(identifier));
        }

        return $"\"{identifier}\"";
    }
}
