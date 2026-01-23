// <copyright file="InvitationType.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharing.Core.Models;

/// <summary>
/// Type of invitation result.
/// </summary>
public enum InvitationType
{
    /// <summary>
    /// User exists, GroupMember created.
    /// </summary>
    DirectMember = 0,

    /// <summary>
    /// User doesn't exist, PendingInvitation created.
    /// </summary>
    PendingInvitation = 1,

    /// <summary>
    /// Error occurred.
    /// </summary>
    Error = 2,
}
