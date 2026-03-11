namespace ReembolsoProcessor.App.Core;

public sealed class RulesConfiguration
{
    public int MaxProcessingDays { get; init; } = 90;
    public int FraudWindowDays { get; init; } = 30;
    public int MaxRequestsPerClientInWindow { get; init; } = 5;
    public decimal MaxReimbursementPerProcedure { get; init; } = 500m;
    public decimal MaxTotalReimbursementPerClientInWindow { get; init; } = 1500m;
}
