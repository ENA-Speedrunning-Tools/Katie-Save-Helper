using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using KatieSaveHelper.Features.Util;
using JoelG.ENA4;

namespace KatieSaveHelper.Features.API
{
    [CustomSaveData("genericData")]
    public class KatieSaveData
    {
        public List<string> achievementList = new List<string>();
    }

    public static class CustomDataHandler
    {
        private static readonly ConditionalWeakTable<object, CustomDataContainer> Data = new ConditionalWeakTable<object, CustomDataContainer>();

        public static CustomDataContainer GetCustomDataContainer(this object obj)
        {
            return Data.GetOrCreateValue(obj);
        }

        public static T GetOrCreateCustomData<T>(this object obj) where T : class, new()
        {
            var type = typeof(T);

            if (!CustomSaveDataTypeRegistry.IsValidType(type))
                throw new InvalidOperationException($"Type {type.FullName} is not registered in CustomSaveDataTypeRegistry");

            var container = obj.GetCustomDataContainer();

            if (!container.Data.TryGetValue(typeof(T), out var value))
            {
                value = new T();
                container.Data[typeof(T)] = value;
            }

            return (T)value;
        }

        public static bool CopyCustomDataTo(this object source, object target)
        {
            if (!source.HasCustomData()) return false;

            var serializable = source.GetCustomDataContainer().ToSerializable();

            target.GetCustomDataContainer().FromSerializable(serializable);

            return true;
        }

        public static bool HasCustomData(this object obj)
        {
            return obj.GetCustomDataContainer().Data.Any();
        }

        public static bool HasCustomData<T>(this object obj)
        {
            return obj.GetCustomDataContainer().Data.ContainsKey(typeof(T));
        }

        public static bool HasCustomData(this object obj, Type type)
        {
            return obj.GetCustomDataContainer().Data.ContainsKey(type.GetType());
        }
    }

    public class CustomDataContainer
    {
        public Dictionary<Type, object> Data = new Dictionary<Type, object>();

        public JObject ToSerializable()
        {
            var root = new Dictionary<string, Dictionary<string, object>>();

            foreach (var kvp in Data)
            {
                var type = kvp.Key;

                var modGuid = KatieUtil.GetModName(type);

                var attr = type.GetCustomAttribute<CustomSaveDataAttribute>();
                if (attr == null)
                    continue;

                string container = attr.Id ?? type.Name;

                if (!root.TryGetValue(modGuid, out var modDict))
                {
                    modDict = new Dictionary<string, object>();
                    root[modGuid] = modDict;
                }

                modDict[container] = kvp.Value;
            }

            return JObject.FromObject(root);
        }

        public bool FromSerializable(JObject obj)
        {
            Data.Clear();

            if (obj == null || !obj.HasValues)
                return false;

            foreach (var modProp in obj.Properties())
            {
                string modName = modProp.Name;

                JObject modObj = modProp.Value as JObject;
                if (modObj == null)
                    continue;

                foreach (var containerProp in modObj.Properties())
                {
                    var type = CustomSaveDataTypeRegistry.GetType(modName, containerProp.Name);
                    if (type == null)
                    {
                        KatieLogger.Warning($"Could not find type matching '{containerProp.Name}' in the CustomSaveDataTypeRegistry");
                        continue;
                    }

                    var value = containerProp.Value.ToObject(type);
                    Data[type] = value;
                }
            }

            return Data.Any();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomSaveDataAttribute : Attribute
    {
        public string Id { get; }

        public CustomSaveDataAttribute(string id = null)
        {
            Id = id;
        }
    }

    public static class CustomSaveDataTypeRegistry
    {
        private static readonly Dictionary<string, Dictionary<string, Type>> registry = new Dictionary<string, Dictionary<string, Type>>();

        private static bool built;

        public static void Build()
        {
            if (built)
                return;

            built = true;

            registry.Clear();

            foreach (var type in GetAllTypesSafe())
            {
                var attr = type.GetCustomAttribute<CustomSaveDataAttribute>();
                if (attr == null)
                    continue;

                string modGuid = KatieUtil.GetModName(type);
                string container = attr.Id ?? type.Name;

                if (!registry.TryGetValue(modGuid, out var modDict))
                {
                    modDict = new Dictionary<string, Type>();
                    registry[modGuid] = modDict;
                }

                if (modDict.ContainsKey(container))
                {
                    KatieLogger.Error($"Type '{type.FullName}' marked with CustomSaveData attribute in mod '{modGuid}' has a duplicate container identifier: '{container}'");
                    KatieLogger.Error("The CustomSaveData attribute on this type will be ignored.");
                    continue;
                }

                modDict[container] = type;
            }
        }

        public static Type GetType(string modGuid, string container)
        {
            if (!built)
                Build();

            return registry.TryGetValue(modGuid, out var modDict) && modDict.TryGetValue(container, out var type) ? type : null;
        }

        public static IEnumerable<Type> GetAllTypesSafe()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (var t in types)
                    yield return t;
            }
        }

        public static bool IsValidType(Type type)
        {
            if (!built)
                Build();

            foreach (var mod in registry.Values)
                if (mod.ContainsValue(type))
                    return true;

            return false;
        }
    }
}
