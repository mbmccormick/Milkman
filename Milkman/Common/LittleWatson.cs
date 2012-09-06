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
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone.Tasks;
using IronCow.Resources;

namespace Milkman.Common
{
    public class LittleWatson
    {
        const string filename = "LittleWatson.txt";

        internal static void ReportException(Exception ex, string extra)
        {
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    SafeDeleteFile(store);
                    using (TextWriter output = new StreamWriter(store.CreateFile(filename)))
                    {
                        output.WriteLine(extra);
                        output.WriteLine();
                        output.WriteLine(ex.Message);
                        output.WriteLine(ex.StackTrace);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void CheckForPreviousException(bool isFirstRun)
        {
            try
            {
                string contents = null;
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(filename))
                    {
                        using (TextReader reader = new StreamReader(store.OpenFile(filename, FileMode.Open, FileAccess.Read, FileShare.None)))
                        {
                            contents = reader.ReadToEnd();
                        }
                        SafeDeleteFile(store);
                    }
                }

                if (contents != null)
                {
                    string messageText = null;
                    if (isFirstRun)
                        messageText = Strings.UnhandledCrashDialog;
                    else
                        messageText = Strings.UnhandledErrorDialog;

                    if (MessageBox.Show(messageText, Strings.UnhandledErrorDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        EmailComposeTask email = new EmailComposeTask();
                        email.To = "milkman@mbmccormick.com";
                        email.Subject = "Milkman Error Report";
                        email.Body = "Version " + App.VersionNumber + " (" + App.PlatformVersionNumber + ")\n" + contents;

                        SafeDeleteFile(IsolatedStorageFile.GetUserStoreForApplication());

                        email.Show();
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                SafeDeleteFile(IsolatedStorageFile.GetUserStoreForApplication());
            }
        }

        private static void SafeDeleteFile(IsolatedStorageFile store)
        {
            try
            {
                store.DeleteFile(filename);
            }
            catch (Exception)
            {
            }
        }
    }
}
