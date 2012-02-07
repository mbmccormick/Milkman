using System;
using IronCow.Rest;

namespace IronCow
{
    public class TaskListCollection : SynchronizedRtmCollection<TaskList>
    {
        public TaskList this[string listName]
        {
            get
            {
                foreach (var item in this)
                {
                    if (item.Name == listName)
                        return item;
                }
                throw new IronCowException(string.Format("No task list with such name: '{0}'.", listName));
            }
        }

        internal TaskListCollection(Rtm owner)
            : base(owner)
        {
        }

        protected override void DoResync(SyncCallback callback)
        {
            Clear();
            var request = new RestRequest("rtm.lists.getList");
            request.Callback = response =>
            {
                SyncFromResponse(response);

                callback();
            };
            Owner.ExecuteRequest(request);
        }

        public void SyncFromResponse(Response response)
        {
            if (response.Lists != null)
            {
                using (new UnsyncedScope(this))
                {
                    foreach (var list in response.Lists)
                    {
                        if (list.Archived == 0 && list.Deleted == 0)
                        {
                            TaskList newList = new TaskList(list);
                            Add(newList);
                        }
                    }

                    Sort();
                }
            }
        }

        protected override void ExecuteAddElementRequest(TaskList item, SyncCallback callback)
        {
            if (string.IsNullOrEmpty(item.Name))
                throw new ArgumentException("The task list has a null or empty name.");

            RestRequest request = new RestRequest("rtm.lists.add", r => item.Sync(r.List));
            request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
            request.Parameters.Add("name", item.Name);
            if (!string.IsNullOrEmpty(item.Filter))
            {
                request.Parameters.Add("filter", item.Filter);
            }
            request.Callback = r => { Sort(); callback(); };
            Owner.ExecuteRequest(request);
        }

        protected override void ExecuteRemoveElementRequest(TaskList item, SyncCallback callback)
        {
            if (item.IsSynced)
            {
                RestRequest request = new RestRequest("rtm.lists.delete", r => item.Sync(r.List));
                request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
                request.Parameters.Add("list_id", item.Id.ToString());
                request.Callback = r => { Sort(); callback(); };
                Owner.ExecuteRequest(request);
            }
        }

        public void Sort()
        {
            Items.Sort();
        }
    }
}
