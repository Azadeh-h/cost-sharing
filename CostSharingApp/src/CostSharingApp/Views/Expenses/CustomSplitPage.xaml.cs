// <copyright file="CustomSplitPage.xaml.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.Views.Expenses;

/// <summary>
/// Page for custom percentage split.
/// </summary>
public partial class CustomSplitPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomSplitPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public CustomSplitPage(ViewModels.Expenses.CustomSplitViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}
