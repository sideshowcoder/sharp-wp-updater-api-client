module Pressevent.Test

open NUnit.Framework
open FsUnit
open Pressevent
open System

[<Test>]
let ``create client`` () =
    createClient "https://example.wordpress.com/xmlrpc.php" "examplekey" |> ignore

