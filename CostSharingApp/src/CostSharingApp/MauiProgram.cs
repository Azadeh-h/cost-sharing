using System.Reflection;
using CostSharing.Core.Services;
using CostSharingApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CostSharingApp;

/// <summary>
/// Configures and creates the MAUI application.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application with required services and fonts.
    /// </summary>
    /// <returns>A configured <see cref="MauiApp"/> instance.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Load configuration from appsettings.json
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("CostSharingApp.appsettings.json");
        if (stream != null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);
        }

        // Register services for dependency injection
        ConfigureServices(builder.Services, builder.Configuration);

#if DEBUG
    builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Phase 2: Foundational Services
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IErrorService, ErrorService>();
        services.AddSingleton<IAuthService, AuthService>();

        // Phase 3: US1 - Group Services
        services.AddSingleton<IGroupService, GroupService>();

        // Phase 4: US2 - Invitation Services
        services.AddSingleton<INotificationService>(sp =>
        {
            var loggingService = sp.GetRequiredService<ILoggingService>();
            
            // Load configuration from appsettings.json
            var sendGridApiKey = configuration["SendGrid:ApiKey"] ?? string.Empty;
            var sendGridFromEmail = configuration["SendGrid:FromEmail"] ?? string.Empty;
            var sendGridFromName = configuration["SendGrid:FromName"] ?? "Cost Sharing App";
            var twilioAccountSid = configuration["Twilio:AccountSid"] ?? string.Empty;
            var twilioAuthToken = configuration["Twilio:AuthToken"] ?? string.Empty;
            var twilioPhoneNumber = configuration["Twilio:PhoneNumber"] ?? string.Empty;
            
            return new NotificationService(
                loggingService,
                sendGridApiKey,
                sendGridFromEmail,
                sendGridFromName,
                twilioAccountSid,
                twilioAuthToken,
                twilioPhoneNumber);
        });
        services.AddSingleton<IInvitationService, InvitationService>();

        // Phase 5: US3 - Expense Services
        services.AddSingleton<ISplitCalculationService, SplitCalculationService>();
        services.AddSingleton<IExpenseService, ExpenseService>();
        services.AddSingleton<IDebtCalculationService, DebtCalculationService>();

        // Phase 7: US5 - Settlement Services
        services.AddSingleton<ISettlementService, SettlementService>();

        // Google Services
        services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
        services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
        services.AddSingleton<IGoogleSyncService, GoogleSyncService>();
        services.AddSingleton<IGoogleInvitationService, GoogleInvitationService>();

        // Phase 3: US1 - ViewModels
        services.AddTransient<ViewModels.Groups.GroupListViewModel>();
        services.AddTransient<ViewModels.Groups.CreateGroupViewModel>();
        services.AddTransient<ViewModels.Groups.GroupDetailsViewModel>();

        // Phase 4: US2 - ViewModels
        services.AddTransient<ViewModels.Members.InviteMemberViewModel>();
        services.AddTransient<ViewModels.Members.AcceptInvitationViewModel>();

        // Phase 5: US3 - ViewModels
        services.AddTransient<ViewModels.Expenses.AddExpenseViewModel>();
        services.AddTransient<ViewModels.Expenses.ExpenseListViewModel>();
        services.AddTransient<ViewModels.Expenses.ExpenseDetailsViewModel>();

        // Phase 6: US4 - ViewModels
        services.AddTransient<ViewModels.Expenses.CustomSplitViewModel>();

        // Phase 7: US5 - ViewModels
        services.AddTransient<ViewModels.Debts.SimplifiedDebtsViewModel>();

        // Phase 8: US6 - ViewModels
        services.AddTransient<ViewModels.Dashboard.DashboardViewModel>();
        services.AddTransient<ViewModels.Dashboard.TransactionHistoryViewModel>();

        // Google Integration ViewModels
        services.AddTransient<ViewModels.GoogleSignInViewModel>();
        services.AddTransient<ViewModels.SyncStatusViewModel>();
        services.AddTransient<ViewModels.ConflictResolutionViewModel>();

        // Phase 4: US2 - Pages
        services.AddTransient<Views.Members.InviteMemberPage>();
        services.AddTransient<Views.Members.AcceptInvitationPage>();

        // Phase 5: US3 - Pages
        services.AddTransient<Views.Expenses.AddExpensePage>();
        services.AddTransient<Views.Expenses.ExpenseDetailsPage>();

        // Phase 6: US4 - Pages
        services.AddTransient<Views.Expenses.CustomSplitPage>();

        // Phase 7: US5 - Pages
        services.AddTransient<Views.Debts.SimplifiedDebtsPage>();

        // Phase 8: US6 - Pages
        services.AddTransient<Views.Dashboard.DashboardPage>();
        services.AddTransient<Views.Dashboard.TransactionHistoryPage>();

        // Phase 3: US1 - Pages
        services.AddTransient<Views.Groups.GroupListPage>();
        services.AddTransient<Views.Groups.CreateGroupPage>();
        services.AddTransient<Views.Groups.GroupDetailsPage>();

        // Google Integration Pages
        services.AddTransient<Views.GoogleSignInPage>();
        services.AddTransient<Views.SyncStatusView>();
        services.AddTransient<Views.ConflictResolutionPage>();

        // Note: Cache initialization moved to App.xaml.cs OnStart() to avoid blocking startup
    }
}
