﻿open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open JsonParserCore
open Microsoft.AspNetCore.Cors
open System.Collections.Generic
open Microsoft.Extensions.Primitives

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

 let addCorsHeadsHACK (ctx: HttpContext) =
     ctx.Response.Headers.Add("Access-Control-Allow-Credentials", StringValues("true"))
     ctx.Response.Headers.Add("Access-Control-Allow-Headers", StringValues("content-type"))
     ctx.Response.Headers.Add("Access-Control-Allow-Methods", StringValues("POST"))
     ctx.Response.Headers.Add("Access-Control-Allow-Origin", StringValues("*"))

     ctx

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
           
           ctx |> addCorsHeadsHACK |> ignore

           return! Giraffe.ResponseWriters.json result next ctx
       }

let webApp =
    choose [
        subRouteCi "/api" (
            POST >=> 
                choose [
                    route "/generate" >=> generationHandler
            ])
    ]

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe webApp
    app.UseCors(new Action<_>(fun (b: Infrastructure.CorsPolicyBuilder) -> 
                                b.AllowAnyHeader() |> ignore
                                b.AllowAnyOrigin() |> ignore
                                b.AllowAnyMethod() |> ignore
                                b.AllowCredentials() |> ignore)) |> ignore

let configureServices (services : IServiceCollection) =
    services.AddCors() |> ignore
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