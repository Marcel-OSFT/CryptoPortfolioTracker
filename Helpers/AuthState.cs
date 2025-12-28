using System;

namespace TemperatureMonitor.Helpers;

public class AuthState
{
    public int FailedAttempts { get; set; }
    public DateTime? LockoutUntil { get; set; }
}

    
