using System.Collections.Generic;

namespace RiftEventCapture.Common;

public class SessionInfo {
    public string ChartName { get; }
    public string ChartID { get; }
    public Difficulty ChartDifficulty { get; }
    public IReadOnlyList<string> Pins { get; }

    public SessionInfo(string chartName, string chartID, Difficulty chartDifficulty, IReadOnlyList<string> pins) {
        ChartName = chartName;
        ChartID = chartID;
        ChartDifficulty = chartDifficulty;
        Pins = pins;
    }
}