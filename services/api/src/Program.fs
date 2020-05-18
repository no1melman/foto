open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Server.Kestrel.Core
open Giraffe.Serialization
open System.Text.Json

let createMongo (connectionString:string) (name:string) =
    let client = MongoDB.Driver.MongoClient connectionString
    fun () -> client.GetDatabase name

let webApp personHandler albumHandler =
    choose [    
        route "/health" >=> text "all good"
        personHandler
        albumHandler
    ]

let configureApp (app : IApplicationBuilder) =
    let configuration: IConfiguration = app.ApplicationServices.GetRequiredService<IConfiguration>()
    let loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>()
    let logger = loggerFactory.CreateLogger "configureApp"

    let mongoConnectionString = configuration.GetValue<string> "MongoHost"
    logger.LogInformation("Using mongo host {MongoHost}", mongoConnectionString)

    let mongoDatabaseFactory = createMongo mongoConnectionString
    let fotoDb = mongoDatabaseFactory "foto"

    // would like to do
    // mongoDatabaseFactory "foto" |> Photo.initialiseRoute |> Album.initialiseRoute
    // that would be sick, or create a chaining operator
    // suppose you could just fold as well passing the factory on each fold...

    let photoHandler = fotoDb
                       |> Photo.initialiseRoute
    let albumHandler = fotoDb
                       |> Album.initialiseRoute
    
    app.UseCors() |> ignore
    app.UseGiraffe (webApp photoHandler albumHandler)

let configureServices (services : IServiceCollection) =
    // Add Giraffe dependencies
    services.AddGiraffe() |> ignore
    services.AddLogging() |> ignore
    
    // setup json serialiser which handles options serialisation
    let serialiserOptions = JsonSerializerOptions()
    serialiserOptions.PropertyNameCaseInsensitive <- true
    serialiserOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    serialiserOptions.Converters.Add(AspNetHelpers.OptionConverterFactory())
    
    services.AddSingleton<IJsonSerializer>(AspNetHelpers.Core3JsonSerializer(serialiserOptions)) |> ignore

    services.AddCors(fun cors -> cors.AddDefaultPolicy(fun policy -> policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin() |> ignore )) |> ignore

let kestrelSetup (options: KestrelServerOptions) =
    options.Limits.MaxRequestBodySize <- 52428800 |> int64 |> Nullable

[<EntryPoint>]
let main _ =
    WebHost.CreateDefaultBuilder()
        .UseKestrel(kestrelSetup)
        
        .ConfigureServices(configureServices)
        .Configure(configureApp)
        .Build()
        .Run()
    0