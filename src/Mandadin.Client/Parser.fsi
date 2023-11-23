namespace Mandadin.Client

open TheBlunt

[<RequireQualifiedAccess>]
module Parse =
    val entries: lines: string array -> Result<obj array array, (string * ParseError) list>
