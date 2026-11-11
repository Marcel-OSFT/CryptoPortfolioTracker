
using System;
using System.Collections.Generic;
using System.Diagnostics;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models.Legacy;

[Serializable]
public partial class GraphV1
{
    public GraphV1()
    {
        DataPointsPortfolio = new List<DataPointV1>();
        DataPointsInFlow = new List<DataPointV1>();
        DataPointsOutFlow = new List<DataPointV1>();
        modifyFromDate = DateOnly.MinValue;
        GraphStatus = GraphStatus.Idle;
    }

    public int Version { get; set; }
    public List<DataPointV1> DataPointsPortfolio { get; set; }
    public List<DataPointV1> DataPointsInFlow {  get; set; }
    public List<DataPointV1> DataPointsOutFlow { get; set; }
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