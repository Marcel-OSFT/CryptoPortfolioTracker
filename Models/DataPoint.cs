
using System;

namespace CryptoPortfolioTracker.Models;
public class DataPoint
{
    public DataPoint()
    {
        
    }

    public DateOnly Date  { get; set; }
    public double Value  { get; set; }
}
