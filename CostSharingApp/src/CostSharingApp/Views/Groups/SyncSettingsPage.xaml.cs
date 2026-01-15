// <copyright file="SyncSettingsPage.xaml.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CostSharingApp.ViewModels.Groups;

namespace CostSharingApp.Views.Groups;

/// <summary>
/// Sync settings page.
/// </summary>
public partial class SyncSettingsPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncSettingsPage"/> class.
    /// </summary>
    /// <param name="viewModel">Sync status view model.</param>
    public SyncSettingsPage(SyncStatusViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}
