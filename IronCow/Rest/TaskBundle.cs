using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Rest
{
    internal enum TaskSyncMode
    {
        Download = 0,
        Upload = 1
    }

    internal class TaskBundle : RawRtmElement
    {
        public TaskSyncMode SyncMode { get; private set; }

        public RawList List { get; private set; }
        public RawTaskSeries Series { get; private set; }
        public RawTask Task { get; private set; }

        internal TaskBundle(RawList list, int seriesIndex, int taskIndex, TaskSyncMode mode)
        {
            List = list;
            Series = list.TaskSeries[seriesIndex];
            Task = Series.Tasks[taskIndex];
            SyncMode = mode;

            Id = Task.Id;
        }

        internal TaskBundle(RawList list, RawTaskSeries series, RawTask task, TaskSyncMode mode)
        {
            List = list;
            Series = series;
            Task = task;
            SyncMode = mode;

            Id = Task.Id;
        }
    }
}
