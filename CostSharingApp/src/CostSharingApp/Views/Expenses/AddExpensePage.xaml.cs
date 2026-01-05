// <copyright file="AddExpensePage.xaml.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.Views.Expenses;

/// <summary>
/// Page for adding new expenses.
/// </summary>
public partial class AddExpensePage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddExpensePage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public AddExpensePage(ViewModels.Expenses.AddExpenseViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}
