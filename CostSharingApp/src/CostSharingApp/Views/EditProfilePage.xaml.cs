namespace CostSharingApp.Views;

/// <summary>
/// Page for editing user profile.
/// </summary>
public partial class EditProfilePage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditProfilePage"/> class.
    /// </summary>
    /// <param name="viewModel">Edit profile view model.</param>
    public EditProfilePage(ViewModels.EditProfileViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}
