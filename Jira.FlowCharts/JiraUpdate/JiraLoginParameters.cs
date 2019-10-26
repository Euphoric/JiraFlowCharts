using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Jira.FlowCharts.JiraUpdate
{
    public class JiraLoginParameters
    {
        public string JiraUrl { get; }
        public string JiraUsername { get; }
        public SecureString JiraPassword { get; }

        public JiraLoginParameters(string jiraUrl, string jiraUsername, SecureString jiraPassword)
        {
            JiraUrl = jiraUrl;
            JiraUsername = jiraUsername;
            JiraPassword = jiraPassword;
        }

        public string PasswordAsNakedString()
        {
            return SecureStringToString(JiraPassword);
        }

        private static String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}