using System;

namespace CryptoPortfolioTracker.Models.Legacy;

public class DataPointV1
{
    public DateOnly Date { get; set; }
    public double Value { get; set; }
}

