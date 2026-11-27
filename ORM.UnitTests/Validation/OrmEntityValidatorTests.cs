
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using ORM;

namespace ORM.UnitTests.Validation;

[TestClass]
public class OrmEntityValidatorTests
{
    [TestMethod]
    public void Validate_ValidEntity_DoesNotThrowException()
    {
        var entity = new TestEntity
        {
            Name = "Test",
            Level = 3
        };

        OrmEntityValidator.Validate(entity);

        Assert.IsTrue(true);
    }

    [TestMethod]
    public void Validate_EmptyName_ThrowsValidationException()
    {
        var entity = new TestEntity
        {
            Name = "",
            Level = 3
        };

        Assert.ThrowsExactly<ValidationException>(() => OrmEntityValidator.Validate(entity));
    }

    [TestMethod]
    public void Validate_InvalidLevel_ThrowsValidationException()
    {
        var entity = new TestEntity
        {
            Name = "Test",
            Level = 10
        };

        Assert.ThrowsExactly<ValidationException>(() => OrmEntityValidator.Validate(entity));
    }

    [TestMethod]
    public void Validate_NullEntity_ThrowsArgumentNullException()
    {
        TestEntity? entity = null;

        Assert.ThrowsExactly<ArgumentNullException>(() => OrmEntityValidator.Validate(entity!));
    }

    public class TestEntity
    {
        [Required]
        public string Name { get; set; } = "";

        [Range(1, 5)]
        public int Level { get; set; }
    }
}
