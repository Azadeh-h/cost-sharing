namespace CostSharingApp.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
/// <summary>
/// Base class for all ViewModels with INotifyPropertyChanged implementation.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    private bool isBusy;
    private bool isRefreshing;
    private string title = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is busy.
    /// </summary>
    public bool IsBusy
    {
        get => this.isBusy;
        set => this.SetProperty(ref this.isBusy, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the view is being refreshed.
    /// </summary>
    public bool IsRefreshing
    {
        get => this.isRefreshing;
        set => this.SetProperty(ref this.isRefreshing, value);
    }

    /// <summary>
    /// Gets or sets the page/view title.
    /// </summary>
    public string Title
    {
        get => this.title;
        set => this.SetProperty(ref this.title, value);
    }
}
