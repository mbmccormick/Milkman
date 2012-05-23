using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Milkman
{
    public class StringsProvider
    {
        private readonly Strings _resources = new Strings();

        public Strings Resources
        {
            get { return _resources; }
        } 
    }
}
