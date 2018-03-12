namespace DisplayModeSwitch

module DisplayMode =
    open Native

    type Display internal (nativeDevice : DisplayDevice) =
        let hasFlag = nativeDevice.StateFlags.HasFlag
        member __.Name = nativeDevice.DeviceName
        member __.DeviceString = nativeDevice.DeviceString
        member __.ID = nativeDevice.DeviceID
        member __.Key = nativeDevice.DeviceKey
        member __.Active = hasFlag StateFlag.Active
        member __.MirroringDriver = hasFlag StateFlag.MirroringDriver
        member __.ModesPruned = hasFlag StateFlag.DisplayDeviceModesPruned
        member __.PrimaryDevice = hasFlag StateFlag.PrimaryDevice
        member __.Removable = hasFlag StateFlag.Removable
        member __.VgaCompatible = hasFlag StateFlag.VgaCompatible

    let GetDisplayDevices() =
        Seq.unfold
            (fun index ->
                let mutable d = NewDisplayDeviceStruct()
                if EnumDisplayDevices(null, index |> uint32, &d, 0u) then
                    let dev = Display d
                    Some (dev, index + 1)
                else None)
            0