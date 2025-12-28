namespace TemperatureMonitor.Models;

public class GraphUpdatedMessage
{

}

public class CurrentModeChangedMessage
{
    public int sampleRate { get; }

    public CurrentModeChangedMessage(int value)
    {
        sampleRate = value;
    }
}
public class SampleRateSettingChangedMessage
{
    public DaySelectorMode mode { get; }

    public SampleRateSettingChangedMessage(DaySelectorMode _mode)
    {
        mode = _mode;
    }
}