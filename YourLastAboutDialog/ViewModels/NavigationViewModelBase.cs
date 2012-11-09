using System.Windows.Navigation;

namespace YourLastAboutDialog.ViewModels
{
    /// <summary>
    /// A view model base class that abstracts the navigation features of Silverlight's page model.
    /// </summary>
    public class NavigationViewModelBase : ViewModelBase
    {
        /// <summary>
        /// Raises the <see cref="E:NavigatingFrom"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigatingCancelEventArgs"/> instance containing the event data.</param>
        protected virtual void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
        }

        /// <summary>
        /// Raises the <see cref="E:NavigatedFrom"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigationEventArgs"/> instance containing the event data.</param>
        protected virtual void OnNavigatedFrom(NavigationEventArgs e)
        {
        }

        /// <summary>
        /// Raises the <see cref="E:NavigatedTo"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigationEventArgs"/> instance containing the event data.</param>
        protected virtual void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        /// <summary>
        /// The internal navigating from method.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigatingCancelEventArgs"/> instance containing the event data.</param>
        internal void InternalNavigatingFrom(NavigatingCancelEventArgs e)
        {
            OnNavigatingFrom(e);
        }

        /// <summary>
        /// The internal navigated from method.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigationEventArgs"/> instance containing the event data.</param>
        internal void InternalNavigatedFrom(NavigationEventArgs e)
        {
            OnNavigatedFrom(e);
        }

        /// <summary>
        /// The internal navigated to method.
        /// </summary>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigationEventArgs"/> instance containing the event data.</param>
        internal void InternalNavigatedTo(NavigationEventArgs e)
        {
            OnNavigatedTo(e);
        }

        /// <summary>
        /// Gets or sets the current navigation context.
        /// </summary>
        public NavigationContext NavigationContext
        {
            get;
            set;
        }
    }
}
