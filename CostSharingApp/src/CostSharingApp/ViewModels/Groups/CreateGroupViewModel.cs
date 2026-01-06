using System.Windows.Input;
using CostSharingApp.Services;
using CostSharing.Core.Models;

namespace CostSharingApp.ViewModels.Groups;

/// <summary>
/// ViewModel for creating a new group.
/// </summary>
public class CreateGroupViewModel : BaseViewModel
{
    private readonly IGroupService groupService;
    private readonly IErrorService errorService;
    private string groupName = string.Empty;
    private string errorMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateGroupViewModel"/> class.
    /// </summary>
    /// <param name="groupService">Group service.</param>
    /// <param name="errorService">Error service.</param>
    public CreateGroupViewModel(IGroupService groupService, IErrorService errorService)
    {
        this.groupService = groupService;
        this.errorService = errorService;
        this.Title = "Create Group";

        this.CreateCommand = new Command(async () => await this.CreateGroupAsync(), this.CanCreate);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string GroupName
    {
        get => this.groupName;
        set
        {
            if (this.SetProperty(ref this.groupName, value))
            {
                ((Command)this.CreateCommand).ChangeCanExecute();
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
    /// Gets the command to create the group.
    /// </summary>
    public ICommand CreateCommand { get; }

    /// <summary>
    /// Gets the command to cancel creation.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Determines if the create command can execute.
    /// </summary>
    /// <returns>True if name is valid.</returns>
    private bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(this.groupName) && this.groupName.Length <= 100;
    }

    /// <summary>
    /// Creates the new group.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task CreateGroupAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            var group = await this.groupService.CreateGroupAsync(this.groupName);

            if (group == null)
            {
                this.ErrorMessage = "Failed to create group. Please try again.";
                return;
            }

            // Navigate back to group list
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "creating group");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Cancels group creation and navigates back.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
