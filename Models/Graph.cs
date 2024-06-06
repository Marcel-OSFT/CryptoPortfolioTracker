
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Services;
using LanguageExt;
using LiveChartsCore.Defaults;
using Serilog;
using Serilog.Core;

namespace CryptoPortfolioTracker.Models;

[Serializable]
public partial class Graph
{
    public Graph()
    {
        Debug.WriteLine("New Instance <Graph>");
        DataPointsPortfolio = new List<DataPoint>();
        DataPointsInFlow = new List<DataPoint>();
        DataPointsOutFlow = new List<DataPoint>();
        //HistoricalDataByIdsBufferList = new List<HistoricalDataById>();
        modifyFromDate = DateOnly.MinValue;
        GraphStatus = GraphStatus.Idle;
    }

    public List<DataPoint> DataPointsPortfolio { get; set; }
    public List<DataPoint> DataPointsInFlow {  get; set; }
    public List<DataPoint> DataPointsOutFlow { get; set; }

    //public List<HistoricalDataById> HistoricalDataByIdsBufferList { get; set; }

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
