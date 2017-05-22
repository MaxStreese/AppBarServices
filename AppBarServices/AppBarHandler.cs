﻿// *** Do not forget to add a description. ***

using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;

using AppBarServices.Enums;
using AppBarServices.Structs;

namespace AppBarServices
{
    public class AppBarHandler
    {
        #region Fields
        // To receive notifications from the OS, the AppBar needs to provide a CallbackID.
        private uint _callbackID;
        // The window which the AppBarHandler is handling.
        private Window _windowToHandle;
        // The AppBarType can be either Standard or AutoHide.
        private AppBarType _appBarType;
        // -
        private bool _appBarIsRegistered;
        #endregion


        #region Properties
        // Encapsulates _appBarType.
        public AppBarType AppBarType
        {
            get { return _appBarType; }
            // Ideally the AppBarType can be changed by the caller at any time.
            set { throw new NotImplementedException(); }
        }
        // Encapsulates _callbackID.
        public uint CallbackID
        {
            get { return _callbackID; }
        }
        // Encapsulates _appBarIsRegistered.
        public bool AppBarIsRegistered
        {
            get { return _appBarIsRegistered; }
        }
        #endregion


        #region Constructors
        public AppBarHandler(Window windowToHandle)
        {
            // Sets the CallbackID field to a unique value based on the datetime when the handler is initialized.
            _callbackID = RegisterWindowMessage(String.Format("AppBarHandler_{0}", DateTime.Now.Ticks));
            // -
            _windowToHandle = windowToHandle;
        }
        #endregion


