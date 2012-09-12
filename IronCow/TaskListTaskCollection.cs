using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using IronCow.Rest;

namespace IronCow
{
    public class TaskListTaskCollection : ICollection<Task>, INotifyCollectionChanged
    {
        #region Private Members
        private TaskList mList;
        private List<Task> mImpl;
        private object mSyncRoot;

        private bool IsListSmart { get { return mList.GetFlag(TaskListFlags.Smart); } }

        private bool Syncing { get { return mList.Syncing; } }
        private bool IsSynced { get { return mList.IsSynced; } }
        #endregion

        #region Internal Properties
        internal Rtm Owner { get { return mList.Owner; } }

        internal object SyncRoot { get { return mSyncRoot; } }
        #endregion

        #region Public Properties
        public Task this[int index]
        {
            get
            {
                lock (mSyncRoot)
                {
                    return mImpl[index];
                }
            }
        }
        #endregion

        #region Construction
        internal TaskListTaskCollection(TaskList list)
        {
            mList = list;
            mImpl = new List<Task>();
            mSyncRoot = new object();
        }
        #endregion

        #region Public Methods
        public void Resync(SyncCallback callback)
        {
            if (Syncing)
            {
                /*
                Owner.GetTasks(mList.Id, (tasks) =>
                {
                    mImpl.Clear();
                    mImpl.AddRange(tasks);
                    Sort();
                    callback();
                });*/
                
                var request = new RestRequest("rtm.tasks.getList");
                request.Parameters.Add("list_id", mList.Id.ToString());
                request.Parameters.Add("filter", "status:Incomplete");
                request.Callback = r =>
                    {
                        mImpl.Clear();

                        if (r.Tasks != null)
                        {
                            InternalSync(r.Tasks);
                        }

                        Sort();

                        callback();
                    };
                Owner.ExecuteRequest(request);
                
            }
        }

        public void SmartResync(SyncCallback callback)
        {
            if (!mList.GetFlag(TaskListFlags.Smart))
                throw new InvalidOperationException("This list isn't a smart list and cannot use SmartResync: " + mList.Name);

            /*
            Owner.GetTasks(mList.Filter, (tasks) => 
            {
                mImpl.Clear();
                mImpl.AddRange(tasks);
                Sort();

                callback();
            }); */

            mImpl.Clear();
            mImpl.AddRange(Owner.SearchTasksLocally(mList.Filter));
            Sort();
            callback();
        }

        internal void InternalSync(RawList[] lists)
        {
            mImpl.Clear();

            foreach (var list in lists)
            {
                if (list == null || list.TaskSeries == null)
                    continue;

                foreach (var series in list.TaskSeries)
                {
                    if (series.Tasks == null)
                        continue;

                    foreach (var task in series.Tasks)
                    {
                        if (IsListSmart)
                        {
                            // Get the task from the list it really belongs to.
                            TaskList parentList = Owner.TaskLists.GetById(list.Id);
                            if (parentList == null)
                                throw new IronCowException(string.Format("Internal Error: Can't find task list with id '{0}'.", list.Id));
                            Task actualTask = parentList.Tasks.GetById(series.Id, task.Id);
                            if (actualTask == null)
                                throw new IronCowException(string.Format("Internal Error: Can't find task with id '{0}' in list '{1}'.", task.Id, list.Id));
                            mImpl.Add(actualTask);
                        }
                        else
                        {
                            // Create the task and add them to this list.
                            if (list.Id != mList.Id)
                                throw new IronCowException(string.Format("Internal Error: Expected tasks for list '{0}' but got list '{1}'.", mList.Id, list.Id));
                            Task newTask = new Task();
                            newTask.SetParentInternal(mList);   // Set the parent now so that the task's Rtm owner is set for the sync below.
                            newTask.Sync(new TaskBundle(list, series, task, TaskSyncMode.Download));
                            mImpl.Add(newTask);
                        }
                    }
                }
            }
        }

        public Task GetById(string seriesId, string taskId)
        {
            return GetById(seriesId, taskId, false);
        }

        public Task GetById(string seriesId, string taskId, bool throwIfNotFound)
        {
            foreach (var item in this)
            {
                if (item.SeriesId == seriesId && item.Id == taskId)
                    return item;
            }
            if (throwIfNotFound)
                throw new IronCowException(string.Format("No task with such ids: '{0}', '{1}'.", seriesId, taskId));
            return null;
        }
        #endregion

