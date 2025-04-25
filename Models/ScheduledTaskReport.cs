using CryptoPortfolioTracker.Enums;
using System;
using System.Collections.Generic;

namespace CryptoPortfolioTracker.Models;
public class ScheduledTaskReport
{
    public DateTime CompletedTimeStamp { get; set; }
    public ScheduledTaskStatus Status { get; set; } = ScheduledTaskStatus.NotStarted;
    public List<string> FaultyChartsApiId { get; set; } = new List<string>();
}


