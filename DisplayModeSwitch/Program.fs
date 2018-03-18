open System

open DisplayModeSwitch.DisplayMode
open DisplayModeSwitch

[<EntryPoint>]
let main argv =
    for d in (GetDisplayDevices() |> Seq.filter (fun d -> d.Active)) do 
        printfn "%s" <| d.ToString()
        
        let m = GetCurrentDisplayMode d
        if m.IsSome then
            printfn "Current mode: %s" <| m.Value.ToString()
        
        printfn "Modes:"

        for m in
            (GetDisplayModes d) |>
            Seq.filter
                (fun m ->
                    match m.FixedOutput with
                    | None -> true
                    | Some FixedOutputMode.Default -> true
                    | _ -> false
                ) do
            printfn "    %s" <| m.ToString()
    0 // return an integer exit code
