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

open Mongo
open Domain
open AspNetHelpers  


let getPhotoCollection = getCollection<Photograph> "photograph"

let getPhotoBucket = getBucket "photos"

let getSinglePhoto (collection: IMongoCollection<Photograph>) id = 
    getSingle<Photograph> (fun p -> p.Id = id) collection
 
let getPhotoGridFile (bucket: IGridFSBucket) photo =  
    let filterDefinitionBuilder = Builders<GridFSFileInfo>.Filter.Eq((fun f -> f.Filename), photo.FileName)
    getGridFile filterDefinitionBuilder bucket photo


let writeFile (ctx: HttpContext) (bucket: GridFSBucket) (gridFile: GridFSFileInfo, photo: Photograph) =
    ctx.Response.ContentLength <- gridFile.Length |> Nullable
    ctx.Response.ContentType <- photo.ContentType
    task {
        do! bucket.DownloadToStreamAsync(gridFile.Id, ctx.Response.Body)
        return Some ctx
    }

let getPhotos (databaseFn: unit-> IMongoDatabase) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getPhotoCollection database
        task {
            let! cursor = collection.FindAsync<Photograph>(fun p -> true)
            let! hasMoved = cursor.MoveNextAsync()
            match hasMoved with
            | true -> return! json { result = Some cursor.Current; error = None } next ctx
            | false -> return! notFound { result = None; error = Some "No Photos found"  } next ctx
        }

let getPhoto (databaseFn: unit -> IMongoDatabase) ( id: string ) : HttpHandler =
    fun (next : HttpFunc) (ctx: HttpContext) ->
        let database = databaseFn()
        let collection = getPhotoCollection database
        task {
            let! photo = id
                         |> toBsonObjectId 
                         |> getSinglePhoto collection
            
            match photo with
            | Some p -> return! json { result = Some p; error = None } next ctx
            | None -> return! notFound { error = Some "No Photo found"; result = None }  next ctx
        }

let savePhoto (databaseFn: unit -> IMongoDatabase ) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getPhotoCollection database
        task {
            let! photo = ctx.BindJsonAsync<Photograph>()
            let photoWithId = { photo with Id = ObjectId.GenerateNewId() |> BsonObjectId }
            do! collection.InsertOneAsync(photoWithId)
            return! json { result = Some photoWithId; error = None } next ctx
        }

let deletePhoto (databaseFn: unit -> IMongoDatabase) (id: string) : HttpHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) -> 
        let database = databaseFn()
        let collection = getPhotoCollection database
        let bucket = getPhotoBucket database
        task {
            let oId = id |> toBsonObjectId
            let! result = collection.FindOneAndDeleteAsync<Photograph>(fun p -> p.Id = oId)

            do! bucket.DeleteAsync(result.GridFsId)

            return! json { result = Some (sprintf "Deleted %s" id); error = None } next ctx 
        }

let uploadPhoto (databaseFn: unit -> IMongoDatabase) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        let database = databaseFn()
        let collection = getPhotoCollection database
        let bucket = getPhotoBucket database

        let uploadFile (file: IFormFile) =
            async {
                let! id = bucket.UploadFromStreamAsync( file.FileName, (file.OpenReadStream()) ) |> Async.AwaitTask
            
                let photo = {
                    Id = BsonObjectId(ObjectId.GenerateNewId());
                    GridFsId = BsonObjectId(id);
                    FileName = file.FileName;
                    ContentType = file.ContentType;
                    Tags = list.Empty
                }
                collection.InsertOneAsync(photo) |> Async.AwaitTask |> ignore
                return photo
            }

        task {
            let! photos = ctx.Request.Form.Files |> Seq.map uploadFile |> Async.Parallel |> Async.StartAsTask

            return! json { result = Some photos; error = None } next ctx
        }

let downloadPhoto (databaseFn: unit -> IMongoDatabase ) (id: string) =
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        let database = databaseFn()
        let collection = getPhotoCollection database
        let bucket = getPhotoBucket database
        
        let getPhotoFromCollection = getSinglePhoto collection
        let getPhotoGridFileFromBucket = getPhotoGridFile bucket

        
        let writeFileToClientFromBucket = writeFile ctx bucket 

        task {
            let! res = id 
                       |> toBsonObjectId  
                       |> getPhotoFromCollection 
                       >>= getPhotoGridFileFromBucket 
                       >>= writeFileToClientFromBucket
                   
            match res with
            | Some c -> return! next c
            | None -> return! notFound { error = Some "No photo found with that Id"; result = None } next ctx
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
