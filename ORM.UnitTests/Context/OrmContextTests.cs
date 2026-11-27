
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ORM;

namespace ORM.UnitTests.Context;

[TestClass]
public class OrmContextTests
{
    private string _connectionString = null!;
    private OrmContext _context = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _connectionString = Environment.GetEnvironmentVariable("ZOMBIE_DB_CONNECTION_STRING") ?? "";

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            Assert.Inconclusive("Не задана переменная ZOMBIE_DB_CONNECTION_STRING");
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
    public async Task InsertAsync_ValidEntity_AddsRecord()
    {
        var entity = new TestEntity
        {
            Name = "Hero",
            Age = 25
        };

        int result = await _context.InsertAsync("orm_test_entities", entity);

        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task GetAllAsync_ExistingRecords_ReturnsAllRecords()
    {
        await _context.InsertAsync("orm_test_entities", new TestEntity { Name = "Hero", Age = 25 });
        await _context.InsertAsync("orm_test_entities", new TestEntity { Name = "Monster", Age = 40 });

        var result = await _context.GetAllAsync<TestEntity>("orm_test_entities");

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(x => x.Name == "Hero"));
        Assert.IsTrue(result.Any(x => x.Name == "Monster"));
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingId_ReturnsEntity()
    {
        await _context.InsertAsync("orm_test_entities", new TestEntity { Name = "Hero", Age = 25 });

        var entities = await _context.GetAllAsync<TestEntity>("orm_test_entities");
        int id = entities[0].Id;
        var result = await _context.GetByIdAsync<TestEntity>("orm_test_entities", id);

        Assert.IsNotNull(result);
        Assert.AreEqual("Hero", result.Name);
        Assert.AreEqual(25, result.Age);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        int id = 999;

        var result = await _context.GetByIdAsync<TestEntity>("orm_test_entities", id);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UpdateAsync_ExistingEntity_UpdatesRecord()
    {
        await _context.InsertAsync("orm_test_entities", new TestEntity { Name = "Hero", Age = 25 });

        var entities = await _context.GetAllAsync<TestEntity>("orm_test_entities");

        int id = entities[0].Id;

        var updatedEntity = new TestEntity
        {
            Name = "Updated Hero",
            Age = 30
        };

        int result = await _context.UpdateAsync("orm_test_entities", id, updatedEntity);

        var updated = await _context.GetByIdAsync<TestEntity>("orm_test_entities", id);

        Assert.AreEqual(1, result);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Hero", updated.Name);
        Assert.AreEqual(30, updated.Age);
    }

    [TestMethod]
    public async Task DeleteAsync_ExistingId_DeletesRecord()
    {
        await _context.InsertAsync("orm_test_entities", new TestEntity { Name = "Hero", Age = 25 });

        var entities = await _context.GetAllAsync<TestEntity>("orm_test_entities");
        int id = entities[0].Id;
        int result = await _context.DeleteAsync("orm_test_entities", id);
        var deleted = await _context.GetByIdAsync<TestEntity>("orm_test_entities", id);

        Assert.AreEqual(1, result);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task GetAllAsync_WrongTableName_ThrowsException()
    {
        string wrongTableName = "wrong_table";

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await _context.GetAllAsync<TestEntity>(wrongTableName));
    }

    [Table("orm_test_entities")]
    public class TestEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = "";

        [Range(1, 150)]
        [Column("age")]
        public int Age { get; set; }
    }
}
