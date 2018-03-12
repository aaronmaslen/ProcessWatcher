namespace DisplayModeSwitch

#nowarn "9"

module internal Native =
    open System
    open System.Runtime.InteropServices

    [<Flags>]
    type StateFlag =
    | Active                    = 0x0000001u
    | MultiDriver               = 0x0000002u
    | PrimaryDevice             = 0x0000004u
    | MirroringDriver           = 0x0000008u
    | VgaCompatible             = 0x0000010u
    | Removable                 = 0x0000020u
    | Disconnected              = 0x2000000u
    | Remote                    = 0x4000000u
    | DisplayDeviceModesPruned  = 0x8000000u

    [<Struct; StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)>]
    type DisplayDevice =
        [<MarshalAs(UnmanagedType.U4)>]
        val mutable cb : uint32

        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)>]
        val mutable DeviceName : string

        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)>]
        val mutable DeviceString : string

        [<MarshalAs(UnmanagedType.U4)>]
        val mutable StateFlags : StateFlag

        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)>]
        val mutable DeviceID : string

        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)>]
        val mutable DeviceKey : string

    let NewDisplayDeviceStruct() =
        let mutable d = new DisplayDevice()
        d.cb <- Marshal.SizeOf(d) |> uint32
        d

    [<DllImport(@"user32.dll", EntryPoint = @"EnumDisplayDevicesW")>]
    extern bool EnumDisplayDevices(
        string lpDevice,
        uint32 iDevNum,
        DisplayDevice& lpDisplayDevice,
        uint32 dwFlags
    )

    type DisplayOrientation =
    | Default       = 0x0u
    | Rotated90     = 0x1u
    | Rotated180    = 0x2u
    | Rotated270    = 0x4u

    type FixedOutputMode =
    | Default = 0x0u
    | Stretch = 0x1u
    | Center  = 0x2u

    [<Flags>]
    type DisplayFlags =
    | Grayscale  = 0x1
    | Interlaced = 0x2

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type DisplayDeviceModeFields =
        val mutable PositionX : int32
        val mutable PositionY : int32
        val mutable Orientation : uint32
        val mutable FixedOutput : uint32
    
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type PrintDeviceModeFields =
        val mutable Orientation : uint16
        val mutable PaperSize : uint16
        val mutable PaperLength : uint16
        val mutable PaperWidth : uint16
        val mutable Scale : uint16
        val mutable Copies : uint16
        val mutable DefaultSource : uint16
        val mutable PrintQuality : uint16
    
    [<Struct; StructLayout(LayoutKind.Explicit)>]
    type DeviceAttributesUnion =
        [<FieldOffset(0)>] val mutable Display : DisplayDeviceModeFields
        [<FieldOffset(0)>] val mutable Paper : PrintDeviceModeFields

    [<Struct; StructLayout(LayoutKind.Explicit)>]
    type DeviceFlagsUnion =
        [<FieldOffset(0)>] val mutable DisplayFlags : DisplayFlags
        [<FieldOffset(0)>] val mutable Nup : uint32

    [<Struct; StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)>]
    type DevMode =
        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)>] val mutable DeviceName : string
        val mutable SpecVersion : uint16
        val mutable DriverVersion : uint16
        val mutable Size : uint16
        val mutable DriverExtra : uint16
        val mutable Fields : uint32
        val mutable AttributesUnion : DeviceAttributesUnion
        val mutable Color : int16
        val mutable Duplex : int16
        val mutable YResolution : int16
        val mutable TTOption : int16
        val mutable Collate : int16
        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)>] val mutable FormName : string
        val mutable LogPixels : uint16
        val mutable BitsPerPixel : uint32
        val mutable PelsWidth : uint32
        val mutable PelsHeight : uint32
        val mutable FlagsUnion : DeviceFlagsUnion
        val mutable DisplayFrequency : uint32
        val mutable ICMMethod : uint32
        val mutable ICMIntent : uint32
        val mutable MediaType : uint32
        val mutable DitherType : uint32
        val mutable Reserved1 : uint32
        val mutable Reserved2 : uint32
        val mutable PanningWidth : uint32
        val mutable PanningHeight : uint32

    let NewDevModeStruct() =
        let mutable s = new DevMode()
        s.Size <- Marshal.SizeOf(s) |> uint16
        s

    [<DllImport(@"user32.dll", EntryPoint = "EnumDisplaySettingsExW")>]
    extern bool EnumDisplaySettingsEx(
        string lpszDeviceName,
        uint32 iModeNum,
        DevMode& lpDevMode,
        uint32 dwFlags
    )

    type ChangeDisplaySettingsResult =
    | Successful        = 0
    | ChangeRestart     = 1
    | ChangeFailed      = -1
    | BadMode           = -2
    | ChangeNotUpdated  = -3
    | BadFlags          = -4
    | BadParam          = -5
    | BadDualView       = -6

    [<Flags>]
    type ChangeDisplayModeFlags =
    | UpdateRegistry    = 0x00000001u
    | Test              = 0x00000002u
    | FullScreen        = 0x00000004u
    | Global            = 0x00000008u
    | SetPrimary        = 0x00000010u
    | NoReset           = 0x10000000u
    | Reset             = 0x40000000u
    
    [<DllImport(@"user32.dll", EntryPoint = "ChangeDisplaySettingsExW")>]
    extern ChangeDisplaySettingsResult ChangeDisplaySettingsEx(
        string lpszDeviceName,
        DevMode& lpDevMode,
        IntPtr hwnd,
        ChangeDisplayModeFlags flags,
        IntPtr lParam
    )