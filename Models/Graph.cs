
using System;
using System.Collections.Generic;
using System.Diagnostics;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

[Serializable]
public partial class Graph
{
    public Graph()
    {
        DataPointsPortfolio = new List<DataPoint>();
        DataPointsInFlow = new List<DataPoint>();
        DataPointsOutFlow = new List<DataPoint>();
        modifyFromDate = DateOnly.MinValue;
        GraphStatus = GraphStatus.Idle;
    }

    public List<DataPoint> DataPointsPortfolio { get; set; }
    public List<DataPoint> DataPointsInFlow {  get; set; }
    public List<DataPoint> DataPointsOutFlow { get; set; }
    public GraphStatus GraphStatus { get; set; }
    public bool IsModificationRequested { get; set; }

    private DateOnly modifyFromDate;
    public DateOnly ModifyFromDate
    {
        get { return modifyFromDate; }
        set
        {
            if (value != modifyFromDate)
            {
                modifyFromDate = value;
                IsModificationRequested = modifyFromDate.Equals(DateOnly.MinValue) ? false : true;
            }
        }
    }

    
}
