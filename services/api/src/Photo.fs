module Photo
    
open Giraffe
open FSharp.Control.Tasks.V2
open FSharp.Control.Tasks.V2.ContextInsensitive
open MongoDB.Driver
open Microsoft.AspNetCore.Http
open MongoDB.Bson
open MongoDB.Driver.GridFS
open System.Threading.Tasks
open System
   
type NotFoundMessage = { message: string; }

type Photograph = { Id: BsonObjectId; FileName: string; GridFsId: BsonObjectId; ContentType: string; }

let compose (switchFn: _ -> Task<_ option>) (twoTrackInput: Task<_ option>) = 
    task {
        let! li = twoTrackInput
        match li with
        | Some s -> return! switchFn s
        | None -> return None
    }

let (>>=) (twoTrackInput: Task<_ option>) (switchFn: _ -> Task<_ option>) =
    compose switchFn twoTrackInput

let getCollection (database: IMongoDatabase) = database.GetCollection<Photograph> "photographs"
let getBucket (database: IMongoDatabase) = 
    let gridFsOptions = GridFSBucketOptions()
    gridFsOptions.BucketName <- "photos"
    gridFsOptions.ChunkSizeBytes <- 4194304 // 4mb
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
                    let message = { message = "No Photos found" }
                    RequestErrors.notFound (json message) next ctx
            return! result
        }

let getPhoto (databaseFn: unit -> IMongoDatabase) ( id: string ) : HttpHandler =
    fun (next : HttpFunc) (ctx: HttpContext) ->
        let database = databaseFn()
        let collection = getCollection database
        task {
            let inputId = ObjectId.Parse id |> BsonObjectId
            let! cursor = collection.FindAsync<Photograph>(fun p -> p.Id = inputId )
            let! hasMoved = cursor.MoveNextAsync()
            let result = 
                match hasMoved with
                | true when cursor.Current |> List.ofSeq |> List.length <> 0  -> json (cursor.Current |> Seq.head) next ctx
                | _ ->
                    let message = { message = "No Photo found" }
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
                }
                collection.InsertOneAsync(photo) |> Async.AwaitTask |> ignore
                return photo
            }

        task {
            let! photos = ctx.Request.Form.Files |> Seq.map uploadFile |> Async.Parallel |> Async.StartAsTask

            return! json photos next ctx
        }

let downloadPhoto (databaseFn: unit -> IMongoDatabase ) (id: string) =
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        let database = databaseFn()
        let collection = getCollection database
        let bucket = getBucket database
        let getPhoto id =
            task {
                let! cursor = collection.FindAsync<Photograph>(fun p -> p.Id = id )
                let! hasMoved = cursor.MoveNextAsync()
                return match hasMoved with
                       | true when cursor.Current |> List.ofSeq |> List.length <> 0 -> cursor.Current |> Seq.tryHead
                       | _ -> None
            }

        let getGridFile photo =
            task {
                let builder = Builders<GridFSFileInfo>.Filter.Eq((fun f -> f.Filename), photo.FileName)
                let findOptions = new GridFSFindOptions()
                findOptions.Limit <- 1 |> Nullable
                let! cursor = bucket.FindAsync(builder, findOptions)
                let! hasMoved = cursor.MoveNextAsync()
                match hasMoved with
                | true -> match cursor.Current |> Seq.tryHead with
                          | Some gridFile -> return Some (gridFile, photo)
                          | None -> return None
                | false -> return None
            }

        let writeFile (bucket: GridFSBucket) (gridFile: GridFSFileInfo, photo: Photograph) =
            ctx.Response.ContentLength <- gridFile.Length |> Nullable
            ctx.Response.ContentType <- photo.ContentType
            task {
                do! bucket.DownloadToStreamAsync(gridFile.Id, ctx.Response.Body)
                return Some ctx
            }
        
        let toBsonObjectId = ObjectId.Parse >> BsonObjectId
        let writeFileToBucket = writeFile bucket

        task {
            let! res = toBsonObjectId id 
                       |> getPhoto 
                       >>= getGridFile 
                       >>= writeFileToBucket
                   
            match res with
            | Some c -> return! next c
            | None -> return! RequestErrors.notFound (json { message = "No photo found with that Id" }) next ctx
        }
        

let photoHandler getPhotos getPhoto downloadPhoto uploadPhoto deletePhoto = 
    let path = "/api/photos"
    choose [
        GET >=> choose [
            route path >=> getPhotos
            routef "/api/photos/%s" getPhoto
            routef "/api/photos/%s/download" downloadPhoto
        ]
        POST >=> choose [
            route path >=> uploadPhoto
        ]
        DELETE >=> choose [
            routef "/api/photos/%s" deletePhoto
        ]
    ]

let initialiseRoute ( databaseFn: unit -> IMongoDatabase ) =
    let getPhotosHandler = getPhotos databaseFn
    let uploadPhotoHandler = uploadPhoto databaseFn
    let downloadPhotoHandler = downloadPhoto databaseFn
    let deletePhotoHandler = deletePhoto databaseFn
    let getPhotoHandler = getPhoto databaseFn

    photoHandler getPhotosHandler getPhotoHandler downloadPhotoHandler uploadPhotoHandler deletePhotoHandler
