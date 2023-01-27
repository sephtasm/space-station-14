using Content.Server.Medical.Components;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Genetics
{
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
        public Gene(GeneType type, List<Block> blocks, MarkingCategories? markingCategory = null, bool active = true, bool damaged = false)
        {
            Type = type;
            Blocks = blocks;
            MarkingCategory = markingCategory;
            Active = active;
            Damaged = damaged;
        }

        public bool Active { get; set; } = true;
        public bool Damaged { get; set; } = false;

        public GeneType Type { get; set; }

        public MarkingCategories? MarkingCategory { get; set; } = null;
        public List<Block> Blocks { get; set; } = new();

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
