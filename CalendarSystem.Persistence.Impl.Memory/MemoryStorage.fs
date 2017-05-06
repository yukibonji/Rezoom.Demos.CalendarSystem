﻿/// Implements the functionality required by the persistence layer using in-memory storage (dictionaries).
module private CalendarSystem.Persistence.Impl.Memory.Storage
open CalendarSystem.Model
open CalendarSystem.Model.Membership
open CalendarSystem.Model.Calendar
open System
open System.Collections.Generic

let private newId<'a> =
    let mutable counter = 0
    fun () ->
        counter <- counter + 1
        Id counter : 'a Id

let private store (dict : Dictionary<_ Id, _>) makeThing =
    let id = newId()
    let thing = makeThing id
    dict.[id] <- thing
    thing

type private StorageUser =
    {   User : User
        UserAuthInfo : Choice<UserPasswordHash, UserSetupToken>
    }

let private users = Dictionary()
let private sessions = Dictionary()
let private calendarEvents = Dictionary()
let private calendarEventVersions = Dictionary()

let reset() =
    users.Clear()
    sessions.Clear()
    calendarEvents.Clear()

let private occurence sessionId =
    {   Who = sessionId
        When = DateTimeOffset.UtcNow
    }

module Users =
    let createUser createdBy email setupToken name role =
        let user =
            store users <| fun id ->
                let user =
                    {   Id = id
                        Name = name
                        Email = email
                        Role = role
                        Created = occurence createdBy
                        Updated = occurence createdBy
                    }
                { User = user; UserAuthInfo = Choice2Of2 setupToken }
        user.User.Id

    let getUserByEmail email =
        let user = users.Values |> Seq.tryFind (fun u -> u.User.Email = email)
        match user with
        | None -> None
        | Some user -> Some (user.User, match user.UserAuthInfo with | Choice1Of2 hash -> Some hash | _ -> None)

    let getUserById id = users.[id].User

    let updateUser updatedBy updateUser email name =
        let user = users.[updateUser]
        users.[updateUser] <- { user with User = { user.User with Email = email; Name = name } }

    let updateUserPassword by userId hash =
        let user = users.[userId]
        users.[userId] <-
            { user with
                UserAuthInfo = Choice1Of2 hash; User = { user.User with Updated = occurence by }
            }

module Sessions =
    let createSession sessionToken impersonator sessionUserId sessionValidTo =
        let session =
            {   Id = newId()
                UserId = sessionUserId
                Impersonated = impersonator |> Option.map occurence
                Token = sessionToken
                Created = DateTimeOffset.UtcNow
                ValidTo = sessionValidTo
            }
        sessions.[session.Id] <- session
        session.Id

    let getValidSessionByToken token =
        let found = sessions.Values |> Seq.tryFind (fun s -> s.Token = token && s.ValidTo >= DateTimeOffset.UtcNow)
        found |> Option.map (fun s -> s, users.[s.UserId].User)

module CalendarEvents =
    let createCalendarEvent createdBy client consultant name duration =
        let eventId = newId()
        let version = store calendarEventVersions <| fun id ->
            {   Id = id
                CalendarEventId = eventId
                Name = name
                Duration = duration
                ConsultantId = consultant
            }
        let event =
            {   Id = eventId
                ClientId = client
                Created = occurence createdBy
                Deleted = None
                CurrentVersion = version
            }
        calendarEvents.[event.Id] <- event
        event.Id

    let createCalendarEventVersion createdBy calendarEvent consultant name duration =
        let version = store calendarEventVersions <| fun id ->
            {   Id = id
                CalendarEventId = calendarEvent
                Name = name
                Duration = duration
                ConsultantId = consultant
            }
        let event = calendarEvents.[calendarEvent]
        calendarEvents.[calendarEvent] <-
            { event with
                CurrentVersion = version
            }
        version.Id

    let deleteCalendarEvent deletedBy calendarEvent =
        let event = calendarEvents.[calendarEvent]
        calendarEvents.[calendarEvent] <-
            { event with
                Deleted = Some (occurence deletedBy)
            }

    let getCalendarEvents filterToClient filterToConsultant touchesDuration =
        let events = calendarEvents.Values :> _ seq
        let events =
            match filterToClient with
            | None -> events
            | Some clientId -> events |> Seq.filter (fun e -> e.ClientId = clientId)
        let events =
            match filterToConsultant with
            | None -> events
            | Some consultantId -> events |> Seq.filter (fun e -> e.CurrentVersion.ConsultantId = consultantId)
        events
        |> Seq.filter (fun e -> e.CurrentVersion.Duration.Overlaps(touchesDuration))
        |> Seq.sortBy (fun e -> e.CurrentVersion.Duration.First)
        |> ResizeArray
        :> _ IReadOnlyList




    

