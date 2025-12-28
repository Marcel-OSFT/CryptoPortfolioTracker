
namespace TemperatureMonitor.Models;

[Serializable]
public partial class Graph
{
    public Graph()
    {
       // DataPointsPortfolio = new List<DataPoint>();
        GraphStatus = GraphStatus.Idle;
    }

    public List<DataPoint> DataPoints { get; set; }
    
    public GraphStatus GraphStatus { get; set; }

}
