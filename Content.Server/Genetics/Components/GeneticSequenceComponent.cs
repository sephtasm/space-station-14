using Content.Shared.Genetics;

namespace Content.Server.Genetics
{
    /// <summary>
    /// This component is for mobs that have genes.
    /// </summary>
    [RegisterComponent]
    public sealed class GeneticSequenceComponent : Component
    {
        [DataField("genes")]
        public List<Gene> Genes = new();

        [DataField("mutationEffectsEnabled")]
        public bool MutationEffectsEnabled = false;

        [DataField("randomDormantMutationsOnInit")]
        public int RandomDormantMutationsOnInit = 0;

        [DataField("forcedActiveMutationsOnInit")]
        public HashSet<string> ForcedActiveMutationsOnInit = new();
    }
}
