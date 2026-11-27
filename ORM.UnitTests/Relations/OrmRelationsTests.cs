
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Npgsql;
using ORM;

namespace ORM.UnitTests.Relations;

[TestClass]
public class OrmRelationsTests
{
    private string _connectionString = null!;
    private OrmContext _context = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _connectionString = Environment.GetEnvironmentVariable("ZOMBIE_DB_CONNECTION_STRING") ?? "";

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            Assert.Inconclusive("Не задана переменная ZOMBIE_DB_CONNECTION_STRING.");
        }

        await TestDatabase.ResetAsync(_connectionString);

        _context = new OrmContext(_connectionString);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            await TestDatabase.ClearAsync(_connectionString);
        }
    }

    [TestMethod]
    public async Task GetRelatedAsync_ExistingRelations_ReturnsAllRelatedEntities()
    {

        int characterId = await InsertAndReturnIdAsync(
            "INSERT INTO orm_test_characters (name) VALUES ('Hero') RETURNING id");

        await InsertAsync(
            "INSERT INTO orm_test_children (character_id, name) VALUES (@id, 'First')",
            characterId);

        await InsertAsync(
            "INSERT INTO orm_test_children (character_id, name) VALUES (@id, 'Second')",
            characterId);

        var result = await _context.GetRelatedAsync<TestChild>("orm_test_children", "character_id", characterId);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(x => x.Name == "First"));
        Assert.IsTrue(result.Any(x => x.Name == "Second"));
    }

    [TestMethod]
    public async Task GetRelatedOneAsync_ExistingRelation_ReturnsEntity()
    {
        int characterId = await InsertAndReturnIdAsync(
            "INSERT INTO orm_test_characters (name) VALUES ('Hero') RETURNING id");

        await InsertAsync(
            "INSERT INTO orm_test_profiles (character_id, biography) VALUES (@id, 'Test biography')",
            characterId);

        var result = await _context.GetRelatedOneAsync<TestProfile>("orm_test_profiles", "character_id", characterId);

        Assert.IsNotNull(result);
        Assert.AreEqual("Test biography", result.Biography);
    }

    [TestMethod]
    public async Task GetManyToManyAsync_ExistingRelations_ReturnsRelatedEntities()
    {
        int characterId = await InsertAndReturnIdAsync(
            "INSERT INTO orm_test_characters (name) VALUES ('Hero') RETURNING id");

        int firstWeaponId = await InsertAndReturnIdAsync(
            "INSERT INTO orm_test_weapons (name) VALUES ('Sword') RETURNING id");

        int secondWeaponId = await InsertAndReturnIdAsync(
            "INSERT INTO orm_test_weapons (name) VALUES ('Bow') RETURNING id");

        await InsertAsync(
            """
            INSERT INTO orm_test_character_weapons
                (character_id, weapon_id)
            VALUES (@id, @relatedId)
            """,
            characterId,
            firstWeaponId);

        await InsertAsync(
            """
            INSERT INTO orm_test_character_weapons
                (character_id, weapon_id)
            VALUES (@id, @relatedId)
            """,
            characterId,
            secondWeaponId);

        var result = await _context.GetManyToManyAsync<TestWeapon>(
            "orm_test_weapons",
            "orm_test_character_weapons",
            "character_id",
            "weapon_id",
            characterId);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(x => x.Name == "Sword"));
        Assert.IsTrue(result.Any(x => x.Name == "Bow"));
    }

    private async Task<int> InsertAndReturnIdAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task InsertAsync(
        string sql,
        int id,
        int? relatedId = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        if (relatedId.HasValue)
        {
            command.Parameters.AddWithValue("relatedId", relatedId.Value);
        }
        await command.ExecuteNonQueryAsync();
    }

    [Table("orm_test_children")]
    public class TestChild
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("character_id")]
        public int CharacterId { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";
    }

    [Table("orm_test_profiles")]
    public class TestProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("character_id")]
        public int CharacterId { get; set; }

        [Column("biography")]
        public string Biography { get; set; } = "";
    }

    [Table("orm_test_weapons")]
    public class TestWeapon
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";
    }
}
