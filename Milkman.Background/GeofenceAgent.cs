using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Shell;
using Milkman.Background;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation.Geofencing;

namespace BackgroundTask
{
    public sealed class GeofenceAgent : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var geofenceMonitor = GeofenceMonitor.Current;

            var geoReports = geofenceMonitor.ReadReports();
            foreach (var geofenceStateChangeReport in geoReports)
            {
                var id = geofenceStateChangeReport.Geofence.Id;
                var newState = geofenceStateChangeReport.NewState;

                if (newState == GeofenceState.Entered &&
                    App.RtmClient.TaskLists != null)
                {
                    foreach (TaskList l in App.RtmClient.TaskLists)
                    {
                        if (l.IsSmart == false &&
                            l.Tasks != null)
                        {
                            foreach (Task t in l.Tasks)
                            {
                                if (t.HasDueTime &&
                                    t.DueDateTime.Value <= DateTime.Now)
                                {
                                    ShellToast toast = new ShellToast();

                                    toast.Title = t.Location.Name;
                                    toast.Content = t.Name;
                                    toast.NavigationUri = new Uri("/TaskDetailsPage.xaml?id=" + t.Id, UriKind.Relative);
                                    toast.Show();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
