// Development-only endpoints
if (app.Environment.IsDevelopment())
{
    // Simple health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    // Configuration validation endpoint
    app.MapGet("/config/validate", async (IStartupValidationService validationService) =>
    {
        var errors = await validationService.GetValidationErrorsAsync();
        return Results.Ok(new
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        });
    });

    // Configuration diagnostics endpoint
    app.MapGet("/config/diagnostics", async (IConfigurationDiagnosticsService diagnosticsService) =>
    {
        var diagnostics = await diagnosticsService.RunDiagnosticsAsync();
        return Results.Ok(diagnostics);
    });

    // Configuration report endpoint
    app.MapGet("/config/report", async (IConfigurationDiagnosticsService diagnosticsService) =>
    {
        var report = await diagnosticsService.GenerateConfigurationReportAsync();
        return Results.Text(report, "text/plain");
    });

    // Configuration settings overview (non-sensitive data only)
    app.MapGet("/config/overview", () =>
    {
        var overview = new
        {
            Environment = app.Environment.EnvironmentName,
            HasDatabase = !string.IsNullOrEmpty(ConfigurationHelper.GetConnectionString()),
            HasSendGrid = !string.IsNullOrEmpty(ConfigurationHelper.EmailSettingsTyped.SendGrid.ApiKey),
            HasStripe = !string.IsNullOrEmpty(ConfigurationHelper.PaymentSettingsTyped.Stripe.PublicKey),
            Features = new
            {
                LiveBetting = ConfigurationHelper.Features.EnableLiveBetting,
                CashOut = ConfigurationHelper.Features.EnableCashOut,
                Promotions = ConfigurationHelper.Features.EnablePromotions,
                ApiV2 = ConfigurationHelper.Features.EnableApiV2
            },
            BettingLimits = new
            {
                MinimumBet = ConfigurationHelper.ApplicationSettingsTyped.Betting.MinimumBet,
                MaximumBet = ConfigurationHelper.ApplicationSettingsTyped.Betting.MaximumBet,
                DailyLimit = ConfigurationHelper.ApplicationSettingsTyped.Betting.DailyBetLimit
            },
            Timestamp = DateTime.UtcNow
        };

        return Results.Ok(overview);
    });
}