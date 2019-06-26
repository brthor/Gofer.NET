---
layout: default
title: Task Queues
nav_order: 2
description: "Gofer.NET is an easy C# API for distributed background tasks/jobs for .NET Core."
permalink: /taskqueue
---

## Task Queues

The `TaskQueue` is the fundamental object for queuing tasks to be run. 

#### Redis Must be Running
{: .mt-6 .mb-3 }

In order to create one, you must have a running instance of redis, and a connection string for that instance. For testing, we recommend using [docker](https://docs.docker.com/install/) to set up a local redis instance:

```bash
$ docker run -d -p 127.0.0.1:6379:6379 redis:4-alpine
```

#### Create a TaskQueue
{: .mt-6 .mb-3 }

```c#
public async Task Main()
{
    var connectionString = "127.0.0.1:6379";
    var taskQueue = TaskQueue.Redis(connectionString);
}
```

#### More than One TaskQueue per Redis
{: .mt-6 .mb-3 }

You can operate more than one independent `TaskQueue` per instance of Redis by specifying a name for the queue.

```c#
public async Task Main()
{
    var connectionString = "127.0.0.1:6379";

    // taskQueue1 is completely separate from taskQueue2.
    // A worker for taskQueue1 will not run tasks on taskQueue2.
    var taskQueue1 = TaskQueue.Redis(connectionString, "myTaskQueue1");
    var taskQueue2 = TaskQueue.Redis(connectionString, "myOtherTaskQueue");
}
```

### Using the TaskClient

The `TaskClient` class is required to add scheduled or recurring tasks, or to start a worker. 

[Read more about scheduled and recurring tasks.](./jobs)

[Read more about starting a worker.](./workers)

Use the `TaskClient.TaskQueue` property to access the TaskQueue and enqueue tasks:

```c#
public async Task Main()
{
    var connectionString = "127.0.0.1:6379";
    var taskQueue = TaskQueue.Redis(connectionString);
    var taskClient = new TaskClient(taskQueue);

    // Use the taskClient.TaskQueue to enqueue a job
    await taskClient.TaskQueue.Enqueue(() => Console.WriteLine("hello world!"));
}
```