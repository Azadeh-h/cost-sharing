namespace CostSharingApp.Views.Members;

/// <summary>
/// Page for inviting members to a group.
/// </summary>
public partial class InviteMemberPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberPage"/> class.
    /// </summary>
    public InviteMemberPage()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Handles invitation method selection change.
    /// </summary>
    private void OnMethodChanged(object? sender, CheckedChangedEventArgs e)
    {
        // Method change handled by binding
    }
}
