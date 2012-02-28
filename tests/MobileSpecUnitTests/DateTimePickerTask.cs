/*
 * Copyright (c) 2012, Sergey Grebnov
 * 
 * Implementation is based on DateTimePickerBase
 * http://silverlight.codeplex.com/SourceControl/changeset/view/71382#1510211
 * 
 * (c) Copyright Microsoft Corporation.
 * This source is subject to the Microsoft Public License (Ms-PL).
 * Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
 * All other rights reserved.
 * 
 */

using System;
using System.IO;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Controls.Primitives;
using System.Reflection;
using System.Windows.Navigation; // Silverlight for Windows Phone Toolkit

namespace PhoneGap.Extension.Commands
{
    /// <summary>
    /// Allows an application to launch the Audio Recording application. 
    /// Use this to allow users to record audio from your application.
    /// </summary>
    public class DateTimePickerTask
    {
        /// <summary>
        /// Represents date/time information returned from a call to the Show method of
        /// a PhoneGap.Extension.Commands.DateTimePickerTask object
        /// </summary>
        public class DateTimeResult : TaskEventArgs
        {
            /// <summary>
            /// Initializes a new instance of the DateTimeResult class.
            /// </summary>
            public DateTimeResult()
            { }

            /// <summary>
            /// Initializes a new instance of the DateTimeResult class
            /// with the specified Microsoft.Phone.Tasks.TaskResult.
            /// </summary>
            /// <param name="taskResult">Associated Microsoft.Phone.Tasks.TaskResult</param>
            public DateTimeResult(TaskResult taskResult)
                : base(taskResult)
            { }

            /// <summary>
            ///  Gets the datetime value selected
            /// </summary>
            public DateTime? Value { get; internal set; }

        }

        /// <summary>
        /// Occurs when a audio recording task is completed.
        /// </summary>
        public event EventHandler<DateTimeResult> Completed;

        #region Internal fields

        private PhoneApplicationFrame _frame;
        private object _frameContentWhenOpened;
        private NavigationInTransition _savedNavigationInTransition;
        private NavigationOutTransition _savedNavigationOutTransition;
        private IDateTimePickerPage _dateTimePickerPage;

        private Uri PickerPageUri = new System.Uri("/Microsoft.Phone.Controls.Toolkit;component/DateTimePickers/DatePickerPage.xaml?dummy=" + Guid.NewGuid().ToString(), UriKind.Relative);

        #endregion

        #region Pulbic properties
        
        public DateTime? Value {get; set;}

        #endregion

        /// <summary>
        /// Shows Audio Recording application
        /// </summary>
        public void Show()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var root = Application.Current.RootVisual as PhoneApplicationFrame;

                //root.Navigated += new System.Windows.Navigation.NavigatedEventHandler(NavigationService_Navigated);
                //MethodInfo dynMethod = this.GetType().GetMethod("OpenPickerPage", BindingFlags.NonPublic | BindingFlags.Instance);
                //dynMethod.Invoke(this, null);

                OpenPickerPage();
                // dummy parameter is used to always open a fresh version
                //root.Navigate(new System.Uri("/Microsoft.Phone.Controls.Toolkit;component/DateTimePickers/DatePickerPage.xaml?dummy=" + Guid.NewGuid().ToString(), UriKind.Relative));
            });
        }

        /// <summary>
        /// Opens the page to select the date or time value
        /// </summary>
        private void OpenPickerPage()
        {
            if (null == PickerPageUri)
            {
                throw new ArgumentException("PickerPageUri property must not be null.");
            }

            if (null == _frame)
            {
                // Hook up to necessary events and navigate
                _frame = Application.Current.RootVisual as PhoneApplicationFrame;
                if (null != _frame)
                {
                    _frameContentWhenOpened = _frame.Content;

                    // Save and clear host page transitions for the upcoming "popup" navigation
                    UIElement frameContentWhenOpenedAsUIElement = _frameContentWhenOpened as UIElement;
                    if (null != frameContentWhenOpenedAsUIElement)
                    {
                        _savedNavigationInTransition = TransitionService.GetNavigationInTransition(frameContentWhenOpenedAsUIElement);
                        TransitionService.SetNavigationInTransition(frameContentWhenOpenedAsUIElement, null);
                        _savedNavigationOutTransition = TransitionService.GetNavigationOutTransition(frameContentWhenOpenedAsUIElement);
                        TransitionService.SetNavigationOutTransition(frameContentWhenOpenedAsUIElement, null);
                    }

                    _frame.Navigated += OnFrameNavigated;
                    _frame.NavigationStopped += OnFrameNavigationStoppedOrFailed;
                    _frame.NavigationFailed += OnFrameNavigationStoppedOrFailed;

                    _frame.Navigate(PickerPageUri);
                }
            }

        }

        /// <summary>
        /// Closes tdate/time selection page
        /// </summary>
        private void ClosePickerPage()
        {
            // Unhook from events
            if (null != _frame)
            {
                _frame.Navigated -= OnFrameNavigated;
                _frame.NavigationStopped -= OnFrameNavigationStoppedOrFailed;
                _frame.NavigationFailed -= OnFrameNavigationStoppedOrFailed;

                // Restore host page transitions for the completed "popup" navigation
                UIElement frameContentWhenOpenedAsUIElement = _frameContentWhenOpened as UIElement;
                if (null != frameContentWhenOpenedAsUIElement)
                {
                    TransitionService.SetNavigationInTransition(frameContentWhenOpenedAsUIElement, _savedNavigationInTransition);
                    _savedNavigationInTransition = null;
                    TransitionService.SetNavigationOutTransition(frameContentWhenOpenedAsUIElement, _savedNavigationOutTransition);
                    _savedNavigationOutTransition = null;
                }

                _frame = null;
                _frameContentWhenOpened = null;
            }

            // default result
            DateTimeResult taskResult = new DateTimeResult(TaskResult.Cancel);

            // Commit the value if available
            if (null != _dateTimePickerPage)
            {
                if (_dateTimePickerPage.Value.HasValue)
                {
                    Value = _dateTimePickerPage.Value.Value;

                    taskResult = new DateTimeResult(TaskResult.OK);
                    taskResult.Value = Value.Value;
                }
                _dateTimePickerPage = null;
            }

            this.Completed(this, taskResult);
        }

        /// <summary>
        /// Controls page transitions: implements the way to initialize and then dispose picker page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            if (e.Content == _frameContentWhenOpened)
            {
                // Navigation to original page; close the picker page
                ClosePickerPage();
            }
            else if (null == _dateTimePickerPage)
            {
                // Navigation to a new page; capture it and push the value in
                _dateTimePickerPage = e.Content as IDateTimePickerPage;
                if (null != _dateTimePickerPage)
                {
                    _dateTimePickerPage.Value = Value.GetValueOrDefault(DateTime.Now);
                }
            }
        }

        /// <summary>
        /// Abnormal pages transition handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFrameNavigationStoppedOrFailed(object sender, EventArgs e)
        {
            // Abort
            ClosePickerPage();
        }
        
    }
}
