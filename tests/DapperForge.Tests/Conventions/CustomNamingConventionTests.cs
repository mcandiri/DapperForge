using DapperForge.Configuration;
using DapperForge.Conventions;
using FluentAssertions;
using Xunit;

namespace DapperForge.Tests.Conventions;

public class CustomNamingConventionTests
{
    public class Student { }
    public class Teacher { }

    [Fact]
    public void SetConvention_ShouldConfigureCustomPrefixes()
    {
        var options = new ForgeOptions();
        options.SetConvention(c =>
        {
            c.SelectPrefix = "sel";
            c.UpsertPrefix = "up";
            c.DeletePrefix = "del";
        });

        options.Convention.ResolveSelect<Student>().Should().Be("sel_Students");
        options.Convention.ResolveUpsert<Student>().Should().Be("up_Students");
        options.Convention.ResolveDelete<Student>().Should().Be("del_Students");
    }

    [Fact]
    public void SetConvention_WithSchema_ShouldPrependSchema()
    {
        var options = new ForgeOptions();
        options.SetConvention(c =>
        {
            c.SelectPrefix = "sel";
            c.UpsertPrefix = "up";
            c.DeletePrefix = "del";
            c.Schema = "dbo";
        });

        options.Convention.ResolveSelect<Student>().Should().Be("dbo.sel_Students");
    }

    [Fact]
    public void SetConvention_WithCustomSeparator_ShouldApply()
    {
        var options = new ForgeOptions();
        options.SetConvention(c =>
        {
            c.SelectPrefix = "sp_Get";
            c.UpsertPrefix = "sp_Save";
            c.DeletePrefix = "sp_Remove";
            c.Separator = "__";
        });

        options.Convention.ResolveSelect<Student>().Should().Be("sp_Get__Students");
    }

    [Fact]
    public void SetConvention_WithMapEntity_ShouldOverrideName()
    {
        var options = new ForgeOptions();
        options.MapEntity<Student>("Ogrenci");

        options.SetConvention(c =>
        {
            c.SelectPrefix = "sel";
            c.UpsertPrefix = "up";
            c.DeletePrefix = "del";
        });

        options.Convention.ResolveSelect<Student>().Should().Be("sel_Ogrenci");
    }

    [Fact]
    public void SetConvention_NullConfigure_ShouldThrow()
    {
        var options = new ForgeOptions();

        var act = () => options.SetConvention(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConventionBuilder_Defaults_ShouldBeSameAsDefaultConvention()
    {
        var options = new ForgeOptions();
        options.SetConvention(_ => { }); // Use defaults

        options.Convention.ResolveSelect<Student>().Should().Be("Get_Students");
        options.Convention.ResolveUpsert<Student>().Should().Be("Save_Students");
        options.Convention.ResolveDelete<Student>().Should().Be("Remove_Students");
    }

    [Fact]
    public void MultipleDifferentEntities_ShouldResolveCorrectly()
    {
        var options = new ForgeOptions();
        options.SetConvention(c =>
        {
            c.SelectPrefix = "sel";
            c.UpsertPrefix = "up";
            c.DeletePrefix = "del";
            c.Schema = "dbo";
        });

        options.Convention.ResolveSelect<Student>().Should().Be("dbo.sel_Students");
        options.Convention.ResolveSelect<Teacher>().Should().Be("dbo.sel_Teachers");
    }
}
