module AspNetHelpers

open Giraffe

type ResponseMessage<'T> = { result: 'T option; error: string option }

let notFound (message: ResponseMessage<'T>) = RequestErrors.notFound (json message)