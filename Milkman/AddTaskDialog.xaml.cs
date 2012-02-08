using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using Microsoft.Phone.Shell;

namespace Milkman
{
    public delegate void SubmitEventHandler(object sender, SubmitEventArgs e);

    public class SubmitEventArgs : EventArgs
    {
        public string Text { get; set; }

        public SubmitEventArgs(string text)
            : base()
        {
            Text = text;
        }
    }

    public partial class AddTaskDialog : UserControl, INotifyPropertyChanged
    {
        #region IsOpen Property

        private bool _isOpen = false;
        public bool IsOpen
        {
            get
            {
                return _isOpen;
            }

            set
            {
                if (value != _isOpen)
                {
                    if (value)
                    {
                        Open();
                    }
                    else
                    {
                        Close();
                    }
                }
            }
        }

        #endregion

        #region Submit Event

        public event SubmitEventHandler Submit;
        public void DoSubmit()
        {
            Close();

            if (Submit != null)
            {
                Submit(this, new SubmitEventArgs(this.txtDetails.Text));
            }
        }

        #endregion

        #region INofityPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Open and Close Events

        public void Open()
        {
            stbSwivelIn.Begin();
            SystemTray.BackgroundColor = Color.FromArgb(255, 222, 222, 222); // Color.FromArgb(255, 33, 33, 33);
            Visibility = Visibility.Visible;
            
            _isOpen = true;
            OnPropertyChanged("IsOpen");
        }

        public void Close()
        {
            stbSwivelOut.Begin();
            SystemTray.BackgroundColor = Color.FromArgb(255, 255, 255, 255);
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            stbSwivelIn.Completed += (s, ev) =>
            {
                this.txtDetails.Focus();
            };

            stbSwivelOut.Completed += (s, ev) =>
            {
                Visibility = System.Windows.Visibility.Collapsed;
                _isOpen = false;
                this.txtDetails.Text = "";
                OnPropertyChanged("IsOpen");
            };
        }

#endregion

        public AddTaskDialog()
        {
            InitializeComponent();
        }

        private void txtDetails_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoSubmit();
            }
        }

        public void btnAdd_Click(object sender, EventArgs e)
        {
            if (Submit != null)
            {
                DoSubmit();
            }
        }

        public void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
