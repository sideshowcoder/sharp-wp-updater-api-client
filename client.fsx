#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "System.Xml.Linq.dll"

module Client =
    open System.Text
    open System.IO
    open System.Net
    open FSharp.Data

    type T = { Url:string; Key:string }

    let create url key =
        { Url = url; Key = key }

    type private XmlMethod =
        | GetPluginUpdatesAvailable
        | GetCoreUpdatesAvailable

    let private stringify xmlMethod =
        match xmlMethod with
        | GetCoreUpdatesAvailable -> "getCoreUpdatesAvailable"
        | GetPluginUpdatesAvailable -> "getPluginUpdatesAvailable"


    module Version =
        type T = { Major:int; Minor:int; Patch:int } with
            override x.ToString() =
                sprintf "%i.%i.%i" x.Major x.Minor x.Patch

        let empty =
            { Major = 0; Minor = 0; Patch = 0 }
            
        let create major minor patch =
            { Major = major; Minor = minor; Patch = patch }

        let parse (version:string) =
            (* TODO should use TryParse and wrap result in Some and None *)
            let parts = version.Split([|'.'|]) |> Array.toList |> List.map System.Int32.Parse
            match parts with
            | major :: minor :: patch :: [] -> create major minor patch
            | major :: minor :: [] -> create major minor 0
            | major :: [] -> create major 0 0
            | _ -> invalidArg "version" "is not a valid semver"

    type MemberType = Installed | Current | Plugin
    
    let (|Installed|_|) input =
        if input.Equals("installed") then
            Some(Installed)
        else
            None

    let (|Current|_|) input =
        if input.Equals("current") then
            Some(Current)
        else
            None

    let (|Plugin|_|) input =
        if input.Equals("plugin") then
            Some(Plugin)
        else
            None

    type Update =
        { Plugin:string;
          Installed:Version.T;
          Current:Version.T }
        
    let buildUpdate =
        Seq.fold (fun acc (name, value) ->
                  match name with
                  | Installed -> { acc with Installed = (Version.parse value) }
                  | Current -> { acc with Current = (Version.parse value) }
                  | Plugin -> { acc with Plugin = value }
                  | _ -> acc
                  ) { Plugin = "Core"; Installed = Version.empty ; Current = Version.empty }


    type CoreUpdatesMethodResponse = XmlProvider<"""
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
    """>

    type PluginUpdatesMethodResponse = XmlProvider<"""
    <methodResponse>
      <params>
        <param>
          <value>
            <array>
              <data>
                <value>
                  <struct>
                    <member><name>plugin</name><value><string>Autoptimize</string></value></member>
                    <member><name>installed</name><value><string>a.a.a</string></value></member>
                    <member><name>current</name><value><string>b.b.b</string></value></member>
                  </struct>
                </value>
                <value>
                  <struct>
                    <member><name>plugin</name><value><string>Disqus Comment System</string></value></member>
                    <member><name>installed</name><value><string>2.84</string></value></member>
                    <member><name>current</name><value><string>2.85</string></value></member>
                  </struct>
                </value>
            </data></array>
          </value>
        </param>
      </params>
    </methodResponse>
    """>

    let private xmlRPCRequest client (methodName:XmlMethod) =
        let xml = sprintf """<?xml version=\"1.0\" ?>
                <methodCall>
                    <methodName>%O</methodName>
                    <params>
                        <param><value><string>%s</string></value></param>
                    </params>
                </methodCall>""" (stringify methodName) client.Key

        let postBody = Encoding.UTF8.GetBytes xml

        let request = HttpWebRequest.Create(client.Url) :?> HttpWebRequest
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
        
    let getCoreUpdates client =
        let coreUpdatesResponse = CoreUpdatesMethodResponse.Parse (xmlRPCRequest client GetCoreUpdatesAvailable)
        seq { for m in coreUpdatesResponse.Param.Value.Struct.Members -> (m.Name, m.Value.String) }
        |> buildUpdate
        |> Seq.singleton

    let getPluginUpdates client =
        (PluginUpdatesMethodResponse.Parse (xmlRPCRequest client GetPluginUpdatesAvailable)).Param.Value.Array.Data.Values
        |> Array.toSeq
        |> Seq.map (fun v -> v.Struct.Members)
        |> Seq.map (fun ms -> ms |> Array.toSeq |> Seq.choose (fun m ->
                                                match m.Value.String.String with
                                                | Some s -> Some (m.Name, s)
                                                | _ -> None
                                                ) |> buildUpdate)



