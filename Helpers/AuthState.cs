using System;

namespace CryptoPortfolioTracker.Helpers;

public class AuthState
{
    public int FailedAttempts { get; set; }
    public DateTime? LockoutUntil { get; set; }
}

    
