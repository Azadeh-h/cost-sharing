// <copyright file="CustomSplitViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.ViewModels.Expenses;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// View model for custom percentage split.
/// </summary>
public partial class CustomSplitViewModel : BaseViewModel, IQueryAttributable
{
    [ObservableProperty]
    private decimal totalAmount;

    [ObservableProperty]
    private decimal totalPercentage;

    [ObservableProperty]
    private bool isValid;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MemberSplitItem> memberSplits = new();

    private List<Guid> memberIds = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomSplitViewModel"/> class.
    /// </summary>
    public CustomSplitViewModel()
    {
    }

    /// <summary>
    /// Applies query parameters.
    /// </summary>
    /// <param name="query">Query dictionary.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("amount", out var amountObj) && amountObj is string amountStr)
        {
            this.TotalAmount = decimal.Parse(amountStr);
        }

        if (query.TryGetValue("memberIds", out var memberIdsObj) && memberIdsObj is string memberIdsStr)
        {
            // Parse comma-separated GUIDs
            this.memberIds = memberIdsStr.Split(',')
                .Select(id => Guid.Parse(id.Trim()))
                .ToList();

            this.InitializeMemberSplits();
        }
    }

    /// <summary>
    /// Equal split command.
    /// </summary>
    [RelayCommand]
    private void EqualSplit()
    {
        if (this.MemberSplits.Count == 0)
        {
            return;
        }

        var equalPercentage = Math.Round(100m / this.MemberSplits.Count, 2);
        var remainder = 100m - (equalPercentage * this.MemberSplits.Count);

        for (int i = 0; i < this.MemberSplits.Count; i++)
        {
            this.MemberSplits[i].Percentage = equalPercentage + (i == 0 ? remainder : 0);
        }

        this.CalculateTotals();
    }

    /// <summary>
    /// Reset command.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        foreach (var split in this.MemberSplits)
        {
            split.Percentage = 0;
        }

        this.CalculateTotals();
    }

    /// <summary>
    /// Apply split command.
    /// </summary>
    [RelayCommand]
    private async Task ApplySplitAsync()
    {
        if (!this.IsValid)
        {
            this.ErrorMessage = "Percentages must sum to 100%";
            return;
        }

        // Build percentages dictionary
        var percentages = new Dictionary<Guid, decimal>();
        foreach (var split in this.MemberSplits)
        {
            percentages[split.UserId] = split.Percentage;
        }

        // Pass back to AddExpensePage via MessagingCenter or shell navigation with parameters
        await Shell.Current.GoToAsync("..", new Dictionary<string, object>
        {
            ["customPercentages"] = percentages,
        });
    }

    /// <summary>
    /// Cancel command.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Initializes member splits.
    /// </summary>
    private void InitializeMemberSplits()
    {
        this.MemberSplits.Clear();

        foreach (var memberId in this.memberIds)
        {
            var item = new MemberSplitItem
            {
                UserId = memberId,
                Percentage = 0,
                TotalAmount = this.TotalAmount,
            };

            // Subscribe to percentage changes
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MemberSplitItem.Percentage))
                {
                    this.CalculateTotals();
                }
            };

            this.MemberSplits.Add(item);
        }

        // Initialize with equal split
        this.EqualSplit();
    }

    /// <summary>
    /// Calculates totals and validates.
    /// </summary>
    private void CalculateTotals()
    {
        this.TotalPercentage = this.MemberSplits.Sum(m => m.Percentage);
        this.IsValid = Math.Abs(this.TotalPercentage - 100m) < 0.01m;

        // Update calculated amounts
        foreach (var split in this.MemberSplits)
        {
            split.UpdateCalculatedAmount();
        }

        if (!this.IsValid)
        {
            this.ErrorMessage = $"Total must be 100% (currently {this.TotalPercentage:F1}%)";
        }
        else
        {
            this.ErrorMessage = string.Empty;
        }
    }
}

/// <summary>
/// Member split item with percentage.
/// </summary>
public partial class MemberSplitItem : ObservableObject
{
    [ObservableProperty]
    private Guid userId;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private decimal percentage;

    [ObservableProperty]
    private decimal totalAmount;

    [ObservableProperty]
    private decimal calculatedAmount;

    /// <summary>
    /// Updates the calculated amount based on percentage.
    /// </summary>
    public void UpdateCalculatedAmount()
    {
        this.CalculatedAmount = Math.Round((this.TotalAmount * this.Percentage) / 100m, 2);
    }
}
