using System.Collections.Generic;
using System.Text;

namespace IronCow
{
    public class TaskTagCollection : SynchronizedTaskCollection<string>, IList<string>
    {
        #region Static Methods
        private static RestRequest CreateStandardRequest(Task task, string method)
        {
            RestRequest request = new RestRequest(method);
            request.Parameters.Add("timeline", task.Owner.GetTimeline().ToString());
            request.Parameters.Add("list_id", task.Parent.Id.ToString());
            request.Parameters.Add("taskseries_id", task.SeriesId.ToString());
            request.Parameters.Add("task_id", task.Id.ToString());
            return request;
        }

        private static RestRequest CreateSetTagsRequest(Task task, IEnumerable<string> tags)
        {
            StringBuilder tagsBuilder = new StringBuilder();
            foreach (string tag in tags)
            {
                if (tagsBuilder.Length > 0)
                    tagsBuilder.Append(",");
                tagsBuilder.Append(tag);
            }

            return CreateSetTagsRequest(task, tagsBuilder.ToString());
        }

        private static RestRequest CreateSetTagsRequest(Task task, string formattedTags)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.setTags");
            request.Parameters.Add("tags", formattedTags);
            return request;
        }

        private static RestRequest CreateAddTagsRequest(Task task, string formattedTags)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.addTags");
            request.Parameters.Add("tags", formattedTags);
            return request;
        }

        private static RestRequest CreateRemoveTagsRequest(Task task, string formattedTags)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.removeTags");
            request.Parameters.Add("tags", formattedTags);
            return request;
        }
        #endregion

        #region Construction
        internal TaskTagCollection(Task task)
            : base(task)
        {
        } 
        #endregion

        #region Public Convenience Methods
        public void SetTags(IEnumerable<string> tags)
        {
            using (new UnsyncedScope(this))
            {
                Clear();

                if (tags != null)
                {
                    foreach (string tag in tags)
                    {
                        Add(tag);
                    }
                }

                if (Task.IsSynced)
                    UploadTags();
            }
        }

        public void SetTagsNoSync(IEnumerable<string> tags)
        {
            Items.Clear();
            foreach (string t in tags) Items.Add(t);
        }

        public void AddRange(IEnumerable<string> tags)
        {
            using (new UnsyncedScope(this))
            {
                bool added = false;
                if (tags != null)
                {
                    foreach (string tag in tags)
                    {
                        added = true;
                        Add(tag);
                    }
                }

                if (added && Task.IsSynced)
                    UploadTags();
            }
        } 
        #endregion

        #region Internal and Private Methods
        internal void UploadTags()
        {
            RestRequest request = CreateSetTagsRequest(Task, this);
            Task.Owner.ExecuteRequest(request);
        }

        internal RestRequest GetFirstSyncRequest()
        {
            if (Count == 0)
                return null;

            return CreateSetTagsRequest(Task, this);
        }
        #endregion

        #region SynchronizedTaskCollection<string> Members
        protected override Request CreateClearItemsRequest()
        {
            return CreateSetTagsRequest(Task, string.Empty);
        }

        protected override Request CreateAddItemRequest(string item)
        {
            return CreateAddTagsRequest(Task, item);
        }

        protected override Request CreateRemoveItemRequest(string item)
        {
            return CreateRemoveTagsRequest(Task, item);
        }
        #endregion

        #region IList<string> Members
        public int IndexOf(string item)
        {
            lock (SyncRoot)
            {
                return Items.IndexOf(item);
            }
        }

        public void Insert(int index, string item)
        {
            lock (SyncRoot)
            {
                Items.Insert(index, item);
                UploadTags();
            }
        }

        public void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                Items.RemoveAt(index);
                UploadTags();
            }
        }

        public string this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return Items[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    Items[index] = value;
                    UploadTags();
                }
            }
        }

        #endregion
    }
}
