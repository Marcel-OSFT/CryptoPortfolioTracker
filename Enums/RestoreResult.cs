namespace CryptoPortfolioTracker.Enums;

public enum RestoreResult
{
    None = 0,
    Succesfull = 1,
    Warning = 2,
    ErrorNonCritical = 3,
    ErrorCriticalCanContinue = 4,
    ErrorCriticalNeedRecovery = 5,

}