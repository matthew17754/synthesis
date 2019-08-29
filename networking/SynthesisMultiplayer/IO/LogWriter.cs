using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LoggerFunc = System.Action<string>;
namespace Multiplayer.IO
{
    public class LogWriter : TextWriter
    {
        public LogWriter(LoggerFunc loggerFunc)
        {
            LoggerFunc = loggerFunc;
            Buffer = new List<char>();
        }

        public override void Write(char c)
        {
            Buffer.Add(c);
        }
        public override void Write(string s)
        {
            Buffer.AddRange(s);
        }
        public override void WriteLine(string s)
        {
            Buffer.AddRange(s+"\n"); 
        }
        public override void Flush()
        {
            LoggerFunc(string.Concat(Buffer));
            Buffer.Clear();
        }

        public List<char> Buffer;
        public override Encoding Encoding => Encoding.UTF8;
        private readonly LoggerFunc LoggerFunc;
    }
}
