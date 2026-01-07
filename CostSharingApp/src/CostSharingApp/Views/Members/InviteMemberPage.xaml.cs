namespace CostSharingApp.Views.Members;

/// <summary>
/// Page for inviting members to a group.
/// </summary>
public partial class InviteMemberPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberPage"/> class.
    /// </summary>
    /// <param name="viewModel">The invite member view model.</param>
    public InviteMemberPage(ViewModels.Members.InviteMemberViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }

    /// <summary>
    /// Handles invitation method selection change.
    /// </summary>
    private void OnMethodChanged(object? sender, CheckedChangedEventArgs e)
    {
        // Method change handled by binding
    }
}
