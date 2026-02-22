using DapperForge.Configuration;
using DapperForge.Conventions;
using FluentAssertions;
using Xunit;

namespace DapperForge.Tests.Conventions;

public class DefaultNamingConventionTests
{
    public class Student { }
    public class Order { }

    [Fact]
    public void Default_ResolveSelect_ShouldUseGetPrefix()
    {
        var convention = new DefaultNamingConvention();

        convention.ResolveSelect<Student>().Should().Be("Get_Students");
    }

    [Fact]
    public void Default_ResolveUpsert_ShouldUseSavePrefix()
    {
        var convention = new DefaultNamingConvention();

        convention.ResolveUpsert<Student>().Should().Be("Save_Students");
    }

    [Fact]
    public void Default_ResolveDelete_ShouldUseRemovePrefix()
    {
        var convention = new DefaultNamingConvention();

        convention.ResolveDelete<Student>().Should().Be("Remove_Students");
    }

    [Fact]
    public void Default_WithType_ShouldResolveCorrectly()
    {
        var convention = new DefaultNamingConvention();

        convention.ResolveSelect(typeof(Order)).Should().Be("Get_Orders");
        convention.ResolveUpsert(typeof(Order)).Should().Be("Save_Orders");
        convention.ResolveDelete(typeof(Order)).Should().Be("Remove_Orders");
    }

    [Fact]
    public void WithSchema_ShouldPrependSchema()
    {
        var options = new ForgeOptions();
        var convention = new DefaultNamingConvention("Get", "Save", "Remove", "dbo", "_", options);

        convention.ResolveSelect<Student>().Should().Be("dbo.Get_Students");
    }

    [Fact]
    public void WithCustomSeparator_ShouldUseSeparator()
    {
        var options = new ForgeOptions();
        var convention = new DefaultNamingConvention("sp_Get", "sp_Save", "sp_Remove", "", "_", options);

        convention.ResolveSelect<Student>().Should().Be("sp_Get_Students");
    }

    [Fact]
    public void WithSchemaAndCustomPrefix_ShouldCombineCorrectly()
    {
        var options = new ForgeOptions();
        var convention = new DefaultNamingConvention("sel", "up", "del", "dbo", "_", options);

        convention.ResolveSelect<Student>().Should().Be("dbo.sel_Students");
        convention.ResolveUpsert<Student>().Should().Be("dbo.up_Students");
        convention.ResolveDelete<Student>().Should().Be("dbo.del_Students");
    }

    [Fact]
    public void WithMapEntity_ShouldUseCustomName()
    {
        var options = new ForgeOptions();
        options.MapEntity<Student>("Ogrenciler");
        var convention = new DefaultNamingConvention("Get", "Save", "Remove", "", "_", options);

        convention.ResolveSelect<Student>().Should().Be("Get_Ogrenciler");
    }

    [Fact]
    public void WithCustomEntityNameResolver_ShouldUseResolver()
    {
        var options = new ForgeOptions();
        options.EntityNameResolver = type => $"tbl_{type.Name}";
        var convention = new DefaultNamingConvention("Get", "Save", "Remove", "", "_", options);

        convention.ResolveSelect<Student>().Should().Be("Get_tbl_Student");
    }

    [Fact]
    public void GenericAndTypeOverloads_ShouldReturnIdenticalResults()
    {
        var convention = new DefaultNamingConvention();

        convention.ResolveSelect<Student>().Should().Be(convention.ResolveSelect(typeof(Student)));
        convention.ResolveUpsert<Student>().Should().Be(convention.ResolveUpsert(typeof(Student)));
        convention.ResolveDelete<Student>().Should().Be(convention.ResolveDelete(typeof(Student)));
    }

    [Fact]
    public void GenericAndTypeOverloads_WithCustomOptions_ShouldReturnIdenticalResults()
    {
        var options = new ForgeOptions();
        options.MapEntity<Student>("Ogrenciler");
        var convention = new DefaultNamingConvention("sel", "up", "del", "dbo", "_", options);

        convention.ResolveSelect<Student>().Should().Be(convention.ResolveSelect(typeof(Student)));
        convention.ResolveUpsert<Student>().Should().Be(convention.ResolveUpsert(typeof(Student)));
        convention.ResolveDelete<Student>().Should().Be(convention.ResolveDelete(typeof(Student)));
    }

    [Fact]
    public void GenericAndTypeOverloads_WithEntityNameResolver_ShouldReturnIdenticalResults()
    {
        var options = new ForgeOptions { EntityNameResolver = t => t.Name.ToLowerInvariant() };
        var convention = new DefaultNamingConvention("Get", "Save", "Remove", "", "_", options);

        convention.ResolveSelect<Order>().Should().Be(convention.ResolveSelect(typeof(Order)));
        convention.ResolveUpsert<Order>().Should().Be(convention.ResolveUpsert(typeof(Order)));
        convention.ResolveDelete<Order>().Should().Be(convention.ResolveDelete(typeof(Order)));
    }

    [Fact]
    public void NullSelectPrefix_ShouldThrow()
    {
        var act = () => new DefaultNamingConvention(null!, "Save", "Remove", "", "_", null);

        act.Should().Throw<ArgumentNullException>().WithParameterName("selectPrefix");
    }

    [Fact]
    public void NullUpsertPrefix_ShouldThrow()
    {
        var act = () => new DefaultNamingConvention("Get", null!, "Remove", "", "_", null);

        act.Should().Throw<ArgumentNullException>().WithParameterName("upsertPrefix");
    }

    [Fact]
    public void NullDeletePrefix_ShouldThrow()
    {
        var act = () => new DefaultNamingConvention("Get", "Save", null!, "", "_", null);

        act.Should().Throw<ArgumentNullException>().WithParameterName("deletePrefix");
    }
}
