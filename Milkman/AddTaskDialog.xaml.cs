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
using Microsoft.Phone.Controls;
using System.Windows.Controls.Primitives;

namespace Milkman
{
    public partial class AddTaskDialog : UserControl, INotifyPropertyChanged
    {
        private static WeakReference _currentInstance;
        private static readonly double _screenWidth = Application.Current.Host.Content.ActualWidth;
        private static readonly double _screenHeight = Application.Current.Host.Content.ActualHeight;
        private const double _systemTrayHeightInPortrait = 32.0;
        private const double _systemTrayWidthInLandscape = 72.0;
        private static bool _mustRestore = true;
        private Popup _popup;
        private Grid _container;
        private PhoneApplicationFrame _frame;
        private PhoneApplicationPage _page;
        private bool _hasApplicationBar;
        private Color _systemTrayColor;
        private double _systemTrayOpacity;
        public event EventHandler<DismissingEventArgs> Dismissing;
        public event EventHandler<DismissedEventArgs> Dismissed;

        private void OnBackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Dismiss(CustomMessageBoxResult.None, true);
        }

        private void OnNavigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            Dismiss(CustomMessageBoxResult.None, false);
        }

        public void Show()
        {
            if (_popup != null)
            {
                if (_popup.IsOpen)
                {
                    return;
                }
            }

            LayoutUpdated += CustomMessageBox_LayoutUpdated;

            _frame = Application.Current.RootVisual as PhoneApplicationFrame;
            _page = _frame.Content as PhoneApplicationPage;

            // Change the color and opacity of the system tray if necessary.
            if (SystemTray.IsVisible)
            {
                // Cache the original color of the system tray.
                _systemTrayColor = SystemTray.BackgroundColor;

                // Cache the original opacity of the system tray.
                _systemTrayOpacity = SystemTray.Opacity;

                // Change the color of the system tray to match the message box.
                if (Background is SolidColorBrush)
                {
                    SystemTray.BackgroundColor = ((SolidColorBrush)Background).Color;
                }
                else
                {
                    SystemTray.BackgroundColor = (Color)Application.Current.Resources["PhoneChromeColor"];
                }

                // Change the opacity of the system tray to match the message box.
                SystemTray.Opacity = 1.0;
            }

            // Hide the application bar if necessary.
            if (_page.ApplicationBar != null)
            {
                // Cache the original visibility of the system tray.
                _hasApplicationBar = _page.ApplicationBar.IsVisible;

                // Hide it.
                if (_hasApplicationBar)
                {
                    _page.ApplicationBar.IsVisible = false;
                }
            }
            else
            {
                _hasApplicationBar = false;
            }

            // Dismiss the current message box if there is any.
            if (_currentInstance != null)
            {
                _mustRestore = false;

                CustomMessageBox target = _currentInstance.Target as CustomMessageBox;

                if (target != null)
                {
                    target.Dismiss();
                }
            }

            _mustRestore = true;

            // Insert the overlay.
            Rectangle overlay = new Rectangle();
            Color backgroundColor = (Color)Application.Current.Resources["PhoneBackgroundColor"];
            overlay.Fill = new SolidColorBrush(Color.FromArgb(0x99, backgroundColor.R, backgroundColor.G, backgroundColor.B));
            _container = new Grid();
            _container.Children.Add(overlay);

            // Insert the message box.
            _container.Children.Add(this);

            // Create and open the popup.
            _popup = new Popup();
            _popup.Child = _container;
            SetSizeAndOffset();
            _popup.IsOpen = true;
            _currentInstance = new WeakReference(this);

            // Attach event handlers.
            if (_page != null)
            {
                _page.BackKeyPress += OnBackKeyPress;
                _page.OrientationChanged += OnOrientationChanged;
            }

            if (_frame != null)
            {
                _frame.Navigating += OnNavigating;
            }
        }

        public void Dismiss()
        {
            Dismiss(CustomMessageBoxResult.None, true);
        }

        private void Dismiss(CustomMessageBoxResult source, bool useTransition)
        {
            // Handle the dismissing event.
            var handlerDismissing = Dismissing;
            if (handlerDismissing != null)
            {
                DismissingEventArgs args = new DismissingEventArgs(source);
                handlerDismissing(this, args);

                if (args.Cancel)
                {
                    return;
                }
            }

            // Handle the dismissed event.
            var handlerDismissed = Dismissed;
            if (handlerDismissed != null)
            {
                DismissedEventArgs args = new DismissedEventArgs(source);
                handlerDismissed(this, args);
            }

            // Set the current instance to null.
            _currentInstance = null;

            // Cache this variable to avoid a race condition.
            bool restoreOriginalValues = _mustRestore;

            // Close popup.
            if (useTransition)
            {
                SwivelTransition backwardOut = new SwivelTransition { Mode = SwivelTransitionMode.BackwardOut };
                ITransition swivelTransition = backwardOut.GetTransition(this);
                swivelTransition.Completed += (s, e) =>
                {
                    swivelTransition.Stop();
                    ClosePopup(restoreOriginalValues);
                };
                swivelTransition.Begin();
            }
            else
            {
                ClosePopup(restoreOriginalValues);
            }
        }

        private void ClosePopup(bool restoreOriginalValues)
        {
            // Remove the popup.
            _popup.IsOpen = false;
            _popup = null;

            // If there is no other message box displayed.  
            if (restoreOriginalValues)
            {
                // Set the system tray back to its original 
                // color and opacity if necessary.
                if (SystemTray.IsVisible)
                {
                    SystemTray.BackgroundColor = _systemTrayColor;
                    SystemTray.Opacity = _systemTrayOpacity;
                }

                // Bring the application bar if necessary.
                if (_hasApplicationBar)
                {
                    _hasApplicationBar = false;
                    _page.ApplicationBar.IsVisible = true;
                }
            }

            // Dettach event handlers.
            if (_page != null)
            {
                _page.BackKeyPress -= OnBackKeyPress;
                _page.OrientationChanged -= OnOrientationChanged;
                _page = null;
            }

            if (_frame != null)
            {
                _frame.Navigating -= OnNavigating;
                _frame = null;
            }
        }

        private void CustomMessageBox_LayoutUpdated(object sender, EventArgs e)
        {
            SwivelTransition backwardIn = new SwivelTransition { Mode = SwivelTransitionMode.BackwardIn };
            ITransition swivelTransition = backwardIn.GetTransition(this);
            swivelTransition.Completed += (s1, e1) => swivelTransition.Stop();
            swivelTransition.Begin();
            LayoutUpdated -= CustomMessageBox_LayoutUpdated;
        }

        private void OnOrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            SetSizeAndOffset();
        }

        private void SetSizeAndOffset()
        {
            // Set the size the container.
            Rect client = GetTransformedRect();
            if (_container != null)
            {
                _container.RenderTransform = GetTransform();

                _container.Width = client.Width;
                _container.Height = client.Height;
            }

            // Set the vertical and horizontal offset of the popup.
            if (SystemTray.IsVisible && _popup != null)
            {
                PageOrientation orientation = GetPageOrientation();

                switch (orientation)
                {
                    case PageOrientation.PortraitUp:
                        _popup.HorizontalOffset = 0.0;
                        _popup.VerticalOffset = _systemTrayHeightInPortrait;
                        _container.Height -= _systemTrayHeightInPortrait;
                        break;
                    case PageOrientation.LandscapeLeft:
                        _popup.HorizontalOffset = 0.0;
                        _popup.VerticalOffset = _systemTrayWidthInLandscape;
                        break;
                    case PageOrientation.LandscapeRight:
                        _popup.HorizontalOffset = 0.0;
                        _popup.VerticalOffset = 0.0;
                        break;
                }
            }
        }

        private static Rect GetTransformedRect()
        {
            bool isLandscape = IsLandscape(GetPageOrientation());

            return new Rect(0, 0,
                isLandscape ? _screenHeight : _screenWidth,
                isLandscape ? _screenWidth : _screenHeight);
        }

        private static bool IsLandscape(PageOrientation orientation)
        {
            return (orientation == PageOrientation.Landscape) || (orientation == PageOrientation.LandscapeLeft) || (orientation == PageOrientation.LandscapeRight);
        }

        private static Transform GetTransform()
        {
            PageOrientation orientation = GetPageOrientation();

            switch (orientation)
            {
                case PageOrientation.LandscapeLeft:
                case PageOrientation.Landscape:
                    return new CompositeTransform() { Rotation = 90, TranslateX = _screenWidth };
                case PageOrientation.LandscapeRight:
                    return new CompositeTransform() { Rotation = -90, TranslateY = _screenHeight };
                default:
                    return null;
            }
        }

        private static PageOrientation GetPageOrientation()
        {
            PhoneApplicationFrame frame = Application.Current.RootVisual as PhoneApplicationFrame;

            if (frame != null)
            {
                PhoneApplicationPage page = frame.Content as PhoneApplicationPage;

                if (page != null)
                {
                    return page.Orientation;
                }
            }

            return PageOrientation.None;
        }

        private void txtDetails_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Dismiss(CustomMessageBoxResult.LeftButton, true);
            }
        }

        private void Shortcut_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            this.txtDetails.Text = this.txtDetails.Text + " " + target.Text;

            this.txtDetails.Focus();
            this.txtDetails.SelectionStart = this.txtDetails.Text.Length;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Dismiss(CustomMessageBoxResult.LeftButton, true);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Dismiss(CustomMessageBoxResult.RightButton, true);
        }

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
    }

    public class SubmitEventArgs : EventArgs
    {
        public string Text { get; set; }

        public SubmitEventArgs(string text)
            : base()
        {
            Text = text;
        }
    }
}
