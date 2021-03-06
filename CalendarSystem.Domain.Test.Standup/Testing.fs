﻿module CalendarSystem.Domain.Test.Testing
open System.Threading.Tasks
open Rezoom

let private installed =
    lazy
        CalendarSystem.Persistence.Impl.Memory.Install.install()
        CalendarSystem.Domain.Calendar.Impl.Server.Install.install()
        CalendarSystem.Domain.Membership.Impl.Server.Install.install()

let rootEmail = CalendarSystem.Persistence.Impl.Memory.SeedInfo.rootEmail
let rootPass = CalendarSystem.Persistence.Impl.Memory.SeedInfo.rootPass

let runPlan (plan : 'a Plan) =
    installed.Force()
    let task : 'a Task = Execution.execute Execution.ExecutionConfig.Default plan
    // normally we wouldn't use .Result because it blocks, but that's ok for tests
    task.Result

