using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using IronCow.Rest;
using System.Windows;
using IronCow.Common;

namespace IronCow
{
    public class Rtm
    {
        public ResourceDictionary Resources;

        #region Callback Delegates

        public delegate void VoidCallback();
        public delegate void ResponsesCallback(Response listsResponse, Response tasksResponse);

        // authentication delegates
        public delegate void FrobCallback(string frob);
        public delegate void TokenCallback(string token, User user);
        public delegate void CheckTokenCallback(Authentication authentication, bool success);
        public delegate void CheckLoginCallback(bool success);

        // timeline and transaction delegates
        public delegate void TimelineCallback(int timeline);

        // time delegates
        public delegate void DateTimeCallback(DateTime time);

        // tasks delegates
        public delegate void TaskArrayCallback(Task[] tasks);

        // task list delegates
        public delegate void TaskListCallback(TaskList list);

        // cache events
        public event ResponseCallback CacheTasksEvent;
        public event ResponseCallback CacheListsEvent;
        public event ResponseCallback CacheLocationsEvent;
        public event ResponseCallback CacheUserSettingsEvent;

        #endregion

        #region Private Members
        private Queue<Request> mRequestQueue = new Queue<Request>();
        #endregion

        #region Internal Members
        internal bool? SyncingInternal { get; private set; }
        #endregion

        #region Syncing Methods

        public void SyncEverything(SyncCallback callback)
        {
            CacheUserSettings(() =>
            {
                CacheLocations(() =>
                {
                    CacheLists(() =>
                    {
                        CacheTasks(() =>
                        {
                            StartTimeline((timeline) =>
                            {
                                callback();
                            });
                        });
                    });
                });
            });
        }

        public void SyncUserSettings(SyncCallback callback)
        {
            GetResponse("rtm.settings.getList", (response) =>
            {
                UserSettings tmp = new UserSettings(response.Settings);
                System.Threading.Interlocked.Exchange(ref mUserSettings, tmp);
                callback();
            });
        }

        public void SyncTaskLists(SyncCallback callback)
        {
            TaskListCollection tmp = new TaskListCollection(this);

            tmp.Resync(() =>
            {
                System.Threading.Interlocked.Exchange(ref mTaskLists, tmp);
                callback();
            });
        }

        public void SyncContacts(SyncCallback callback)
        {
            ContactCollection tmp = new ContactCollection(this);
            tmp.Resync(() =>
            {
                System.Threading.Interlocked.Exchange(ref mContacts, tmp);
                callback();
            });
        }

        public void SyncContactGroups(SyncCallback callback)
        {
            ContactGroupCollection tmp = new ContactGroupCollection(this);
            tmp.Resync(() =>
            {
                System.Threading.Interlocked.Exchange(ref mContactGroups, tmp);
                callback();
            });
        }

        public void SyncLocations(SyncCallback callback)
        {
            LocationCollection tmp = new LocationCollection(this);
            tmp.Resync(() =>
            {
                System.Threading.Interlocked.Exchange(ref mLocations, tmp);
                callback();
            });
        }

        #endregion

        #region Public Properties
        public static Dispatcher Dispatcher { get; set; }

        private IRestClient mClient;
        public IRestClient Client
        {
            get { return mClient; }
        }

        public string AuthToken
        {
            get { return mClient.AuthToken; }
            set { mClient.AuthToken = value; }
        }

        public TimeSpan RequestThrottling
        {
            get { return mClient.Throttling; }
            set { mClient.Throttling = value; }
        }

        public int CurrentTimeline { get; set; }
        public bool HasTimeline { get { return CurrentTimeline > 0; } }

        public bool Syncing { get { return SyncingInternal.GetValueOrDefault(true); } }

        private SearchMode mSearchMode = SearchMode.RemoteOnly;
        public SearchMode SearchMode
        {
            get { return mSearchMode; }
            set
            {
                if (value != mSearchMode)
                {
                    if (value == SearchMode.LocalAndRemote || value == SearchMode.LocalOnly)
                    {
                        CacheTasks(() => { });
                    }
                    mSearchMode = value;
                }
            }
        }

        private ContactCollection mContacts;
        public ContactCollection Contacts
        {
            get
            {
                return mContacts;
            }
        }

