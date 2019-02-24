﻿open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open JsonParserCore

type ListGeneratorType =
    | List
    | Array
    | CharpList

type TypeGeneration =
    | JustTypes
    | NewtosoftAttributes

[<CLIMutable>]
type GenerationParams =
    { Data: string
      RootObjectName : string
      ListGeneratorType: ListGeneratorType 
      TypeGeneration:  TypeGeneration }

 let generationHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let view = function
                | JustTypes -> FsharpSimpleTypeHandler.toView
                | NewtosoftAttributes -> FsharpNewtonsoftHandler.toView

            let collectionGenerator = function
                | List -> FsharpCommon.listGenerator
                | Array -> FsharpCommon.arrayGenerator
                | CharpList -> FsharpCommon.charpListGenerator
                
            let generate rootObjectName collectionGenerator view json =
                generateRecords FsharpCommon.fixName rootObjectName collectionGenerator json |> view
                
            let! generationParams = ctx.BindModelAsync<GenerationParams>()

            let result =
                (generate generationParams.RootObjectName (collectionGenerator generationParams.ListGeneratorType) (view generationParams.TypeGeneration) generationParams.Data)
            
            return! Giraffe.ResponseWriters.json result next ctx
        }

let webApp =
    choose [
        POST >=> 
            choose [
                route "/generate" >=> generationHandler
            ]
    ]

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddSingleton<Giraffe.Serialization.Json.IJsonSerializer>(Thoth.Json.Giraffe.ThothSerializer()) |> ignore
    services.AddGiraffe() |> ignore

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0