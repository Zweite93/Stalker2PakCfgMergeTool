using System.Runtime.InteropServices;

namespace Stalker2PakCfgMergeTool;

public class AesKeyGetter
{
    [DllImport("AESDumpster.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetAesKey([MarshalAs(UnmanagedType.LPWStr)] string exePath);

    public static string? Get(string exePath)
    {
        var keysPtr = GetAesKey(exePath);
        return keysPtr == IntPtr.Zero ? null : Marshal.PtrToStringAuto(keysPtr)?.Split('\n').FirstOrDefault();
    }
}