        private ContactGroupCollection mContactGroups;
        public ContactGroupCollection ContactGroups
        {
            get
            {
                return mContactGroups;
            }
        }

        private LocationCollection mLocations;
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// The API doesn't expose methods for adding/removing locations yet
        /// so the LocationCollection is only exposed as an IEnumerable.
        /// </remarks>
        public IEnumerable<Location> Locations
        {
            get
            {
                return mLocations;
            }
        }

        private TaskListCollection mTaskLists;
        public TaskListCollection TaskLists
        {
            get
            {
                return mTaskLists;
            }
            set
            {
                mTaskLists = value;
            }
        }

        private ObservableCollection<Task> mTasks;
        public ObservableCollection<Task> Tasks
        {
            get
            {
                return mTasks;
            }
        }

        private UserSettings mUserSettings;
        public UserSettings UserSettings
        {
            get
            {
                return mUserSettings;
            }
        }
        #endregion

        #region Public Events
        public event EventHandler PreviewGotResponse;
        public event EventHandler GotResponse;
        #endregion

        #region Construction
        static Rtm()
        {
            Dispatcher = null;
        }

        private Rtm()
        {
            SyncingInternal = null;
            CurrentTimeline = -1;
        }

        public Rtm(string apiKey, string sharedSecret)
            : this(apiKey, sharedSecret, null)
        {
        }

        public Rtm(string apiKey, string sharedSecret, string token)
            : this()
        {
            mClient = new RestClient(apiKey, sharedSecret, token);
        }

        public Rtm(IRestClient client)
            : this()
        {
            mClient = client;
        }
        #endregion

        #region Public Methods
        #region Authentication
        public string GetAuthenticationUrl(string frob, AuthenticationPermissions permission)
        {
            return RtmAuthentication.GetAuthenticationUrl(frob, Client.ApiKey, Client.SharedSecret, permission);
        }

        public void GetFrob(FrobCallback callback)
        {
            if (!Syncing)
            {
                callback(string.Empty);
            }
            else
            {
                GetResponse("rtm.auth.getFrob", (response) => { callback(response.Frob); });
            }
        }

        public void GetToken(string frob, TokenCallback callback)
        {
            if (!Syncing)
            {
                User user = new User("rtm.is.unsynced", "IronCow is currently disabled");
                callback(string.Empty, user);
            }
            else
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("frob", frob);

                GetResponse("rtm.auth.getToken", parameters, (response) =>
                {
                    AuthToken = response.Authentication.Token;
                    User user = new User(response.Authentication.User);
                    callback(response.Authentication.Token, user);
                });
            }
        }

        public void CheckToken(string token, CheckTokenCallback callback)
        {
            if (!Syncing)
            {
                Authentication authentication = new Authentication(new RawAuthentication()
                {
                    Permissions = AuthenticationPermissions.Delete,
                    Token = string.Empty,
                    User = new RawUser() { Id = RtmElement.UnsyncedId, UserName = "rtm.is.unsynced", FullName = "IronCow is currently disabled" }
                });
                callback(authentication, true);
            }
            else
            {

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("auth_token", token);

                GetResponse("rtm.auth.checkToken", parameters, false, (response) =>
                {
                    if (response.Status == ResponseStatus.OK)
                    {
                        Authentication authentication = new Authentication(response.Authentication);
                        callback(authentication, true);
                    }
                    else
                    {
                        Authentication authentication = null;
                        if (response.Error.Code == ErrorCode.LoginFailedOrInvalidAuthToken)
                        {
                            callback(authentication, true);
                        }
                        else
                        {
                            throw new RtmException(response.Error);
                        }
                    }
                });
            }
        }

        public void CheckLogin(CheckLoginCallback callback)
        {
            if (!Syncing)
            {
                callback(true);
            }
            else
            {
                GetResponse("rtm.test.login", false, (response) =>
                {
                    if (response.Status == ResponseStatus.OK)
                    {
                        callback(true);
                    }
                    else
                    {
                        if (response.Error.Code == ErrorCode.UserNotLoggedInOrInsufficientPermissions)
                        {
                            callback(false);
                        }
                        else
                        {
                            throw new RtmException(response.Error);
                        }
                    }
                });
            }
        }
        #endregion

