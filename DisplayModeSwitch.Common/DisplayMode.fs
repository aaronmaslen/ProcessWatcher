namespace DisplayModeSwitch

module DisplayMode =
    open System

    open Native

    type Device internal (device : DisplayDevice) =
        let hasFlag = device.StateFlags.HasFlag
        member internal __.NativeDevice = device
        member __.Name = device.DeviceName
        member __.DeviceString = device.DeviceString
        member __.ID = device.DeviceID
        member __.Key = device.DeviceKey
        member __.Active = hasFlag StateFlags.Active
        member __.MirroringDriver = hasFlag StateFlags.MirroringDriver
        member __.ModesPruned = hasFlag StateFlags.DisplayDeviceModesPruned
        member __.PrimaryDevice = hasFlag StateFlags.PrimaryDevice
        member __.Removable = hasFlag StateFlags.Removable
        member __.VgaCompatible = hasFlag StateFlags.VgaCompatible
        override this.ToString() =
            sprintf
                "Name: %s \nDeviceString: %s\nID: %s\nKey: %s"
                this.Name
                this.DeviceString
                this.ID
                this.Key

    let GetDisplayDevices() =
        Seq.unfold
            (fun index ->
                let mutable d = NewDisplayDeviceStruct()
                if EnumDisplayDevices(null, index |> uint32, &d, 0u) then
                    Some (Device d, index + 1)
                else None
            )
            0

    type DisplayMode internal (mode : DevMode) =
        let displayAttributes = 
            mode
            |> GetAttributes
            |> Seq.collect
                    (fun a ->
                        match a with
                        | Display d -> Seq.singleton d
                        | _ -> Seq.empty
                    )
        member internal __.NativeDevMode = mode
        member __.DeviceName = mode.DeviceName
        member __.Position =
            displayAttributes
            |> Seq.collect
                    (fun d ->
                        match d with
                        | Position (x, y) -> Seq.singleton (x, y)
                        | _ -> Seq.empty
                    )
            |> Seq.tryHead
        member __.Orientation =
            displayAttributes
            |> Seq.collect 
                (fun d ->
                    match d with 
                    | DevModeDisplayAttribute.Orientation o -> Seq.singleton o
                    | _ -> Seq.empty
                )
            |> Seq.tryHead
        member __.FixedOutput =
            displayAttributes
            |> Seq.collect
                (fun d ->
                    match d with
                    | FixedOutput f -> Seq.singleton f
                    | _ -> Seq.empty
                )
            |> Seq.tryHead
        member __.LogPixels =
            displayAttributes
            |> Seq.collect
                (fun d ->
                    match d with
                    | LogPixels l -> Seq.singleton l
                    | _ -> Seq.empty
                )
            |> Seq.tryHead
        member __.BitsPerPixel =
            displayAttributes
            |> Seq.collect
                (fun d ->
                    match d with
                    | BitsPerPixel b -> Seq.singleton b
                    | _ -> Seq.empty
                )
            |> Seq.tryHead
        member __.Resolution =
            let xres =
                displayAttributes
                |> Seq.collect
                    (fun d ->
                        match d with
                        | PixelsWidth x -> Seq.singleton x
                        | _ -> Seq.empty
                    ) 
                |> Seq.tryHead
            let yres =
                displayAttributes
                |> Seq.collect
                    (fun d ->
                        match d with
                        | PixelsHeight y -> Seq.singleton y
                        | _ -> Seq.empty
                    )
                |> Seq.tryHead
                    
            xres
            |> Option.bind
                (fun x ->
                    yres
                    |> Option.map (fun y -> (x,y))
                )
        member __.RefreshRate =
            displayAttributes
            |> Seq.collect
                (fun d ->
                    match d with
                    | Frequency f -> Seq.singleton f
                    | _ -> Seq.empty
                )
            |> Seq.tryHead
        override this.ToString() =
            let resolution =
                match this.Resolution with
                | Some (x, y) -> sprintf "%ux%u" x y
                | None -> "Unspecified resolution"
            let refreshRate =
                match this.RefreshRate with
                | Some r -> Some <| sprintf "%uHz" r
                | None -> None
            let scaleMode =
                this.FixedOutput
                |> Option.bind
                    (fun f ->
                        match f with
                        | FixedOutputMode.Stretch -> Some "Stretch"
                        | FixedOutputMode.Center -> Some "Center"
                        | _ -> None
                    )
            let output = sprintf "%s" resolution
            let output = match refreshRate with Some r -> sprintf "%s@%s" output r | None -> output
            let output = match scaleMode with Some s -> sprintf "%s (%s)" output s | None -> output
            
            output
    
    let GetDisplayModes (dev : Device) =
        Seq.unfold
            (fun index ->
                let mutable m = NewDevModeStruct()
                if EnumDisplaySettingsEx
                    (
                        dev.NativeDevice.DeviceName,
                        index |> uint32,
                        &m,
                        EnumDisplaySettingsExFlags.None
                    )
                then Some (DisplayMode m, index + 1)
                else None)
            0
    let GetCurrentDisplayMode (dev : Device) =
        let mutable m = NewDevModeStruct()
        if EnumDisplaySettingsEx
            (
                dev.NativeDevice.DeviceName,
                EnumDisplaySettingsModeNum.CurrentSettings |> uint32,
                &m,
                EnumDisplaySettingsExFlags.None
            )
        then Some <| DisplayMode m
        else None

    let internal setDisplayMode (dev : Device) (mode : DisplayMode) (flags : ChangeDisplayModeFlags) =
        let mutable devMode = mode.NativeDevMode
        ChangeDisplaySettingsEx(dev.NativeDevice.DeviceName, &devMode, IntPtr.Zero, flags, IntPtr.Zero)

    type SetDisplayModeResult =
    | RestartNeeded
    | Failure of uint32

    type SetDisplayModeOptions =
    | Temporary
    | Reset
    | SetPrimary
    | Test

    let SetDisplayMode dev mode (options : SetDisplayModeOptions seq) =
        let flags =
            (ChangeDisplayModeFlags.None, options)
            ||> Seq.fold
                (fun s c ->
                    match c with
                    | Temporary -> ChangeDisplayModeFlags.FullScreen
                    | Reset -> ChangeDisplayModeFlags.Reset
                    | SetPrimary -> ChangeDisplayModeFlags.SetPrimary
                    | Test -> ChangeDisplayModeFlags.Test

                    ||| s
                )
        
        let result = setDisplayMode dev mode flags

        match result with
        | ChangeDisplaySettingsResult.Successful -> Ok()
        | ChangeDisplaySettingsResult.ChangeRestart -> Error <| RestartNeeded
        | _ -> Error <| Failure (result |> uint32)
