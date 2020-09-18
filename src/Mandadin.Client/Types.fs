namespace Mandadin.Client

open Bolero
open System

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
    Hide: bool
  }


type TrackList = { Id: string; Name: string }

type TrackListItem =
  {
    Id: string
    Name: string
    IsDone: bool
    ListId: string
  }
