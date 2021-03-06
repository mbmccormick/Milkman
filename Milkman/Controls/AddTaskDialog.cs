﻿using IronCow.Resources;
using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Milkman.Controls
{
    public class AddTaskDialog
    {
        public TextBox txtDetails;

        public CustomMessageBox CreateDialog(string defaultText)
        {
            StackPanel stkContent = new StackPanel();

            txtDetails = new TextBox()
            {
                Margin = new Thickness(0, 24, 12, 6),
                InputScope = new InputScope()
                {
                    Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } }
                },
                Text = defaultText
            };
            stkContent.Children.Add(txtDetails);

            WrapPanel wrpShortcuts = new WrapPanel()
            {
                Margin = new Thickness(0, 0, 12, 18),
            };

            TextBlock txtDueDate = new TextBlock()
            {
                Text = "^",
                Width = 56,
                TextAlignment = TextAlignment.Center,
                FontSize = (double)Application.Current.Resources["PhoneFontSizeLarge"]
            };
            txtDueDate.Tap += Shortcut_Tap;
            wrpShortcuts.Children.Add(txtDueDate);

            TextBlock txtPriority = new TextBlock()
            {
                Text = "!",
                Width = 56,
                TextAlignment = TextAlignment.Center,
                FontSize = (double)Application.Current.Resources["PhoneFontSizeLarge"]
            };
            txtPriority.Tap += Shortcut_Tap;
            wrpShortcuts.Children.Add(txtPriority);

            TextBlock txtListTag = new TextBlock()
            {
                Text = "#",
                Width = 56,
                TextAlignment = TextAlignment.Center,
                FontSize = (double)Application.Current.Resources["PhoneFontSizeLarge"]
            };
            txtListTag.Tap += Shortcut_Tap;
            wrpShortcuts.Children.Add(txtListTag);

            TextBlock txtLocation = new TextBlock()
            {
                Text = "@",
                Width = 56,
                TextAlignment = TextAlignment.Center,
                FontSize = (double)Application.Current.Resources["PhoneFontSizeLarge"]
            };
            txtLocation.Tap += Shortcut_Tap;
            wrpShortcuts.Children.Add(txtLocation);

            TextBlock txtRecurrence = new TextBlock()
            {
                Text = "*",
                Width = 56,
                TextAlignment = TextAlignment.Center,
                FontSize = (double)Application.Current.Resources["PhoneFontSizeLarge"]
            };
            txtRecurrence.Tap += Shortcut_Tap;
            wrpShortcuts.Children.Add(txtRecurrence);

            TextBlock txtEstimate = new TextBlock()
            {
                Text = "=",
                Width = 56,
                TextAlignment = TextAlignment.Center,
                FontSize = (double)Application.Current.Resources["PhoneFontSizeLarge"]
            };
            txtEstimate.Tap += Shortcut_Tap;
            wrpShortcuts.Children.Add(txtEstimate);

            stkContent.Children.Add(wrpShortcuts);

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.AddTaskDialogTitle,
                Content = stkContent,
                LeftButtonContent = Strings.AddButton,
                RightButtonContent = Strings.CancelButton,
                IsFullScreen = false
            };

            messageBox.Loaded += (s1, e1) =>
            {
                txtDetails.Focus();
            };

            return messageBox;
        }

        private void Shortcut_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            if (txtDetails.Text.Substring(Math.Max(0, txtDetails.Text.Length - 1)).Equals(" ") == false)
                txtDetails.Text = txtDetails.Text + " " + target.Text;
            else
                txtDetails.Text = txtDetails.Text + target.Text;

            txtDetails.Focus();
            txtDetails.SelectionStart = this.txtDetails.Text.Length;
        }
    }
}
