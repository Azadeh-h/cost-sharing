// <copyright file="ExpenseDetailsPage.xaml.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.Views.Expenses;

/// <summary>
/// Page for displaying expense details.
/// </summary>
public partial class ExpenseDetailsPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseDetailsPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public ExpenseDetailsPage(ViewModels.Expenses.ExpenseDetailsViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}
