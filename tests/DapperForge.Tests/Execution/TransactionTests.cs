using System.Data;
using DapperForge.Conventions;
using DapperForge.Diagnostics;
using DapperForge.Execution;
using DapperForge.Transaction;
using FluentAssertions;
using Moq;
using Xunit;

namespace DapperForge.Tests.Execution;

public class TransactionTests
{
    [Fact]
    public void ForgeTransaction_Constructor_NullTransaction_ShouldThrow()
    {
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();

        var act = () => new ForgeTransaction(null!, mockExecutor.Object, mockConvention.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("transaction");
    }

    [Fact]
    public void ForgeTransaction_Constructor_NullExecutor_ShouldThrow()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockConvention = new Mock<ISpNamingConvention>();

        var act = () => new ForgeTransaction(mockTransaction.Object, null!, mockConvention.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("executor");
    }

    [Fact]
    public void ForgeTransaction_Constructor_NullConvention_ShouldThrow()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();

        var act = () => new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("convention");
    }

    [Fact]
    public void Commit_ShouldCallTransactionCommit()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();

        var forgeTx = new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, mockConvention.Object);
        forgeTx.Commit();

        mockTransaction.Verify(t => t.Commit(), Times.Once);
    }

    [Fact]
    public void Rollback_ShouldCallTransactionRollback()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();

        var forgeTx = new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, mockConvention.Object);
        forgeTx.Rollback();

        mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldDisposeTransaction()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();

        var forgeTx = new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, mockConvention.Object);
        forgeTx.Dispose();

        mockTransaction.Verify(t => t.Dispose(), Times.Once);
    }

    [Fact]
    public void AfterDispose_Commit_ShouldThrow()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();

        var forgeTx = new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, mockConvention.Object);
        forgeTx.Dispose();

        var act = () => forgeTx.Commit();

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void AfterDispose_Rollback_ShouldThrow()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();

        var forgeTx = new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, mockConvention.Object);
        forgeTx.Dispose();

        var act = () => forgeTx.Rollback();

        act.Should().Throw<ObjectDisposedException>();
    }

    public class Student { }

    [Fact]
    public async Task GetAsync_ShouldResolveConventionAndCallExecutor()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();

        mockConvention.Setup(c => c.ResolveSelect<Student>()).Returns("Get_Students");
        mockExecutor.Setup(e => e.QueryAsync<Student>("Get_Students", null, mockTransaction.Object, default))
            .ReturnsAsync(new List<Student>());

        var forgeTx = new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, mockConvention.Object);
        var result = await forgeTx.GetAsync<Student>();

        mockConvention.Verify(c => c.ResolveSelect<Student>(), Times.Once);
        mockExecutor.Verify(e => e.QueryAsync<Student>("Get_Students", null, mockTransaction.Object, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldResolveConventionAndCallExecutor()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();
        var student = new Student();

        mockConvention.Setup(c => c.ResolveUpsert<Student>()).Returns("Save_Students");
        mockExecutor.Setup(e => e.ExecuteAsync("Save_Students", student, mockTransaction.Object, default))
            .ReturnsAsync(1);

        var forgeTx = new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, mockConvention.Object);
        var result = await forgeTx.SaveAsync(student);

        result.Should().Be(1);
    }

    [Fact]
    public async Task RemoveAsync_ShouldResolveConventionAndCallExecutor()
    {
        var mockTransaction = new Mock<IDbTransaction>();
        var mockExecutor = new Mock<ISpExecutor>();
        var mockConvention = new Mock<ISpNamingConvention>();
        var parameters = new { Id = 5 };

        mockConvention.Setup(c => c.ResolveDelete<Student>()).Returns("Remove_Students");
        mockExecutor.Setup(e => e.ExecuteAsync("Remove_Students", parameters, mockTransaction.Object, default))
            .ReturnsAsync(1);

        var forgeTx = new ForgeTransaction(mockTransaction.Object, mockExecutor.Object, mockConvention.Object);
        var result = await forgeTx.RemoveAsync<Student>(parameters);

        result.Should().Be(1);
    }
}
