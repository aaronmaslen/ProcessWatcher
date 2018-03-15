namespace DisplayModeSwitch

module DisplayMode =
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

    let GetDisplayDevices() =
        Seq.unfold
            (fun index ->
                let mutable d = NewDisplayDeviceStruct()
                if EnumDisplayDevices(null, index |> uint32, &d, 0u) then
                    Some (Device d, index + 1)
                else None)
            0

    type DisplayMode internal (mode : DevMode) =
        let displayAttributes = 
            mode |> (GetAttributes >> 
                        Seq.collect (fun a ->
                                        match a with
                                        | Display d -> Seq.singleton d
                                        | _ -> Seq.empty))
        member internal __.NativeDevMode = mode
        member __.DeviceName = mode.DeviceName
        member __.Position =
            displayAttributes |>
                (Seq.collect (
                    fun d ->
                        match d with
                        | Position (x, y) -> Seq.singleton (x, y)
                        | _ -> Seq.empty
                    ) >> Seq.tryHead)
        member __.Orientation =
            displayAttributes |>
                (Seq.collect (
                    fun d ->
                        match d with 
                        | DevModeDisplayAttribute.Orientation o -> Seq.singleton o
                        | _ -> Seq.empty
                    ) >> Seq.tryHead)
        member __.FixedOutput =
            displayAttributes |>
                (Seq.collect (
                    fun d ->
                        match d with
                        | FixedOutput f -> Seq.singleton f
                        | _ -> Seq.empty
                    ) >> Seq.tryHead)
        member __.LogPixels =
            displayAttributes |>
                (Seq.collect (
                    fun d ->
                        match d with
                        | LogPixels l -> Seq.singleton l
                        | _ -> Seq.empty
                    ) >> Seq.tryHead)
        member __.BitsPerPixel =
            displayAttributes |>
                (Seq.collect (
                    fun d ->
                        match d with
                        | BitsPerPixel b -> Seq.singleton b
                        | _ -> Seq.empty
                    ) >> Seq.tryHead)
        member __.Resolution =
            let xres =
                displayAttributes |>
                    (Seq.collect (
                        fun d ->
                            match d with
                            | PixelsWidth x -> Seq.singleton x
                            | _ -> Seq.empty
                    ) >> Seq.tryHead)
            let yres =
                displayAttributes |>
                    (Seq.collect (
                        fun d ->
                            match d with
                            | PixelsHeight y -> Seq.singleton y
                            | _ -> Seq.empty
                    ) >> Seq.tryHead)
                    
            xres |>
            Option.bind (
                fun x ->
                    yres |>
                    Option.map (fun y -> (x,y))
                )
        member __.RefreshRate =
            displayAttributes |>
                (Seq.collect (
                    fun d ->
                        match d with
                        | Frequency f -> Seq.singleton f
                        | _ -> Seq.empty
                ) >> Seq.tryHead)
    
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
        if EnumDisplaySettingsEx(dev.NativeDevice.DeviceName, -1 |> uint32 , &m, EnumDisplaySettingsExFlags.None)
        then DisplayMode m |> Some
        else None