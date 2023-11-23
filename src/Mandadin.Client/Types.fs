namespace Mandadin.Client

open Bolero

[<RequireQualifiedAccess>]
type View =
  | [<EndPoint "/">] Lists
  | [<EndPoint "/notes">] Notes
  | [<EndPoint "lists/{listId}">] ListDetail of listId: string
  | [<EndPoint "/import">] Import

[<RequireQualifiedAccess; Struct>]
type Theme =
  | Light
  | Dark
  | Custom

[<Struct>]
type SaveResult = { Id: string; Ok: bool; Rev: string }

[<Struct>]
type Note =
  { Id: string
    Content: string
    Rev: string }

[<Struct>]
type TrackList = { Id: string; Rev: string }

[<Struct>]
type TrackListItem =
  { Id: string
    IsDone: bool
    ListId: string
    Name: string
    Rev: string }

[<Struct>]
type Icon =
  | Copy
  | Share
  | Trash
  | Clipboard
  | Save
  | Text
  | Import
  | Close
  | Check
  | Back

[<RequireQualifiedAccess>]
module Icon =
  type Copy = Template<"wwwroot/icons/copy.html">
  type Share = Template<"wwwroot/icons/share.html">
  type Trash = Template<"wwwroot/icons/trash.html">
  type Clipboard = Template<"wwwroot/icons/clipboard.html">
  type Save = Template<"wwwroot/icons/save.html">
  type Text = Template<"wwwroot/icons/text.html">
  type Import = Template<"wwwroot/icons/import.html">
  type Close = Template<"wwwroot/icons/close.html">
  type Check = Template<"wwwroot/icons/check.html">
  type Back = Template<"wwwroot/icons/back.html">

type Icon with

  static member Get(icon: Icon, ?color: string) =
    let color = defaultArg color "currentColor"

    match icon with
    | Copy -> Icon.Copy().Fill(color).Elt()
    | Share -> Icon.Share().Fill(color).Elt()
    | Trash -> Icon.Trash().Fill(color).Elt()
    | Clipboard -> Icon.Clipboard().Fill(color).Elt()
    | Save -> Icon.Save().Fill(color).Elt()
    | Text -> Icon.Text().Fill(color).Elt()
    | Import -> Icon.Import().Fill(color).Elt()
    | Close -> Icon.Close().Fill(color).Elt()
    | Check -> Icon.Check().Fill(color).Elt()
    | Back -> Icon.Back().Fill(color).Elt()
