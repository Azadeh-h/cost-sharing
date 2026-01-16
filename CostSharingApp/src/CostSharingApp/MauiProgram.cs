using System.Reflection;
using CostSharing.Core.Interfaces;
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
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<IDriveAuthService, DriveAuthService>();

        // Phase 3: US1 - Group Services
        services.AddSingleton<IGroupService, GroupService>();



        // Phase 5: US3 - Expense Services
        services.AddSingleton<ISplitCalculationService, SplitCalculationService>();
        services.AddSingleton<IExpenseService, ExpenseService>();
        services.AddSingleton<IDebtCalculationService, DebtCalculationService>();

        // Phase 7: US5 - Settlement Services
        services.AddSingleton<ISettlementService, SettlementService>();

        // Phase 3 (P2P Sync): Drive Sync Services
        services.AddSingleton<DriveErrorHandler>();
        services.AddSingleton<IDriveSyncService, DriveSyncService>();
        services.AddSingleton<IConflictResolver, ConflictResolutionService>();
        services.AddSingleton<IOfflineQueueService, OfflineQueueService>();

        // Gmail Invitation Service
        services.AddSingleton<IGmailInvitationService, GmailInvitationService>();

        // Phase 3: US1 - ViewModels
        services.AddTransient<ViewModels.Groups.GroupListViewModel>();
        services.AddTransient<ViewModels.Groups.CreateGroupViewModel>();
        services.AddTransient<ViewModels.Groups.GroupDetailsViewModel>();

        // Phase 4: US2 - ViewModels
        services.AddTransient<ViewModels.Members.InviteMemberViewModel>();

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

        // Phase 3 (P2P Sync): Sync ViewModels
        services.AddTransient<ViewModels.Groups.SyncStatusViewModel>();

        // General ViewModels
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<ViewModels.EditProfileViewModel>();

        // Phase 4: US2 - Pages
        services.AddTransient<Views.Members.InviteMemberPage>();

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

        // Phase 3 (P2P Sync): Sync Pages
        services.AddTransient<Views.Groups.SyncSettingsPage>();

        // General Pages
        services.AddTransient<Views.SettingsPage>();
        services.AddTransient<Views.EditProfilePage>();

        // Note: Cache initialization moved to App.xaml.cs OnStart() to avoid blocking startup
    }
}
