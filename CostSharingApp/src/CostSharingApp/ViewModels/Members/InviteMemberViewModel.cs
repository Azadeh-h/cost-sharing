using System.Windows.Input;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels.Members;

/// <summary>
/// ViewModel for inviting members to a group.
/// </summary>
public class InviteMemberViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IInvitationService invitationService;
    private readonly IErrorService errorService;
    private Guid groupId;
    private bool isEmailMethod = true;
    private bool isSmsMethod;
    private string inviteeEmail = string.Empty;
    private string inviteePhone = string.Empty;
    private string errorMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberViewModel"/> class.
    /// Default constructor for XAML.
    /// </summary>
    public InviteMemberViewModel()
    {
        this.invitationService = null!;
        this.errorService = null!;
        this.Title = "Invite Member";
        this.SendInvitationCommand = new Command(async () => await this.SendInvitationAsync(), this.CanSendInvitation);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InviteMemberViewModel"/> class.
    /// </summary>
    public InviteMemberViewModel(IInvitationService invitationService, IErrorService errorService)
    {
        this.invitationService = invitationService;
        this.errorService = errorService;
        this.Title = "Invite Member";

        this.SendInvitationCommand = new Command(async () => await this.SendInvitationAsync(), this.CanSendInvitation);
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    /// <summary>
    /// Gets or sets a value indicating whether email method is selected.
    /// </summary>
    public bool IsEmailMethod
    {
        get => this.isEmailMethod;
        set
        {
            if (this.SetProperty(ref this.isEmailMethod, value))
            {
                if (value)
                {
                    this.IsSmsMethod = false;
                }

                ((Command)this.SendInvitationCommand).ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether SMS method is selected.
    /// </summary>
    public bool IsSmsMethod
    {
        get => this.isSmsMethod;
        set
        {
            if (this.SetProperty(ref this.isSmsMethod, value))
            {
                if (value)
                {
                    this.IsEmailMethod = false;
                }

                ((Command)this.SendInvitationCommand).ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Gets or sets the invitee email.
    /// </summary>
    public string InviteeEmail
    {
        get => this.inviteeEmail;
        set
        {
            if (this.SetProperty(ref this.inviteeEmail, value))
            {
                ((Command)this.SendInvitationCommand).ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Gets or sets the invitee phone.
    /// </summary>
    public string InviteePhone
    {
        get => this.inviteePhone;
        set
        {
            if (this.SetProperty(ref this.inviteePhone, value))
            {
                ((Command)this.SendInvitationCommand).ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage
    {
        get => this.errorMessage;
        set => this.SetProperty(ref this.errorMessage, value);
    }

    /// <summary>
    /// Gets the command to send invitation.
    /// </summary>
    public ICommand SendInvitationCommand { get; }

    /// <summary>
    /// Gets the command to cancel.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Applies query attributes from navigation.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var groupIdObj) && groupIdObj is string groupIdString)
        {
            if (Guid.TryParse(groupIdString, out var parsedGroupId))
            {
                this.groupId = parsedGroupId;
            }
        }
    }

    /// <summary>
    /// Determines if invitation can be sent.
    /// </summary>
    private bool CanSendInvitation()
    {
        if (this.IsEmailMethod)
        {
            return !string.IsNullOrWhiteSpace(this.inviteeEmail) && this.inviteeEmail.Contains("@");
        }
        else if (this.IsSmsMethod)
        {
            return !string.IsNullOrWhiteSpace(this.inviteePhone) && this.inviteePhone.StartsWith("+");
        }

        return false;
    }

    /// <summary>
    /// Sends the invitation.
    /// </summary>
    private async Task SendInvitationAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            CostSharing.Core.Models.Invitation? invitation;

            if (this.IsEmailMethod)
            {
                invitation = await this.invitationService.SendEmailInvitationAsync(this.groupId, this.inviteeEmail);
            }
            else
            {
                invitation = await this.invitationService.SendSmsInvitationAsync(this.groupId, this.inviteePhone);
            }

            if (invitation == null)
            {
                if (this.IsEmailMethod)
                {
                    this.ErrorMessage = $"Failed to send email invitation to {this.inviteeEmail}. Please check:\n" +
                        "• Email address is correct\n" +
                        "• You have internet connection\n" +
                        "• Email service is configured";
                }
                else
                {
                    this.ErrorMessage = $"Failed to send SMS invitation to {this.inviteePhone}. Please check:\n" +
                        "• Phone number is in E.164 format (+country code)\n" +
                        "• You have internet connection\n" +
                        "• SMS service is configured";
                }
                
                await Application.Current!.MainPage!.DisplayAlert("Invitation Failed", this.ErrorMessage, "OK");
                return;
            }

            await Application.Current!.MainPage!.DisplayAlert(
                "Invitation Sent",
                $"Invitation sent successfully to {(this.IsEmailMethod ? this.inviteeEmail : this.inviteePhone)}",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "sending invitation");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Cancels and navigates back.
    /// </summary>
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
