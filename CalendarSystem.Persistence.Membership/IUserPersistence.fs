﻿namespace CalendarSystem.Persistence.Membership
open System
open Rezoom
open CalendarSystem.Model
open CalendarSystem.Model.Membership

/// Defines the API for persisting users.
type IUserPersistence =
    /// Store a user.
    abstract member CreateUser
        : createdBy : Session Id
        * email : EmailAddress
        * setupToken : UserSetupToken
        * name : string
        * role : Role
        -> User Id Plan

    /// Update a user's regular metadata in the database. Sets the Updated occurence.
    abstract member Update
        : updatedBy : Session Id
        * updateUser : User Id
        * email : EmailAddress
        * name : string
        -> unit Plan

    /// Update a user's password hash in the database. Sets the Updated occurence.
    abstract member UpdatePassword
        : updatedBy : Session Id
        * updateUser : User Id
        * password : InputPassword
        -> unit Plan

    /// Find a user by their ID.
    abstract member GetUserById : id : User Id -> User Plan

    /// Attempt to find a user by their email address. If no such user exists, return None.
    /// You get the password hash as well since a major reason to use this method is to implement login
    /// functionality.
    abstract member GetUserByEmail : email : EmailAddress -> (User * UserPasswordHash option) option Plan

    /// Attempt to find a user ID and the time they were created, given a setup token.
    abstract member GetUserBySetupToken : token : UserSetupToken -> (User Id * DateTimeOffset) option Plan