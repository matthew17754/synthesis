using SynthesisMultiplayer.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StateData = System.Collections.Generic.Dictionary<string, dynamic>;
namespace SynthesisMultiplayer.Util
{
    public static class StateBackup
    {
        // TODO: Split into readable form
        public static StateData DumpState(object obj, StateData currentState = null) =>
            obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Cast<MemberInfo>()
                .Concat(obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.CanWrite && p.CanRead))
                .Where(f => f.GetCustomAttributes(typeof(SavedState), false).Length > 0)
                .Select(field =>
                    {
                        var stateInfo = (SavedState)field.GetCustomAttribute(typeof(SavedState));
                        var stateName = stateInfo.Name ?? field.DeclaringType.ToString() + "." + field.Name;
                        if (currentState != null && currentState.ContainsKey(stateName))
                            throw new Exception("Duplicate state entry '" + stateName + "'");
                        return (stateName, field.MemberType == MemberTypes.Field ?
                            (dynamic)((FieldInfo)field).GetValue(obj) : (dynamic)((PropertyInfo)field).GetValue(obj));
                    })
                .Concat((currentState ?? new StateData()).ToList().Select(kv => (kv.Key, kv.Value)))
                .ToDictionary(x => x.Item1, x => x.Item2);

        // TODO: Add checks to insure that correctly scoped data is restore
        // TODO: Add check to not set value of non SavedState fields
        public static void RestoreState(object obj, StateData oldState = null)
        {
            if (oldState == null)
                return;
            oldState.ToList().ForEach(kv =>
                {
                    var member = (
                        (MemberInfo)obj.GetType().GetField(kv.Key.Substring(kv.Key.LastIndexOf('.') + 1), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                        (MemberInfo)obj.GetType().GetProperty(kv.Key.Substring(kv.Key.LastIndexOf('.') + 1), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                    if (member.MemberType == MemberTypes.Field)
                        ((FieldInfo)member).SetValue(obj, kv.Value);
                    else
                        ((PropertyInfo)member).SetValue(obj, kv.Value);
                });
        }
    }
}