        #region Settings
        public void CacheUserSettings(VoidCallback callback)
        {
            GetResponse("rtm.settings.getList", response =>
            {
                LoadUserSettingsFromResponse(response);

                if (CacheUserSettingsEvent != null)
                    CacheUserSettingsEvent(response);

                callback();
            });
        }

        public void LoadUserSettingsFromResponse(Response response)
        {
            if (response.Settings != null)
            {
                UserSettings temp = new IronCow.UserSettings(response.Settings);
                System.Threading.Interlocked.Exchange(ref mUserSettings, temp);
            }
        }
        #endregion

        #region Timelines and Transactions
        public void StartTimeline(TimelineCallback callback)
        {
            if (Syncing)
            {
                GetResponse("rtm.timelines.create", (response) =>
                {
                    if (response.Timeline == 0)
                        throw new Exception("Got null timeline.");
                    CurrentTimeline = response.Timeline;
                    callback(CurrentTimeline);
                });
            }
            else
            {
                CurrentTimeline = 123456789;
                callback(CurrentTimeline);
            }
        }

        public int GetTimeline()
        {
            return CurrentTimeline;
        }

        public void GetOrStartTimeline(TimelineCallback callback)
        {
            if (!HasTimeline)
                StartTimeline(callback);
            else
                callback(CurrentTimeline);
        }

        public void UndoTransaction(Transaction transaction, VoidCallback callback)
        {
            UndoTransaction(transaction.Id, callback);
        }

