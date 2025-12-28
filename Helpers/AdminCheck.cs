using System;
using System.Security.Principal;

namespace TemperatureMonitor
{
    public static class AdminCheck
    {
        public static bool IsRunAsAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
