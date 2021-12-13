namespace Mandadin

open Bolero
open Bolero.Html
open System.Threading.Tasks

[<RequireQualifiedAccess>]
type View =
  | [<EndPoint "/">] Lists
  | [<EndPoint "/notes">] Notes
  | [<EndPoint "lists/{listId}">] ListDetail of listId: string
  | [<EndPoint "/import">] Import

[<RequireQualifiedAccess>]
type Theme =
  | Light
  | Dark
  | Custom

type SaveResult = { Id: string; Ok: bool; Rev: string }

type Note =
  { Id: string
    Content: string
    Rev: string }


type TrackList = { Id: string; Rev: string }

type TrackListItem =
  { Id: string
    IsDone: bool
    ListId: string
    Name: string
    Rev: string }

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

  let Get (icon: Icon) (color: Option<string>) : Node =
    let color = defaultArg color "currentColor"

    match icon with
    | Copy -> Copy().Fill(color).Elt()
    | Share -> Share().Fill(color).Elt()
    | Trash -> Trash().Fill(color).Elt()
    | Clipboard -> Clipboard().Fill(color).Elt()
    | Save -> Save().Fill(color).Elt()
    | Text -> Text().Fill(color).Elt()
    | Import -> Import().Fill(color).Elt()
    | Close -> Close().Fill(color).Elt()
    | Check -> Check().Fill(color).Elt()
    | Back -> Back().Fill(color).Elt()


type IClipboardService =
  abstract SendToClipboard : string -> Task<bool>
  abstract GetFromClipboard : unit -> Task<string>

type IShareService =
  abstract member GetCanShare : unit -> Task<bool>
  abstract member ShareContent : string * string -> Task<bool>
  abstract member ShareContent : string * string * string option -> Task<unit>
  abstract member ImportShareContent : unit -> Task<string>

type ITrackListItemService =
  abstract member Find : string * bool -> Task<TrackListItem list>
  abstract member Create : string * string -> Task<TrackListItem>
  abstract member Update : TrackListItem -> Task<TrackListItem>
  abstract member Delete : TrackListItem -> Task<TrackListItem>
  abstract member Exists : string * string -> Task<bool>
  abstract member ShareItems : string * TrackListItem list -> Task<bool>
  abstract member SendToClipboard : TrackListItem list -> Task<bool>
