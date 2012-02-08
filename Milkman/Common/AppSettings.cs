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

        const string BackgroundWorkerEnabledSettingKeyName = "BackgroundWorkerEnabled";
        const string LocationNotificationsEnabledSettingKeyName = "LocationNotificationsEnabled";
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

        public bool BackgroundWorkerEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(BackgroundWorkerEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(BackgroundWorkerEnabledSettingKeyName, value);
                Save();
            }
        }

        public bool LocationNotificationsEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(LocationNotificationsEnabledSettingKeyName, true);
            }
            set
            {
                AddOrUpdateValue(LocationNotificationsEnabledSettingKeyName, value);
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
    }
}
