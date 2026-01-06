namespace CostSharingApp.Views.Members;

/// <summary>
/// Page for accepting group invitations.
/// </summary>
public partial class AcceptInvitationPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptInvitationPage"/> class.
    /// </summary>
    /// <param name="viewModel">The accept invitation view model.</param>
    public AcceptInvitationPage(ViewModels.Members.AcceptInvitationViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}
