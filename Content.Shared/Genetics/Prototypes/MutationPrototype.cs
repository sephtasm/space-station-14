using Content.Server.Genetics.MutationEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Genetics.Prototypes;

[Prototype("mutation")]
public sealed class MutationPrototype : IPrototype
{
    /// <summary>
    /// Prototype ID of the mutation.
    /// </summary>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// ID of the localization used as a player-visible name.
    /// </summary>
    [DataField("localizationStringId", required: true)]
    public string LocalizationStringId { get; } = default!;

    /// <summary>
    /// ID of the localization string used as a hint to the type of the mutation.
    /// </summary>
    [DataField("classificationStringId", required: true)]
    public string ClassificationStringId { get; } = default!;

    /// <summary>
    ///     Effect.
    /// </summary>
    [DataField("effects", serverOnly: true)]
    public List<MutationEffect> Effects { get; } = default!;

    /// <summary>
    ///     Strength of the effect, on a scale from 0->1.
    /// </summary>
    [DataField("strength")]
    public float Strength { get; } = default!;

    /// <summary>
    ///     Likelihood of a random person having the dormant gene.
    /// </summary>
    [DataField("weight")]
    public float Weight { get; } = default!;


}

