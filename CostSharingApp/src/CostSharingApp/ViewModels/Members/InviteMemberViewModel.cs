// <copyright file="InviteMemberViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.ViewModels.Members;

using System.Windows.Input;
using CostSharing.Core.Interfaces;
using CostSharing.Core.Models;
using CostSharingApp.Services;

/// <summary>
/// ViewModel for adding members to a group.
/// </summary>
public class InviteMemberViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IInvitationLinkingService invitationLinkingService;
    private readonly IErrorService errorService;
    private readonly IAuthService authService;
    private Guid groupId;
    private string groupName = string.Empty;
    private string memberEmail = string.Empty;
    private string errorMessage = string.Empty;
    private bool sendEmailInvite = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberViewModel"/> class.
    /// Default constructor for XAML.
    /// </summary>
    public InviteMemberViewModel()
    {
        this.invitationLinkingService = null!;
        this.errorService = null!;
        this.authService = null!;
        this.Title = "Invite Member";
        this.InviteMemberCommand = new Command(async () => await this.InviteMemberAsync(), this.CanInviteMember);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberViewModel"/> class.
    /// </summary>
    /// <param name="invitationLinkingService">Invitation linking service.</param>
    /// <param name="errorService">Error service.</param>
    /// <param name="authService">Auth service.</param>
    public InviteMemberViewModel(
        IInvitationLinkingService invitationLinkingService,
        IErrorService errorService,
        IAuthService authService)
    {
        this.invitationLinkingService = invitationLinkingService;
        this.errorService = errorService;
        this.authService = authService;
        this.Title = "Invite Member";

        this.InviteMemberCommand = new Command(async () => await this.InviteMemberAsync(), this.CanInviteMember);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Gets or sets the member email.
    /// </summary>
    public string MemberEmail
    {
        get => this.memberEmail;
        set
        {
            if (this.SetProperty(ref this.memberEmail, value))
            {
                ((Command)this.InviteMemberCommand).ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to send an email invitation.
    /// </summary>
    public bool SendEmailInvite
    {
        get => this.sendEmailInvite;
        set => this.SetProperty(ref this.sendEmailInvite, value);
    }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage
    {
        get => this.errorMessage;
        set => this.SetProperty(ref this.errorMessage, value);
    }

    /// <summary>
    /// Gets the command to invite member.
    /// </summary>
    public ICommand InviteMemberCommand { get; }

    /// <summary>
    /// Gets the command to add member (legacy - maps to InviteMemberCommand).
    /// </summary>
    public ICommand AddMemberCommand => this.InviteMemberCommand;

    /// <summary>
    /// Gets the command to cancel.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Applies query attributes from navigation.
    /// </summary>
    /// <param name="query">The query attributes dictionary.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var groupIdObj) && groupIdObj is string groupIdString)
        {
            if (Guid.TryParse(groupIdString, out var parsedGroupId))
            {
                this.groupId = parsedGroupId;
            }
        }

        if (query.TryGetValue("groupName", out var groupNameObj) && groupNameObj is string name)
        {
            // Decode the URL-encoded group name
            this.groupName = Uri.UnescapeDataString(name);
        }
    }

    /// <summary>
    /// Determines if member can be invited.
    /// </summary>
    private bool CanInviteMember()
    {
        return !string.IsNullOrWhiteSpace(this.memberEmail) &&
               this.memberEmail.Contains('@');
    }

    /// <summary>
    /// Invites the member to the group using InvitationLinkingService.
    /// </summary>
    private async Task InviteMemberAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null || currentUser.Id == Guid.Empty)
            {
                this.ErrorMessage = "You must be signed in to invite members";
                await Shell.Current.DisplayAlert("Error", this.ErrorMessage, "OK");
                return;
            }

            // Use the new InvitationLinkingService
            var result = await this.invitationLinkingService.InviteToGroupAsync(
                this.groupId,
                this.memberEmail.Trim(),
                currentUser.Id,
                this.sendEmailInvite);

            if (!result.Success)
            {
                this.ErrorMessage = result.Message;
                await Shell.Current.DisplayAlert("Error", this.ErrorMessage, "OK");
                return;
            }

            // Use the result message which includes email status
            await Shell.Current.DisplayAlert("Success", result.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "inviting member");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Cancels and navigates back.
    /// </summary>
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
