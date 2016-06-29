// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#r "../../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "System.Xml.Linq.dll"
#load "Client.fs"
open Pressevent

let client = createClient "https://sideshowcoder.com/xmlrpc.php" "nx46gbvjGmw0jk1JNP4LEUq3mXmzzaPZ"

Seq.iter (fun u -> printfn "%A" u) (getCoreUpdates client)
Seq.iter (fun u -> printfn "%A" u) (getPluginUpdates client)
