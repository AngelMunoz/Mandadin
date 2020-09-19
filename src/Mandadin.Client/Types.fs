namespace Mandadin.Client

open Bolero
open Bolero.Html

[<RequireQualifiedAccess>]
type View =
  | [<EndPoint "/">] Notes
  | [<EndPoint "/lists">] Lists
  | [<EndPoint "lists/{listId}">] ListDetail of listId: string

[<RequireQualifiedAccess>]
type Theme =
  | Light
  | Dark
  | Custom

type SaveResult = { Id: string; Ok: bool; Rev: string }

type Note =
  {
    Id: string
    Content: string
    Rev: string
  }


type TrackList =
  {
    Id: string
    Name: string
    Rev: string
  }

type TrackListItem =
  {
    Id: string
    Name: string
    IsDone: bool
    ListId: string
    Rev: string
  }

type Icon =
  | Copy
  | Share
  | Trash
  | Clipboard
  | Save

[<RequireQualifiedAccess>]
module Icon =
  type Copy = Template<"wwwroot/icons/copy.html">
  type Share = Template<"wwwroot/icons/share.html">
  type Trash = Template<"wwwroot/icons/trash.html">
  type Clipboard = Template<"wwwroot/icons/clipboard.html">
  type Save = Template<"wwwroot/icons/save.html">

  let Get (icon: Icon) (color: Option<string>): Node =
    let color = defaultArg color "currentColor"
    match icon with
    | Copy -> Copy().Fill(color).Elt()
    | Share -> Share().Fill(color).Elt()
    | Trash -> Trash().Fill(color).Elt()
    | Clipboard -> Clipboard().Fill(color).Elt()
    | Save -> Save().Fill(color).Elt()
