﻿[<RequireQualifiedAccess>]
module FsharpSimpleTypeHandler

open Types

let private fieldToView (field: Field) = sprintf "%s: %s" field.Name (field.Template.Replace("%s", field.Type))

let private typeToView (typeDef: Type) =
    let ident = "    "

    match typeDef.Fields with
    | [] -> sprintf "type %s = { }" typeDef.Name
    | [x] -> sprintf "type %s = { %s }" typeDef.Name (fieldToView x)
    | xs -> 
        let head = sprintf "type %s =" typeDef.Name

        let fieldBlock =
            xs 
            |> List.map fieldToView
            |> List.mapi(fun i x -> 
                            match i with
                            | 0 -> sprintf "{ %s" x
                            | index when index = typeDef.Fields.Length - 1 -> sprintf "  %s }" x
                            | _ -> sprintf "  %s" x)
            |> List.map (sprintf "%s%s" ident)        
            |> List.reduce (sprintf "%s\n%s")

        sprintf "%s\n%s" head fieldBlock

let toView = 
    function 
    | Ok types -> Ok (types |> List.map typeToView |> List.reduce (sprintf "%s\n\n%s"))
    | Error msg -> Error msg
