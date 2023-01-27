using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Genetics.GeneticsConsole
{
    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly EntityUid? PodBodyUid;
        public readonly PodStatus PodStatus;
        public readonly bool PodConnected;
        public readonly bool PodInRange;
        public readonly TimeSpan TimeRemaining;
        public readonly TimeSpan TotalTime;
        public readonly List<GeneDisplay> SequencedGenes;
        public readonly Dictionary<long, string> KnownMutations;
        public readonly Gene? ActivationTargetGene;
        public readonly GenePuzzle? Puzzle;
        public readonly bool ForceUpdate;

        public GeneticsConsoleBoundUserInterfaceState(EntityUid? podBodyUid, PodStatus podStatus, bool podConnected, bool podInRange,
            TimeSpan timeRemaining, TimeSpan totalTime, List<GeneDisplay> sequencedGenes, Dictionary<long, string> knownMutations,
            Gene? activationTargetGene, GenePuzzle? puzzle, bool forceUpdate)
        {
            PodBodyUid = podBodyUid;
            PodStatus = podStatus;
            PodInRange = podInRange;
            PodConnected = podConnected;
            TimeRemaining = timeRemaining;
            TotalTime = totalTime;
            SequencedGenes = sequencedGenes;
            KnownMutations = knownMutations;
            ActivationTargetGene = activationTargetGene;
            Puzzle = puzzle;
            ForceUpdate = forceUpdate;
        }
    }

    [Serializable, NetSerializable]
    public enum PodStatus : byte
    {
        PodEmpty,
        PodOccupantAlive,
        PodOccupantDead,
        PodOccupantNoGenes,
        ScanStarted,
        ScanComplete
    }

    [Serializable, NetSerializable]
    public sealed class GenePuzzle
    {
        public readonly string Solution;
        public readonly List<List<BasePair>> UnusedBlocks;
        public readonly List<List<BasePair>> UsedBlocks;

        public GenePuzzle(string solution, List<List<BasePair>> unusedBlocks, List<List<BasePair>> usedBlocks)
        {
            Solution = solution;
            UnusedBlocks = unusedBlocks;
            UsedBlocks = usedBlocks;
        }
    }

    [Serializable, NetSerializable]
    public enum GeneticsConsoleUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum UiButton : byte
    {
        Sequence,
        Eject
    }

    public enum GeneticsConsoleScreen
    {
        Status,
        GeneRepair,
        Activation
    }

    [Serializable, NetSerializable]
    public sealed class SequenceButtonPressedMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ActivateButtonPressedMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class CancelActivationButtonPressedMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class RepairButtonPressedMessage : BoundUserInterfaceMessage
    {
        public int Index { get; set; }

        public RepairButtonPressedMessage(int index)
        {
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public sealed class StartActivationButtonPressedMessage : BoundUserInterfaceMessage
    {
        public int Index { get; set; }

        public StartActivationButtonPressedMessage(int index)
        {
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public sealed class UnusedBlockButtonPressedMessage : BoundUserInterfaceMessage
    {
        public int Index { get; set; }

        public UnusedBlockButtonPressedMessage(int index)
        {
            Index = index;
        }
    }

    
    [Serializable, NetSerializable]
    public sealed class UsedBlockButtonPressedMessage : BoundUserInterfaceMessage
    {
        public int Index { get; set; }

        public UsedBlockButtonPressedMessage(int index)
        {
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public sealed class FillBasePairButtonPressedMessage : BoundUserInterfaceMessage
    {
        public int TargetBlockIndex { get; set; }
        public int TargetPairIndex { get; set; }
        public bool IsTop { get; set; }

        public Base NewBase { get; set; }

        public FillBasePairButtonPressedMessage(Base newBase, int targetBlockIndex, int targetPairIndex, bool isTop)
        {
            NewBase = newBase;
            TargetBlockIndex = targetBlockIndex;
            TargetPairIndex = targetPairIndex;
            IsTop = isTop;
        }
    }

    public sealed class PuzzleChecker
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


        public static List<BasePair> CombineBlocks(List<List<BasePair>> blocks)
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
                    previous = combined[combined.Count-1];
                }
            }
            return combined;
        }
        public static bool CanSubmitPuzzle(GenePuzzle puzzle)
        {
            if (puzzle == null || puzzle.UnusedBlocks.Count>0) return false;
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
            for(var i=0; i< puzzle.Solution.Length; ++i)
            {
                if (submitted[i] != puzzle.Solution[i]) diff++;
            }

            return diff;
        }
    }

}
