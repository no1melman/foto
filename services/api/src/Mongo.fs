module Mongo

open MongoDB.Bson
open MongoDB.Driver
open MongoDB.Driver.GridFS
open FSharp.Control.Tasks.V2
open FSharp.Control.Tasks.V2.ContextInsensitive
open System

let getCollection<'T> (collectioName: string) (database: IMongoDatabase) = database.GetCollection<'T> collectioName

let getBucket (bucketName: string) (database: IMongoDatabase) = 
    let gridFsOptions = GridFSBucketOptions()
    gridFsOptions.BucketName <- bucketName
    gridFsOptions.ChunkSizeBytes <- 4194304 // 4mb
    gridFsOptions.WriteConcern <- WriteConcern.WMajority
    gridFsOptions.ReadPreference <- ReadPreference.Secondary

    GridFSBucket(database, gridFsOptions)

let toBsonObjectId = ObjectId.Parse >> BsonObjectId

let getSingle<'T> (selector: 'T -> bool) (collection: IMongoCollection<'T>) =
    task {
        let! cursor = collection.FindAsync<'T>(selector)
        let! hasMoved = cursor.MoveNextAsync()
        match hasMoved with
        | true when cursor.Current |> List.ofSeq |> List.length <> 0 -> return cursor.Current |> Seq.tryHead
        | _ -> return None
    }

let getGridFile<'T> (builder: FilterDefinition<GridFSFileInfo>) (bucket: IGridFSBucket) (a: 'T) =
    task {
        let findOptions = new GridFSFindOptions()
        findOptions.Limit <- 1 |> Nullable
        let! cursor = bucket.FindAsync(builder, findOptions)
        let! hasMoved = cursor.MoveNextAsync()
        match hasMoved with
        | true -> match cursor.Current |> Seq.tryHead with
                  | Some gridFile -> return Some (gridFile, a)
                  | None -> return None
        | false -> return None
    }
