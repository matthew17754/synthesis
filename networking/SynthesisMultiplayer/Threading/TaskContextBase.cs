using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{
    public class TaskContextBase : ITaskContext
    {
        bool disposedValue = false;
        Dictionary<string, dynamic> data;
        public dynamic GetObject(string key)
        {
            try
            {
                return GetType().GetProperty(key).GetValue(this, null);
            }
            catch (Exception e)
            {
                if (data.ContainsKey(key))
                {
                    return data[key];
                }
                throw new Exception("Failed to find key '"+key+"'");
            }
        }

        public void PutObject(string key, dynamic value)
        {
            try
            {
                GetType().GetProperty(key).SetValue(this, value, null);
            } 
            catch(Exception e)
            {
                data[key] = value;
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
