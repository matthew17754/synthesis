using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.IO
{
    public class SinkWriter : TextWriter
    {
        // Writes to nothing
        public SinkWriter() { }
        public override void Write(char value)
        {
            return;
        }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
