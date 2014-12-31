// -----------------------------------------------------------------------
//  <copyright file="MainWindow.xaml.cs" author="Rimmon">
//      Copyright (c) Rimmon All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace Rimmon.ShutdownTimer
{
    using System;
    using System.ComponentModel;
    using System.Management;
    using System.Runtime.CompilerServices;
    using System.Timers;

    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Fields

        private readonly Timer _timer;
        private int _minutes;
        private int _seconds;

        #endregion

        #region Constructors

        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = this;

            this.Minutes = 60;
            this.Seconds = 0;

            this._timer = new Timer(1000) { AutoReset = true };
            this._timer.Elapsed += this.TimerElapsed;
            this._timer.Start();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Properties

        public int Minutes
        {
            get
            {
                return this._minutes;
            }
            set
            {
                this._minutes = value;
                this.OnPropertyChanged();
            }
        }

        public int Seconds
        {
            get
            {
                return this._seconds;
            }
            set
            {
                this._seconds = value;
                this.OnPropertyChanged();
            }
        }

        #endregion

        #region Protected Methods

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this._timer.Dispose();
        }

        #endregion

        #region Private Methods

        private static void OnShutdown()
        {
            if (!PrivilegesToken.SetPrivilege("SeShutdownPrivilege", true))
            {
                return;
            }

            var mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();
            mcWin32.Scope.Options.EnablePrivileges = true;

            ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
            mboShutdownParams["Flags"] = "5";
            mboShutdownParams["Reserved"] = "0";

            foreach (ManagementBaseObject o in mcWin32.GetInstances())
            {
                var manObj = (ManagementObject)o;
                manObj.InvokeMethod("Win32Shutdown", mboShutdownParams, null);
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            var minutes = TimeSpan.FromMinutes(this.Minutes);
            var seconds = minutes.Add(TimeSpan.FromSeconds(this.Seconds));
            var timeToGo = seconds.Subtract(TimeSpan.FromSeconds(1));

            this.Minutes = Math.Max((int)Math.Floor(timeToGo.TotalMinutes), 0);
            this.Seconds = Math.Max(timeToGo.Seconds, 0);

            if (this.Minutes == 0 && this.Seconds == 0)
            {
                this._timer.Stop();
                OnShutdown();
            }
        }

        #endregion
    }
}