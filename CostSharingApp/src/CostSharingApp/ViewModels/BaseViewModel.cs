using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CostSharingApp.ViewModels;

/// <summary>
/// Base class for all ViewModels with INotifyPropertyChanged implementation.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool isBusy;
    private string title = string.Empty;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is busy.
    /// </summary>
    public bool IsBusy
    {
        get => this.isBusy;
        set => this.SetProperty(ref this.isBusy, value);
    }

    /// <summary>
    /// Gets or sets the page/view title.
    /// </summary>
    public string Title
    {
        get => this.title;
        set => this.SetProperty(ref this.title, value);
    }

    /// <summary>
    /// Raises PropertyChanged event for specified property.
    /// </summary>
    /// <param name="propertyName">Property name (auto-filled).</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets property value and raises PropertyChanged if value changed.
    /// </summary>
    /// <typeparam name="T">Property type.</typeparam>
    /// <param name="field">Backing field reference.</param>
    /// <param name="value">New value.</param>
    /// <param name="propertyName">Property name (auto-filled).</param>
    /// <returns>True if value changed.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }
}
