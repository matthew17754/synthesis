using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiplayer.Actor
{
    public class TaskContext : ITaskContext
    {
        bool disposedValue = false;

        public Dictionary<string, dynamic> Data { get; set; }

        public TaskContext()
        {
            Data = new Dictionary<string, dynamic>();
        }
        public dynamic GetObject(string key)
        {
            try
            {
                return GetType().GetProperty(key).GetValue(this, null);
            }
            catch (Exception)
            {
                if (Data.ContainsKey(key))
                {
                    return Data[key];
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
            catch(Exception)
            {
                Data[key] = value;
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

        public string[] Keys()
        {
            return Data.Keys.ToArray();
        }

        public bool ContainsKey(string key)
        {
            return Data.ContainsKey(key);
        }

        public void Merge(ITaskContext other)
        {
            foreach(var key in other.Keys())
            {
                PutObject(key, other.GetObject(key));    
            }
        }
    }
}
