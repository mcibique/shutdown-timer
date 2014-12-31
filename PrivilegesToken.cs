// -----------------------------------------------------------------------
//  <copyright file="PrivilegesToken.cs" author="Rimmon">
//      Copyright (c) Rimmon All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace Rimmon.ShutdownTimer
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security;

    public sealed class PrivilegesToken
    {
        [DllImport("advapi32", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        static extern int OpenProcessToken(IntPtr processHandle, int desiredAccess, ref IntPtr tokenHandle);

        [DllImport("kernel32", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int AdjustTokenPrivileges(IntPtr tokenHandle, int disableAllPrivileges, IntPtr newState, int bufferLength, IntPtr previousState, ref int returnLength);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref Luid lpLuid);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Luid
        {
            internal int LowPart;
            internal int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TokenPrivileges
        {
            internal int PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            internal int[] Privileges;
        }

        #region Constants

        private const int SePrivilegeEnabled = 0x00000002;
        private const int TokenAdjustPrivileges = 0x00000020;
        private const int TokenQuery = 0x00000008;

        #endregion

        #region Public Methods

        public static bool SetPrivilege(string lpszPrivilege, bool bEnablePrivilege)
        {
            bool retval = false;
            int ltkpOld = 0;
            IntPtr hToken = IntPtr.Zero;
            var tkp = new TokenPrivileges { Privileges = new int[3] };
            var tLuid = new Luid();
            tkp.PrivilegeCount = 1;

            if (bEnablePrivilege)
            {
                tkp.Privileges[2] = SePrivilegeEnabled;
            }
            else
            {
                tkp.Privileges[2] = 0;
            }

            if (LookupPrivilegeValue(null, lpszPrivilege, ref tLuid))
            {
                Process proc = Process.GetCurrentProcess();
                if (proc.Handle != IntPtr.Zero)
                {
                    if (OpenProcessToken(proc.Handle, TokenAdjustPrivileges | TokenQuery, ref hToken) != 0)
                    {
                        tkp.PrivilegeCount = 1;
                        tkp.Privileges[2] = SePrivilegeEnabled;
                        tkp.Privileges[1] = tLuid.HighPart;
                        tkp.Privileges[0] = tLuid.LowPart;
                        const int bufLength = 256;
                        IntPtr tu = Marshal.AllocHGlobal(bufLength);
                        Marshal.StructureToPtr(tkp, tu, true);
                        if (AdjustTokenPrivileges(hToken, 0, tu, bufLength, IntPtr.Zero, ref ltkpOld) != 0)
                        {
                            if (Marshal.GetLastWin32Error() == 0)
                            {
                                retval = true;
                            }
                        }
                        Marshal.PtrToStructure(tu, typeof(TokenPrivileges));
                        Marshal.FreeHGlobal(tu);
                    }
                }
            }

            if (hToken != IntPtr.Zero)
            {
                CloseHandle(hToken);
            }

            return retval;
        }

        #endregion
    }
}