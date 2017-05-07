﻿module private CalendarSystem.Domain.Membership.Impl.Server.UserService
open Rezoom
open CalendarSystem.Model.Membership
open CalendarSystem.Persistence.Membership
open CalendarSystem.Domain.Membership
open CalendarSystem.Model

let service =
    { new IUserService with
        member this.CreateUser(claim, email, name, role) =
            plan {
                let! sessionId, me = Membership.Authentication.Authenticate(claim)
                let create =
                    plan {
                        let setupToken = UserSetupToken.Generate()
                        return!
                            MembershipPersistence.Users.CreateUser
                                ( sessionId
                                , email
                                , setupToken
                                , name
                                , role
                                )
                    }
                return
                    match me.Role, role with
                    | SuperUser, _ -> Ok create
                    | AdminUser, SuperUser -> Error [Invalid "Only super-users can create other super-users"]
                    | AdminUser, _ -> Ok create
                    | ConsultantUser, _
                    | ClientUser _, _ -> Error [Invalid "You must be an admin or super-user to create other users"]
            }
        member this.GetUserById(claim, userId) =
            plan {
                let! _ = Membership.Authentication.Authenticate(claim)
                return! MembershipPersistence.Users.GetUserById(userId)
            }
    }