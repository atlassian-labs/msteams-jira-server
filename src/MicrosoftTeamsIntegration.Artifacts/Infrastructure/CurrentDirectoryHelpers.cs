using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
#pragma warning disable

// ReSharper disable All
namespace MicrosoftTeamsIntegration.Artifacts.Infrastructure
{
    // Workaround for base dir when hosting in-process https://github.com/aspnet/Docs/pull/9873
    [PublicAPI]
    [SuppressMessage("ReSharper", "SA1300", Justification = "External code")]
    [SuppressMessage("ReSharper", "SA1202", Justification = "External code")]
    [SuppressMessage("ReSharper", "SA1307", Justification = "External code")]
    public static class CurrentDirectoryHelpers
    {
        private const string AspNetCoreModuleDll = "aspnetcorev2_inprocess.dll";

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_application_properties(ref IISConfigurationData iiConfigData);

        public static void SetCurrentDirectory()
        {
            try
            {
                // Check if physical path was provided by ANCM
                var sitePhysicalPath = Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH");
                if (string.IsNullOrEmpty(sitePhysicalPath))
                {
                    // Skip if not running ANCM InProcess
                    if (GetModuleHandle(AspNetCoreModuleDll) == IntPtr.Zero)
                    {
                        return;
                    }

                    IISConfigurationData configurationData = default;
                    if (http_get_application_properties(ref configurationData) != 0)
                    {
                        return;
                    }

                    sitePhysicalPath = configurationData.pwzFullApplicationPath;
                }

                Environment.CurrentDirectory = sitePhysicalPath;
            }
            catch
            {
                // ignore
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IISConfigurationData
        {
            public readonly IntPtr pNativeApplication;
            [MarshalAs(UnmanagedType.BStr)]
            public readonly string pwzFullApplicationPath;
            [MarshalAs(UnmanagedType.BStr)]
            public readonly string pwzVirtualApplicationPath;
            public readonly bool fWindowsAuthEnabled;
            public readonly bool fBasicAuthEnabled;
            public readonly bool fAnonymousAuthEnable;
        }
    }
}
#pragma warning restore
