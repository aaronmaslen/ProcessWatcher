namespace DisplayModeSwitch

module DisplayMode =
    open Native

    type Device internal (nativeDevice : DisplayDevice) =
        let hasFlag = nativeDevice.StateFlags.HasFlag
        member __.Name = nativeDevice.DeviceName
        member __.DeviceString = nativeDevice.DeviceString
        member __.ID = nativeDevice.DeviceID
        member __.Key = nativeDevice.DeviceKey
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
                    let dev = Device d
                    Some (dev, index + 1)
                else None)
            0

    type DisplayMode internal (mode : DevMode) =
        let displayAttributes = 
            mode |> (GetAttributes >> 
                        Seq.collect (fun a ->
                                        match a with
                                        | Display d -> Seq.singleton d
                                        | _ -> Seq.empty))
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

            // Which is easier to read?
            // This?
            xres |>
            Option.bind (
                fun x ->
                    yres |>
                    Option.map (fun y -> (x,y))
                )
            // // Or this?
            // match xres with
            // | Some x ->
            //     match yres with
            //     | Some y -> Some (x, y)
            //     | None -> None
            // | None -> None