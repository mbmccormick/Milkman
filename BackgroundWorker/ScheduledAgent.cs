using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Collections.ObjectModel;
using BackgroundWorker.Common;
using IronCow;
using System.ComponentModel;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System.Device.Location;
using Milkman.Common;

namespace BackgroundWorker
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;

        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is WebException)
            {

            }
            else
            {
                LittleWatson.ReportException(e.ExceptionObject, null);
            }

            e.Handled = true;
            
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        GeoCoordinateWatcher _watcher = null;

        protected override void OnInvoke(ScheduledTask task)
        {
            App.LoadData();

            AppSettings settings = new AppSettings();

            if (settings.LocationRemindersEnabled == true)
            {
                _watcher = new GeoCoordinateWatcher();
                _watcher.Start();
            }

            SyncData();
        }

        private void SyncData()
        {
            if (!string.IsNullOrEmpty(App.RtmClient.AuthToken))
            {
                App.RtmClient.SyncEverything(() =>
                {
                    LoadData();

                    if (System.Diagnostics.Debugger.IsAttached)
                        ScheduledActionService.LaunchForTest("BackgroundWorker", new TimeSpan(0, 0, 1, 0)); // every minute

                    App.SaveData();

                    NotifyComplete();
                });
            }
            else
            {
                NotificationsManager.ClearNotifications();
                
                if (System.Diagnostics.Debugger.IsAttached)
                    ScheduledActionService.LaunchForTest("BackgroundWorker", new TimeSpan(0, 0, 1, 0)); // every minute

                NotifyComplete();
            }
        }

        private void LoadData()
        {
            if (_watcher != null)
                NotificationsManager.SetupNotifications(_watcher.Position.Location);
            else
                NotificationsManager.SetupNotifications(null);
        }
    }
}