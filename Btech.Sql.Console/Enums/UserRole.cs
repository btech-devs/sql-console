namespace Btech.Sql.Console.Enums;

// Roles should be in ascending order of access (0 - lowest, admin - highest)
// The index of the role must be a degree of two. (0, 1, 2, 4, 8, 16 ...)
[Flags]
public enum UserRole
{
    None = 0,
    Client = 1,
    Editor = 2,
    Admin = 4
}