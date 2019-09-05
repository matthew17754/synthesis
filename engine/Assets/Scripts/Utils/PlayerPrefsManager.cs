using System;
using System.Linq;
using UnityEngine;

namespace Synthesis.Utils
{
    // TODO - This is currently unused, but we should replace all uses of PlayerPrefs with it

    /// <summary>
    /// A more efficient and safe interface with PlayerPrefs
    /// 
    /// Accessed using constant values instead of strings, better type safety, and value-caching
    /// </summary>
    public static class PlayerPrefsManager
    {
        /// <summary>
        /// Accessor key for player prefs
        /// </summary>
        public enum Key
        {
            // Analytics
            AnalyticsGUID,
            AnalyticsEnabled,

            // Field
            FieldDirectory,
            ActiveFieldDirectory,
            ActiveFieldName,

            // Replay
            ActiveReplay,

            // Robot
            RobotDirectory,
            ActiveRobotName,

            // Settings
            FullScreen,
            Resolution,
            QualityLevel,
            MeasurementSystem,

            // Emulation
            UserProgramType,

            // Controls
            // TODO
            /*
            ControlsGlobal,
            ControlsPlayer1Arcade,
            ControlsPlayer1Mecanum,
            ControlsPlayer1Tank,
            ControlsPlayer2Arcade,
            ControlsPlayer2Mecanum,
            ControlsPlayer2Tank,
            ControlsPlayer3Arcade,
            ControlsPlayer3Mecanum,
            ControlsPlayer3Tank,
            ControlsPlayer4Arcade,
            ControlsPlayer4Mecanum,
            ControlsPlayer4Tank,
            ControlsPlayer5Arcade,
            ControlsPlayer5Mecanum,
            ControlsPlayer5Tank,
            ControlsPlayer6Arcade,
            ControlsPlayer6Mecanum,
            ControlsPlayer6Tank,
            */
        }

        private const string MetaPrefix = "Meta.";
        private const string MetaPrefixType = MetaPrefix + "Type.";

        /// <summary>
        /// Internal cache of all player pref values
        /// </summary>
        private static readonly dynamic[] CachedPlayerPrefs;

        static PlayerPrefsManager()
        {
            CachedPlayerPrefs = new dynamic[(int)Enum.GetValues(typeof(Key)).Cast<Key>().Max() + 1];

            TryLoadAll();
        }

        /// <summary>
        /// Check if key has a cached value
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if cache exists</returns>
        private static bool IsSet(Key key)
        {
            return CachedPlayerPrefs[(int)key] == null;
        }

        /// <summary>
        /// Create the key containing the type information for a player pref
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string CreateMetaTypeKey(Key key)
        {
            return MetaPrefixType + key.ToString();
        }

        /// <summary>
        /// Load a player pref into the cache
        /// </summary>
        /// <param name="key"></param>
        public static void Load(Key key)
        {
            if (!PlayerPrefs.HasKey(key.ToString()))
            {
                throw new Exception("Player pref \"" + key.ToString() + "\" missing");
            }

            string metaTypeKey = CreateMetaTypeKey(key);

            if (!PlayerPrefs.HasKey(metaTypeKey))
            {
                throw new Exception("Meta type missing for player pref \"" + key.ToString() + "\" (\"" + metaTypeKey + "\")");
            }

            string type = PlayerPrefs.GetString(metaTypeKey); // Type information saved to help determine which PlayerPrefs Get function to call

            switch (type)
            {
                case "string":
                    CachedPlayerPrefs[(int)key] = PlayerPrefs.GetString(key.ToString());
                    break;
                case "int":
                    CachedPlayerPrefs[(int)key] = PlayerPrefs.GetInt(key.ToString());
                    break;
                case "float":
                    CachedPlayerPrefs[(int)key] = PlayerPrefs.GetFloat(key.ToString());
                    break;
                default:
                    throw new Exception("Unsupported player pref type " + type + " for \"" + key.ToString() + "\"");
            }
        }

        /// <summary>
        /// Get the cached player pref value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static dynamic Get(Key key)
        {
            if (!IsSet(key))
            {
                try
                {
                    Load(key);
                }
                catch (Exception) { }
            }
            return CachedPlayerPrefs[(int)key];
        }

        /// <summary>
        /// Save a player pref
        /// </summary>
        /// <param name="key"></param>
        private static void Save(Key key)
        {
            if (!IsSet(key)) {
                throw new Exception("Saving unset player pref \"" + key.ToString() + "\"");
            }

            var value = CachedPlayerPrefs[(int)key];
            string metaTypeKey = CreateMetaTypeKey(key);

            if (value is string)
            {
                PlayerPrefs.SetString(key.ToString(), value);
                PlayerPrefs.SetString(metaTypeKey, "string"); // Save type information as well
            }
            else if (value is int)
            {
                PlayerPrefs.SetInt(key.ToString(), value);
                PlayerPrefs.SetString(metaTypeKey, "int"); // Save type information as well
            }
            else if (value is float)
            {
                PlayerPrefs.SetFloat(key.ToString(), value);
                PlayerPrefs.SetString(metaTypeKey, "float"); // Save type information as well
            }
            else
            {
                throw new Exception("Unsupported player pref type " + value.GetType().FullName + " for \"" + key.ToString() + "\"");
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Assign the player pref and save
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set(Key key, dynamic value)
        {
            if(!(value is string || value is int || value is float))
            {
                throw new Exception("Unsupported player pref type " + value.GetType().FullName +" for \"" + key.ToString() + "\"");
            }

            if (IsSet(key) && CachedPlayerPrefs[(int)key].GetType() != value.GetType())
            {
                throw new Exception("Reassigning type of player pref \"" + key.ToString() + "\" from " + CachedPlayerPrefs[(int)key].GetType().FullName + " to " + value.GetType().FullName);
            }

            CachedPlayerPrefs[(int)key] = value;

            Save(key);
        }

        /// <summary>
        /// Delete a given key and its meta information
        /// </summary>
        /// <param name="key"></param>
        public static void Delete(Key key)
        {
            PlayerPrefs.DeleteKey(key.ToString());
            PlayerPrefs.DeleteKey(CreateMetaTypeKey(key));
        }

        /// <summary>
        /// Delete all player prefs
        /// </summary>
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        /// <summary>
        /// Try to load all player prefs into cache
        /// </summary>
        public static void TryLoadAll()
        {
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                try
                {
                    Load(key);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Try to save all player prefs
        /// </summary>
        public static void TrySaveAll()
        {
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (IsSet(key))
                {
                    Save(key);
                }
            }
            PlayerPrefs.Save();
        }
    }
}
