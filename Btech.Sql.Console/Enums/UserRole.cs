namespace Btech.Sql.Console.Enums;

/// <summary>
/// Represents the roles of a user.
/// </summary>
/// <remarks>
/// Roles should be in ascending order of access (0 - lowest, admin - highest).
/// The index of the role must be a degree of two. (0, 1, 2, 4, 8, 16 ...)
/// </remarks>
[Flags]
public enum UserRole
{
    /// <summary>
    /// No role assigned.
    /// </summary>
    None = 0,

    /// <summary>
    /// The user has client privileges.
    /// </summary>
    Client = 1,

    /// <summary>
    /// The user has editor privileges.
    /// </summary>
    Editor = 2,

    /// <summary>
    /// The user has admin privileges.
    /// </summary>
    Admin = 4
}