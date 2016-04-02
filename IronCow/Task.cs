using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using IronCow.Rest;
using IronCow.Resources;
using System.Windows;

namespace IronCow
{
    public class Task : RtmFatElement, INotifyPropertyChanged, IComparable, IComparable<Task>
    {
        #region Callback Delegates

        public delegate void VoidCallback();

        #endregion

        #region Sync Requests
        private static RestRequest CreateStandardRequest(Task task, string method, VoidCallback callback)
        {
            RestRequest request = new RestRequest(method);
            request.Parameters.Add("timeline", task.Owner.CurrentTimeline.ToString());
            request.Parameters.Add("list_id", task.Parent.Id.ToString());
            request.Parameters.Add("taskseries_id", task.SeriesId.ToString());
            request.Parameters.Add("task_id", task.Id.ToString());
            request.Callback = (response) => { callback(); };
            return request;
        }

        private static RestRequest CreateSetUrlRequest(Task task, string url, VoidCallback callback)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.setURL", callback);
            if (url != null)
                request.Parameters.Add("url", url);
            return request;
        }

        private static RestRequest CreateSetLocationRequest(Task task, string locationId, VoidCallback callback)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.setLocation", callback);
            if (locationId != null && locationId != RtmElement.UnsyncedId)
                request.Parameters.Add("location_id", locationId);
            return request;
        }

        private static RestRequest CreateSetDueDateRequest(Task task, bool hasTime, string due, VoidCallback callback)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.setDueDate", callback);
            if (due != null)
            {
                request.Parameters.Add("due", due);
                if (hasTime) request.Parameters.Add("has_due_time", "1");
                //request.Parameters.Add("parse", "1");
            }
            return request;
        }

        private static RestRequest CreateSetPriorityRequest(Task task, string priority, VoidCallback callback)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.setPriority", callback);
            if (priority != null)
                request.Parameters.Add("priority", priority);
            return request;
        }

        private static string TaskPriorityToPriorityRequestParameter(TaskPriority priority)
        {
            switch (priority)
            {
                case TaskPriority.One:
                    return "1";
                case TaskPriority.Two:
                    return "2";
                case TaskPriority.Three:
                    return "3";
                case TaskPriority.None:
                default:
                    return "";
            }
        }

        private static RestRequest CreateSetRecurrenceRequest(Task task, string recurrence, VoidCallback callback)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.setRecurrence", callback);
            if (recurrence != null)
                request.Parameters.Add("repeat", recurrence);
            return request;
        }

        private static RestRequest CreateSetEstimateRequest(Task task, string estimate, VoidCallback callback)
        {
            RestRequest request = CreateStandardRequest(task, "rtm.tasks.setEstimate", callback);
            if (estimate != null)
                request.Parameters.Add("estimate", estimate);
            return request;
        }
        #endregion

        #region Public Properties
        public string SeriesId { get; private set; }
        public string Source { get; private set; }
        public DateTime Created { get; private set; }
        public DateTime Added { get; private set; }
        public DateTime? Modified { get; private set; }
        public DateTime? Completed { get; private set; }
        public DateTime? Deleted { get; private set; }
        public bool HasDueTime { get; private set; }
        public bool IsLate
        {
            get
            {
                if (DueDateTime.HasValue)
                {
                    if (HasDueTime && DueDateTime.Value < DateTime.Now) return true;
                    if (!HasDueTime && DueDateTime.Value.Date < DateTime.Today) return true;
                }

                return false;
            }
        }
        public int Postponed { get; private set; }
        public TaskTagCollection Tags { get; private set; }
        public TaskTaskNoteCollection Notes { get; private set; }

        public bool IsCompleted { get { return Completed.HasValue; } }
        public bool IsIncomplete { get { return !Completed.HasValue; } }
        public bool IsDeleted { get { return Deleted.HasValue; } }

        public string TagsString
        {
            get
            {
                if (Tags != null)
                {
                    return string.Join(", ", Tags.ToArray());
                }
                return "";
            }
        }

        public bool HasTags
        {
            get
            {
                if (Tags != null && Tags.Count > 0)
                {
                    return true;
                }
                return false;
            }
        }

        public void ChangeTags(string[] tags, VoidCallback callback)
        {
            string[] currentTags = Tags.ToArray();
            // if tags are different
            if (!(tags.Length == currentTags.Length && tags.Intersect(currentTags).Count() == tags.Length))
            {
                RestRequest req = CreateStandardRequest(this, "rtm.tasks.setTags", () =>
                {
                    Tags.SetTagsNoSync(tags);
                    OnPropertyChanged("Tags");
                    OnPropertyChanged("HasTags");
                    OnPropertyChanged("TagsString");
                    callback();
                });

                req.Parameters.Add("tags", string.Join(",", tags));

                Owner.ExecuteRequest(req);
            }
            else
            {
                callback();
            }
        }

        private TaskList mParent;
        public TaskList Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != value)
                {
                    if (value != null)
                        value.Tasks.Add(this);
                    else
                        mParent.Tasks.Remove(this);
                }

                OnPropertyChanged("List");
            }
        }


        public string List
        {
            get
            {
                if (Parent != null)
                    return Parent.Name;
                else
                    return null;
            }
        }

        internal void SetParentInternal(TaskList parent)
        {
            mParent = parent;
            if (mParent == null)
                Owner = null;
            else
                Owner = mParent.Owner;
            OnPropertyChanged("Parent");
            OnPropertyChanged("List");
        }

        public void ChangeList(TaskList list, VoidCallback callback)
        {
            if (mParent != list)
            {
                RestRequest req = CreateStandardRequest(this, "rtm.tasks.moveTo", () =>
                {
                    Rtm.Dispatcher.BeginInvoke(() =>
                    {
                        mParent.Tasks.RemoveNoSync(this);
                        mParent = list;
                        if (Parent.Tasks != null)
                            Parent.Tasks.AddNoSync(this);
                        OnPropertyChanged("Parent");
                        OnPropertyChanged("List");
                    });
                    callback();
                });

                req.Parameters.Remove("list_id");
                req.Parameters.Add("from_list_id", Parent.Id.ToString());
                req.Parameters.Add("to_list_id", list.Id.ToString());

                Owner.ExecuteRequest(req);
            }
            else
            {
                callback();
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
                        RestRequest request = CreateStandardRequest(this, "rtm.tasks.setName", () => { });
                        request.Parameters.Add("name", value);
                        Owner.ExecuteRequest(request);
                    }

                    OnPropertyChanged("Name");
                    OnPropertyChanged("NameUpper");
                }
            }
        }
        public string NameUpper
        {
            get { return Name.ToUpper(); }
        }
        public void ChangeName(string name, VoidCallback callback)
        {
            if (name != mName)
            {
                RestRequest request = CreateStandardRequest(this, "rtm.tasks.setName", () =>
                {
                    mName = name;
                    OnPropertyChanged("Name");
                    OnPropertyChanged("NameUpper");

                    callback();
                });
                request.Parameters.Add("name", name);
                Owner.ExecuteRequest(request);
            }
            else
            {
                callback();
            }
        }

        private string mUrl;
        public string Url
        {
            get
            {
                if (HasUrl)
                {
                    if (mUrl.StartsWith("http:") || mUrl.StartsWith("https:") || mUrl.StartsWith("ftp:") || mUrl.StartsWith("file:"))
                        return mUrl;
                    else
                        return "http://" + mUrl;
                }
                else
                {
                    return "";
                }
            }
            set
            {
                if (mUrl != value)
                {
                    mUrl = value;

                    if (Syncing)
                    {
                        if (IsSynced)
                        {
                            RestRequest request = CreateSetUrlRequest(this, mUrl, () => { });
                            Owner.ExecuteRequest(request);
                        }
                    }

                    OnPropertyChanged("Url");
                    OnPropertyChanged("HasUrl");
                }
            }
        }
        public void ChangeUrl(string url, VoidCallback callback)
        {
            if (url != mUrl)
            {
                RestRequest request = CreateSetUrlRequest(this, url, () =>
                {
                    mUrl = url;
                    OnPropertyChanged("Url");
                    OnPropertyChanged("HasUrl");

                    callback();
                });
                Owner.ExecuteRequest(request);
            }
            else
            {
                callback();
            }
        }

        public bool HasUrl
        {
            get
            {
                return !string.IsNullOrEmpty(mUrl);
            }
        }

        private string mLocationId = RtmElement.UnsyncedId;
        private Location mLocation;
        public Location Location
        {
            get
            {
                if (Owner == null) return null;                
                if (Owner.Locations == null) return null;

                if (mLocation == null && mLocationId != RtmElement.UnsyncedId)
                {
                    if (!IsSynced)
                        throw new IronCowException("This task has a valid location ID but is not synced... impossible!");
                    foreach (var location in Owner.Locations)
                    {
                        if (location.Id == mLocationId)
                        {
                            mLocation = location;
                            break;
                        }
                    }
                    if (mLocation == null)
                        throw new IronCowException(string.Format("Can't find location with ID '{0}'.", mLocationId));
                }
                return mLocation;
            }
            set
            {
                if (mLocation != value)
                {
                    if (mLocation != null && !mLocation.IsSynced)
                    {
                        // The location was set to a new unsynced location, but is now changed
                        // to a new location before the first one had a chance to be synced. We
                        // therefore have to remove our location-first-sync handler.
                        mLocation.Synced -= new EventHandler(SetLocationWhenLocationFirstSynced);
                    }

                    mLocation = value;

                    if (Syncing)
                    {
                        if (IsSynced)
                        {
                            if (mLocation == null || mLocation.IsSynced)
                            {
                                mLocationId = mLocation == null ? RtmElement.UnsyncedId : mLocation.Id;
                                RestRequest request = CreateSetLocationRequest(this, mLocationId == RtmElement.UnsyncedId ? null : mLocationId.ToString(), () => { });
                                Owner.ExecuteRequest(request);
                            }
                            else
                            {
                                // The location is not synced, so we have to wait for it to be synced
                                // in order to have an ID for it. We'll listen to its Sync event.
                                mLocation.Synced += new EventHandler(SetLocationWhenLocationFirstSynced);
                            }
                        }
                    }

                    OnPropertyChanged("Location");
                    OnPropertyChanged("LocationName");
                }
            }
        }

        private void SetLocationWhenLocationFirstSynced(object sender, EventArgs e)
        {
            mLocationId = mLocation.Id;
            System.Diagnostics.Debug.Assert(mLocationId != RtmElement.UnsyncedId);
            mLocation.Synced -= new EventHandler(SetLocationWhenLocationFirstSynced);

            RestRequest request = CreateSetLocationRequest(this, mLocationId.ToString(), () => { });
            Owner.ExecuteRequest(request);
        }

        public string LocationName
        {
            get
            {
                if (Location == null)
                    return null;
                return Location.Name;
            }
        }

        public void ChangeLocation(Location location, VoidCallback callback)
        {
            if (location != mLocation)
            {
                string locationId = location == null ? RtmElement.UnsyncedId : location.Id;

                RestRequest request = CreateSetLocationRequest(this, locationId, () =>
                {
                    mLocation = location;
                    mLocationId = locationId;
                    OnPropertyChanged("Location");
                    OnPropertyChanged("LocationName");

                    callback();
                });
                Owner.ExecuteRequest(request);
            }
            else
            {
                callback();
            }
        }

        private string mDue;
        public string Due
        {
            get { return mDue; }
            set
            {
                if (mDue != value)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        HasDueTime = false;
                        mDueDateTime = null;
                        mDue = null;
                    }
                    else
                    {
                        var dateFormat = /*IsSynced ? Owner.UserSettings.DateFormat :*/ DateFormat.Default;
                        var timeFormat = /*IsSynced ? Owner.UserSettings.TimeFormat :*/ TimeFormat.Default;
                        var dateTime = DateConverter.ParseDateTime(value, dateFormat);

                        HasDueTime = dateTime.HasTime;
                        mDueDateTime = dateTime.DateTime;
                        mDue = DateConverter.FormatDateTime(dateTime, dateFormat, timeFormat);
                    }

                    if (Syncing)
                    {
                        if (IsSynced)
                        {
                            RestRequest request = CreateSetDueDateRequest(this, HasDueTime, mDue, () => { });
                            Owner.ExecuteRequest(request);
                        }
                    }

                    OnPropertyChanged("Due");
                    OnPropertyChanged("DueDateTime");
                    OnPropertyChanged("DueString");
                    OnPropertyChanged("IsLate");
                    OnPropertyChanged("HasDueTime");
                }
            }
        }
        public void ChangeDue(DateTime? due, bool hasTime, VoidCallback callback)
        {
            if (due != mDueDateTime)
            {
                RestRequest request = CreateSetDueDateRequest(this, hasTime,
                    due.HasValue ? due.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                    () =>
                    {
                        this.mDueDateTime = due;
                        this.HasDueTime = hasTime;
                        OnPropertyChanged("Due");
                        OnPropertyChanged("DueString");
                        OnPropertyChanged("DueDateTime");
                        OnPropertyChanged("IsLate");
                        OnPropertyChanged("HasDueTime");
                        callback();
                    });
                Owner.ExecuteRequest(request);
            }
            else
            {
                callback();
            }
        }

        public string DueString
        {
            get
            {
                string dueString = "";
                if (DueDateTime.HasValue)
                {
                    if (DueDateTime.Value.Date == DateTime.Today && !HasDueTime)
                    {
                        dueString += Strings.TodayLower;
                    }
                    else if (HasDueTime && DueDateTime.Value.Date == DateTime.Today)
                    {
                        dueString += DueDateTime.Value.ToString("t");
                    }
                    else if (DateTime.Today.AddDays(1) == DueDateTime.Value.Date)
                    {
                        dueString += Strings.TomorrowLower;
                    }
                    else if (DateTime.Today < DueDateTime.Value.Date && DateTime.Today.AddDays(6) >= DueDateTime.Value.Date)
                    {
                        dueString += DueDateTime.Value.ToString("dddd");
                    }
                    else
                    {
                        dueString += DueDateTime.Value.ToString("d MMM");
                    }


                }

                return dueString;
            }
        }

        public string LongDueDateString
        {
            get
            {
                string dueString = Strings.Never;

                if (DueDateTime.HasValue)
                {
                    if (this.HasDueTime)
                    {
                        dueString = DueDateTime.Value.ToString("f");
                    }
                    else
                    {
                        dueString = DueDateTime.Value.ToString("D");
                    }
                }

                return dueString;
            }
        }

        private DateTime? mDueDateTime;
        public DateTime? DueDateTime
        {
            get
            {
                return mDueDateTime;
            }
            set
            {
                if (value == null)
                {
                    Due = null;
                }
                else
                {
                    Due = value.Value.ToString("s");
                }
            }
        }

        public FuzzyDateTime FuzzyDueDateTime
        {
            get
            {
                if (!DueDateTime.HasValue)
                    throw new InvalidOperationException("There is no due date time on this task.");
                return new FuzzyDateTime(DueDateTime.Value, HasDueTime);
            }
            set
            {
                if (value.HasTime)
                    DueDateTime = value.DateTime;
                else
                    Due = value.DateTime.ToString("yyyy-MM-dd");
            }
        }

        private TaskPriority mPriority;
        public TaskPriority Priority
        {
            get { return mPriority; }
            set
            {
                if (mPriority != value)
                {
                    mPriority = value;

                    if (Syncing)
                    {
                        if (IsSynced)
                        {
                            RestRequest request = CreateSetPriorityRequest(this, TaskPriorityToPriorityRequestParameter(mPriority), () => { });
                            Owner.ExecuteRequest(request);
                        }
                    }

                    OnPropertyChanged("Priority");
                    OnPropertyChanged("Importance");
                    OnPropertyChanged("PriorityColor");
                }
            }
        }
        public void ChangePriority(TaskPriority priority, VoidCallback callback)
        {
            if (priority != mPriority)
            {
                RestRequest request = CreateSetPriorityRequest(this, TaskPriorityToPriorityRequestParameter(priority), () =>
                {
                    mPriority = priority;
                    OnPropertyChanged("Priority");
                    OnPropertyChanged("Importance");
                    OnPropertyChanged("PriorityColor");
                    callback();
                });
                Owner.ExecuteRequest(request);
            }
            else
            {
                callback();
            }
        }

        public int Importance
        {
            get
            {
                switch (Priority)
                {
                    case TaskPriority.One:
                        return 0;
                    case TaskPriority.Two:
                        return 1;
                    case TaskPriority.Three:
                        return 2;
                    case TaskPriority.None:
                    default:
                        return 3;
                }
            }
        }

        private string mRecurrence;
        public string Recurrence
        {
            get { return mRecurrence; }
            set
            {
                if (mRecurrence != value)
                {
                    mRecurrence = value;

                    if (Syncing)
                    {
                        if (IsSynced)
                        {
                            RestRequest request = CreateSetRecurrenceRequest(this, mRecurrence, () => { });
                            Owner.ExecuteRequest(request);
                        }
                    }

                    OnPropertyChanged("Recurrence");
                }
            }
        }
        public void ChangeRecurrence(string recurrence, VoidCallback callback)
        {
            if (recurrence != mRecurrence)
            {
                RestRequest request = CreateSetRecurrenceRequest(this, recurrence, () =>
                {
                    mRecurrence = recurrence;
                    OnPropertyChanged("Recurrence");
                    OnPropertyChanged("HasRecurrence");

                    callback();
                });
                Owner.ExecuteRequest(request);
            }
            else
            {
                callback();
            }
        }

        public bool HasRecurrence
        {
            get { return !string.IsNullOrEmpty(Recurrence); }
        }

        private string mEstimate;
        public string Estimate
        {
            get { return mEstimate; }
            set
            {
                if (mEstimate != value)
                {
                    mEstimate = value;

                    if (Syncing)
                    {
                        if (IsSynced)
                        {
                            RestRequest request = CreateSetEstimateRequest(this, mEstimate, () => { });
                            Owner.ExecuteRequest(request);
                        }
                    }

                    OnPropertyChanged("Estimate");
                }
            }
        }
        public void ChangeEstimate(string estimate, VoidCallback callback)
        {
            if (estimate != mEstimate)
            {
                RestRequest request = CreateSetEstimateRequest(this, estimate, () =>
                {
                    mEstimate = estimate;
                    OnPropertyChanged("Estimate");

                    callback();
                });
                Owner.ExecuteRequest(request);
            }
            else
            {
                callback();
            }
        }
        #endregion

        #region Construction
        public Task()
        {
            Syncing = true;
            Tags = new TaskTagCollection(this);
            Notes = new TaskTaskNoteCollection(this);
        }

        public Task(string name)
            : this()
        {
            mName = name;
        }
        #endregion

        #region Public Methods
        public void Complete(VoidCallback callback)
        {
            if (Syncing)
            {
                if (!IsSynced)
                    throw new InvalidOperationException("Can't complete a task that has not been synced.");

                Request request = CreateStandardRequest(this, "rtm.tasks.complete", () =>
                {
                    Completed = DateTime.Now;
                    OnPropertyChanged("Completed");
                    OnPropertyChanged("IsCompleted");
                    OnPropertyChanged("IsIncomplete");

                    callback();
                });

                Owner.ExecuteRequest(request);
            }
        }

        public void Uncomplete(VoidCallback callback)
        {
            if (Syncing)
            {
                if (!IsSynced)
                    throw new InvalidOperationException("Can't uncomplete a task that has not been synced.");

                Request request = CreateStandardRequest(this, "rtm.tasks.uncomplete", () =>
                {
                    Completed = null;
                    OnPropertyChanged("Completed");
                    OnPropertyChanged("IsCompleted");
                    OnPropertyChanged("IsIncomplete");
                    callback();
                });
                Owner.ExecuteRequest(request);
            }
        }

        public void Postpone(VoidCallback callback)
        {
            if (Syncing)
            {
                if (!IsSynced)
                    throw new InvalidOperationException("Can't postpone a task that has not been synced.");

                Request request = CreateStandardRequest(this, "rtm.tasks.postpone", () =>
                {
                    Postponed++;


                    if (DueDateTime.HasValue)
                    {
                        if (DueDateTime.Value.Date < DateTime.Today)
                        {
                            // task is overdue
                            DateTime newDate = DateTime.Today;
                            if (HasDueTime)
                            {
                                newDate += DueDateTime.Value.TimeOfDay;
                            }
                            SetDueAndIsLate(newDate, HasDueTime);
                        }
                        else
                        {
                            SetDueAndIsLate(DueDateTime.Value.AddDays(1), HasDueTime);
                        }
                    }
                    else
                    {
                        SetDueAndIsLate(DateTime.Today, false);
                    }

                    OnPropertyChanged("Postponed");
                    OnPropertyChanged("DueString");
                    OnPropertyChanged("DueDateTime");
                    OnPropertyChanged("Due");

                    callback();
                });
                Owner.ExecuteRequest(request);
            }
        }

        public void Delete(VoidCallback callback)
        {
            if (Syncing)
            {
                if (!IsSynced)
                    throw new InvalidOperationException("Can't delete a task that has not been synced.");

                Request request = CreateStandardRequest(this, "rtm.tasks.delete", () =>
                {
                    Deleted = DateTime.Now;
                    OnPropertyChanged("Deleted");
                    callback();
                });
                Owner.ExecuteRequest(request);
            }
        }

        public void AddNote(string title, string body, VoidCallback callback)
        {
            if (Syncing)
            {
                if (!IsSynced)
                    throw new InvalidOperationException("Can't add note to a task that has not been synced.");

                RestRequest request = CreateStandardRequest(this, "rtm.tasks.notes.add", () =>
                {
                    OnPropertyChanged("Notes");
                    callback();
                });
                request.Parameters.Add("note_title", title);
                request.Parameters.Add("note_text", body);
                Owner.ExecuteRequest(request);
            }
        }

        public void DeleteNote(TaskNote note, VoidCallback callback)
        {
            note.Delete(() =>
            {
                OnPropertyChanged("Notes");
                callback();
            });
        }

        public void EditNote(TaskNote note, string title, string body, VoidCallback callback)
        {
            note.Edit(title, body, () =>
            {
                OnPropertyChanged("Notes");
                callback();
            });
        }

        public void SetTags(string formattedTags, char[] separators)
        {
            string[] tags = formattedTags.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            SetTags(tags);
        }

        public void SetTags(IEnumerable<string> tags)
        {
            Tags.SetTags(tags);
        }

        public string GetTags(string separator)
        {
            StringBuilder tagsBuilder = new StringBuilder();
            foreach (var tag in Tags)
            {
                if (tagsBuilder.Length > 0)
                    tagsBuilder.Append(separator);
                tagsBuilder.Append(tag);
            }
            return tagsBuilder.ToString();
        }
        #endregion

        #region Sync Methods
        protected override void OnOwnerChanged()
        {
            // Update the user-friendly version of the due date depending on
            // the UserSettings (accessible only from Rtm).
            if (DueDateTime.HasValue)
                SetDueAndIsLate(DueDateTime, HasDueTime);
            // Notify our notes.
            Notes.OnOwnerChanged();
            base.OnOwnerChanged();
        }

        protected override void OnSyncingChanged()
        {
            Tags.Syncing = Syncing;
            Notes.Syncing = Syncing;
            base.OnSyncingChanged();
        }

        protected override void DoSync(bool firstSync, RawRtmElement element)
        {
            base.DoSync(firstSync, element);

            TaskBundle bundle = (TaskBundle)element;
            SeriesId = bundle.Series.Id;
            switch (bundle.SyncMode)
            {
                case TaskSyncMode.Download:
                    DoDownloadSync(firstSync, bundle);
                    break;
                case TaskSyncMode.Upload:
                    DoUploadSync(firstSync, bundle);
                    break;
                default:
                    throw new NotImplementedException("Internal Error: unsupported TaskSyncMode.");
            }
        }

        private void DoDownloadSync(bool firstSync, TaskBundle bundle)
        {
            // Retrieve all our values from the server.
            RawTaskSeries series = bundle.Series;
            DoDownloadSyncFromTaskSeries(firstSync, series);

            RawTask task = bundle.Task;
            DoDownloadSyncFromTask(firstSync, task);

            // Notify of property changes.
            OnPropertyChanged("Added");
            OnPropertyChanged("Completed");
            OnPropertyChanged("Created");
            OnPropertyChanged("Deleted");
            OnPropertyChanged("Due");
            OnPropertyChanged("DueDateTime");
            OnPropertyChanged("Estimate");
            OnPropertyChanged("HasDueTime");
            OnPropertyChanged("IsCompleted");
            OnPropertyChanged("IsDeleted");
            OnPropertyChanged("IsLate");
            OnPropertyChanged("IsIncomplete");
            OnPropertyChanged("Location");
            OnPropertyChanged("Modified");
            OnPropertyChanged("Name");
            OnPropertyChanged("Notes");
            OnPropertyChanged("Parent");
            OnPropertyChanged("ParentName");
            OnPropertyChanged("Postponed");
            OnPropertyChanged("Priority");
            OnPropertyChanged("Recurrence");
            OnPropertyChanged("SeriesId");
            OnPropertyChanged("Source");
            OnPropertyChanged("Tags");
            OnPropertyChanged("Url");
        }

        private void DoDownloadSyncFromTaskSeries(bool firstSync, RawTaskSeries series)
        {
            Source = series.Source;

            mName = series.Name;
            mUrl = series.Url;

            Created = DateTime.Parse(series.Created);
            mLocationId = RtmElement.UnsyncedId;
            if (!string.IsNullOrEmpty(series.LocationId))
                mLocationId = series.LocationId;
            if (!string.IsNullOrEmpty(series.Modified))
                Modified = DateTime.Parse(series.Modified);
            else
                Modified = null;

            SetRecurrence(series.RepeatRule);

            DownloadSyncTags(firstSync, series);
            DownloadSyncNotes(firstSync, series);
        }

        private void DoDownloadSyncFromTask(bool firstSync, RawTask task)
        {
            if (string.IsNullOrEmpty(task.Due))
            {
                SetDueAndIsLate(null, false);
            }
            else
            {
                DateTime dueDateTime = DateTime.Parse(task.Due);
                SetDueAndIsLate(dueDateTime, task.HasDueTime == 1);
            }

            mEstimate = task.Estimate;
            Added = DateTime.Parse(task.Added);

            if (!string.IsNullOrEmpty(task.Completed))
                Completed = DateTime.Parse(task.Completed);
            else
                Completed = null;

            if (!string.IsNullOrEmpty(task.Deleted))
                Deleted = DateTime.Parse(task.Deleted);
            else
                Deleted = null;

            if (string.IsNullOrEmpty(task.Priority) || task.Priority == "N")
                mPriority = TaskPriority.None;
            else
                mPriority = (TaskPriority)int.Parse(task.Priority);

            if (!string.IsNullOrEmpty(task.Postponed))
                Postponed = int.Parse(task.Postponed);
            else
                Postponed = 0;
        }

        private void DownloadSyncTags(bool firstSync, RawTaskSeries series)
        {
            Tags.UnsyncedClear();
            if (series.Tags != null)
            {
                foreach (var tag in series.Tags)
                {
                    Tags.UnsyncedAdd(tag);
                }
            }
        }

        private void DownloadSyncNotes(bool firstSync, RawTaskSeries series)
        {
            Notes.UnsyncedClear();
            if (series.Notes != null)
            {
                foreach (var note in series.Notes)
                {
                    Notes.UnsyncedAdd(new TaskNote(note));
                }
            }
        }

        private void DoUploadSync(bool firstSync, TaskBundle bundle)
        {
            // Upload all our values to the server.
            if (!string.IsNullOrEmpty(mDue))
            {
                RestRequest request = CreateSetDueDateRequest(this, HasDueTime, mDue, () => { });
                Owner.ExecuteRequest(request);
            }
            if (!string.IsNullOrEmpty(mEstimate))
            {
                RestRequest request = CreateSetEstimateRequest(this, mEstimate, () => { });
                Owner.ExecuteRequest(request);
            }
            if (mLocation != null)
            {
                if (mLocation.IsSynced)
                {
                    RestRequest request = CreateSetLocationRequest(this, mLocation.Id.ToString(), () => { });
                    Owner.ExecuteRequest(request);
                }
            }
            if (mPriority != TaskPriority.None)
            {
                RestRequest request = CreateSetPriorityRequest(this, TaskPriorityToPriorityRequestParameter(mPriority), () => { });
                Owner.ExecuteRequest(request);
            }
            if (!string.IsNullOrEmpty(mRecurrence))
            {
                RestRequest request = CreateSetRecurrenceRequest(this, mRecurrence, () => { });
                Owner.ExecuteRequest(request);
            }
            if (!string.IsNullOrEmpty(mUrl))
            {
                RestRequest request = CreateSetUrlRequest(this, mUrl, () => { });
                Owner.ExecuteRequest(request);
            }
            if (Tags.Count > 0)
            {
                Tags.UploadTags();
            }
            if (Notes.Count > 0)
            {
                Notes.UploadNotes();
            }
        }

        private void SetDueAndIsLate(DateTime? dueDateTime, bool hasDueTime)
        {
            if (dueDateTime == null)
            {
                mDue = null;
                mDueDateTime = null;
                HasDueTime = false;
            }
            else
            {
                UserSettings userSettings = null;
                if (Owner != null)
                    userSettings = Owner.UserSettings;

                HasDueTime = hasDueTime;
                mDueDateTime = dueDateTime;

                FuzzyDateTime fuzzyDueDateTime = new FuzzyDateTime(dueDateTime.Value, hasDueTime);
                mDue = DateConverter.FormatDateTime(fuzzyDueDateTime,
                    userSettings != null ? userSettings.DateFormat : DateFormat.Default,
                    userSettings != null ? userSettings.TimeFormat : TimeFormat.Default);
            }
        }

        private void SetRecurrence(RawRepeatRule rawRepeatRule)
        {
            if (rawRepeatRule == null)
            {
                mRecurrence = null;
                return;
            }


            DateFormat dateFormat = /*IsSynced ? Owner.UserSettings.DateFormat :*/ DateFormat.Default;
            mRecurrence = RecurrenceConverter.FormatRecurrence(rawRepeatRule.Rule, rawRepeatRule.Every == 1, dateFormat);
        }

        public TaskNote GetNote(string id)
        {
            foreach (TaskNote n in Notes)
            {
                if (n.Id == id) return n;
            }

            return null;
        }
        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(this, propertyName, PropertyChanged);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is Task)
            {
                Task other = obj as Task;

                return Task.CompareByDate(this, other);
            }
            else
            {
                throw new ArgumentException("Cannot compare Task to other types of objetcs.");
            }
        }

        public int CompareTo(Task other)
        {
            return Task.CompareByDate(this, other);
        }

        public static int CompareByDate(Task a, Task b)
        {
            int cmp = 0;

            if (a.DueDateTime.HasValue && b.DueDateTime.HasValue)
            {
                cmp = a.FuzzyDueDateTime.CompareTo(b.FuzzyDueDateTime);
            }
            else
            {
                cmp = (b.DueDateTime.HasValue ? 1 : 0) - (a.DueDateTime.HasValue ? 1 : 0);
            }

            if (cmp == 0)
            {
                if (a.Priority != b.Priority)
                {
                    if (a.Priority == TaskPriority.None) cmp = 1;
                    else if (b.Priority == TaskPriority.None) cmp = -1;
                    else cmp = a.Priority.CompareTo(b.Priority);
                }
                else
                {
                    cmp = a.Name.CompareTo(b.Name);
                }
            }

            return cmp;
        }

        public static int CompareByPriority(Task a, Task b)
        {
            int cmp = 0;

            if (a.Priority != b.Priority)
            {
                if (a.Priority == TaskPriority.None) cmp = 1;
                else if (b.Priority == TaskPriority.None) cmp = -1;
                else cmp = a.Priority.CompareTo(b.Priority);
            }
            else
            {
                if (a.DueDateTime.HasValue && b.DueDateTime.HasValue)
                {
                    cmp = a.FuzzyDueDateTime.CompareTo(b.FuzzyDueDateTime);
                }
                else
                {
                    cmp = (b.DueDateTime.HasValue ? 1 : 0) - (a.DueDateTime.HasValue ? 1 : 0);
                }

                if (cmp == 0)
                {
                    cmp = a.Name.CompareTo(b.Name);
                }
            }

            return cmp;
        }

        public static int CompareByName(Task a, Task b)
        {
            int cmp = a.Name.CompareTo(b.Name);
            if (cmp == 0)
            {
                if (a.Priority != b.Priority)
                {
                    if (a.Priority == TaskPriority.None) cmp = 1;
                    else if (b.Priority == TaskPriority.None) cmp = -1;
                    else cmp = a.Priority.CompareTo(b.Priority);
                }
                else
                {
                    if (a.DueDateTime.HasValue && b.DueDateTime.HasValue)
                    {
                        cmp = a.FuzzyDueDateTime.CompareTo(b.FuzzyDueDateTime);
                    }
                    else
                    {
                        cmp = (b.DueDateTime.HasValue ? 1 : 0) - (a.DueDateTime.HasValue ? 1 : 0);
                    }
                }
            }

            return cmp;
        }

        #endregion

        public bool ClientSyncing
        {
            get
            {
                return Owner.Syncing;
            }
        }

        public string FriendlyDueDate
        {
            get
            {
                if (this.DueDateTime.HasValue == true)
                {
                    if (this.DueDateTime.Value.Date > DateTime.Now.AddDays(6).Date ||
                        this.DueDateTime.Value.Date < DateTime.Now.Date)
                    {
                        if (this.HasDueTime)
                            return Strings.Due + " " + this.LocalizedDueDate + " " + Strings.DueAt + " " + this.LocalizedDueTime;
                        else
                            return Strings.Due + " " + this.LocalizedDueDate;
                    }
                    else
                    {
                        if (this.DueDateTime.Value.Date == DateTime.Now.Date)
                        {
                            if (this.HasDueTime)
                                return Strings.Due + " " + Strings.TodayLower + " " + Strings.DueAt + " " + this.LocalizedDueTime;
                            else
                                return Strings.Due + " " + Strings.TodayLower;
                        }
                        else
                        {
                            if (this.HasDueTime)
                                return Strings.Due + " " + this.DueString + " " + Strings.DueAt + " " + this.LocalizedDueTime;
                            else
                                return Strings.Due + " " + this.DueString;
                        }
                    }
                }
                else
                {
                    return Strings.NoDueDate;
                }
            }
        }

        public string FriendlyShortDueDate
        {
            get
            {
                if (this.DueDateTime.HasValue == true)
                {
                    if (this.DueDateTime.Value.Date > DateTime.Now.AddDays(6).Date ||
                        this.DueDateTime.Value.Date < DateTime.Now.Date)
                    {
                        return this.LocalizedShortDueDate;
                    }
                    else
                    {
                        if (this.DueDateTime.Value.Date == DateTime.Now.Date)
                        {
                            if (this.HasDueTime)
                                return this.LocalizedDueTime;
                            else
                                return Strings.Today;
                        }
                        else
                        {
                            return this.DueDateTime.Value.ToString("ddd");
                        }
                    }
                }
                else
                {
                    return "";
                }
            }
        }

        public string LocalizedDueDate
        {
            get
            {
                if (Owner.UserSettings != null &&
                    Owner.UserSettings.DateFormat == DateFormat.European)
                    return this.DueDateTime.Value.ToString("dddd, d MMMM");
                else
                    return this.DueDateTime.Value.ToString("dddd, MMMM d");
            }
        }

        public string LocalizedShortDueDate
        {
            get
            {
                if (Owner.UserSettings != null &&
                    Owner.UserSettings.DateFormat == DateFormat.European)
                    return this.DueDateTime.Value.ToString("d MMM");
                else
                    return this.DueDateTime.Value.ToString("MMM d");
            }
        }

        public string LocalizedDueTime
        {
            get
            {
                if (Owner.UserSettings != null &&
                    Owner.UserSettings.TimeFormat == TimeFormat.TwentyFourHours)
                    return this.DueDateTime.Value.ToString("H:mm").ToLower();
                else
                    return this.DueDateTime.Value.ToString("h:mmt").ToLower();
            }
        }

        public string PostponedString
        {
            get
            {
                if (this.Postponed == 0)
                    return "";
                else if (this.Postponed == 1)
                    return this.Postponed + " " + Strings.TimeSingle;
                else
                    return this.Postponed + " " + Strings.TimePlural;
            }
        }

        public string FriendlyTagsString
        {
            get
            {
                if (String.IsNullOrEmpty(TagsString) == false)
                {
                    return "#" + TagsString.Replace(", ", " #");
                }
                return "";
            }
        }

        private double _Distance = 0;

        public double Distance
        {
            get
            {
                return _Distance;
            }

            set
            {
                _Distance = value;
            }
        }

        public string FriendlyDistance
        {
            get
            {
                return _Distance.ToString("0.0") + " miles";
            }
        }

        public SolidColorBrush DueDateForegroundBrush
        {
            get
            {
                if (this.DueDateTime.HasValue &&
                    this.DueDateTime.Value.Date <= DateTime.Now.Date)
                    return (SolidColorBrush)Owner.Resources["PhoneAccentBrush"];
                else
                    if (Owner == null)
                        return null;
                    else
                        return (SolidColorBrush)Owner.Resources["PhoneSubtleBrush"];
            }
        }

        public SolidColorBrush ShortDueDateForegroundBrush
        {
            get
            {
                if (this.DueDateTime.HasValue &&
                    this.DueDateTime.Value.Date <= DateTime.Now.Date)
                    return (SolidColorBrush)Owner.Resources["PhoneAccentBrush"];
                else
                    if (Owner == null)
                        return null;
                    else
                        return (SolidColorBrush)Owner.Resources["PhoneForegroundBrush"];
            }
        }

        public SolidColorBrush PriorityForegroundBrush
        {
            get
            {
                if (this.Priority == TaskPriority.One)
                    return new SolidColorBrush(Color.FromArgb(255, 234, 82, 0));
                else if (this.Priority == TaskPriority.Two)
                    return new SolidColorBrush(Color.FromArgb(255, 0, 96, 191));
                else if (this.Priority == TaskPriority.Three)
                    return new SolidColorBrush(Color.FromArgb(255, 53, 154, 255));
                else
                    if (Owner == null)
                        return null;
                    else
                        return (SolidColorBrush)Owner.Resources["PhoneForegroundBrush"];
            }
        }

        public Visibility HasNotesVisibility
        {
            get
            {
                if (this.Notes.Count > 0)
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Collapsed;
            }
        }

        public Visibility HasRecurrenceVisibility
        {
            get
            {
                if (this.HasRecurrence == true)
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Collapsed;
            }
        }
    }
}
