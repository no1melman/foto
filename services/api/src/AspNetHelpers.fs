module AspNetHelpers

open Giraffe
open System
open Newtonsoft.Json

type ResponseMessage<'T> = { result: 'T option; error: string option }

let notFound (message: ResponseMessage<'T>) = RequestErrors.notFound (json message)

type OptionConverter =
    inherit JsonConverter
    new() = { inherit JsonConverter }
    override _.CanConvert(objectType: Type) =
        objectType.GetGenericTypeDefinition() = typedefof<Option<_>>
    override _.WriteJson(writer: JsonWriter, value, serializer: JsonSerializer) =
        let properties = value.GetType().GetProperties()
        let isSomeProperty = properties |> Array.find (fun prop -> prop.Name = "IsSome")
        match isSomeProperty.GetValue(value, [| value |]) :?> bool with
        | true -> 
            let valueProperty = properties |> Array.find(fun prop -> prop.Name = "Value")
            let valueValue = valueProperty.GetValue value
            serializer.Serialize(writer, valueValue)
        | false -> 
            writer.WriteNull()

    override _.ReadJson(reader, objectType, existingValue, serializer) =
        failwith "Not implemented"
