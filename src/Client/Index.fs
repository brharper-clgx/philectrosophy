module Client.Index

open Client.Components
open Client.Pages
open Client.Urls
open Elmish
open Shared
open Feliz
open Feliz.Router

(*******************************************
*        TYPES
*******************************************)
[<RequireQualifiedAccess>]
type Page =
    | About of About.State
    | Blog of Blog.State
    | BlogEntry of BlogEntry.State
    | Search of Search.State
    | NotFound
    | UnexpectedError

type State = { CurrentUrl: Url; CurrentPage: Page }

[<RequireQualifiedAccess>]
type Msg =
    | About of About.Msg
    | Blog of Blog.Msg
    | BlogEntry of BlogEntry.Msg
    | Search of Search.Msg
    | UrlChanged of Url

(*******************************************
*        HELPERS
*******************************************)
let parseUrl url =
    match url with
    | [] -> Url.Blog
    | [ _: string; slug: string ] -> Url.BlogEntry slug
    | [ page: string ] ->
        match Url.fromString page with
        | Some url -> url
        | None -> Url.NotFound
    | _ -> Url.NotFound

let onUrlChanged state dispatch url =
    match parseUrl url with
    | url when state.CurrentUrl = url -> ()
    | url -> url |> Msg.UrlChanged |> dispatch

let pageInitFromUrl url =
    let initializer (state, cmd) pageMapper msgMapper =
        {
            CurrentUrl = url
            CurrentPage = pageMapper state
        },
        Cmd.map msgMapper cmd

    match url with
    | Url.About -> initializer (About.init ()) Page.About Msg.About
    | Url.Blog -> initializer (Blog.init ()) Page.Blog Msg.Blog
    | Url.BlogEntry slug -> initializer (BlogEntry.init slug) Page.BlogEntry Msg.BlogEntry
    | Url.Search -> initializer (Search.init ()) Page.Search Msg.Search
    | Url.UnexpectedError ->
        {
            CurrentUrl = url
            CurrentPage = Page.UnexpectedError
        },
        Cmd.none
    | Url.NotFound ->
        {
            CurrentUrl = url
            CurrentPage = Page.NotFound
        },
        Cmd.none

(*******************************************
*        INIT & UPDATE
*******************************************)
let init (): State * Cmd<Msg> =
    Router.currentUrl ()
    |> parseUrl
    |> pageInitFromUrl

let update (msg: Msg) (state: State): State * Cmd<Msg> =
    let updater pageMsg pageState pageUpdater msgMapper pageMapper =
        let newState, newCmd = pageUpdater pageMsg pageState
        let cmd = Cmd.map msgMapper newCmd
        { state with
            CurrentPage = pageMapper newState
        },
        cmd

    match msg, state.CurrentPage with
    | Msg.About msg', Page.About state' -> updater msg' state' About.update Msg.About Page.About
    | Msg.Blog msg', Page.Blog state' -> updater msg' state' Blog.update Msg.Blog Page.Blog
    | Msg.BlogEntry msg', Page.BlogEntry state' -> updater msg' state' BlogEntry.update Msg.BlogEntry Page.BlogEntry
    | Msg.Search msg', Page.Search state' -> updater msg' state' Search.update Msg.Search Page.Search
    | Msg.UrlChanged nextUrl, _ -> pageInitFromUrl nextUrl
    | _ -> state, Cmd.none

(*******************************************
*        RENDER
*******************************************)
open Fable.React

let render (state: State) (dispatch: Msg -> unit): ReactElement =
    let activePage =
        match state.CurrentPage with
        | Page.About state -> About.render state (Msg.About >> dispatch)
        | Page.Blog state -> Blog.render state (Msg.Blog >> dispatch)
        | Page.BlogEntry state -> BlogEntry.render state (Msg.BlogEntry >> dispatch)
        | Page.Search state -> Search.render state (Msg.Search >> dispatch)
        | Page.UnexpectedError -> UnexpectedError.render
        | Page.NotFound -> NotFound.render

    Html.div
        [
            prop.children [
                Navbar.render state.CurrentUrl
                React.router [
                    router.onUrlChanged (onUrlChanged state dispatch)
                    router.children activePage
                ]
            ]
        ]
