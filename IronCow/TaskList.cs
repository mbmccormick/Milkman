using System;
using System.ComponentModel;
using IronCow.Rest;
using IronCow.Resources;

namespace IronCow
{
    public enum TaskListSortOrder
    {
        Date = 1,
        Priority = 0,
        Name = 2
    }

    public class TaskList : RtmFatElement, INotifyPropertyChanged, IComparable
    {
        #region Callback Delegates

        public delegate void VoidCallback();

        #endregion

        #region Public Properties
        public int Position { get; private set; }
        public string Filter { get; private set; }
        public TaskListFlags Flags { get; private set; }

        public TaskListSortOrder SortOrder { get; private set; }

        public bool IsSmart
        {
            get
            {
                return GetFlag(TaskListFlags.Smart);
            }
        }

        public bool IsNormal
        {
            get
            {
                return !GetFlag(TaskListFlags.Smart);
            }
        }

        private bool mIsFrozen = false;
        public bool IsFrozen
        {
            get { return mIsFrozen; }
            set
            {
                if (!GetFlag(TaskListFlags.Smart))
                    throw new InvalidOperationException("Only smart lists can be frozen.");
                mIsFrozen = true;
            }
        }

        private string mName;
        public string Name
        {
            get { return mName; }
            set
            {
                if (mName != value)
                {
                    mName = value;

                    if (Syncing && IsSynced)
                    {
                        RestRequest request = new RestRequest("rtm.lists.setName");
                        request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
                        request.Parameters.Add("list_id", Id.ToString());
                        request.Parameters.Add("name", value);
                        Owner.ExecuteRequest(request);
                    }

                    OnPropertyChanged("Name");
                }
            }
        }

        public string NameLower
        {
            get
            {
                if (Name != null)
                    return Name.ToLower();
                else
                    return null;
            }
        }

        private TaskListTaskCollection mTasks;
        public TaskListTaskCollection Tasks
        {
            get
            {
                return mTasks;
            }

            set
            {
                mTasks = value;
                OnPropertyChanged("Count");
            }
        }

        public int Count
        {
            get { return mTasks == null ? 0 : mTasks.Count; }
        }

        public int ActiveCount
        {
            get
            {
                int count = 0;
                if (mTasks != null)
                {
                    foreach (Task t in mTasks)
                    {
                        if (t.IsCompleted == true ||
                            t.IsDeleted == true) continue;

                        count++;
                    }
                }

                return count;
            }
        }

        public string CountString
        {
            get
            {
                if (ActiveCount == 0)
                    return Strings.NoTasks;
                else if (ActiveCount == 1)
                    return ActiveCount + " " + Strings.TaskSingle;
                else
                    return ActiveCount + " " + Strings.TaskPlural;
            }
        }

        #endregion

        #region Construction
        public TaskList()
        {
        }

        public TaskList(string name)
        {
            mName = name;
        }

        internal TaskList(RawList list)
        {
            Sync(list);
        }
        #endregion

        #region Public Methods
        public void Archive(VoidCallback callback)
        {
            if (Syncing)
            {
                if (!GetFlag(TaskListFlags.Archived))
                {
                    RestRequest request = new RestRequest("rtm.lists.archive");
                    request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
                    request.Parameters.Add("list_id", Id.ToString());
                    request.Callback = (response) => { callback(); };
                    Owner.ExecuteRequest(request);
                }
            }

            SetFlag(TaskListFlags.Archived, true);
        }

        public void Unarchive(VoidCallback callback)
        {
            if (Syncing)
            {
                if (GetFlag(TaskListFlags.Archived))
                {
                    RestRequest request = new RestRequest("rtm.lists.unarchive");
                    request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
                    request.Parameters.Add("list_id", Id.ToString());
                    request.Callback = (response) => { callback(); };
                    Owner.ExecuteRequest(request);

                    SetFlag(TaskListFlags.Archived, false);
                }
            }

            SetFlag(TaskListFlags.Archived, false);
        }

        public void AddTask(Task task, VoidCallback callback)
        {
            // Freeze this task list so that we don't needlessly resync a smart-list
            // while adding a task to it.
            bool previousFreezeState = mIsFrozen;
            mIsFrozen = true;
            try
            {
                Tasks.Add(task);
            }
            finally
            {
                mIsFrozen = previousFreezeState;
            }
        }

        public bool GetFlag(TaskListFlags flag)
        {
            return (Flags & flag) == flag;
        }
        #endregion

        #region Syncing
        internal void InternalSync(RawList rawList)
        {
            lock (this)
            {
                if (mTasks == null)
                {
                    TaskListTaskCollection tmp = new TaskListTaskCollection(this);
                    tmp.InternalSync(new RawList[] { rawList });
                    System.Threading.Interlocked.Exchange(ref mTasks, tmp);
                }
                else
                {
                    mTasks.InternalSync(new RawList[] { rawList });
                }

                mTasks.Sort();
                OnPropertyChanged("Tasks");
                OnPropertyChanged("Count");
            }
        }

        protected override void DoSync(bool firstSync, RawRtmElement element)
        {
            base.DoSync(firstSync, element);

            RawList list = (RawList)element;
            mName = list.Name;
            Filter = list.Filter;
            Position = list.Position;
            SortOrder = (TaskListSortOrder)list.SortOrder;
            SetFlag(TaskListFlags.Archived, list.Archived == 1);
            SetFlag(TaskListFlags.Deleted, list.Deleted == 1);
            SetFlag(TaskListFlags.Locked, list.Locked == 1);
            SetFlag(TaskListFlags.Smart, list.Smart == 1);
        }

        public void SyncTasks(SyncCallback callback)
        {
            TaskListTaskCollection tmp = new TaskListTaskCollection(this);

            if (!GetFlag(TaskListFlags.Smart))
            {
                tmp.Resync(() =>
                {
                    mTasks = tmp;
                    OnPropertyChanged("Tasks");
                    OnPropertyChanged("Count");
                    callback();
                });
            }
            else if (!IsFrozen && GetFlag(TaskListFlags.Smart))
            {
                // Resync all the time for smart lists...
                //TODO: maybe use cache like normal lists, but for only a short time?
                tmp.SmartResync(() =>
                {
                    mTasks = tmp;
                    OnPropertyChanged("Tasks");
                    OnPropertyChanged("Count");
                    callback();
                });
            }
        }

        #endregion

        #region Private Methods
        private void SetFlag(TaskListFlags flag, bool value)
        {
            if (value)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }
        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(this, propertyName, PropertyChanged);
        }

        #endregion

        public int CompareTo(object obj)
        {
            if (obj is TaskList)
            {
                TaskList other = obj as TaskList;

                if (this.Name == "Inbox") return -1;
                if (other.Name == "Inbox") return 1;
                if (this.Name == "Sent") return 1;
                if (other.Name == "Sent") return -1;

                if (this.IsSmart != other.IsSmart)
                {
                    if (this.IsSmart) return 1;
                    else return -1;
                }
                else
                {
                    return this.Name.CompareTo(other.Name);
                }
            }
            else
            {
                throw new ArgumentException("Cannot compare TaskList to other types of objects.");
            }
        }
    }
}
