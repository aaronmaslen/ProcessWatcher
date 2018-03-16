open System

open DisplayModeSwitch.DisplayMode
open DisplayModeSwitch

[<EntryPoint>]
let main argv =
    for d in (GetDisplayDevices() |> Seq.filter (fun d -> d.Active)) do 
        printfn "%s" <| d.ToString()
        for m in (GetDisplayModes d) do
            let (xres, yres) =
                match m.Resolution with
                | Some (x, y) -> (x, y)
                | None -> (0u, 0u)
            let freq =
                match m.RefreshRate with
                | Some f -> f
                | None -> 0u
            let bpp =
                match m.BitsPerPixel with
                | Some b -> b
                | None -> 0u
            let scaleMode =
                match m.FixedOutput with
                | Some f ->
                    match f with
                    | FixedOutputMode.Default -> "Default"
                    | FixedOutputMode.Center -> "Center"
                    | FixedOutputMode.Stretch -> "Stretch"
                    | _ -> "Unknown"
                | None -> "None"
            let orientation =
                match m.Orientation with
                | Some f ->
                    match f with
                    | DisplayOrientation.Default -> ""
                    | DisplayOrientation.Rotated90 -> "rotated 90 degrees"
                    | DisplayOrientation.Rotated180 -> "rotated 180 degrees"
                    | DisplayOrientation.Rotated270 -> "rotated 270 degrees"
                    | _ -> "unknown rotation"
                | None -> "no rotation"
            printfn "    %ux%u@%uHz (%ubpp, %s) %s" xres yres freq bpp scaleMode orientation
        let m = GetCurrentDisplayMode d
        if m.IsSome then
            let m = m.Value
            let (xres, yres) =
                    match m.Resolution with
                    | Some (x, y) -> (x, y)
                    | None -> (0u, 0u)
            let freq =
                match m.RefreshRate with
                | Some f -> f
                | None -> 0u
            let bpp =
                match m.BitsPerPixel with
                | Some b -> b
                | None -> 0u
            let scaleMode =
                match m.FixedOutput with
                | Some f ->
                    match f with
                    | FixedOutputMode.Default -> "Default"
                    | FixedOutputMode.Center -> "Center"
                    | FixedOutputMode.Stretch -> "Stretch"
                    | _ -> "Unknown"
                | None -> "None"
            printfn "Current mode: %ux%u@%uHz (%ubpp, %s)" xres yres freq bpp scaleMode
    0 // return an integer exit code
