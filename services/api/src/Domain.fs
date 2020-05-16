module Domain

open MongoDB.Bson
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open FSharp.Control.Tasks.V2.ContextInsensitive

type TagName = TagName of string
type Tag = { Id: BsonObjectId; Name: TagName }

type Photograph = { Id: BsonObjectId; FileName: string; GridFsId: BsonObjectId; ContentType: string; Tags: Tag list }

type Album = { Id: BsonObjectId; Name: string; Photos: Photograph list }

let compose (switchFn: _ -> Task<_ option>) (twoTrackInput: Task<_ option>) = 
    task {
        let! li = twoTrackInput
        match li with
        | Some s -> return! switchFn s
        | None -> return None
    }

let (>>=) (twoTrackInput: Task<_ option>) (switchFn: _ -> Task<_ option>) =
    compose switchFn twoTrackInput