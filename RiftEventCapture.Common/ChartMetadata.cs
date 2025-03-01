namespace RiftEventCapture.Common;

public readonly struct ChartMetadata {
    public readonly string Name;
    public readonly Difficulty Difficulty;

    public ChartMetadata(string name, Difficulty difficulty) {
        Name = name;
        Difficulty = difficulty;
    }
}