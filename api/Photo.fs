module Photo
    
open Giraffe
open FSharp.Control.Tasks.V2
open FSharp.Control.Tasks.V2.ContextInsensitive
open MongoDB.Driver
open Microsoft.AspNetCore.Http
open MongoDB.Bson
open MongoDB.Driver.GridFS
open Microsoft.AspNetCore.Http.Features
open System.Threading

    
type NotFoundMessage = { message: string; }

type Photograph = { Id: BsonObjectId; FileName: string; GridFsId: BsonObjectId; ContentType: string; ContentDisposition: string }

let getCollection (database: IMongoDatabase) = database.GetCollection<Photograph> "photographs"
let getBucket (database: IMongoDatabase) = 
    let gridFsOptions = GridFSBucketOptions()
    gridFsOptions.BucketName <- "photos"
    gridFsOptions.ChunkSizeBytes <- 4194304
    gridFsOptions.WriteConcern <- WriteConcern.WMajority
    gridFsOptions.ReadPreference <- ReadPreference.Secondary

    GridFSBucket(database, gridFsOptions)

let getPhotos (databaseFn: unit-> IMongoDatabase) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getCollection database
        task {
            let! cursor = collection.FindAsync<Photograph>(fun p -> true)
            let! hasMoved = cursor.MoveNextAsync()
            let result = 
                match hasMoved with
                | true -> json cursor.Current next ctx
                | false -> 
                    let message = { message = "No People found" }
                    RequestErrors.notFound (json message) next ctx
            return! result
        }

let savePhoto (databaseFn: unit -> IMongoDatabase ) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getCollection database
        task {
            let! photo = ctx.BindJsonAsync<Photograph>()
            let photoWithId = { photo with Id = BsonObjectId ( ObjectId.GenerateNewId() ) }
            do! collection.InsertOneAsync(photoWithId)
            return! json photoWithId next ctx
        }

let deletePhoto (databaseFn: unit -> IMongoDatabase ) (id: string) : HttpHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getCollection database
        let bucket = getBucket database
        task {
            let oId = BsonObjectId (ObjectId id) 
            let! result = collection.FindOneAndDeleteAsync<Photograph>(fun p -> p.Id = oId)

            do! bucket.DeleteAsync(result.GridFsId)

            return! text "awesome sauce" next ctx 
        }

let uploadPhoto (databaseFn: unit -> IMongoDatabase) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        let database = databaseFn()
        let collection = getCollection database
        let bucket = getBucket database

        let uploadFile (file: IFormFile) =
            async {
                let! id = bucket.UploadFromStreamAsync( file.FileName, (file.OpenReadStream()) ) |> Async.AwaitTask
            
                let photo = {
                    Id = BsonObjectId(ObjectId.GenerateNewId());
                    GridFsId = BsonObjectId(id);
                    FileName = file.FileName
                    ContentType = file.ContentType
                    ContentDisposition = file.ContentDisposition
                }
                collection.InsertOneAsync(photo) |> Async.AwaitTask |> ignore
                return photo
            }

        task {
            let! photos = ctx.Request.Form.Files |> Seq.map uploadFile |> Async.Parallel |> Async.StartAsTask

            return! json photos next ctx
        }
        //task {
            
            

        //    let file = ctx.Request.Form.Files |> Seq.head
        //    let! id = bucket.UploadFromStreamAsync( file.FileName, (file.OpenReadStream()) )

        //    let photo = {
        //        Id = BsonObjectId(ObjectId.GenerateNewId());
        //        GridFsId = BsonObjectId(id);
        //        FileName = file.FileName
        //        ContentType = file.ContentType
        //        ContentDisposition = file.ContentDisposition
        //    }
        //    let! result = collection.InsertOneAsync(photo)
            
        //    return! json photo next ctx         
        //}


let photoHandler getPhotos uploadPhoto deletePhoto = 
    let path = "/photos"
    choose [
        GET >=> choose [
            route path >=> getPhotos
        ]
        POST >=> choose [
            route path >=> uploadPhoto
        ]
        DELETE >=> choose [
            routef "/photos/%s" deletePhoto
        ]
    ]

let initialiseRoute ( databaseFn: unit -> IMongoDatabase ) =
    let getPhotoHandler = getPhotos databaseFn
    let uploadPhotoHandler = uploadPhoto databaseFn
    let deletePhotoHandler = deletePhoto databaseFn
    
    photoHandler getPhotoHandler uploadPhotoHandler deletePhotoHandler
