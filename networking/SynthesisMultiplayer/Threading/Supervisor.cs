using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Threading
{

    class Supervisor : ISupervisor
    {
        public Supervisor()
        {

        }

        Dictionary<string, (IManagedTask, Task)> Children;

        public (IManagedTask, Task) GetChild(string name)
        {
            if(!Children.ContainsKey(name))
            {
                throw new Exception("No task named '" + name + "' was found.");
            }
            return Children[name];
        }

        public void RestartChild(string name)
        {
            var (taskObject, task) = GetChild(name);
            
        }

        public string SpawnChild(IManagedTask taskObject, string name = "")
        {
            var task = ManagedTaskHelper.Run(taskObject, new TaskContextBase());
            if (name == "")
            {
                var guid = new Guid();
                name = guid.ToString();
            }
            Children[name] = (taskObject, task);
            return name;
        }
    }
}
