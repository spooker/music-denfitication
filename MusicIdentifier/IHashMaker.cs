using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;

namespace MusicIdentifier
{
    public interface IHashMaker
    {
        long[] GetHash(Complex[][] data);
        int ChunkSize { get; set; }
        int StepSize { get; set; }
    }
}
