using System;

namespace TemperatureMonitor.Models;
public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

public class CapitalFlowPoint
{
    public string Year { get; set; }
    public double Value { get; set; }
}
