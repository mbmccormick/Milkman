using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.IsolatedStorage;

namespace Milkman.Background.Common
{
    public class AppSettings
    {
        IsolatedStorageSettings isolatedStore;

        const string AddTaskDialogEnabledSettingKeyName = "AddTaskDialogEnabled";
        const string IgnorePriorityEnabledSettingKeyName = "IgnorePriorityEnabled";
        const string LocationRemindersEnabledSettingKeyName = "LocationRemindersEnabled";
        const string NearbyRadiusSettingKeyName = "NearbyRadius";
        const string TaskRemindersEnabledSettingKeyName = "TaskRemindersEnabled";

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

        public bool AddTaskDialogEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(AddTaskDialogEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(AddTaskDialogEnabledSettingKeyName, value);
                Save();
            }
        }

        public bool IgnorePriorityEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(IgnorePriorityEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(IgnorePriorityEnabledSettingKeyName, value);
                Save();
            }
        }

        public bool LocationRemindersEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(LocationRemindersEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(LocationRemindersEnabledSettingKeyName, value);
                Save();
            }
        }

        public int NearbyRadius
        {
            get
            {
                return GetValueOrDefault<int>(NearbyRadiusSettingKeyName, 1);
            }
            set
            {
                AddOrUpdateValue(NearbyRadiusSettingKeyName, value);
                Save();
            }
        }

        public int TaskRemindersEnabled
        {
            get
            {
                return GetValueOrDefault<int>(TaskRemindersEnabledSettingKeyName, 1);
            }
            set
            {
                AddOrUpdateValue(TaskRemindersEnabledSettingKeyName, value);
                Save();
            }
        }
    }
}
