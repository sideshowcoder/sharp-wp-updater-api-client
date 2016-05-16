#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "System.Xml.Linq.dll"

open System.Text
open System.IO
open System.Net
open FSharp.Data

let xmlTemplate methodName key =
    sprintf
      """<?xml version=\"1.0\" ?>
         <methodCall>
           <methodName>%s</methodName>
           <params>
             <param><value><string>%s</string></value></param>
           </params>
         </methodCall>"""
      methodName key

let xmlRPCRequest (url:string) methodName key =
    let xml = xmlTemplate methodName key
    let postBody = Encoding.UTF8.GetBytes xml

    let request = HttpWebRequest.Create(url) :?> HttpWebRequest
    request.ProtocolVersion <- HttpVersion.Version11
    request.Method <- "POST"
    request.ContentType <- "text/xml; charset=utf-8"
    request.ContentLength <- int64 postBody.Length

    let requestStream = request.GetRequestStream()
    requestStream.Write(postBody, 0, postBody.Length)
    requestStream.Close()

    let response = request.GetResponse()
    let responseStream = response.GetResponseStream()
    let reader = new StreamReader(responseStream)
    reader.ReadToEnd()

let url = "https://sideshowcoder.com/xmlrpc.php"
let key = "nx46gbvjGmw0jk1JNP4LEUq3mXmzzaPZ"
let xmlMethod = "getCoreUpdatesAvailable"

[<Literal>]
let coreUpdatesXMLSample = """
<methodResponse>
  <params>
    <param>
      <value>
        <struct>
          <member><name>installed</name><value><string>a.a.a</string></value></member>
          <member><name>current</name><value><string>b.b.b</string></value></member>
        </struct>
      </value>
    </param>
  </params>
</methodResponse>
"""
type CoreUpdatesMethodResponse = XmlProvider<coreUpdatesXMLSample>

let coreUpdatesXML = xmlRPCRequest url xmlMethod key

let coreUpdates = CoreUpdatesMethodResponse.Parse coreUpdatesXML

type Update = { ID: string; Version: string }

let updates = seq { for m in coreUpdates.Param.Value.Struct.Members -> { ID = m.Name; Version =  m.Value.String } }

Seq.iter (fun u -> printfn "ID: %s, Version: %s" u.ID u.Version) updates
    