        public void UndoTransaction(int transactionId, VoidCallback callback)
        {
            if (CurrentTimeline == -1)
                throw new InvalidOperationException("No timeline has been started.");

            if (Syncing)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters["timeline"] = CurrentTimeline.ToString();
                parameters["transaction_id"] = transactionId.ToString();
                GetResponse("rtm.transactions.undo", true, (r) => { callback(); });
            }
            else
            {
                callback();
            }
        }
        #endregion

        #region Time
        public void ParseTime(string text, string timezone, TimeFormat timeFormat, DateTimeCallback callback)
        {
            //TODO: handle this with DateConverter if SearchMode is local.
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["text"] = text;
            parameters["dateFormat"] = ((int)timeFormat).ToString();
            if (!string.IsNullOrEmpty(timezone))
                parameters["timezone"] = timezone;

            GetResponse("rtm.time.parse", parameters, (response) =>
            {
                callback(response.Time.Time);
            });
        }
        #endregion

        #region Tasks
        public void GetTasks(string listId, TaskArrayCallback callback)
        {
            GetTasks(listId, null, callback);
        }

        public void GetTasksFromFilter(string filter, TaskArrayCallback callback)
        {
            GetTasks(RtmElement.UnsyncedId, filter, callback);
        }

        public List<Task> SearchTasksLocally(string filter)
        {
            try
            {
                var lexicalAnalyzer = new Search.LexicalAnalyzer();
                var tokens = lexicalAnalyzer.Tokenize(filter);
                var astRoot = lexicalAnalyzer.BuildAst(tokens);

                bool includeArchivedLists = astRoot.NeedsArchivedLists();

                var resultTasks = new List<Task>();
                var searchableTaskLists = GetSearchableTaskLists(includeArchivedLists);
                foreach (var list in searchableTaskLists)
                {
                    if (list.Tasks != null)
                    {
                        foreach (var task in list.Tasks)
                        {
                            var context = new Search.SearchContext(task, DateFormat.Default);
                            if (astRoot.ShouldInclude(context))
                                resultTasks.Add(task);
                        }
                    }
                }

                return resultTasks;
            }
            catch (Exception e)
            {
                // if there was an exception parsing the filter,
                // just return an empty list and silently fail the search

                return new List<Task>();
            }
        }

        public void GetTasks(string listId, string filter, TaskArrayCallback callback)
        {
            if (SearchMode == SearchMode.LocalOnly || SearchMode == SearchMode.LocalAndRemote)
            {
                // Local search mode... use our client side parser for the query.
                if (!string.IsNullOrEmpty(filter))
                {
                    try
                    {
                        var lexicalAnalyzer = new Search.LexicalAnalyzer();
                        var tokens = lexicalAnalyzer.Tokenize(filter);
                        var astRoot = lexicalAnalyzer.BuildAst(tokens);

                        bool includeArchivedLists = astRoot.NeedsArchivedLists();

                        var resultTasks = new List<Task>();
                        var searchableTaskLists = GetSearchableTaskLists(includeArchivedLists);
                        if (listId != RtmElement.UnsyncedId)
                            searchableTaskLists = searchableTaskLists.Where(tl => tl.Id == listId);
                        foreach (var list in searchableTaskLists)
                        {
                            foreach (var task in list.Tasks)
                            {
                                var context = new Search.SearchContext(task, UserSettings.DateFormat);
                                if (astRoot.ShouldInclude(context))
                                    resultTasks.Add(task);
                            }
                        }
                        callback(resultTasks.ToArray());
                        return;
                    }
                    catch (Exception)
                    {
                        if (SearchMode == SearchMode.LocalAndRemote && Syncing)
                        {
                            // Log the error and move on to the remote search.
                            //IronCowTraceSource.TraceSource.TraceEvent(System.Diagnostics.TraceEventType.Error, 0, e.Message);
                            //IronCowTraceSource.TraceSource.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Due to previous error message, falling back to remote server query.");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            if (!Syncing)
            {
                callback(new Task[0]);
                return;
            }

            // If we get here, we need to perform a remote search.
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (listId != RtmElement.UnsyncedId) parameters["list_id"] = listId.ToString();
            if (filter != null) parameters["filter"] = "(" + filter + ") AND status:Incomplete";
            else parameters["filter"] = "status:Incomplete";
            GetResponse("rtm.tasks.getList", parameters, (response) =>
            {
                List<Task> tasks = new List<Task>();
                foreach (var list in response.Tasks)
                {
                    foreach (var series in list.TaskSeries)
                    {
                        foreach (var task in series.Tasks)
                        {
                            //Task newTask = taskList.Tasks.GetById(series.Id, task.Id, true);
                            Task newTask = GetTask(task.Id);
                            tasks.Add(newTask);
                        }
                    }
                }
                callback(tasks.ToArray());
            });
        }

        public List<Task> GetOverdueTasks()
        {
            List<Task> tasks = new List<Task>();

            foreach (TaskList list in TaskLists)
            {
                if (list.IsNormal && list.Tasks != null)
                {
                    foreach (Task task in list.Tasks)
                    {
                        if (task.IsIncomplete && task.DueDateTime.HasValue && (task.DueDateTime.Value < DateTime.Today || (task.HasDueTime && task.DueDateTime.Value < DateTime.Now)))
                        {
                            tasks.Add(task);
                        }
                    }
                }
            }

            tasks.Sort(Task.CompareByDate);

            return tasks;
        }

        public List<Task> GetTodayTasks()
        {
            List<Task> tasks = new List<Task>();

            foreach (TaskList list in TaskLists)
            {
                if (list.IsNormal && list.Tasks != null)
                {
                    foreach (Task task in list.Tasks)
                    {
                        if (task.IsIncomplete && task.DueDateTime.HasValue && task.DueDateTime.Value.Date == DateTime.Today)
                        {
                            if (!task.HasDueTime || (task.HasDueTime && task.DueDateTime > DateTime.Now))
                            {
                                tasks.Add(task);
                            }
                        }
                    }
                }
            }

            tasks.Sort(Task.CompareByDate);

            return tasks;
        }

        public List<Task> GetTomorrowTasks()
        {
            List<Task> tasks = new List<Task>();

            foreach (TaskList list in TaskLists)
            {
                if (list.IsNormal && list.Tasks != null)
                {
                    foreach (Task task in list.Tasks)
                    {
                        if (task.IsIncomplete && task.DueDateTime.HasValue && task.DueDateTime.Value.Date == DateTime.Today.AddDays(1))
                        {
                            tasks.Add(task);
                        }
                    }
                }
            }

            tasks.Sort(Task.CompareByDate);

            return tasks;
        }

        public List<Task> GetWeekTasks()
        {
            List<Task> tasks = new List<Task>();

            foreach (TaskList list in TaskLists)
            {
                if (list.IsNormal && list.Tasks != null)
                {
                    foreach (Task task in list.Tasks)
                    {
                        if (task.IsIncomplete && task.DueDateTime.HasValue && task.DueDateTime.Value.Date > DateTime.Today.AddDays(1) && task.DueDateTime.Value.Date <= DateTime.Today.AddDays(6))
                        {
                            tasks.Add(task);
                        }
                    }
                }
            }

            tasks.Sort(Task.CompareByDate);

            return tasks;
        }

        public List<Task> GetNoDueTasks()
        {
            List<Task> tasks = new List<Task>();

            foreach (TaskList list in TaskLists)
            {
                if (list.IsNormal && list.Tasks != null)
                {
                    foreach (Task task in list.Tasks)
                    {
                        if (task.IsIncomplete && !task.DueDateTime.HasValue)
                        {
                            tasks.Add(task);
                        }
                    }
                }
            }

            tasks.Sort(Task.CompareByDate);

            return tasks;
        }

        public List<Task> GetNearbyTasks(double latitude, double longitude, double radius)
        {
            List<Task> tasks = new List<Task>();

            foreach (TaskList list in TaskLists)
            {
                if (list.IsNormal && list.Tasks != null)
                {
                    foreach (Task task in list.Tasks)
                    {
                        if (task.IsIncomplete && task.Location != null)
                        {
                            task.Distance = LocationHelper.Distance(latitude, longitude, task.Location.Latitude, task.Location.Longitude);

                            if (task.Distance <= radius)
                            {
                                tasks.Add(task);
                            }
                        }
                    }
                }
            }

            return tasks.OrderBy(z => z.Distance).ToList();
        }

        public Task GetTask(string id)
        {
            if (TaskLists != null)
            {
                foreach (TaskList list in TaskLists)
                {
                    if (list != null && list.Tasks != null)
                    {
                        foreach (Task t in list.Tasks)
                        {
                            if (t != null && t.Id == id)
                            {
                                return t;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public void CacheTasks(VoidCallback callback)
        {
            if (!Syncing)
                throw new InvalidOperationException();

            // Get the tasks asynchronously.
            var taskLists = TaskLists;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["filter"] = "status:incomplete";

            GetResponse("rtm.tasks.getList", parameters, (response) =>
            {
                LoadTasksFromResponse(response);

                if (CacheTasksEvent != null)
                    CacheTasksEvent(response);

                callback();
            });
        }

        public void LoadTasksFromResponse(Response response)
        {
            foreach (var list in response.Tasks)
            {
                TaskList taskList = TaskLists.GetById(list.Id);
                if (taskList != null)
                    taskList.InternalSync(list);
            }

            // reload every smart list
            foreach (var list in TaskLists)
            {
                if (list.IsSmart) list.SyncTasks(() => { });
            }
        }

        public void AddTask(string name, bool parse, string listId, VoidCallback callback)
        {
            if (Client is RestClient)
            {
                GetOrStartTimeline((t) =>
                {
                    (Client as RestClient).AddTask(name, parse, listId, CurrentTimeline, (list) =>
                    {
                        CacheTasks(() =>
                        {
                            callback();
                        });
                    });
                });
            }
        }

        #endregion

        #region Task Lists
        public void CacheLists(VoidCallback callback)
        {
            GetResponse("rtm.lists.getList", response =>
            {
                LoadListsFromResponse(response);

                if (CacheListsEvent != null)
                    CacheListsEvent(response);

                callback();
            });
        }

        public void LoadListsFromResponse(Response response)
        {
            TaskListCollection temp = new TaskListCollection(this);

            if (response.Lists != null)
            {
                using (new UnsyncedScope(temp))
                {
                    foreach (var list in response.Lists)
                    {
                        if (list.Archived == 0 && list.Deleted == 0)
                        {
                            TaskList newList = new TaskList(list);
                            temp.Add(newList);
                        }
                    }

                    temp.Sort();
                }
            }

            System.Threading.Interlocked.Exchange(ref mTaskLists, temp);
        }

        public TaskList GetInbox()
        {
            return TaskLists["Inbox"];
        }

        public TaskList GetDefaultTaskList()
        {
            try
            {
                string listName = UserSettings.DefaultList;
                if (string.IsNullOrEmpty(listName))
                    listName = "Inbox";

                try
                {
                    return TaskLists[listName];
                }
                catch (Exception ex)
                {
                    return TaskLists.SingleOrDefault<TaskList>(l => l.Id == listName);
                }
            }
            catch (Exception ex)
            {
                return TaskLists.FirstOrDefault<TaskList>();
            }
        }

        public IEnumerable<TaskList> GetParentableTaskLists(bool includeSmartLists)
        {
            IEnumerable<TaskList> result = from tl in this.TaskLists
                                           where !tl.GetFlag(TaskListFlags.Deleted) &&
                                                 !tl.GetFlag(TaskListFlags.Archived) &&
                                                 (!tl.GetFlag(TaskListFlags.Smart) || includeSmartLists) &&
                                                 (!tl.GetFlag(TaskListFlags.Locked) || tl.Name == "Inbox")
                                           select tl;
            return result;
        }

        public IEnumerable<TaskList> GetSearchableTaskLists(bool includeArchivedLists)
        {
            IEnumerable<TaskList> result = from tl in this.TaskLists
                                           where !tl.GetFlag(TaskListFlags.Deleted) &&
                                                 !tl.GetFlag(TaskListFlags.Smart) &&
                                                 (!tl.GetFlag(TaskListFlags.Archived) || includeArchivedLists)
                                           select tl;
            return result;
        }
        #endregion

        #region Locations
        public void CacheLocations(VoidCallback callback)
        {
            GetResponse("rtm.locations.getList", response =>
            {
                LoadLocationsFromResponse(response);

                if (CacheLocationsEvent != null)
                    CacheLocationsEvent(response);

                callback();
            });
        }

        public void LoadLocationsFromResponse(Response response)
        {
            LocationCollection temp = new LocationCollection(this);

            if (response.Locations != null)
            {
                foreach (var location in response.Locations)
                {
                    Location newLocation = new Location(location);
                    temp.Add(newLocation);
                }
            }

            System.Threading.Interlocked.Exchange(ref mLocations, temp);
        }
        #endregion

        #region Tags
        public string[] GetTags()
        {
            var tasksByTag = GetTasksByTag();

            string[] tags = new string[tasksByTag.Keys.Count];
            tasksByTag.Keys.CopyTo(tags, 0);
            return tags;
        }

        public Dictionary<string, ObservableCollection<Task>> GetTasksByTag()
        {
            var tasksByTag = new Dictionary<string, ObservableCollection<Task>>();
            foreach (var taskList in GetParentableTaskLists(false))
            {
                if (taskList.Tasks != null)
                {
                    foreach (var task in taskList.Tasks)
                    {
                        foreach (var tag in task.Tags)
                        {
                            if (tasksByTag.ContainsKey(tag))
                            {
                                tasksByTag[tag].Add(task);
                            }
                            else
                            {
                                var tasks = new ObservableCollection<Task>();
                                tasks.Add(task);
                                tasksByTag.Add(tag, tasks);
                            }
                        }
                    }
                }
            }
            return tasksByTag;
        }
        #endregion

        #region Syncing
        public void DisableSyncing()
        {
            lock (this)
            {
                SyncingInternal = false;

                if (mContacts != null) mContacts.Syncing = false;
                if (mContactGroups != null) mContactGroups.Syncing = false;
                if (mTaskLists != null) mTaskLists.Syncing = false;
            }
        }
        #endregion

        #region Requests Methods
        public void ExecuteRequest(Request request)
        {
            request.Execute(this);
        }
        #endregion
        #endregion

        #region Internal Response Methods
        internal void GetResponse(string method, ResponseCallback callback)
        {
            GetResponse(method, true, callback);
        }

        internal void GetResponse(string method, bool throwOnError, ResponseCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            GetResponse(method, parameters, throwOnError, callback);
        }

        internal void GetResponse(string method, Dictionary<string, string> parameters, ResponseCallback callback)
        {
            GetResponse(method, parameters, true, callback);
        }

        internal void GetResponse(string method, Dictionary<string, string> parameters, bool throwOnError, ResponseCallback callback)
        {
            if (!Syncing)
                throw new InvalidOperationException("This instance of RTM is not syncing and as such, no web requests should be sent.");

            if (PreviewGotResponse != null)
                PreviewGotResponse(this, EventArgs.Empty);

            Client.GetResponse(method, parameters, throwOnError, (response) =>
            {
                if (GotResponse != null)
                    GotResponse(this, EventArgs.Empty);

                callback(response);
            });
        }
        #endregion
    }
}
