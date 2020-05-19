module AspNetHelpers

open Giraffe
open System
open Newtonsoft.Json
open System.Reflection

type ResponseMessage<'T> = { result: 'T option; error: string option }

let notFound (message: ResponseMessage<'T>) = RequestErrors.notFound (json message)

type OptionConverter =
    inherit JsonConverter
    new() = { inherit JsonConverter }
    override _.CanConvert(objectType: Type) =
        let can = objectType.GetGenericTypeDefinition() = typedefof<Option<_>>
        can
    override _.WriteJson(writer: JsonWriter, value, serializer: JsonSerializer) =
        let properties = value.GetType().GetProperties()
        let isSomeProperty = properties |> Array.find (fun prop -> prop.Name = "IsSome")
        let isSome = isSomeProperty.GetValue(value, [| value |]) :?> bool
                       
        match isSome with
        | true -> 
            let valueProperty = properties |> Array.find(fun prop -> prop.Name = "Value")
            let valueValue = valueProperty.GetValue(value)
            serializer.Serialize(writer, valueValue)
        | false -> 
            writer.WriteNull()

    override _.ReadJson(reader, objectType, existingValue, serializer) =
        failwith "Not implemented"
