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

        [DataField("randomDormantMutationsOnInit")]
        public int RandomActiveMutationsOnInit = 2;

        [DataField("forcedActiveMutationsOnInit")]
        public HashSet<string> ForcedActiveMutationsOnInit = new();
    }
}
