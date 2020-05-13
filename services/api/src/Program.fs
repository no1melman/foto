open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

let createMongo (connectionString:string) (name:string) =
    let client = MongoDB.Driver.MongoClient connectionString
    fun () -> client.GetDatabase name

let webApp personHandler =
    choose [    
        route "/health" >=> text "all good"
        personHandler
    ]

let configureApp (app : IApplicationBuilder) =
    
    let configuration: IConfiguration = app.ApplicationServices.GetRequiredService<IConfiguration>()
    let loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>()
    let logger = loggerFactory.CreateLogger "configureApp"

    let mongoConnectionString = configuration.GetValue<string> "MongoHost"
    logger.LogInformation("Using mongo host {MongoHost}", mongoConnectionString)

    let mongoDatabaseFactory = createMongo mongoConnectionString

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
    WebHost.CreateDefaultBuilder()
        .ConfigureServices(configureServices)
        .Configure(configureApp)
        .Build()
        .Run()
    0