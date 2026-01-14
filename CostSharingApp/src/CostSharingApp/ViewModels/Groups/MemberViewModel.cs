
using CostSharing.Core.Models;

namespace CostSharingApp.ViewModels.Groups;
/// <summary>
/// View model for displaying group member information with user details.
/// </summary>
public class MemberViewModel
{
    /// <summary>
    /// Gets or sets the member ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name for display.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the member role.
    /// </summary>
    public GroupRole Role { get; set; }

    /// <summary>
    /// Gets or sets when the user joined.
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Gets or sets who added this member.
    /// </summary>
    public Guid AddedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of who added this member.
    /// </summary>
    public string AddedByName { get; set; } = string.Empty;
}
