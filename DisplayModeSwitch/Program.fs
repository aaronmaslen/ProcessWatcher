open System

open DisplayModeSwitch.DisplayMode

[<EntryPoint>]
let main argv =
    for d in (GetDisplayDevices() |> Seq.filter (fun d -> d.Active)) do 
        printfn "%s %s%s" d.DeviceString d.Name (if d.PrimaryDevice then "*" else "")
    0 // return an integer exit code
