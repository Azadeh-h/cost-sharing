using System.Windows.Input;
using CostSharingApp.Services;
using CostSharing.Core.Models;

namespace CostSharingApp.ViewModels.Groups;

/// <summary>
/// ViewModel for creating or editing a group.
/// </summary>
public class CreateGroupViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IGroupService groupService;
    private readonly IErrorService errorService;
    private string groupName = string.Empty;
    private string errorMessage = string.Empty;
    private string buttonText = "Create";
    private Guid? groupId;
    private bool isEditMode;

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

        this.CreateCommand = new Command(async () => await this.SaveGroupAsync(), this.CanCreate);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Applies query attributes for navigation parameters.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var groupIdObj) && groupIdObj is string groupIdString)
        {
            if (Guid.TryParse(groupIdString, out var parsedGroupId))
            {
                this.groupId = parsedGroupId;
                this.isEditMode = true;
                this.Title = "Edit Group";
                this.ButtonText = "Save";
                _ = LoadGroupAsync(parsedGroupId);
            }
        }
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
    /// Gets the button text (Create or Save).
    /// </summary>
    public string ButtonText
    {
        get => this.buttonText;
        private set => this.SetProperty(ref this.buttonText, value);
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
    /// Loads an existing group for editing.
    /// </summary>
    private async Task LoadGroupAsync(Guid groupId)
    {
        try
        {
            this.IsBusy = true;
            var group = await this.groupService.GetGroupAsync(groupId);
            if (group != null)
            {
                this.GroupName = group.Name;
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "loading group");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Creates or updates the group.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task SaveGroupAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            if (this.isEditMode && this.groupId.HasValue)
            {
                // Update existing group
                var existingGroup = await this.groupService.GetGroupAsync(this.groupId.Value);
                if (existingGroup != null)
                {
                    existingGroup.Name = this.groupName;
                    existingGroup.UpdatedAt = DateTime.UtcNow;
                    var success = await this.groupService.UpdateGroupAsync(existingGroup);
                    
                    if (!success)
                    {
                        this.ErrorMessage = "Failed to update group. Please try again.";
                        return;
                    }
                }
            }
            else
            {
                // Create new group
                var group = await this.groupService.CreateGroupAsync(this.groupName);

                if (group == null)
                {
                    this.ErrorMessage = "Failed to create group. Please try again.";
                    return;
                }
            }

            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, this.isEditMode ? "updating group" : "creating group");
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
