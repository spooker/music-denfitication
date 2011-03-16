using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;

namespace Shazam
{
    public interface IHashMaker
    {
        long[] GetHash(Complex[][] data);
        int ChunkSize { get; }
    }
}
