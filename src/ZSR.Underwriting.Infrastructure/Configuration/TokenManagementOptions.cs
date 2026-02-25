namespace ZSR.Underwriting.Infrastructure.Configuration;

public class TokenManagementOptions
{
    public int MaxConversationMessages { get; set; } = 20;
    public int MaxConversationTokens { get; set; } = 150_000;
    public int DailyUserTokenBudget { get; set; } = 500_000;
    public int DealTokenBudget { get; set; } = 1_000_000;
}
