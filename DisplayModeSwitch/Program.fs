open System

open DisplayModeSwitch.DisplayMode

[<EntryPoint>]
let main argv =
    for d in (Seq.filter (fun (d : Display) -> d.Active) (GetDisplayDevices())) do 
        printfn "%s %s%s" d.DeviceString d.Name (if d.PrimaryDevice then "*" else "")
    0 // return an integer exit code
