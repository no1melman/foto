open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

let createMongo (connectionString:string) (name:string) =
    let client = MongoDB.Driver.MongoClient connectionString
    fun () -> client.GetDatabase name

let webApp personHandler =
    choose [    
        route "/health" >=> text "all good"
        personHandler
    ]

let configureApp (app : IApplicationBuilder) =
    // Add Giraffe to the ASP.NET Core pipeline

    let mongoDatabaseFactory = createMongo "mongodb://localhost:27017"

    let photoHandler = mongoDatabaseFactory "foto" 
                       |> Photo.initialiseRoute

    app.UseCors() |> ignore
    app.UseGiraffe (webApp photoHandler)

let configureServices (services : IServiceCollection) =
    // Add Giraffe dependencies
    services.AddGiraffe() |> ignore
    services.AddLogging() |> ignore
    
    services.AddCors(fun cors -> cors.AddDefaultPolicy(fun policy -> policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin() |> ignore )) |> ignore

[<EntryPoint>]
let main _ =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                    |> ignore)
        .Build()
        .Run()
    0