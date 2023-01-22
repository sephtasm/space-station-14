using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Genetics
{
    [Serializable]
    public enum BasePair : byte
    {
        A,
        T,
        G,
        C
    }
}
