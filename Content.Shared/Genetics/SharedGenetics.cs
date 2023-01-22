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
        Gender,
        BaseLayer,
    }

    [Serializable, NetSerializable]
    public enum BlockType : byte
    {
        Primary,
        Modifier,
    }

    [Serializable, NetSerializable]
    public sealed class Gene
    {
        public Gene(GeneType type, List<Block> blocks, MarkingCategories? markingCategory = null)
        {
            Type = type;
            Blocks = blocks;
            MarkingCategory = markingCategory;
        }

        public GeneType Type { get; set; }

        public MarkingCategories? MarkingCategory { get; set; } = null;
        public List<Block> Blocks { get; set; } = new();

    }

    [Serializable, NetSerializable]
    public sealed class Block
    {
        public Block(long value, BlockType blockType = BlockType.Primary)
        {
            Type = blockType;
            Value = value;
        }
        public BlockType Type { get; set; } = BlockType.Primary;
        public long Value { get; set; }
    }
}
