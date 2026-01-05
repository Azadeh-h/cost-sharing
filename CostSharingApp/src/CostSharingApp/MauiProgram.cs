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

		// Initialize cache on startup
		var serviceProvider = services.BuildServiceProvider();
		var cacheService = serviceProvider.GetRequiredService<ICacheService>();
		cacheService.InitializeAsync().Wait();
	}
}
