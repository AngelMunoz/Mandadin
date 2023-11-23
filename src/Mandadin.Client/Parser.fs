namespace Mandadin.Client

open TheBlunt
open FsToolkit.ErrorHandling

module Parse =
  let openCheckBox =
    pchar (fun c -> c = '[') (fun c ->
      $"'{c}' is not a valid character at this position")

  let closeCheckBox =
    pchar (fun c -> c = ']') (fun c ->
      $"'{c}' is not a valid character at this position")

  let completedCheckBox =
    pchar (fun c -> c = 'x') (fun c ->
      $"'{c}' is not a valid character at this position")

  let checkBox =
    parse {
      let! ``open`` = openCheckBox
      let! em1 = ptry blanks

      let! isChecked =
        ptry completedCheckBox
        |> map (fun check ->
          match check with
          | Some _ -> true
          | None -> false)

      let! em2 = ptry blanks
      let! ``close`` = closeCheckBox

      return
        { range =
            Range.merge
              [ ``open``.range
                em1.range
                isChecked.range
                em2.range
                ``close``.range ]
          result = isChecked.result }
    }

  let eol =
    pchoice [ pstr "\r\n"; pstr "\n"; pstr "\r" ]

  let content =
    parse {
      let! init = blanks
      let! content = many anyChar |> pconcat
      let! endl = eoi

      return
        { range =
            Range.merge
              [ init.range
                content.range
                endl.range ]
          result = content.result }
    }

  let mandadinEntry =
    content |> andThen checkBox

  let inline parseLine line =
    match run line mandadinEntry with
    | POk { result = isChecked, content } ->
      Ok([| box isChecked; box content |])
    | PError e -> Error(line, e)

  let entries lines =
    lines
    |> List.ofArray
    |> List.traverseResultA parseLine
    |> Result.map List.toArray
