
using Npgsql;

namespace ORM.UnitTests;

public static class TestDatabase
{
    public static async Task ResetAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        string sql = """
            DROP TABLE IF EXISTS
                orm_test_character_weapons,
                orm_test_profiles,
                orm_test_children,
                orm_test_weapons,
                orm_test_characters,
                orm_test_entities
            CASCADE;

            CREATE TABLE orm_test_entities
            (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL,
                age INTEGER NOT NULL
            );

            CREATE TABLE orm_test_characters
            (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE orm_test_children
            (
                id SERIAL PRIMARY KEY,
                character_id INTEGER NOT NULL
                    REFERENCES orm_test_characters(id),
                name TEXT NOT NULL
            );

            CREATE TABLE orm_test_profiles
            (
                id SERIAL PRIMARY KEY,
                character_id INTEGER UNIQUE NOT NULL
                    REFERENCES orm_test_characters(id),
                biography TEXT NOT NULL
            );

            CREATE TABLE orm_test_weapons
            (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE orm_test_character_weapons
            (
                character_id INTEGER NOT NULL
                    REFERENCES orm_test_characters(id),
                weapon_id INTEGER NOT NULL
                    REFERENCES orm_test_weapons(id),
                PRIMARY KEY (character_id, weapon_id)
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task ClearAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        string sql = """
            DROP TABLE IF EXISTS
                orm_test_character_weapons,
                orm_test_profiles,
                orm_test_children,
                orm_test_weapons,
                orm_test_characters,
                orm_test_entities
            CASCADE;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
