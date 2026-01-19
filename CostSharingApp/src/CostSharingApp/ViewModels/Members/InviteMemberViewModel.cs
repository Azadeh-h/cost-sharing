// <copyright file="InviteMemberViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.ViewModels.Members;

using System.Windows.Input;
using CostSharing.Core.Interfaces;
using CostSharingApp.Services;

/// <summary>
/// ViewModel for adding members to a group.
/// </summary>
public class InviteMemberViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IGroupService groupService;
    private readonly IErrorService errorService;
    private readonly IAuthService authService;
    private readonly IGmailInvitationService? gmailService;
    private Guid groupId;
    private string groupName = string.Empty;
    private string memberName = string.Empty;
    private string memberEmail = string.Empty;
    private string errorMessage = string.Empty;
    private bool sendEmailInvite = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberViewModel"/> class.
    /// Default constructor for XAML.
    /// </summary>
    public InviteMemberViewModel()
    {
        this.groupService = null!;
        this.errorService = null!;
        this.authService = null!;
        this.gmailService = null;
        this.Title = "Add Member";
        this.AddMemberCommand = new Command(async () => await this.AddMemberAsync(), this.CanAddMember);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberViewModel"/> class.
    /// </summary>
    /// <param name="groupService">Group service.</param>
    /// <param name="errorService">Error service.</param>
    /// <param name="authService">Auth service.</param>
    /// <param name="gmailService">Gmail invitation service.</param>
    public InviteMemberViewModel(
        IGroupService groupService,
        IErrorService errorService,
        IAuthService authService,
        IGmailInvitationService gmailService)
    {
        this.groupService = groupService;
        this.errorService = errorService;
        this.authService = authService;
        this.gmailService = gmailService;
        this.Title = "Add Member";

        this.AddMemberCommand = new Command(async () => await this.AddMemberAsync(), this.CanAddMember);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Gets or sets the member name.
    /// </summary>
    public string MemberName
    {
        get => this.memberName;
        set
        {
            if (this.SetProperty(ref this.memberName, value))
            {
                ((Command)this.AddMemberCommand).ChangeCanExecute();
            }
        }
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
                ((Command)this.AddMemberCommand).ChangeCanExecute();
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
    /// Gets the command to add member.
    /// </summary>
    public ICommand AddMemberCommand { get; }

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
    /// Determines if member can be added.
    /// </summary>
    private bool CanAddMember()
    {
        return !string.IsNullOrWhiteSpace(this.memberName) &&
               !string.IsNullOrWhiteSpace(this.memberEmail) &&
               this.memberEmail.Contains("@");
    }

    /// <summary>
    /// Adds the member to the group.
    /// </summary>
    private async Task AddMemberAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            var (success, errorMessage) = await this.groupService.AddMemberByEmailAsync(
                this.groupId,
                this.memberEmail.Trim(),
                this.memberName.Trim());

            if (!success)
            {
                this.ErrorMessage = errorMessage ?? "Failed to add member";
                await Shell.Current.DisplayAlert("Error", this.ErrorMessage, "OK");
                return;
            }

            // Send email invitation if enabled
            if (this.sendEmailInvite)
            {
                if (this.gmailService == null)
                {
                    await Shell.Current.DisplayAlert(
                        "Member Added",
                        $"{this.memberName} has been added to the group.\n\nNote: Gmail service is not available.",
                        "OK");
                }
                else
                {
                    var currentUser = this.authService.GetCurrentUser();
                    if (currentUser == null || currentUser.Id == Guid.Empty)
                    {
                        await Shell.Current.DisplayAlert(
                            "Member Added",
                            $"{this.memberName} has been added to the group.\n\nNote: Could not send email - user not logged in.",
                            "OK");
                    }
                    else
                    {
                        var inviterName = currentUser.Name ?? "A friend";

                        var (emailSuccess, emailError) = await this.gmailService.SendInvitationAsync(
                            this.memberEmail.Trim(),
                            this.memberName.Trim(),
                            this.groupName,
                            inviterName,
                            currentUser.Id);

                        if (emailSuccess)
                        {
                            await Shell.Current.DisplayAlert(
                                "Member Added",
                                $"{this.memberName} has been added to the group and an invitation email has been sent!",
                                "OK");
                        }
                        else
                        {
                            await Shell.Current.DisplayAlert(
                                "Member Added",
                                $"{this.memberName} has been added to the group.\n\nNote: Email could not be sent: {emailError}\n\nYou can share the app manually.",
                                "OK");
                        }
                    }
                }
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "Member Added",
                    $"{this.memberName} has been added to the group!",
                    "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "adding member");
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
