using SQLite;

namespace CostSharing.Core.Models;

/// <summary>
/// Represents a user's membership in a specific group with role information.
/// </summary>
public class GroupMember
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [PrimaryKey]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the group reference.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the user reference.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the member role (Admin or Member).
    /// </summary>
    public GroupRole Role { get; set; }

    /// <summary>
    /// Gets or sets when user joined group.
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Gets or sets who invited/added this member.
    /// </summary>
    public Guid AddedBy { get; set; }
}

/// <summary>
/// Defines group member roles.
/// </summary>
public enum GroupRole
{
    /// <summary>
    /// Standard member with basic permissions.
    /// </summary>
    Member = 0,

    /// <summary>
    /// Admin with ability to invite/remove members and delete group.
    /// </summary>
    Admin = 1
}
