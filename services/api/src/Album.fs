module Album

open Giraffe
open FSharp.Control.Tasks.V2
open FSharp.Control.Tasks.V2.ContextInsensitive
open MongoDB.Driver
open Microsoft.AspNetCore.Http
open MongoDB.Bson

open Mongo
open Domain
open AspNetHelpers

let getAlbumCollection = getCollection<Album> "albums"

let getSingleAlbum (collection: IMongoCollection<Album>) id = 
    getSingle<Album> (fun p -> p.Id = id) collection

let getAlbums (databaseFn: unit-> IMongoDatabase) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getAlbumCollection database
        task {
            let! cursor = collection.FindAsync<Album>(fun p -> true)
            let! hasMoved = cursor.MoveNextAsync()
            let result = 
                match hasMoved with
                | true -> json cursor.Current next ctx
                | false -> 
                    let message = { result = None; error = Some "No Photos found" }
                    notFound message next ctx
            return! result
        }

let getAlbum (databaseFn: unit -> IMongoDatabase) ( id: string ) : HttpHandler =
    fun (next : HttpFunc) (ctx: HttpContext) ->
        let database = databaseFn()
        let collection = getAlbumCollection database
        task {
            let! photo = id
                         |> toBsonObjectId 
                         |> getSingleAlbum collection
            
            match photo with
            | Some p -> return! json { result = Some p; error = None } next ctx
            | None -> return! notFound { error = Some "No Photo found"; result = None }  next ctx
        }

let saveAlbum (databaseFn: unit -> IMongoDatabase ) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getAlbumCollection database
        task {
            let! album = ctx.BindJsonAsync<Album>()
            let albumWithId = { album with Id = ObjectId.GenerateNewId() |> BsonObjectId }
            do! collection.InsertOneAsync(albumWithId)
            return! json { result = Some albumWithId; error = None } next ctx
        }

let deleteAlbum (databaseFn: unit -> IMongoDatabase) (id: string) : HttpHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getAlbumCollection database
        task {
            let oId = id |> toBsonObjectId
            let! result = collection.FindOneAndDeleteAsync<Album>(fun p -> p.Id = oId)

            return! json { result = Some (sprintf "Deleted %s" id); error = None } next ctx 
        }

let albumHandler getAlbums getAlbum saveAlbum deleteAlbum = 
    let path = "/api/photos"
    choose [
        GET >=> choose [
            route path >=> getAlbums
            routef "/api/photos/%s" getAlbum
        ]
        POST >=> choose [
            route path >=> saveAlbum
        ]
        DELETE >=> choose [
            routef "/api/photos/%s" deleteAlbum
        ]
    ]

let initialiseRoute ( databaseFn: unit -> IMongoDatabase ) =
    let getAlbumsHandler = getAlbums databaseFn
    let getAlbumHandler = getAlbum databaseFn
    let saveAlbumHandler = saveAlbum databaseFn
    let deleteAlbumHandler = deleteAlbum databaseFn
    
    albumHandler getAlbumsHandler getAlbumHandler saveAlbumHandler deleteAlbumHandler



