
using System;
using System.Collections.Generic;
using System.Diagnostics;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public partial class HeatMap
{
    public HeatMap()
    {
        DataPoints = new List<HeatMapPoint>();
        HeatMapStatus = HeatMapStatus.Idle;
    }

    public List<HeatMapPoint> DataPoints { get; set; }
    public HeatMapStatus HeatMapStatus { get; set; }

    
}
