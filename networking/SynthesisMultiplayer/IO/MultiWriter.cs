using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.IO
{
    public class MultiWriter : TextWriter
    {

        public bool AutoFlush { get; private set; }
        public MultiWriter(bool autoFlush = false, params TextWriter[] writers)
        {
            AutoFlush = autoFlush;
            Writers = new List<TextWriter>();
            Writers.AddRange(writers);
        }

        public void RegisterWriter(TextWriter w)
        {
            Writers.Add(w);
        }

        public override void Write(char c)
        {
            foreach(var w in Writers)
            {
                try
                {
                    w.Write(c);
                    if (AutoFlush)
                        w.Flush();
                }
                catch(ObjectDisposedException)
                {
                    Info.Log("Attempted to write to disposed writer. Removing.");
                    Writers.Remove(w);
                }
            }
        }
        public override void Write(string s)
        {
            foreach(var w in Writers)
            {
                try
                {
                    w.Write(s);
                    if (AutoFlush)
                        w.Flush();
                }
                catch(ObjectDisposedException)
                {
                    Info.Log("Attempted to write to disposed writer. Removing.");
                    Writers.Remove(w);
                }
            }
        }
        public override void WriteLine(string s)
        {
            foreach(var w in Writers)
            {
                try
                {
                    w.WriteLine(s);
                    if (AutoFlush)
                        w.Flush();
                }
                catch(ObjectDisposedException)
                {
                    Info.Log("Attempted to write to disposed writer. Removing.");
                    Writers.Remove(w);
                }
            }
        }


        public override void Flush()
        {
            foreach (var w in Writers)
            {
                w.Flush();
            }
        }
        List<TextWriter> Writers;

        public override Encoding Encoding => Encoding.UTF8;
    }
}
