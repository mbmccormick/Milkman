using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Collections.Generic;

namespace Milkman.Common
{
    public class AppSettings
    {
        IsolatedStorageSettings isolatedStore;

        const string AutomaticSyncEnabledSettingKeyName = "AutomaticSyncEnabled";
        const string LocationServiceEnabledSettingKeyName = "LocationServiceEnabled";
        const string TaskRemindersEnabledSettingKeyName = "TaskRemindersEnabled";
        const string LightThemeEnabledSettingKeyName = "LightThemeEnabled";

        public AppSettings()
        {
            try
            {
                // Get the settings for this application.
                isolatedStore = IsolatedStorageSettings.ApplicationSettings;

            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while using IsolatedStorageSettings: " + e.ToString());
            }
        }

        private bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            try
            {
                // if new value is different, set the new value.
                if (isolatedStore[Key] != value)
                {
                    isolatedStore[Key] = value;
                    valueChanged = true;
                }
            }
            catch (KeyNotFoundException)
            {
                isolatedStore.Add(Key, value);
                valueChanged = true;
            }
            catch (ArgumentException)
            {
                isolatedStore.Add(Key, value);
                valueChanged = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while using IsolatedStorageSettings: " + e.ToString());
            }

            return valueChanged;
        }

        private valueType GetValueOrDefault<valueType>(string Key, valueType defaultValue)
        {
            valueType value;

            try
            {
                value = (valueType)isolatedStore[Key];
            }
            catch (KeyNotFoundException)
            {
                value = defaultValue;
            }
            catch (ArgumentException)
            {
                value = defaultValue;
            }

            return value;
        }

        public void Save()
        {
            isolatedStore.Save();
        }

        public bool AutomaticSyncEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(AutomaticSyncEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(AutomaticSyncEnabledSettingKeyName, value);
                Save();
            }
        }

        public bool LocationServiceEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(LocationServiceEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(LocationServiceEnabledSettingKeyName, value);
                Save();
            }
        }

        public bool TaskRemindersEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(TaskRemindersEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(TaskRemindersEnabledSettingKeyName, value);
                Save();
            }
        }

        public bool LightThemeEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(LightThemeEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(LightThemeEnabledSettingKeyName, value);
                Save();
            }
        }
    }
}
