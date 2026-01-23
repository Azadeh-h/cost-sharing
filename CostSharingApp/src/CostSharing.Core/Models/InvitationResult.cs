// <copyright file="InvitationResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharing.Core.Models;

/// <summary>
/// Result of an invitation operation.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Type">The type of result (DirectMember, PendingInvitation, or Error).</param>
/// <param name="Message">User-friendly message describing the result.</param>
/// <param name="MemberOrInvitationId">The ID of the created GroupMember or PendingInvitation.</param>
public record InvitationResult(
    bool Success,
    InvitationType Type,
    string Message,
    Guid? MemberOrInvitationId = null);
