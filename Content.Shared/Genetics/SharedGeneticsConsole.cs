using Robust.Shared.Serialization;

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
    public sealed class PrintReportButtonPressedMessage : BoundUserInterfaceMessage
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

}
