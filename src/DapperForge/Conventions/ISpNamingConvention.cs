namespace DapperForge.Conventions;

/// <summary>
/// Defines the contract for resolving stored procedure names from entity types.
/// </summary>
public interface ISpNamingConvention
{
    /// <summary>
    /// Resolves the full stored procedure name for a SELECT operation.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The fully qualified stored procedure name.</returns>
    string ResolveSelect<T>();

    /// <summary>
    /// Resolves the full stored procedure name for an INSERT/UPDATE operation.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The fully qualified stored procedure name.</returns>
    string ResolveUpsert<T>();

    /// <summary>
    /// Resolves the full stored procedure name for a DELETE operation.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The fully qualified stored procedure name.</returns>
    string ResolveDelete<T>();

    /// <summary>
    /// Resolves the full stored procedure name for a SELECT operation on the specified type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The fully qualified stored procedure name.</returns>
    string ResolveSelect(Type entityType);

    /// <summary>
    /// Resolves the full stored procedure name for an INSERT/UPDATE operation on the specified type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The fully qualified stored procedure name.</returns>
    string ResolveUpsert(Type entityType);

    /// <summary>
    /// Resolves the full stored procedure name for a DELETE operation on the specified type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The fully qualified stored procedure name.</returns>
    string ResolveDelete(Type entityType);
}
