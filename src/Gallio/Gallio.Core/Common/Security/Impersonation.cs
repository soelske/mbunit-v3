// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
// Licensed under the Apache License, Version 2.0 (the "License").

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Gallio.Common.Security
{
    /// <summary>
    /// Impersonates a user according to the specified credentials.
    /// .NET 8 replacement: uses ImpersonateLoggedOnUser P/Invoke directly
    /// because WindowsImpersonationContext is removed from the .NET 8 reference assembly.
    /// </summary>
    public class Impersonation : IDisposable
    {
        private bool impersonating;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ImpersonateLoggedOnUser(IntPtr token);

        /// <summary>
        /// Starts the impersonation process.
        /// </summary>
        public Impersonation(string userName, string domain, string password)
        {
            if (userName == null) throw new ArgumentNullException("userName");
            if (domain == null)   throw new ArgumentNullException("domain");
            if (password == null) throw new ArgumentNullException("password");

            Start(userName, domain, password);
        }

        private void Start(string userName, string domain, string password)
        {
            IntPtr token          = IntPtr.Zero;
            IntPtr tokenDuplicate = IntPtr.Zero;

            try
            {
                if (NativeMethods.RevertToSelf()
                    && NativeMethods.LogonUser(userName, domain, password, 2, 0, ref token) != 0
                    && NativeMethods.DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                {
                    if (!ImpersonateLoggedOnUser(tokenDuplicate))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    impersonating = true;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Win32Exception ex)
            {
                throw new ImpersonationException(
                    String.Format("Cannot impersonate the specified user ({0})", ex.Message), ex);
            }
            finally
            {
                if (token          != IntPtr.Zero) NativeMethods.CloseHandle(token);
                if (tokenDuplicate != IntPtr.Zero) NativeMethods.CloseHandle(tokenDuplicate);
            }
        }

        /// <summary>Stops the impersonation process.</summary>
        public void Dispose()
        {
            if (impersonating)
            {
                NativeMethods.RevertToSelf();
                impersonating = false;
            }
        }
    }
}
