using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using Windows.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IronCow.Rest
{
    public class RestClient : IRestClient
    {
        #region Callback Delegates

        // general callbacks
        public delegate void VoidCallback();

        // authentication callbacks
        public delegate void CheckTokenCallback(RawAuthentication authentication, bool success);
        public delegate void FrobCallback(string frob);
        public delegate void TokenCallback(string token, RawUser user);

        // contacts callbacks
        public delegate void RawContactCallback(RawContact contact);
        public delegate void RawContactArrayCallback(RawContact[] contacts);

        // group callbacks
        public delegate void RawGroupCallback(RawGroup group);
        public delegate void RawGroupArrayCallback(RawGroup[] groups);

        // list callbacks
        public delegate void RawListCallback(RawList list);
        public delegate void RawListArrayCallback(RawList[] list);

        // location callbacks
        public delegate void RawLocationArrayCallback(RawLocation[] locations);

        // settings callbacks
        public delegate void RawSettingsCallback(RawSettings settings);

        // task note callbacks
        public delegate void RawNoteCallback(RawNote note);

        // test callbacks
        public delegate void TestCallback(bool success);

        // time callbacks
        public delegate void DateTimeCallback(DateTime time);

        // timeline callbacks
        public delegate void TimelineCallback(int timeline);

        // timezone callbacks
        public delegate void RawTimezoneArrayCallback(RawTimezone[] timezones);

        // response callbacks
        public delegate void RawResponseCallback(string responseString);

        #endregion

        #region Constants
        private const string UserAgent = "Mozilla/4.0 IronCow API (compatible; MSIE 6.0; Windows NT 5.1)";
        private const string ApiUrl = "http://api.rememberthemilk.com/services/rest/";
        private const string SecureApiUrl = "https://api.rememberthemilk.com/services/rest/";
        #endregion

        #region Static Members
        private static XmlSerializer sResponseSerializer = new XmlSerializer(typeof(Response));
        #endregion

        #region Private Members
        private object mThrottlingLock = new object();
        private DateTime mLastRequestTime = DateTime.MinValue;
        #endregion

        #region Public Properties
        public string ApiKey { get; set; }
        public string SharedSecret { get; set; }
        public string AuthToken { get; set; }

        public bool UseHttps { get; set; }
        public int Timeout { get; set; }
        public TimeSpan Throttling { get; set; }

        public IResponseCache Cache { get; set; }
        #endregion

        #region Construction
        public RestClient(string apiKey, string sharedSecret)
            : this(apiKey, sharedSecret, null)
        {
        }

        public RestClient(string apiKey, string sharedSecret, string token)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException("apiKey");

            ApiKey = apiKey;
            SharedSecret = sharedSecret;
            AuthToken = token;

            UseHttps = false;
            Timeout = 5000;
            Throttling = TimeSpan.FromSeconds(1);
        }
        #endregion

        #region Authentication Methods

        public void CheckToken(string token, CheckTokenCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("auth_token", token);

            GetResponse("rtm.auth.checkToken", parameters, false, (response) =>
            {
                if (response.Status == ResponseStatus.OK)
                {
                    callback(response.Authentication, true);
                }
                else
                {
                    if (response.Error.Code == ErrorCode.LoginFailedOrInvalidAuthToken)
                        callback(null, false);
                    else
                        throw new RtmException(response.Error);
                }
            });
        }

        public void GetFrob(FrobCallback callback)
        {
            GetResponse("rtm.auth.getFrob", (response) =>
            {
                callback(response.Frob);
            });
        }

        public void GetToken(string frob, TokenCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("frob", frob);

            GetResponse("rtm.auth.getToken", parameters, (response) =>
            {
                AuthToken = response.Authentication.Token;
                callback(response.Authentication.Token, response.Authentication.User);
            });
            
        }
        #endregion

        #region Contacts Methods
        public void GetContacts(RawContactArrayCallback callback)
        {
            GetResponse("rtm.contacts.getList", (response) =>
            {
                if (response.Contacts != null)
                    callback(response.Contacts);
                else
                    callback(new RawContact[0]);
            });
        }

        public void AddContact(string contact, int timeline, RawContactCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("contact", contact);
            parameters.Add("timeline", timeline.ToString());
            GetResponse("rtm.contacts.add", parameters, (response) =>
            {
                callback(response.Contact);
            });
        }

        public void DeleteContact(int contactId, int timeline, VoidCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("contact_id", contactId.ToString());
            parameters.Add("timeline", timeline.ToString());
            GetResponse("rtm.contacts.delete", parameters, (response) => { callback(); });
        }
        #endregion

        #region Groups Methods
        public void GetContactGroups(RawGroupArrayCallback callback)
        {
            GetResponse("rtm.groups.getList", (response) => {
                if (response.Groups != null)
                    callback(response.Groups);
                else
                    callback(new RawGroup[0]);
            });
        }

        public void AddContactGroup(string groupName, int timeline, RawGroupCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("group", groupName);
            parameters.Add("timeline", timeline.ToString());
            GetResponse("rtm.groups.add", parameters, (response) => { callback(response.Group); });
        }

        public void DeleteContactGroup(int groupId, int timeline, VoidCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("group_id", groupId.ToString());
            parameters.Add("timeline", timeline.ToString());
            GetResponse("rtm.groups.delete", parameters, (response) => { callback(); });
        }

        public void AddContactToContactGroup(int contactId, int groupId, int timeline, VoidCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("group_id", groupId.ToString());
            parameters.Add("contact_id", contactId.ToString());
            GetResponse("rtm.groups.addContact", parameters, (response) => { callback(); });
        }

        public void RemoveContactFromContactGroup(int contactId, int groupId, int timeline, VoidCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("group_id", groupId.ToString());
            parameters.Add("contact_id", contactId.ToString());
            GetResponse("rtm.groups.removeContact", (response) => { callback(); });
        }
        #endregion

        #region Lists Methods

        public void AddList(string listName, int timeline, RawListCallback callback)
        {
            AddList(listName, null, timeline, callback);
        }

        public void AddList(string listName, string filter, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("name", listName);
            if (filter != null)
                parameters.Add("filter", filter);
            GetResponse("rtm.lists.add", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void ArchiveList(int listId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            GetResponse("rtm.lists.archive", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void DeleteList(int listId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            GetResponse("rtm.lists.delete", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void GetLists(RawListArrayCallback callback)
        {
            GetResponse("rtm.lists.getList", (response) =>
            {
                callback(response.Lists);
            });
        }

        public void SetDefaultList(int? listId, int timeline, VoidCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            if (listId.HasValue)
                parameters.Add("list_id", listId.ToString());
            GetResponse("rtm.lists.setDefaultList", parameters, (r) => { callback(); });
        }

        public void SetListName(int listId, string name, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("name", name);
            GetResponse("rtm.lists.setName", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void UnarchiveList(int listId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            GetResponse("rtm.lists.unarchive", parameters, (response) =>
            {
                callback(response.List);
            });
        }
        #endregion

        #region Locations Methods
        public void GetLocations(RawLocationArrayCallback callback)
        {
            GetResponse("rtm.locations.getList", (response) => { callback(response.Locations); });
        }
        #endregion

        #region Settings Methods
        public void GetSettings(RawSettingsCallback callback)
        {
            GetResponse("rtm.settings.getList", (response) => { callback(response.Settings); });
        }
        #endregion

        #region Tasks Methods
        public void AddTask(string name, int timeline, RawListCallback callback)
        {
            AddTask(name, false, timeline, callback);
        }

        public void AddTask(string name, bool parse, int timeline, RawListCallback callback)
        {
            AddTask(name, parse, null, timeline, callback);
        }

        public void AddTask(string name, string listId, int timeline, RawListCallback callback)
        {
            AddTask(name, false, listId, timeline, callback);
        }

        public void AddTask(string name, bool parse, string listId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            if (listId != null)
                parameters.Add("list_id", listId.ToString());
            parameters.Add("name", name);
            if (parse)
                parameters.Add("parse", "1");
            GetResponse("rtm.tasks.add", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void AddTaskTags(int listId, int taskSeriesId, int taskId, string tags, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("tags", tags);
            GetResponse("rtm.tasks.addTags", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void CompleteTask(int listId, int taskSeriesId, int taskId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            GetResponse("rtm.tasks.complete", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void DeleteTask(int listId, int taskSeriesId, int taskId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            GetResponse("rtm.tasks.delete", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void GetTasks(int listId, RawListArrayCallback callback)
        {
            GetTasks(listId, null, null, callback);
        }

        public void GetTasks(string filter, RawListArrayCallback callback)
        {
            GetTasks(null, filter, null, callback);
        }

        public void GetTasks(int? listId, string filter, DateTime? lastSync, RawListArrayCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (listId != null) parameters["list_id"] = listId.Value.ToString();
            if (filter != null) parameters["filter"] = filter;
            if (lastSync != null) parameters["last_sync"] = lastSync.Value.ToString("s");
            GetResponse("rtm.tasks.getList", parameters, (response) =>
            {
                callback(response.Tasks);
            });
        }

        public void MoveTaskPriorityUp(int listId, int taskSeriesId, int taskId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("direction", "up");
            GetResponse("rtm.tasks.movePriority", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void MoveTaskPriorityDown(int listId, int taskSeriesId, int taskId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("direction", "down");
            GetResponse("rtm.tasks.movePriority", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void MoveTaskTo(int fromListId, int toListId, int taskSeriesId, int taskId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("from_list_id", fromListId.ToString());
            parameters.Add("to_list_id", toListId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            GetResponse("rtm.tasks.moveTo", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void PostponeTask(int listId, int taskSeriesId, int taskId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            GetResponse("rtm.tasks.postpone", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void RemoveTaskTags(int listId, int taskSeriesId, int taskId, string tags, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("tags", tags);
            GetResponse("rtm.tasks.removeTags", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void SetTaskDueDate(int listId, int taskSeriesId, int taskId, string due, int timeline, RawListCallback callback)
        {
            SetTaskDueDate(listId, taskSeriesId, taskId, due, false, true, timeline, callback);
        }

        public void SetTaskDueDate(int listId, int taskSeriesId, int taskId, string due, bool hasDueTime, bool parse, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            if (due != null)
                parameters.Add("due", due);
            if (hasDueTime)
                parameters.Add("has_due_time", "1");
            if (parse)
                parameters.Add("parse", "1");
            GetResponse("rtm.tasks.setDueDate", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void SetTaskEstimate(int listId, int taskSeriesId, int taskId, string estimate, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("estimate", estimate);
            GetResponse("rtm.tasks.setEstimate", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void SetTaskLocation(int listId, int taskSeriesId, int taskId, int locationId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("location_id", locationId.ToString());
            GetResponse("rtm.tasks.setLocation", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void SetTaskName(int listId, int taskSeriesId, int taskId, string name, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("name", name);
            GetResponse("rtm.tasks.setName", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void SetTaskPriority(int listId, int taskSeriesId, int taskId, int? priority, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            if (priority.HasValue)
                parameters.Add("priority", priority.ToString());
            GetResponse("rtm.tasks.setPriority", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void ResetTaskPriority(int listId, int taskSeriesId, int taskId, int timeline, RawListCallback callback)
        {
            SetTaskPriority(listId, taskSeriesId, taskId, null, timeline, callback);
        }

        public void SetTaskRecurrence(int listId, int taskSeriesId, int taskId, string recurrence, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("repeat", recurrence);
            GetResponse("rtm.tasks.setRecurrence", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void SetTaskTags(int listId, int taskSeriesId, int taskId, string tags, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("tags", tags);
            GetResponse("rtm.tasks.setTags", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void SetTaskUrl(int listId, int taskSeriesId, int taskId, string url, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("url", url);
            GetResponse("rtm.tasks.setURL", parameters, (response) =>
            {
                callback(response.List);
            });
        }

        public void UncompleteTask(int listId, int taskSeriesId, int taskId, int timeline, RawListCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            GetResponse("rtm.tasks.uncomplete", parameters, (response) =>
            {
                callback(response.List);
            });
        }
        #endregion

        #region Task Notes Methods
        public void AddTaskNote(int listId, int taskSeriesId, int taskId, string noteTitle, string noteText, int timeline, RawNoteCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("list_id", listId.ToString());
            parameters.Add("taskseries_id", taskSeriesId.ToString());
            parameters.Add("task_id", taskId.ToString());
            parameters.Add("note_title", noteTitle);
            parameters.Add("note_text", noteText);
            GetResponse("rtm.notes.add", parameters, (response) =>
            {
                callback(response.Note);
            });
        }

        public void DeleteTaskNode(int noteId, int timeline, VoidCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("note_id", noteId.ToString());
            GetResponse("rtm.notes.delete", parameters, (response) => 
            {
                callback();
            });
        }

        public void EditTaskNote(int noteId, string noteTitle, string noteText, int timeline, RawNoteCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("timeline", timeline.ToString());
            parameters.Add("note_id", noteId.ToString());
            parameters.Add("note_title", noteTitle);
            parameters.Add("note_text", noteText);
            GetResponse("rtm.notes.edit", parameters, (response) => 
            {
                callback(response.Note);
            });
        }
        #endregion

        #region Test Methods
        public void TestLogin(TestCallback callback)
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
                        callback(false);
                    else
                        throw new RtmException(response.Error);
                }
            });
            
        }
        #endregion

        #region Time Methods
        public void ParseTime(string text, string timezone, TimeFormat timeFormat, DateTimeCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["text"] = text;
            parameters["dateFormat"] = ((int)timeFormat).ToString();
            if (!string.IsNullOrEmpty(timezone))
                parameters["timezone"] = timezone;
            GetResponse("rtm.time.parse", parameters, (response) => { callback(response.Time.Time); });
        }
        #endregion

        #region Timeline Methods
        public void CreateTimeline(TimelineCallback callback)
        {
            GetResponse("rtm.timelines.create", (response) => { callback(response.Timeline); });
        }
        #endregion

        #region Timezone Methods
        public void GetTimezones(RawTimezoneArrayCallback callback)
        {
            GetResponse("rtm.timezones.getList", (response) => { callback(response.Timezones); });
        }
        #endregion

        #region Response Methods
        public string EscapeRequestParameter(string value)
        {
            value = Uri.EscapeDataString(value);
            return value;
        }


        public void GetResponse(string method, ResponseCallback callback)
        {
            GetResponse(method, true, callback);
        }

        public void GetResponse(string method, bool throwOnError, ResponseCallback callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            GetResponse(method, parameters, throwOnError, callback);
        }

        public void GetResponse(string method, Dictionary<string, string> parameters, ResponseCallback callback)
        {
            GetResponse(method, parameters, true, callback);
        }

        public void GetResponse(string method, Dictionary<string, string> parameters, bool throwOnError, ResponseCallback callback)
        {
            parameters["method"] = method;
            parameters["api_key"] = ApiKey;
            if (AuthToken != null)
                parameters["auth_token"] = AuthToken;

            string[] keys = parameters.Keys.ToArray();
            Array.Sort(keys);

            StringBuilder variablesBuilder = new StringBuilder();

            foreach (string key in keys)
            {
                if (variablesBuilder.Length > 0)
                    variablesBuilder.Append("&");

                variablesBuilder.Append(key);
                variablesBuilder.Append("=");
                variablesBuilder.Append(EscapeRequestParameter(parameters[key]));
            }

            if (SharedSecret != null)
            {
                if (variablesBuilder.Length > 0)
                    variablesBuilder.Append("&");

                string apiSig = RtmAuthentication.GetApiSig(SharedSecret, parameters);
                variablesBuilder.Append("api_sig=");
                variablesBuilder.Append(apiSig);
            }

            string url = UseHttps ? SecureApiUrl : ApiUrl;
            string variables = variablesBuilder.ToString();
            string rawUrl = url + "?" + variables;
            string responseString = null;

            if (Cache != null)
            {
                if (Cache.HasCachedResponse(rawUrl))
                {
                    responseString = Cache.GetCachedResponse(rawUrl);
                    //IronCowTraceSource.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "Got response for '{0}' from cache.", method);
                }
            }
            if (responseString == null)
            {
                lock (mThrottlingLock)
                {
                    //TimeSpan timeSinceLastRequest = DateTime.Now - mLastRequestTime;
                    //if (timeSinceLastRequest < Throttling)
                    //{
                    //    TimeSpan wait = Throttling - timeSinceLastRequest;
                    //    System.Threading.Thread.Sleep((int)Math.Ceiling(wait.TotalMilliseconds));
                    //}

                    GetRawResponse(url, variables, (r) => {
                        if (Cache != null)
                            Cache.CacheResponse(rawUrl, r);

                        StringReader responseReader = new StringReader(r);
                        Response response = (Response)sResponseSerializer.Deserialize(responseReader);
                        responseReader.Dispose();

                        if (throwOnError && response.Status != ResponseStatus.OK)
                            throw new RtmException(response.Error);
                        
                        callback(response);
                    });
                    mLastRequestTime = DateTime.Now;
                }
            }
        }

        private void GetRawResponse(string url, string variables, RawResponseCallback callback)
        {
            if (variables.Length < 2000)
            {
                url += "?" + variables;
                variables = "";
            }
            // Initialise the web request
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            // req.UserAgent = UserAgent;

            IAsyncResult res = (IAsyncResult)req.BeginGetResponse((IAsyncResult result) =>
            {

                HttpWebRequest request = (HttpWebRequest)result.AsyncState;
                WebResponse response = request.EndGetResponse(result);

                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string contents = reader.ReadToEnd();
                        callback(contents);
                    }
                }
            }, req);
        }
        #endregion
    }
}
