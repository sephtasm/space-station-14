using Content.Shared.Genetics.GeneticsConsole;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Genetics
{
    public sealed class SharedGenetics
    {
        public static readonly Dictionary<Base, string> BaseToChar = new Dictionary<Base, string>
        {
            { Base.A, "A" },
            { Base.T, "T" },
            { Base.G, "G" },
            { Base.C, "C" },
            { Base.Unknown, "?" },
            { Base.Empty, " " }
        };

        /// <summary>
        /// Converts the given decimal number to a base 4 system using gene pairs.
        /// </summary>
        /// <param name="decimalNumber">The number to convert.</param>
        /// <returns></returns>
        public static string DecimalToGene(long decimalNumber)
        {
            const int radix = 4;
            const int bitsInLong = 64;
            const string digits = "ATGC";

            if (decimalNumber == 0)
                return "A";

            int index = bitsInLong - 1;
            long currentNumber = Math.Abs(decimalNumber);
            char[] charArray = new char[bitsInLong];

            while (currentNumber != 0)
            {
                int remainder = (int) (currentNumber % radix);
                charArray[index--] = digits[remainder];
                currentNumber = currentNumber / radix;
            }

            string result = new string(charArray, index + 1, bitsInLong - index - 1);
            if (decimalNumber < 0)
            {
                result = "-" + result;
            }

            return result;
        }

        public static string GetGeneTypeLoc(Gene gene, Dictionary<long, string> knownMutations)
        {
            switch (gene.Type)
            {
                case GeneType.Species:
                    return Loc.GetString("genetics-console-ui-window-gene-block-type-species");
                case GeneType.BaseLayer:
                    return Loc.GetString("genetics-console-ui-window-gene-block-type-base-layer");
                case GeneType.SkinColor:
                    return Loc.GetString("genetics-console-ui-window-gene-block-type-skin-color");
                case GeneType.EyeColor:
                    return Loc.GetString("genetics-console-ui-window-gene-block-type-eye-color");
                case GeneType.Markings:
                    return Loc.GetString("genetics-console-ui-window-gene-block-type-marking");
                case GeneType.Sex:
                    return Loc.GetString("genetics-console-ui-window-gene-block-type-sex");
                case GeneType.Mutation:
                    var mutationId = gene.Mutation!.Id;

                    if (knownMutations.ContainsKey(mutationId)) return knownMutations[mutationId];
                    var classification = gene.Mutation.Classification;
                    var classificationString = Loc.GetString("genetics-console-ui-window-classification-unknown", ("classification", classification));
                    return classificationString;
                default:
                    return Loc.GetString("genetics-console-ui-window-gene-block-type-unknown");
            }
        }
        private static List<BasePair> CombineBlocks(List<List<BasePair>> blocks)
        {
            var combined = new List<BasePair>();
            BasePair? previous = null;
            foreach (var block in blocks)
            {
                foreach (var pair in block)
                {
                    if (previous == null)
                        combined.Add(new BasePair(pair));

                    //  xo x
                    //  x ox
                    else if (pair.TopActual == Base.Empty && previous?.TopActual != Base.Empty && previous?.BotActual == Base.Empty)
                    {
                        previous.BotActual = pair.BotActual;
                        previous.BotAssigned = pair.BotAssigned;
                    }
                    //  x ox
                    //  xo x
                    else if (pair.BotActual == Base.Empty && previous?.BotActual != Base.Empty && previous?.TopActual == Base.Empty)
                    {
                        previous.TopActual = pair.TopActual;
                        previous.TopAssigned = pair.TopAssigned;
                    }

                    else
                        combined.Add(new BasePair(pair));
                    previous = combined[combined.Count - 1];
                }
            }
            return combined;
        }
        public static bool CanSubmitPuzzle(GenePuzzle puzzle)
        {
            if (puzzle == null || puzzle.UnusedBlocks.Count > 0) return false;
            var pairs = CombineBlocks(puzzle.UsedBlocks);
            return ValidateCombinedPairs(pairs);
        }

        private static bool ValidateCombinedPairs(List<BasePair> pairs)
        {
            foreach (var pair in pairs)
            {
                if (pair.TopAssigned == Base.Empty || pair.BotAssigned == Base.Empty) return false;
                if (pair.TopAssigned == Base.Empty || pair.BotAssigned == Base.Empty) return false;

                if (pair.TopAssigned == Base.Unknown || pair.BotAssigned == Base.Unknown) return false;
                if (pair.TopAssigned == Base.Unknown || pair.BotAssigned == Base.Unknown) return false;
            }

            return true;
        }

        public static string CalculateSubmittedSequence(List<List<BasePair>> blocks)
        {
            var combined = CombineBlocks(blocks);
            return string.Join("", combined.Select(pair => BaseToChar[pair.TopAssigned]).ToList());
        }

        public static int CalculatePuzzleSolutionDiff(GenePuzzle puzzle)
        {
            if (puzzle == null || puzzle.UnusedBlocks.Count > 0) return 0;
            var combined = CombineBlocks(puzzle.UsedBlocks);

            if (!ValidateCombinedPairs(combined))
                return puzzle.Solution.Length;

            var submitted = string.Join("", combined.Select(pair => BaseToChar[pair.TopAssigned]).ToList());

            // shouldn't be possible, but we'll check anyway
            if (submitted.Length != puzzle.Solution.Length) return Math.Abs(submitted.Length - puzzle.Solution.Length);

            int diff = 0;
            for (var i = 0; i < puzzle.Solution.Length; ++i)
            {
                if (submitted[i] != puzzle.Solution[i]) diff++;
            }

            return diff;
        }

        private static List<BasePair> GenerateSolvedPairs(string solution)
        {
            var charToBP = new Dictionary<char, Base>
            {
                { 'A', Base.A },
                { 'T', Base.T },
                { 'G', Base.G },
                { 'C', Base.C }
            };
            var charToOpposite = new Dictionary<char, Base>
            {
                { 'A', Base.T },
                { 'T', Base.A },
                { 'G', Base.C },
                { 'C', Base.G }
            };

            var pairs = new List<BasePair>();
            foreach (char c in solution)
            {
                var p = new BasePair(charToBP[c], charToBP[c], charToOpposite[c], charToOpposite[c]);
                pairs.Add(p);
            }
            return pairs;
        }
        public static GenePuzzle GeneratePuzzle(string solution, IRobustRandom random)
        {
            int cuts = 3;
            int doubleUnkowns = 2;
            int unknowns = 4;
            List<BasePair> solved = GenerateSolvedPairs(solution);
            List<List<BasePair>> puzzle = new List<List<BasePair>>();
            int totalLength = solved.Count;

            // insert single unknown bases
            for (var i = 0; i < unknowns; ++i)
            {
                int index = random.Next(0, totalLength);
                if (random.Next(0, 2) == 0)
                {
                    solved[index].TopAssigned = Base.Unknown;
                    solved[index].TopActual = Base.Unknown;
                }
                else
                {
                    solved[index].BotAssigned = Base.Unknown;
                    solved[index].BotActual = Base.Unknown;
                }
            }

            // insert double unknowns
            for (var i = 0; i < doubleUnkowns; ++i)
            {
                int index = random.Next(0, totalLength);
                solved[index].TopAssigned = Base.Unknown;
                solved[index].TopActual = Base.Unknown;
                solved[index].BotAssigned = Base.Unknown;
                solved[index].BotActual = Base.Unknown;
            }

            // make cuts
            int start = 0;
            for (var i = 0; i < cuts; ++i)
            {
                int min = Math.Max(i + 2, (totalLength / cuts) * i);
                int max = Math.Min(totalLength - 1, (totalLength / cuts) * (i + 1));

                int cut = random.Next(min, max);
                int stop = Math.Min(cut, totalLength);
                List<BasePair> block = new List<BasePair>();
                for (var j = start; j < stop; j++)
                {
                    block.Add(solved[j]);
                }
                start += block.Count;
                puzzle.Add(block);
            }
            List<BasePair> remaining = new List<BasePair>();
            for (var j = start; j < totalLength; j++)
            {
                remaining.Add(solved[j]);
            }
            if (remaining.Count > 0) puzzle.Add(remaining);

            // for each resulting segment, decide if it will be a clean or jagged cut
            List<BasePair>? previous = null;
            foreach (var block in puzzle)
            {
                if (previous != null)
                {
                    var dieRoll = random.Next(0, 10);
                    if (dieRoll < 4)
                    {
                        var splitPairs = SplitPair(previous.Pop());
                        previous.Add(splitPairs.Item1);
                        block.Insert(0, splitPairs.Item2);
                    }
                    else if (dieRoll < 8)
                    {
                        var splitPairs = SplitPair(previous.Pop());
                        previous.Add(splitPairs.Item2);
                        block.Insert(0, splitPairs.Item1);
                    }
                }
                previous = block;
            }
            random.Shuffle(puzzle);
            return new GenePuzzle(solution, puzzle, new List<List<BasePair>>());
        }
        private static (BasePair, BasePair) SplitPair(BasePair pair)
        {
            var top = new BasePair(pair.TopActual, pair.TopAssigned, Base.Empty, Base.Empty);
            var bot = new BasePair(Base.Empty, Base.Empty, pair.BotActual, pair.BotAssigned);
            return (top, bot);
        }
    }

    [Serializable, NetSerializable]
    public enum GeneModificationMethod : byte
    {
        DNASequencer,
        Mutation,
        Radiation,
    }

    [Serializable, NetSerializable]
    public enum GeneType : byte
    {
        Species,
        SkinColor,
        EyeColor,
        Markings,
        Sex,
        Gender, // not used
        BaseLayer,
        Mutation,
    }

    [Serializable, NetSerializable]
    public enum Base
    {
        A, T, G, C, Unknown, Empty
    }

    [Serializable, NetSerializable]
    public class BasePair
    {
        public Base TopActual { get; set; }
        public Base TopAssigned { get; set; }
        public Base BotActual { get; set; }
        public Base BotAssigned { get; set; }

        public BasePair(Base topActual, Base topAssigned, Base botActual, Base botAssigned)
        {
            TopActual = topActual;
            TopAssigned = topAssigned;
            BotActual = botActual;
            BotAssigned = botAssigned;
        }

        public BasePair(BasePair other)
        {
            TopActual = other.TopActual;
            TopAssigned = other.TopAssigned;
            BotActual = other.BotActual;
            BotAssigned = other.BotAssigned;
        }

    }


    [Serializable, NetSerializable]
    public enum BlockType : byte
    {
        Primary,
        Modifier,
    }

    [Serializable, NetSerializable]
    public sealed class GeneDisplay
    {
        public GeneDisplay(Gene gene, string display)
        {
            Gene = gene;
            Display = display;
        }

        public Gene Gene { get; set; }
        public string Display { get; set; }

    }

    [Serializable, NetSerializable]
    public sealed class Gene
    {
        public Gene(GeneType type, List<Block> blocks, MarkingCategories? markingCategory = null, bool active = true, MutationSpecifier? mutation = null, bool damaged = false)
        {
            Type = type;
            Blocks = blocks;
            MarkingCategory = markingCategory;
            Active = active;
            Damaged = damaged;
            Mutation = mutation;
        }

        public bool Active { get; set; } = true;
        public bool Damaged { get; set; } = false;

        public GeneType Type { get; set; }

        public MutationSpecifier? Mutation { get; set; }

        public MarkingCategories? MarkingCategory { get; set; } = null;
        public List<Block> Blocks { get; set; } = new();

    }

    [Serializable, NetSerializable]
    public sealed class MutationSpecifier
    {
        public string Classification { get; set; }
        public long Id { get; set; }

        public MutationSpecifier(string classification, long id)
        {
            Classification = classification;
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class Block
    {
        public Block(uint value, string display, BlockType blockType = BlockType.Primary)
        {
            Type = blockType;
            Value = value;
            Display = display;
        }
        public BlockType Type { get; set; } = BlockType.Primary;
        public uint Value { get; set; }
        public string Display { get; set; }
    }

    public abstract class MutationChangedEvent : HandledEntityEventArgs
    {
        public MutationChangedEvent(EntityUid uid, uint value, EntityUid? consoleUid)
        {
            Uid = uid;
            Value = value;
            ConsoleUid = consoleUid;
        }

        public EntityUid Uid { get; }

        public EntityUid? ConsoleUid { get; }
        public uint Value { get; }
    }

    public sealed class ActivateMutationEvent : MutationChangedEvent
    {
        public ActivateMutationEvent(EntityUid uid, uint value, EntityUid? consoleUid) : base(uid, value, consoleUid) { }
    }
    public sealed class DeactivateMutationEvent : MutationChangedEvent
    {
        public DeactivateMutationEvent(EntityUid uid, uint value, EntityUid? consoleUid) : base(uid, value, consoleUid) { }
    }
}
