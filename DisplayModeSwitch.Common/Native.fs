namespace DisplayModeSwitch
#nowarn "9"

open System

type DisplayOrientation =
| Default       = 0x0u
| Rotated90     = 0x1u
| Rotated180    = 0x2u
| Rotated270    = 0x4u

type FixedOutputMode =
| Default = 0u
| Stretch = 1u
| Center  = 2u

[<Flags>]
type DisplayFlags =
| Grayscale  = 0x1
| Interlaced = 0x2

type PrintColor =
| Monochrome    = 1s
| Color         = 2s

module Native =
    open System.Runtime.InteropServices

    [<Flags>]
    type StateFlags =
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
        val mutable StateFlags : StateFlags

        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)>]
        val mutable DeviceID : string

        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)>]
        val mutable DeviceKey : string

    let NewDisplayDeviceStruct() =
        let mutable d = new DisplayDevice()
        d.cb <- uint32 <| Marshal.SizeOf(d)
        d

    [<DllImport(@"user32.dll", EntryPoint = @"EnumDisplayDevicesW", CharSet = CharSet.Unicode)>]
    extern bool EnumDisplayDevices(
        string lpDevice,
        uint32 iDevNum,
        DisplayDevice& lpDisplayDevice,
        uint32 dwFlags
    )

    [<Flags>]
    type DevModeFieldFlags =
    | PaperOrientation      = 0x00000001u
    | PaperSize             = 0x00000002u
    | PaperLength           = 0x00000004u
    | PaperWidth            = 0x00000008u
    | Scale                 = 0x00000010u
    | Position              = 0x00000020u
    | Nup                   = 0x00000040u
    | DisplayOrientation    = 0x00000080u
    | Copies                = 0x00000100u
    | DefaultSource         = 0x00000200u
    | PrintQuality          = 0x00000400u
    | PrintColor            = 0x00000800u
    | Duplex                = 0x00001000u
    | YResolution           = 0x00002000u
    | TTOption              = 0x00004000u
    | Collate               = 0x00008000u
    | FormName              = 0x00010000u
    | LogPixels             = 0x00020000u
    | BitsPerPixel          = 0x00040000u
    | PixelsWidth           = 0x00080000u
    | PixelsHeight          = 0x00100000u
    | DisplayFlags          = 0x00200000u
    | DisplayFrequency      = 0x00400000u
    | IcmMethod             = 0x00800000u
    | IcmIntent             = 0x01000000u
    | MediaType             = 0x02000000u
    | DitherType            = 0x04000000u
    | PanningWidth          = 0x08000000u
    | PanningHeight         = 0x10000000u
    | DisplayFixedOutput    = 0x20000000u


    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type DisplayDeviceModeFields =
        val mutable PositionX : int32
        val mutable PositionY : int32
        val mutable Orientation : DisplayOrientation
        val mutable FixedOutput : FixedOutputMode
    
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type PrintDeviceModeFields =
        val mutable Orientation : int16
        val mutable PaperSize : int16
        val mutable PaperLength : int16
        val mutable PaperWidth : int16
        val mutable Scale : int16
        val mutable Copies : int16
        val mutable DefaultSource : int16
        val mutable PrintQuality : int16
    
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
        val mutable Fields : DevModeFieldFlags
        val mutable AttributesUnion : DeviceAttributesUnion
        val mutable Color : PrintColor
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

    
    type DevModeDisplayAttribute =
    | Position of int32 * int32
    | Orientation of DisplayOrientation
    | FixedOutput of FixedOutputMode
    | LogPixels of uint16
    | BitsPerPixel of uint32
    | PixelsWidth of uint32
    | PixelsHeight of uint32
    | DisplayFlags of DisplayFlags
    | Frequency of uint32

    type DevModePrintAttribute =
    | Orientation of int16
    | PaperSize of int16
    | PaperLength of int16
    | PaperWidth of int16
    | Scale of int16
    | Copies of int16
    | DefaultSource of int16
    | PrintQuality of int16
    | Nup of uint32
    // | Color of PrintColor

    type DevModeAttributes =
    | Display of DevModeDisplayAttribute
    | Print of DevModePrintAttribute

    let GetAttributes (devMode : DevMode) =
        let values = (Enum.GetValues typeof<DevModeFieldFlags>) :?> DevModeFieldFlags[]
        values |> Seq.collect
            (fun v ->
                let paper = devMode.AttributesUnion.Paper
                let display = devMode.AttributesUnion.Display
                if (not << devMode.Fields.HasFlag) v then Seq.empty
                else
                    match v with
                    | DevModeFieldFlags.PaperOrientation -> 
                        (Seq.singleton << Print << Orientation) paper.Orientation
                    | DevModeFieldFlags.PaperSize ->
                        (Seq.singleton << Print << PaperSize) paper.PaperSize
                    | DevModeFieldFlags.PaperLength ->
                        (Seq.singleton << Print << PaperLength) paper.PaperLength
                    | DevModeFieldFlags.PaperWidth ->
                        (Seq.singleton << Print << PaperWidth) paper.PaperWidth
                    | DevModeFieldFlags.Scale ->
                        (Seq.singleton << Print << Scale) paper.Scale
                    | DevModeFieldFlags.Position ->
                        (Seq.singleton << Display << Position) (display.PositionX, display.PositionY)
                    | DevModeFieldFlags.Nup ->
                        (Seq.singleton << Print << Nup) devMode.FlagsUnion.Nup
                    | DevModeFieldFlags.DisplayOrientation ->
                        (Seq.singleton << Display << DevModeDisplayAttribute.Orientation) display.Orientation
                    | DevModeFieldFlags.Copies ->
                        (Seq.singleton << Print << Copies) paper.Copies
                    | DevModeFieldFlags.DefaultSource ->
                        (Seq.singleton << Print << DefaultSource) paper.DefaultSource
                    | DevModeFieldFlags.PrintQuality ->
                        (Seq.singleton << Print << PrintQuality) paper.PrintQuality
                    // | DevModeFieldFlags.PrintColor ->
                    //     (Some << Print << Color) devMode.Color
                    // | DevModeFieldFlags.Duplex ->
                    //     (Some << Print << Duplex) paper.Duplex
                    // | DevModeFieldFlags.YResolution ->
                    //     (Some << Print << YResolution) devMode.YResolution
                    // | DevModeFieldFlags.TTOption ->
                    //     (Some << Print << TTOption) devMode.TTOption
                    // | DevModeFieldFlags.Collate ->
                    //     (Some << Print << Collate) devMode.Collate
                    // | DevModeFieldFlags.FormName ->
                    //     (Some << Print << FormName) devMode.FormName
                    | DevModeFieldFlags.LogPixels ->
                        (Seq.singleton << Display << LogPixels) devMode.LogPixels
                    | DevModeFieldFlags.BitsPerPixel ->
                        (Seq.singleton << Display << BitsPerPixel) devMode.BitsPerPixel
                    | DevModeFieldFlags.PixelsWidth ->
                        (Seq.singleton << Display << PixelsWidth) devMode.PelsWidth
                    | DevModeFieldFlags.PixelsHeight ->
                        (Seq.singleton << Display << PixelsHeight) devMode.PelsHeight
                    | DevModeFieldFlags.DisplayFlags ->
                        (Seq.singleton << Display << DisplayFlags) devMode.FlagsUnion.DisplayFlags
                    | DevModeFieldFlags.DisplayFrequency ->
                        (Seq.singleton << Display << Frequency) devMode.DisplayFrequency
                    // | DevModeFieldFlags.IcmMethod ->
                    //     (Some << Print << IcmMethod) devMode.ICMMethod
                    // | DevModeFieldFlags.IcmIntent ->
                    //     (Some << Print << IcmIntent) devMode.ICMIntent
                    // | DevModeFieldFlags.MediaType ->
                    //     (Some << Print << MediaType) devMode.MediaType
                    // | DevModeFieldFlags.DitherType ->
                    //     (Some << Print << DitherType) devMode.DitherType
                    | DevModeFieldFlags.DisplayFixedOutput ->
                        (Seq.singleton << Display << FixedOutput) display.FixedOutput
                    | _ -> Seq.empty)

    let NewDevModeStruct() =
        let mutable s = new DevMode()
        s.Size <- uint16 <| Marshal.SizeOf(s)
        s

    type EnumDisplaySettingsModeNum =
    | CurrentSettings       = 0xffffffffu
    | RegistrySettings      = 0xfffffffeu

    type EnumDisplaySettingsExFlags =
    | None          = 0x0u
    | RawMode       = 0x2u
    | RotatedMode   = 0x4u

    [<DllImport(@"user32.dll", EntryPoint = "EnumDisplaySettingsExW", CharSet = CharSet.Unicode)>]
    extern bool EnumDisplaySettingsEx(
        string lpszDeviceName,
        uint32 iModeNum,
        DevMode& lpDevMode,
        EnumDisplaySettingsExFlags dwFlags
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
    | None              = 0x00000000u
    | UpdateRegistry    = 0x00000001u
    | Test              = 0x00000002u
    | FullScreen        = 0x00000004u
    | Global            = 0x00000008u
    | SetPrimary        = 0x00000010u
    | NoReset           = 0x10000000u
    | Reset             = 0x40000000u
    
    [<DllImport(@"user32.dll", EntryPoint = "ChangeDisplaySettingsExW", CharSet = CharSet.Unicode)>]
    extern ChangeDisplaySettingsResult ChangeDisplaySettingsEx(
        string lpszDeviceName,
        DevMode& lpDevMode,
        IntPtr hwnd,
        ChangeDisplayModeFlags flags,
        IntPtr lParam
    )