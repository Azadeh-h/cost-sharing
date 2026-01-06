using CostSharingApp.Services;
using Microsoft.Extensions.Logging;

namespace CostSharingApp;

public static class MauiProgram
{
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

		// Register services for dependency injection
		ConfigureServices(builder.Services);

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		// Phase 2: Foundational Services
		services.AddSingleton<ILoggingService, LoggingService>();
		services.AddSingleton<ICacheService, CacheService>();
		services.AddSingleton<IDriveAuthService, DriveAuthService>();
		services.AddSingleton<IDriveService, DriveService>();
		services.AddSingleton<IErrorService, ErrorService>();
		services.AddSingleton<IAuthService, AuthService>();

		// Phase 3: US1 - Group Services
		services.AddSingleton<IGroupService, GroupService>();

		// Phase 4: US2 - Invitation Services
		services.AddSingleton<INotificationService, NotificationService>();
		services.AddSingleton<IInvitationService, InvitationService>();

		// Phase 5: US3 - Expense Services
		services.AddSingleton<ISplitCalculationService, SplitCalculationService>();
		services.AddSingleton<IExpenseService, ExpenseService>();
		services.AddSingleton<IDebtCalculationService, DebtCalculationService>();

		// Phase 7: US5 - Settlement Services
		services.AddSingleton<ISettlementService, SettlementService>();

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

		// Initialize cache on startup
		var serviceProvider = services.BuildServiceProvider();
		var cacheService = serviceProvider.GetRequiredService<ICacheService>();
		cacheService.InitializeAsync().Wait();
	}
}
