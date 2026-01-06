using System.Windows.Input;
using CostSharingApp.Services;
using CostSharing.Core.Models;

namespace CostSharingApp.ViewModels.Members;

/// <summary>
/// ViewModel for accepting group invitations.
/// </summary>
public class AcceptInvitationViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IInvitationService invitationService;
    private readonly IAuthService authService;
    private readonly IGroupService groupService;
    private readonly IErrorService errorService;
    private readonly ICacheService cacheService;
    private string invitationToken = string.Empty;
    private string inviterName = string.Empty;
    private string groupName = string.Empty;
    private string errorMessage = string.Empty;
    private bool hasInvitation;
    private bool needsAuthentication;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptInvitationViewModel"/> class.
    /// Default constructor for XAML.
    /// </summary>
    public AcceptInvitationViewModel()
    {
        this.invitationService = null!;
        this.authService = null!;
        this.groupService = null!;
        this.errorService = null!;
        this.cacheService = null!;
        this.Title = "Accept Invitation";
        this.AcceptCommand = new Command(async () => await this.AcceptInvitationAsync());
        this.SignInCommand = new Command(async () => await this.NavigateToSignInAsync());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptInvitationViewModel"/> class.
    /// </summary>
    public AcceptInvitationViewModel(
        IInvitationService invitationService,
        IAuthService authService,
        IGroupService groupService,
        IErrorService errorService,
        ICacheService cacheService)
    {
        this.invitationService = invitationService;
        this.authService = authService;
        this.groupService = groupService;
        this.errorService = errorService;
        this.cacheService = cacheService;
        this.Title = "Accept Invitation";

        this.AcceptCommand = new Command(async () => await this.AcceptInvitationAsync());
        this.DeclineCommand = new Command(async () => await this.DeclineInvitationAsync());
        this.SignInCommand = new Command(async () => await this.NavigateToSignInAsync());
    }

    /// <summary>
    /// Gets or sets the inviter name.
    /// </summary>
    public string InviterName
    {
        get => this.inviterName;
        set => this.SetProperty(ref this.inviterName, value);
    }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string GroupName
    {
        get => this.groupName;
        set => this.SetProperty(ref this.groupName, value);
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
    /// Gets or sets a value indicating whether invitation details are loaded.
    /// </summary>
    public bool HasInvitation
    {
        get => this.hasInvitation;
        set => this.SetProperty(ref this.hasInvitation, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether user needs to authenticate.
    /// </summary>
    public bool NeedsAuthentication
    {
        get => this.needsAuthentication;
        set => this.SetProperty(ref this.needsAuthentication, value);
    }

    /// <summary>
    /// Gets the command to accept invitation.
    /// </summary>
    public ICommand AcceptCommand { get; }

    /// <summary>
    /// Gets the command to decline invitation.
    /// </summary>
    public ICommand DeclineCommand { get; }

    /// <summary>
    /// Gets the command to sign in.
    /// </summary>
    public ICommand SignInCommand { get; }

    /// <summary>
    /// Applies query attributes from navigation.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("token", out var tokenObj) && tokenObj is string token)
        {
            this.invitationToken = token;
            _ = this.LoadInvitationDetailsAsync();
        }
    }

    /// <summary>
    /// Loads invitation details.
    /// </summary>
    private async Task LoadInvitationDetailsAsync()
    {
        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            // Check if user is authenticated
            if (!this.authService.IsAuthenticated())
            {
                this.NeedsAuthentication = true;
                this.HasInvitation = false;
                return;
            }

            this.NeedsAuthentication = false;

            // Find invitation
            var allInvitations = await this.cacheService.GetAllAsync<Invitation>();
            var invitation = allInvitations.FirstOrDefault(i => i.Token == this.invitationToken);

            if (invitation == null)
            {
                this.ErrorMessage = "Invitation not found or invalid.";
                return;
            }

            // Check expiration
            if (DateTime.UtcNow > invitation.SentAt.AddDays(7))
            {
                this.ErrorMessage = "This invitation has expired.";
                return;
            }

            // Load group details
            var group = await this.groupService.GetGroupAsync(invitation.GroupId);
            if (group == null)
            {
                this.ErrorMessage = "Group not found.";
                return;
            }

            // Load inviter details
            var inviter = await this.cacheService.GetAsync<User>(invitation.InvitedBy);

            this.GroupName = group.Name;
            this.InviterName = inviter?.Name ?? "Someone";
            this.HasInvitation = true;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "loading invitation");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Accepts the invitation.
    /// </summary>
    private async Task AcceptInvitationAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            var groupId = await this.invitationService.AcceptInvitationAsync(this.invitationToken);

            if (groupId == null)
            {
                this.ErrorMessage = "Failed to accept invitation. It may have expired or already been used.";
                return;
            }

            await Application.Current!.MainPage!.DisplayAlert(
                "Welcome!",
                $"You've joined {this.groupName}!",
                "OK");

            // Navigate to group details
            await Shell.Current.GoToAsync($"//groups/groupdetails?groupId={groupId}");
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "accepting invitation");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Declines the invitation.
    /// </summary>
    private async Task DeclineInvitationAsync()
    {
        await Shell.Current.GoToAsync("//groups");
    }

    /// <summary>
    /// Navigates to sign in page.
    /// </summary>
    private async Task NavigateToSignInAsync()
    {
        // Store token to return after sign in
        await Shell.Current.GoToAsync($"signin?returnToken={this.invitationToken}");
    }
}
