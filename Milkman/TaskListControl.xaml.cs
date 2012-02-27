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
using System.Collections;
using IronCow;
using Microsoft.Phone.Controls;
using Milkman.Common;

namespace Milkman
{
    public delegate void MenuItemClickEventHandler(object sender, MenuItemClickEventArgs e);

    public class MenuItemClickEventArgs : EventArgs
    {
        public MenuItem MenuItem { get; set; }

        public MenuItemClickEventArgs(MenuItem menuItem)
            : base()
        {
            this.MenuItem = menuItem;
        }
    }

    public partial class TaskListControl : UserControl
    {
        public delegate void SubmitEventHandler(object sender, SubmitEventArgs e);

        public TaskListControl()
        {
            InitializeComponent();
        }

        #region Properties

        private TaskList _ItemsSource;
        public object ItemsSource
        {
            get
            {
                return _ItemsSource;
            }

            set
            {
                _ItemsSource = (TaskList)value;
            }
        }

        private Task _CurrentTask;
        public Task CurrentTask
        {
            get
            {
                return _CurrentTask;
            }
        }

        public bool IsSelectionEnabled
        {
            get
            {
                return this.lstTaskList.IsSelectionEnabled;
            }

            set
            {
                this.lstTaskList.IsSelectionEnabled = value;
            }
        }

        public IList SelectedItems
        {
            get
            {
                return this.lstTaskList.SelectedItems;
            }
        }

        #endregion

        #region Events

        public event EventHandler<System.Windows.Input.GestureEventArgs> TaskItemTap;
        public event DependencyPropertyChangedEventHandler IsTaskSelectionEnabledChanged;
        public event SelectionChangedEventHandler TaskSelectionChanged;
        public event MenuItemClickEventHandler MenuClick;
        
        #endregion

        #region Event Handlers

        private void ItemContent_Loaded(object sender, EventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            Task task = (Task)target.DataContext;
            if (task.Priority == TaskPriority.One)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 82, 0));
            else if (task.Priority == TaskPriority.Two)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 96, 191));
            else if (task.Priority == TaskPriority.Three)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 53, 154, 255));
            else
                target.Foreground = new SolidColorBrush(Colors.Black);
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement)
            {
                FrameworkElement frameworkElement = (FrameworkElement)e.OriginalSource;
                if (frameworkElement.DataContext is Task)
                {
                    _CurrentTask = (Task)frameworkElement.DataContext;
                }
            }
            base.OnMouseLeftButtonDown(e);
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (TaskItemTap != null)
            {
                TaskItemTap(sender, e);
            }
        }

        private void MultiselectList_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsTaskSelectionEnabledChanged != null)
            {
                IsTaskSelectionEnabledChanged(sender, e);
            }
        }

        private void MultiselectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaskSelectionChanged != null)
            {
                TaskSelectionChanged(sender, e);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MenuClick != null)
            {
                ContextMenu target = (ContextMenu)sender;
                MenuItem parent = (MenuItem)target.Parent;

                MenuClick(this, new MenuItemClickEventArgs(parent));
            }
        }

        #endregion

        #region Members

        public void UpdateDisplay()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (this.DataContext != null)
                {
                    // hide loading display
                    this.txtLoading.Visibility = System.Windows.Visibility.Collapsed;

                    // update items source property
                    this.lstTaskList.ItemsSource = ((TaskList)this.ItemsSource).Tasks;

                    // toggle empty display
                    if (((TaskList)this.ItemsSource).Tasks.Count == 0)
                        this.txtEmpty.Visibility = System.Windows.Visibility.Visible;
                    else
                        this.txtEmpty.Visibility = System.Windows.Visibility.Collapsed;
                }
            });
        }

        #endregion
    }
}
