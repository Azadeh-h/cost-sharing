using System.Windows.Input;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels.Members;

/// <summary>
/// ViewModel for adding members to a group.
/// </summary>
public class InviteMemberViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IGroupService groupService;
    private readonly IErrorService errorService;
    private Guid groupId;
    private string memberName = string.Empty;
    private string memberEmail = string.Empty;
    private string errorMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberViewModel"/> class.
    /// Default constructor for XAML.
    /// </summary>
    public InviteMemberViewModel()
    {
        this.groupService = null!;
        this.errorService = null!;
        this.Title = "Add Member";
        this.AddMemberCommand = new Command(async () => await this.AddMemberAsync(), this.CanAddMember);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberViewModel"/> class.
    /// </summary>
    /// <param name="groupService">Group service.</param>
    /// <param name="errorService">Error service.</param>
    public InviteMemberViewModel(IGroupService groupService, IErrorService errorService)
    {
        this.groupService = groupService;
        this.errorService = errorService;
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
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var groupIdObj) && groupIdObj is string groupIdString)
        {
            if (Guid.TryParse(groupIdString, out var parsedGroupId))
            {
                this.groupId = parsedGroupId;
            }
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
                await Application.Current!.MainPage!.DisplayAlert("Error", this.ErrorMessage, "OK");
                return;
            }

            await Application.Current!.MainPage!.DisplayAlert(
                "Member Added",
                $"{this.memberName} has been added to the group!",
                "OK");

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
