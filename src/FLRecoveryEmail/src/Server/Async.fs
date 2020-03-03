module Async

let fromTaskF f = f >> Async.AwaitTask
