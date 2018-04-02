open System

open DisplayModeSwitch.DisplayMode
open DisplayModeSwitch

[<EntryPoint>]
let main argv =
    if argv.Length < 2 then
        -1
    else
        let appName = argv.[0]
        let resolution = (argv.[1]).Split('x') |> (Seq.map uint32 >> Seq.toArray)

        let devs = GetDisplayDevices()
        let modes = GetDisplayModes(Seq.head devs)

        let mode =
            modes |>
            Seq.filter(
                fun m ->
                    match m.Resolution with
                    | Some (x,y) ->
                        x = resolution.[0] && y = resolution.[1]
                    | None -> false
                    &&
                    match m.FixedOutput with
                    | None -> true
                    | Some FixedOutputMode.Default -> true
                    | _ -> false
            ) |>
            Seq.tryHead

        0 // return an integer exit code
