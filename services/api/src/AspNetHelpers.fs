module AspNetHelpers

open Giraffe
open System.Text.Json.Serialization
open System
open System.Text.Json
open Giraffe.Serialization
open System.IO
open System.Text
open System.Threading.Tasks

type ResponseMessage<'T> = { result: 'T option; error: string option }

let notFound (message: ResponseMessage<'T>) = RequestErrors.notFound (json message)

type OptionConverter<'T> =
    inherit JsonConverter<'T option>
    new() = { inherit JsonConverter<'T option> }
    override _.Write(writer, value, serializer) =
        match value with
        | Some optionValue -> 
            JsonSerializer.Serialize(writer, optionValue, serializer)
        | None -> 
            writer.WriteNullValue()

    override _.Read(reader, typeToConvert, serializer) =
        failwith "Not implemented"

type OptionConverterFactory =
    inherit JsonConverterFactory
    new() = { inherit JsonConverterFactory }
    override __.CanConvert(typeToConvert: Type) =
        let isGenericType = typeToConvert.IsGenericType
        (isGenericType && typeToConvert.GetGenericTypeDefinition() = typedefof<Option<_>>)
    override __.CreateConverter(typeToConvert: Type, options: JsonSerializerOptions) : JsonConverter =
        let optionType = typeToConvert.GetGenericArguments().[0]
        let converter = Activator.CreateInstance(typedefof<OptionConverter<_>>.MakeGenericType([| optionType |])) :?> JsonConverter
        converter

type Core3JsonSerializer (options: JsonSerializerOptions) =
 
    interface IJsonSerializer with
        member __.SerializeToString (x : 'T) =
            JsonSerializer.Serialize (x, options)

        member __.SerializeToBytes (x : 'T) =
            JsonSerializer.SerializeToUtf8Bytes (x, options)

        member __.SerializeToStreamAsync (x : 'T) (stream : Stream) =
            JsonSerializer.SerializeAsync(stream, x, options)

        member __.Deserialize<'T> (json : string) : 'T =
            let bytes = Encoding.UTF8.GetBytes json
            let rbytes = ReadOnlySpan<byte>(bytes)
            JsonSerializer.Deserialize(rbytes, options)

        member __.Deserialize<'T> (bytes : byte array) : 'T =
            let rbytes = ReadOnlySpan<byte>(bytes)
            JsonSerializer.Deserialize(rbytes, options)

        member __.DeserializeAsync<'T> (stream : Stream) : Task<'T> =
            JsonSerializer.DeserializeAsync(stream, options).AsTask()