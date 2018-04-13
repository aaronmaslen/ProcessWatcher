open System

open DisplayModeSwitch.DisplayMode
open DisplayModeSwitch

[<EntryPoint>]
let main _ =
    for d in (GetDisplayDevices() |> Seq.filter (fun d -> d.Active)) do 
        printfn "%s" <| d.ToString()
        
        let m = GetCurrentDisplayMode d
        if m.IsSome then
            m.Value.ToString() |> printfn "Current mode: %s"
        
        printfn "Modes:"

        for m in
            (GetDisplayModes d)
            |> Seq.filter
                (fun m ->
                    match m.FixedOutput with
                    | None -> true
                    | Some FixedOutputMode.Default -> true
                    | _ -> false
                ) do
            m.ToString() |> printfn "    %s"

    Console.ReadKey() |> ignore

    let d = GetDisplayDevices() |> Seq.filter (fun d -> d.Active) |> Seq.head

    let m =
        GetDisplayModes d
        |> Seq.filter
            (fun m ->
                match m.Resolution with
                | Some (x,y) -> (x = 1920u) && (y = 1080u)
                | _ -> false
                &&
                match m.FixedOutput with
                | Some FixedOutputMode.Default -> true
                | None -> true
                | _ -> false
            )
        |> Seq.head
    
    match (SetDisplayMode d m (Seq.singleton Temporary)) with
    | Ok _ -> "OK"
    | Error _ -> "Error"
    |> printfn "%s"

    Console.ReadKey() |> ignore
    0 // return an integer exit code
