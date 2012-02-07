using System;
using System.ComponentModel;
using IronCow.Rest;

namespace IronCow
{
    public class TaskNote : RtmFatElement, INotifyPropertyChanged
    {
        #region Public Properties
        public DateTime Created { get; private set; }
        public DateTime? Modified { get; private set; }

        private string mTitle = string.Empty;
        public string Title
        {
            get { return mTitle; }
            set
            {
                mTitle = value ?? string.Empty;
                OnPropertyChanged("Title");
            }
        }

        private string mBody = string.Empty;
        public string Body
        {
            get { return mBody; }
            set
            {
                mBody = value ?? string.Empty;
                OnPropertyChanged("Body");
            }
        }

        public DateTime CreatedOrModified
        {
            get
            {
                return Modified ?? Created;
            }
        }
        #endregion

        #region Construction
        public TaskNote()
        {
        }

        public TaskNote(string title)
        {
            Title = title;
        }

        public TaskNote(string title, string body)
            : this(title)
        {
            Body = body;
        }

        internal TaskNote(RawNote note)
        {
            Sync(note);
        }
        #endregion

        #region Syncing Methods
        protected override void DoSync(bool firstSync, RawRtmElement element)
        {
            base.DoSync(firstSync, element);

            RawNote note = (RawNote)element;
            Created = DateTime.Parse(note.Created);
            if (!string.IsNullOrEmpty(note.Modified))
                Modified = DateTime.Parse(note.Modified);

            mTitle = note.Title ?? string.Empty;
            mBody = note.Body ?? string.Empty;

            OnPropertyChanged("Title");
            OnPropertyChanged("Body");
        }

        protected override void OnSyncingChanged()
        {
            base.OnSyncingChanged();
        }

        public void Upload(IronCow.Rest.RestClient.VoidCallback callback)
        {
            if (Syncing && IsSynced)
            {
                RestRequest request = new RestRequest("rtm.tasks.notes.edit", (r) => { callback(); });
                request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
                request.Parameters.Add("note_id", Id.ToString());
                request.Parameters.Add("note_title", Title);
                request.Parameters.Add("note_text", Body);
                Owner.ExecuteRequest(request);
            }
        }

        public void Edit(string title, string body, IronCow.Rest.RestClient.VoidCallback callback)
        {
            if (Syncing && IsSynced)
            {
                RestRequest request = new RestRequest("rtm.tasks.notes.edit", (r) => 
                {
                    Title = title;
                    Body = body;
                    callback(); 
                });
                request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
                request.Parameters.Add("note_id", Id.ToString());
                request.Parameters.Add("note_title", title);
                request.Parameters.Add("note_text", body);
                Owner.ExecuteRequest(request);
            }
        }

        public void Delete(IronCow.Rest.RestClient.VoidCallback callback)
        {
            if (Syncing && IsSynced)
            {
                RestRequest request = new RestRequest("rtm.tasks.notes.delete", (r) => 
                {
                    callback();
                });
                request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
                request.Parameters.Add("note_id", Id.ToString());
                Owner.ExecuteRequest(request);
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
    }
}