        #region Internal Methods
        internal void UnsyncedAdd(Task task)
        {
            mImpl.Add(task);
        }

        internal void UnsyncedRemove(Task task)
        {
            mImpl.Remove(task);
        }
        #endregion

        #region Private Methods
        private void ExecuteAddTaskRequest(Task item)
        {
            RestRequest request = new RestRequest("rtm.tasks.add", r =>
            {
                item.Sync(new TaskBundle(r.List, 0, 0, TaskSyncMode.Upload));
            });
            request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
            request.Parameters.Add("list_id", mList.Id.ToString());
            request.Parameters.Add("name", item.Name);
            request.Parameters.Add("parse", "1");
            Owner.ExecuteRequest(request);
        }

        private void ExecuteMoveTaskRequest(Task item, string fromListId, string toListId)
        {
            RestRequest request = new RestRequest("rtm.tasks.moveTo");
            request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
            request.Parameters.Add("from_list_id", fromListId.ToString());
            request.Parameters.Add("to_list_id", toListId.ToString());
            request.Parameters.Add("taskseries_id", item.SeriesId.ToString());
            request.Parameters.Add("task_id", item.Id.ToString());
            Owner.ExecuteRequest(request);
        }
        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, e);
        }

        #endregion

        #region ICollection<Task> Members

        public void AddNoSync(Task item)
        {
            mImpl.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, mImpl.Count - 1));
        }

        public bool RemoveNoSync(Task item)
        {
            int index = mImpl.IndexOf(item);
            bool result = mImpl.Remove(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));

            return result;
        }

        public void Add(Task item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            lock (mSyncRoot)
            {
                if (Syncing)
                {
                    if (!IsSynced)
                        throw new InvalidOperationException("Can't add items to a list that isn't synced.");

                    if (IsListSmart)
                    {
                        if (item.IsSynced)
                            throw new InvalidOperationException("Can't add a synced (not brand new) task to a smart-list. Change its properties so that it falls into the smart-list's filter instead.");

                        // This is a special case... the user is adding a task to a smart list. That task
                        // may or may not have the smart list's filter criteria, so it may never be added to
                        // this collection (on the RTM website, this translates to the task "disappearing"
                        // immediately).
                        //
                        // Let's send the request, get the task to sync, and add it silently to its real
                        // parent list.
                        RestRequest request = new RestRequest("rtm.tasks.add", r =>
                        {
                            TaskList realParent = Owner.TaskLists.GetById(r.List.Id);
                            if (realParent == null)
                                throw new IronCowException(string.Format("Can't find task list with id '{0}'.", r.List.Id));

                            item.Sync(new TaskBundle(r.List, 0, 0, TaskSyncMode.Upload));

                            if (!realParent.IsSynced)
                                throw new IronCowException(string.Format("Expected task list '{0}' to be synced with the RTM server.", realParent.Name));

                            item.SetParentInternal(realParent);
                            realParent.Tasks.UnsyncedAdd(item);
                        });
                        request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
                        request.Parameters.Add("list_id", mList.Id.ToString());
                        request.Parameters.Add("name", item.Name);
                        request.Parameters.Add("parse", "1");
                        Owner.ExecuteRequest(request);

                        // Since we don't know whether the task will be found through our smart list filter,
                        // we don't know if the item has been added. Better just notify that something may
                        // have changed, and let us be resynced on next access (if we're not frozen).
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        // That's all.
                        return;
                    }

                    if (item.Parent != null)
                    {
                        if (!item.Parent.IsSynced && !item.IsSynced)
                        {
                            // Moving a non-synced task from a non-synced list... just move the task as
                            // if it was new.
                            item.Parent.Tasks.UnsyncedRemove(item);
                            item.SetParentInternal(mList);  // we need to set this now because the ExecuteAddTaskRequest is going to
                                                            // make the task upload all its stuff to the server, and it needs an owner
                                                            // and parent list for that.
                            ExecuteAddTaskRequest(item);
                        }
                        else if (item.Parent.IsSynced && item.IsSynced)
                        {
                            // Move the task here from another synced list.
                            string fromListId = item.Parent.Id;
                            item.Parent.Tasks.UnsyncedRemove(item);
                            item.SetParentInternal(mList);
                            ExecuteMoveTaskRequest(item, fromListId, mList.Id);
                        }
                        else
                        {
                            // Weird mismatched case.
                            throw new InvalidOperationException("You seem to be moving a synced task from an unsynced list, or an unsynced task from a sync list. This is a weird situation that shouldn't have been possible.");
                        }
                    }
                    else
                    {
                        if (item.IsSynced)
                            throw new ArgumentException("The task isn't in a list, but is somehow synced. This situation shouldn't be possible.");

                        // The task is new, and is added for the first time.
                        item.SetParentInternal(mList);  // we need to set this now because the ExecuteAddTaskRequest is going to
                                                        // make the task upload all its stuff to the server, and it needs an owner
                                                        // and parent list for that.
                        ExecuteAddTaskRequest(item);
                    }
                }
                else
                {
                    if (item.Parent != null)
                        item.Parent.Tasks.UnsyncedRemove(item);
                    item.SetParentInternal(mList);
                }
                // Actually add this task to our internal collection!
                mImpl.Add(item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, mImpl.Count - 1));
            }
        }

        public void Clear()
        {
            lock (mSyncRoot)
            {
                if (Syncing)
                {
                    if (IsListSmart)
                        throw new InvalidOperationException("Tasks can't be removed from a smart list.");

                    // Move all the tasks to inbox.
                    TaskList inbox = Owner.GetInbox();
                    if (inbox == mList)
                        throw new InvalidOperationException("Can't clear the 'Inbox' task list.");

                    foreach (var item in this)
                    {
                        inbox.Tasks.Add(item);
                    }

                    if (this.Count > 0)
                        throw new IronCowException("The list should now be empty - all tasks moved to inbox.");
                }
                else
                {
                    foreach (var item in this)
                    {
                        if (item != null)
                            item.SetParentInternal(null);
                    }
                }
                // Actually clear our internal collection.
                mImpl.Clear();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public bool Contains(Task item)
        {
            lock (mSyncRoot)
            {
                return mImpl.Contains(item);
            }
        }

        public void CopyTo(Task[] array, int arrayIndex)
        {
            lock (mSyncRoot)
            {
                mImpl.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (mSyncRoot)
                {
                    return mImpl.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Task item)
        {
            lock (mSyncRoot)
            {
                // Actually remove the task from our internal collection.
                int index = mImpl.IndexOf(item);
                bool result = mImpl.Remove(item);

                if (result)
                {
                    if (Syncing)
                    {
                        if (IsListSmart)
                            throw new InvalidOperationException("Can't manually remove a task from a smart list.");

                        // Move the task to inbox.
                        TaskList inbox = Owner.GetInbox();
                        if (inbox == mList)
                            throw new InvalidOperationException("Can't remove tasks the 'Inbox' task list. Either delete the task, or add it to another list.");

                        inbox.Tasks.Add(item);
                    }
                    else
                    {
                        item.SetParentInternal(null);
                    }
                }
                
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                return result;
            }
        }

        #endregion

        #region IEnumerable<Task> Members

        public IEnumerator<Task> GetEnumerator()
        {
            lock (mSyncRoot)
            {
                return mImpl.GetEnumerator();
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public void Sort()
        {
            switch (this.mList.SortOrder)
            {
                case TaskListSortOrder.Date:
                    mImpl.Sort(Task.CompareByDate);
                    break;
                case TaskListSortOrder.Priority:
                    mImpl.Sort(Task.CompareByPriority);
                    break;
                default:
                    mImpl.Sort(Task.CompareByName);
                    break;
            }
            
            // mImpl.Sort(Task.CompareByDate);
        }

        public void Sort(TaskListSortOrder sortOrder)
        {
            switch (sortOrder)
            {
                case TaskListSortOrder.Date:
                    mImpl.Sort(Task.CompareByDate);
                    break;
                case TaskListSortOrder.Priority:
                    mImpl.Sort(Task.CompareByPriority);
                    break;
                default:
                    mImpl.Sort(Task.CompareByName);
                    break;
            }

            // mImpl.Sort(Task.CompareByDate);
        }
    }
}
