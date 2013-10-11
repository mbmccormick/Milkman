using IronCow.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System;
using System.IO;
using System.IO.IsolatedStorage;

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
                    string messageBoxText = null;
                    if (isFirstRun)
                        messageBoxText = Strings.UnhandledCrashDialog;
                    else
                        messageBoxText = Strings.UnhandledErrorDialog;

                    CustomMessageBox messageBox = new CustomMessageBox()
                    {
                        Caption = Strings.UnhandledErrorDialogTitle,
                        Message = messageBoxText,
                        LeftButtonContent = Strings.YesLower,
                        RightButtonContent = Strings.NoLower,
                        IsFullScreen = false
                    };

                    messageBox.Dismissed += (s1, e1) =>
                    {
                        switch (e1.Result)
                        {
                            case CustomMessageBoxResult.LeftButton:
                                EmailComposeTask email = new EmailComposeTask();
                                email.To = "feedback@mbmccormick.com";
                                email.Subject = "Milkman Error Report";
                                email.Body = "Version " + App.ExtendedVersionNumber + " (" + App.PlatformVersionNumber + ")\n" + contents;

                                SafeDeleteFile(IsolatedStorageFile.GetUserStoreForApplication());

                                email.Show();

                                break;
                            default:
                                break;
                        }
                    };

                    messageBox.Show();
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
