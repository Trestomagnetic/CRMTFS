using System;
using System.Runtime.InteropServices;
using System.Security;

namespace CrmPlayground
{
    public static class SecureStringExtension
    {
        public static string ToUnsecuredString(this SecureString secureString)
        {
            if (secureString == null)
                throw new ArgumentNullException("secureString");

            var unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
