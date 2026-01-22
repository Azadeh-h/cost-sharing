// <copyright file="AuthPage.xaml.cs" company="CostSharingApp">
// Copyright (c) CostSharingApp. All rights reserved.
// </copyright>

using CostSharingApp.ViewModels.Auth;

namespace CostSharingApp.Views.Auth;

/// <summary>
/// Authentication page for Sign In and Sign Up.
/// </summary>
public partial class AuthPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public AuthPage(AuthViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}
