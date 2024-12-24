using System;

namespace CryptoPortfolioTracker.Models;
public class DataPoint
{
   
    public DateOnly Date  { get; set; }
    public double Value  { get; set; }
}

public class CapitalFlowPoint
{

    public string Year { get; set; }
    public double Value { get; set; }
}