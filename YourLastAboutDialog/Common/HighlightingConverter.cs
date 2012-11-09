using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace YourLastAboutDialog.Common
{
    /// <summary>
    /// A value converter that converts an array of strings into a nicely formatted list of text blocks
    /// contained in a stack panel. 
    /// </summary>
    public sealed class HighlightingConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the text title style.
        /// </summary>
        /// <value>
        /// The text title style.
        /// </value>
        public Style TextTitleStyle
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text normal style.
        /// </summary>
        /// <value>
        /// The text normal style.
        /// </value>
        public Style TextNormalStyle
        {
            get;
            set;
        }

        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var lines = value as string[];
            if (lines == null)
            {
                return value;
            }

            // the new content
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(12, -15, 12, 0);

            // a flag signaling highlighting
            var highlight = true;

            // format each line
            foreach (var line in lines)
            {
                // check if line has content
                var isEmpty = string.IsNullOrEmpty(line);

                // do we have content at all?
                if (!isEmpty)
                {
                    // create a text block for the line
                    TextBlock tb = new TextBlock
                                       {
                                           TextWrapping = TextWrapping.Wrap,
                                           Text = line,
                                           Style = highlight ? TextTitleStyle : TextNormalStyle,
                                           Margin = highlight ? new Thickness(0, 3, 0, 0) : new Thickness(0),
                                       };

                    // add 
                    sp.Children.Add(tb);
                }

                // signal highlighting to next line 
                // based on whether the current line is empty or not
                highlight = isEmpty;
            }

            return sp;
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object.  This method is called only in <see cref="F:System.Windows.Data.BindingMode.TwoWay"/> bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the source object.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
