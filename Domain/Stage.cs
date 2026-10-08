namespace EKProno.Domain;

/// <summary>
/// The round of the tournament a match belongs to (spec 002 §5.3). The numeric values are
/// the Stage value object's <c>order</c>, so grouping and display follow the enum itself.
/// </summary>
public enum Stage
{
    GroupStage = 1,
    RoundOf16 = 2,
    QuarterFinal = 3,
    SemiFinal = 4,
    ThirdPlacePlayOff = 5,
    Final = 6,
}

public static class StageExtensions
{
    /// <summary>How a stage reads on screen. The enum names are not for organisers.</summary>
    public static string DisplayName(this Stage stage) => stage switch
    {
        Stage.GroupStage => "Group stage",
        Stage.RoundOf16 => "Round of 16",
        Stage.QuarterFinal => "Quarter-final",
        Stage.SemiFinal => "Semi-final",
        Stage.ThirdPlacePlayOff => "Third-place play-off",
        Stage.Final => "Final",
        _ => stage.ToString(),
    };

    /// <summary>The six stages in tournament order (FR-008, FR-018).</summary>
    public static IReadOnlyList<Stage> All { get; } = Enum.GetValues<Stage>().OrderBy(s => (int)s).ToList();
}
