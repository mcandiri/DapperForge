using DapperForge.Configuration;
using DapperForge.Conventions;
using FluentAssertions;
using Xunit;

namespace DapperForge.Tests.Conventions;

public class SpNameResolverTests
{
    public class Student { }
    public class Teacher { }
    public class Order { }

    [Fact]
    public void ResolveSelect_ShouldDelegateToConvention()
    {
        var options = new ForgeOptions();
        var resolver = new SpNameResolver(options.Convention, options);

        resolver.ResolveSelect<Student>().Should().Be("Get_Students");
    }

    [Fact]
    public void ResolveUpsert_ShouldDelegateToConvention()
    {
        var options = new ForgeOptions();
        var resolver = new SpNameResolver(options.Convention, options);

        resolver.ResolveUpsert<Student>().Should().Be("Save_Students");
    }

    [Fact]
    public void ResolveDelete_ShouldDelegateToConvention()
    {
        var options = new ForgeOptions();
        var resolver = new SpNameResolver(options.Convention, options);

        resolver.ResolveDelete<Student>().Should().Be("Remove_Students");
    }

    [Fact]
    public void GetExpectedSpNames_ShouldReturnThreeNames()
    {
        var options = new ForgeOptions();
        var resolver = new SpNameResolver(options.Convention, options);

        var names = resolver.GetExpectedSpNames(typeof(Student));

        names.Should().HaveCount(3);
        names.Should().Contain("Get_Students");
        names.Should().Contain("Save_Students");
        names.Should().Contain("Remove_Students");
    }

    [Fact]
    public void GetAllExpectedSpNames_WithRegisteredEntities_ShouldReturnAll()
    {
        var options = new ForgeOptions();
        options.RegisterEntity<Student>();
        options.RegisterEntity<Teacher>();
        var resolver = new SpNameResolver(options.Convention, options);

        var all = resolver.GetAllExpectedSpNames();

        all.Should().HaveCount(2);
        all.Should().ContainKey(typeof(Student));
        all.Should().ContainKey(typeof(Teacher));
    }

    [Fact]
    public void GetAllExpectedSpNames_WithNoRegisteredEntities_ShouldReturnEmpty()
    {
        var options = new ForgeOptions();
        var resolver = new SpNameResolver(options.Convention, options);

        var all = resolver.GetAllExpectedSpNames();

        all.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_NullConvention_ShouldThrow()
    {
        var options = new ForgeOptions();

        var act = () => new SpNameResolver(null!, options);

        act.Should().Throw<ArgumentNullException>().WithParameterName("convention");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var options = new ForgeOptions();

        var act = () => new SpNameResolver(options.Convention, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void MapEntity_ShouldOverrideInResolver()
    {
        var options = new ForgeOptions();
        options.MapEntity<Student>("Ogrenciler");

        options.SetConvention(c =>
        {
            c.SelectPrefix = "sel";
            c.UpsertPrefix = "up";
            c.DeletePrefix = "del";
        });

        var resolver = new SpNameResolver(options.Convention, options);

        resolver.ResolveSelect<Student>().Should().Be("sel_Ogrenciler");
    }
}
