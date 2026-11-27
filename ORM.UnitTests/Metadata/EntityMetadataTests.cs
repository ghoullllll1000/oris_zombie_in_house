
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ORM;

namespace ORM.UnitTests.Metadata;

[TestClass]
public class EntityMetadataTests
{
    [TestMethod]
    public void Constructor_ValidModel_FindsTableName()
    {
        EntityMetadata metadata = new(typeof(TestEntity));

        string result = metadata.TableName;

        Assert.AreEqual("test_entities", result);
    }

    [TestMethod]
    public void Constructor_ValidModel_FindsPrimaryKey()
    {
        EntityMetadata metadata = new(typeof(TestEntity));

        string result = metadata.PrimaryKey.Name;

        Assert.AreEqual("Id", result);
    }

    [TestMethod]
    public void GetColumnName_PropertyWithColumnAttribute_ReturnsAttributeName()
    {
        EntityMetadata metadata = new(typeof(TestEntity));
        var property = metadata.Properties.First(p => p.Name == "Name");

        string result = metadata.GetColumnName(property);

        Assert.AreEqual("entity_name", result);
    }

    [TestMethod]
    public void Constructor_ValidModel_SeparatesProperties()
    {
        EntityMetadata metadata = new(typeof(TestEntity));

        bool hasName = metadata.Properties.Any(p => p.Name == "Name");
        bool hasNavigation = metadata.NavigationProperties.Any(p => p.Name == "RelatedEntity");

        Assert.IsTrue(hasName);
        Assert.IsTrue(hasNavigation);
    }

    [TestMethod]
    public void Constructor_ModelWithoutPrimaryKey_ThrowsException()
    {
        Type modelType = typeof(ModelWithoutKey);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => new EntityMetadata(modelType));
    }

    [Table("test_entities")]
    public class TestEntity
    {
        [Key]
        public int Id { get; set; }

        [Column("entity_name")]
        public string Name { get; set; } = "";

        public RelatedEntity? RelatedEntity { get; set; }
    }

    public class RelatedEntity
    {
        public int Id { get; set; }
    }

    public class ModelWithoutKey
    {
        public string Name { get; set; } = "";
    }
}