        #region Public Methods
        // Registers the WindowToHandle as an AppBar and places it to the specified position.
        public bool PlaceAppBar(AppBarType appBarType, ScreenEdge screenEdge, double margin)
        {
            if (!_appBarIsRegistered)
            {
                if (appBarType == AppBarType.Standard)
                {
                    if (HandleAppBarNew() == true)
                    {
                        HandleAppBarQueryPosSetPos(screenEdge, margin);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (appBarType == AppBarType.AutoHide)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            return true;
        }

        // Removes the AppBar and sets the WindowToHandle back to its position before it became an AppBar.
        public bool RemoveAppBar()
        {
            if (_appBarIsRegistered)
            {
                HandleAppBarRemove();
            }

            return true;
        }
        #endregion


        #region Private Methods
        // Processes window messages send by the operating system. This is a callback function that requires a hook on the
        // Win32 representation of the _windowToHandle (HwndSource object). This hook is added upon registration of the AppBar
        // and removed upon removal of the AppBar.
        private IntPtr WindowProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return IntPtr.Zero;
        }

        // Registers the AppBar with the operating system (i.e. calls SHAppBarData with the MessageIdentifier ABM_NEW).
        private bool HandleAppBarNew()
        {
            // Setting up the HwndSource object (Win32 representation of a WPF window) based on the _windowToHandle field.
            WindowInteropHelper windowHelper = new WindowInteropHelper(_windowToHandle);
            HwndSource windowSource = HwndSource.FromHwnd(windowHelper.Handle);

            // Specifying the AppBarData to be supplied to the SHAppBarMessage function.
            AppBarData appBarData = new AppBarData();
            appBarData.cbSize = Marshal.SizeOf(appBarData);
            appBarData.hWnd = windowSource.Handle;
            appBarData.uCallbackMessage = _callbackID;

            // Call the SHAppBarMessage function and let it do its magic. If it fails return false.
            if (SHAppBarMessage((int)MessageIdentifier.ABM_NEW, ref appBarData) == 0)
            {
                return false;
            }

            // Since the AppBar is now registered, it must receive certain notifications and handle them via WindowProc.
            // Therefore a hook is added to the HwndSource object of the WPF window.
            windowSource.AddHook(new HwndSourceHook(WindowProc));

            // Reaching this point signals that everything went as expected, therefore set the registered flag to and return true.
            _appBarIsRegistered = true;
            return true;
        }

        // Works out the position of the AppBar and reserves the space (i.e. calls SHAppBarData with the MessageIdentifiers ABM_QUERYPOS and ABM_SETPOS).
        private void HandleAppBarQueryPosSetPos(ScreenEdge screenEdge, double margin)
        {
            // Setting up the HwndSource object (Win32 representation of a WPF window) based on the _windowToHandle field.
            WindowInteropHelper windowHelper = new WindowInteropHelper(_windowToHandle);
            HwndSource windowSource = HwndSource.FromHwnd(windowHelper.Handle);

            // Specifying the AppBarData to be supplied to the SHAppBarMessage function.
            AppBarData appBarData = new AppBarData();
            appBarData.cbSize = Marshal.SizeOf(appBarData);
            appBarData.hWnd = windowSource.Handle;
            appBarData.uEdge = screenEdge;

            // Specify the dimensions of the AppBar based on the 'screenEdge' and 'margin' parameters.
            if (screenEdge == ScreenEdge.Left || screenEdge == ScreenEdge.Right)
            {
                appBarData.rc.top = 0;
                appBarData.rc.bottom = (int)SystemParameters.PrimaryScreenHeight;
                if (screenEdge == ScreenEdge.Left)
                {
                    appBarData.rc.left = 0;
                    appBarData.rc.right = (int)(SystemParameters.PrimaryScreenWidth * margin);
                }
                else
                {
                    appBarData.rc.right = (int)SystemParameters.PrimaryScreenWidth;
                    appBarData.rc.left = (int)(SystemParameters.PrimaryScreenWidth * (1 - margin));
                }
            }
            else
            {
                appBarData.rc.left = 0;
                appBarData.rc.right = (int)SystemParameters.PrimaryScreenWidth;
                if (screenEdge == ScreenEdge.Top)
                {
                    appBarData.rc.top = 0;
                    appBarData.rc.bottom = (int)(SystemParameters.PrimaryScreenHeight * margin);
                }
                else
                {
                    appBarData.rc.bottom = (int)SystemParameters.PrimaryScreenHeight;
                    appBarData.rc.top = (int)(SystemParameters.PrimaryScreenHeight * (1 - margin));
                }
            }

            // Call the SHAppBarMessage function to first query the position where the AppBar should go an then based on the updated
            // appBarData parameter set the position (reserve it).
            SHAppBarMessage((int)MessageIdentifier.ABM_QUERYPOS, ref appBarData);
            SHAppBarMessage((int)MessageIdentifier.ABM_SETPOS, ref appBarData);

            // Move and size the AppBar to fit the bounding rectangle passed to the operating system by the last call to the 
            // SHAppBarMessage function. I guess this wont work because the space I want to move to is reserved?.
            _windowToHandle.Top = appBarData.rc.top;
            _windowToHandle.Left = appBarData.rc.left;
            _windowToHandle.Width = appBarData.rc.right - appBarData.rc.left;
            _windowToHandle.Height = appBarData.rc.bottom - appBarData.rc.top;

            // *** Not finished yet! ***
            throw new NotImplementedException();
        }

        // Removes the AppBar from the operating system.
        private void HandleAppBarRemove()
        {
            // Setting up the HwndSource object (Win32 representation of a WPF window) based on the _windowToHandle field.
            WindowInteropHelper windowHelper = new WindowInteropHelper(_windowToHandle);
            HwndSource windowSource = HwndSource.FromHwnd(windowHelper.Handle);

            // Specifying the AppBarData to be supplied to the SHAppBarMessage function.
            AppBarData appBarData = new AppBarData();
            appBarData.cbSize = Marshal.SizeOf(appBarData);
            appBarData.hWnd = windowSource.Handle;

            // Call the SHAppBarMessage function to remove the AppBar from the operating system.
            SHAppBarMessage((int)MessageIdentifier.ABM_REMOVE, ref appBarData);

            // Since the AppBar is not registered any longer, no messages from the operating system should receive special treatment anymore.
            // Therefore the hook is removed from the HwndSource object of the WPF window.
            windowSource.RemoveHook(new HwndSourceHook(WindowProc));
        }
        #endregion


        #region External Functions (unmanaged code)
        // Sends an AppBar message to the operating system (i.e. does all the actual AppBar stuff, like registering and removing it). 
        [DllImport("SHELL32", CallingConvention = CallingConvention.StdCall)]
        private static extern uint SHAppBarMessage(int dwMessage, ref AppBarData pData);

        // Registers a message value with the operating system, that is guaranteed to be unique throughout the system for a given 'msg' string.
        // This function is needed in order for the AppBar to be able to receive notifications from the operating system.
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string msg);
        #endregion
    }
}