
using System.Collections.ObjectModel;
using CostSharingApp.Services;
using System.Windows.Input;
using CostSharing.Core.Models;

namespace CostSharingApp.ViewModels.Groups;
/// <summary>
/// ViewModel for the group list page showing all user's groups.
/// </summary>
public class GroupListViewModel : BaseViewModel
{
    private readonly IGroupService groupService;
    private readonly IErrorService errorService;
    private ObservableCollection<Group> groups = new();
    private string errorMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupListViewModel"/> class.
    /// </summary>
    /// <param name="groupService">Group service.</param>
    /// <param name="errorService">Error service.</param>
    public GroupListViewModel(IGroupService groupService, IErrorService errorService)
    {
        this.groupService = groupService;
        this.errorService = errorService;
        this.Title = "My Groups";

        this.LoadGroupsCommand = new Command(async () => await this.LoadGroupsAsync());
        this.CreateGroupCommand = new Command(async () => await this.NavigateToCreateGroupAsync());
        this.SelectGroupCommand = new Command<Group>(async (group) => await this.NavigateToGroupDetailsAsync(group));
    }

    /// <summary>
    /// Gets the collection of groups.
    /// </summary>
    public ObservableCollection<Group> Groups
    {
        get => this.groups;
        set => this.SetProperty(ref this.groups, value);
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
    /// Gets the command to load groups.
    /// </summary>
    public ICommand LoadGroupsCommand { get; }

    /// <summary>
    /// Gets the command to create a new group.
    /// </summary>
    public ICommand CreateGroupCommand { get; }

    /// <summary>
    /// Gets the command to select a group.
    /// </summary>
    public ICommand SelectGroupCommand { get; }

    /// <summary>
    /// Loads the user's groups from the service.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    public async Task LoadGroupsAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            var userGroups = await this.groupService.GetUserGroupsAsync();
            this.Groups.Clear();
            foreach (var group in userGroups.OrderByDescending(g => g.UpdatedAt))
            {
                this.Groups.Add(group);
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "loading groups");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Navigates to the create group page.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task NavigateToCreateGroupAsync()
    {
        await Shell.Current.GoToAsync("creategroup");
    }

    /// <summary>
    /// Navigates to the group details page.
    /// </summary>
    /// <param name="group">Selected group.</param>
    /// <returns>Task for async operation.</returns>
    private async Task NavigateToGroupDetailsAsync(Group? group)
    {
        if (group == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"groupdetails?groupId={group.Id}");
    }
}
