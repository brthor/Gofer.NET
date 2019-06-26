---
layout: default
title: Workers
nav_order: 4
description: "Gofer.NET is an easy C# API for distributed background tasks/jobs for .NET Core."
permalink: /workers
---

## Worker Set-Up

Setting up or adding a new worker is designed to be very simple.

```c#
public class Program
{
    public static async Task Main()
    {
        // Connect to the TaskQueue
        var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
        var taskClient = new TaskClient(taskQueue);

        // Run the Worker. 
        // NOTE: This will loop endlessly waiting for tasks to run.
        await taskClient.Listen();
    }
}
```

### Workers must have access to the queued code.

This is very important, when a function is added to the queue, the worker that picks it up must be able to access the assembly from which the function came. 

As an example, assume you have a ProjectA with class `DatabaseFunctions` and that project queues `DatabaseFunctions.Clean()` as a job. Then assume you have a ProjectB, completely separate from ProjectA, with no access to `DatabaseFunctions`. ProjectB contains the worker, and when the worker tries to run `DatabaseFunctions.Clean()` it will throw an error that it cannot find the function.

### Worker Project Pattern

To resolve this, we recommend adding ProjectA as a `ProjectReference` in ProjectB. You can accomplish this using the dotnet cli like so:

```bash
$ cd ProjectB
$ dotnet add reference "../ProjectA/"
```

In general, we recommend creating a separate project for your workers, with a project-to-project reference to your primary codebase. This is especially useful in web projects, where jobs are queued in response to specific events.

NOTE: If ProjectA is rebuilt and redeployed, ProjectB (the workers) will also need to be rebuilt and redeployed.