# Gofer.NET: Easy distributed tasks/jobs for .NET Core

[![Build Status](https://travis-ci.org/brthor/Gofer.NET.svg?branch=master)](https://travis-ci.org/brthor/Gofer.NET)
[![Nuget Version Number](https://img.shields.io/nuget/v/Gofer.NET.svg)](https://www.nuget.org/packages/Gofer.NET)
[![Join the chat at https://gitter.im/Gofer-NET/Lobby](https://badges.gitter.im/Gofer-NET/Lobby.svg)](https://gitter.im/Gofer-NET/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Documentation

[Read the Docs](https://brthor.github.io/Gofer.NET/)

## What is this?

This is a distributed job runner for .NET Standard 2.0 Applications.

Inspired by Celery for Python, it allows you to quickly queue code execution on a worker pool.

- Use natural expression syntax to queue jobs for execution.

- Queued jobs are persisted, and automatically run by the first available worker.

- Scale your worker pool by simply adding new nodes.

- Backed by Redis, all tasks are persistent.

## Getting Started

### Install the dotnet cli.

We recommend using the [dotnet cli](https://dotnet.microsoft.com/download) to get started, but it's not a necessity. 

The dotnet cli is part of the [.NET Core SDK](https://dotnet.microsoft.com/download).

### Start a Redis instance.

We recommend using [docker](https://docs.docker.com/install/) to start a local Redis instance for testing. Setting up a production-level Redis instance is out of the scope of this documentation.

```bash
$ docker run -d -p 127.0.0.1:6379:6379 redis:4-alpine
```

### Create a project.

Open up a terminal and create a new console project to get started.

```bash
$ mkdir myProject && cd myProject
$ dotnet new console
```

### Add the Gofer.NET NuGet package.

```bash
$ dotnet add package Gofer.NET --version 1.0.0-*
```

### Queue up some jobs.

This example `Program.cs` shows how to queue jobs for the worker pool to process, then start a worker to go and run them. 

Some important notes:
 - Workers would usually be on a separate machine from the code queueing the jobs, this is purely to give an example.

 - More workers can be added at any time, and will start picking up jobs off the queue immediately.

```c#
public class Program
{
    public static async Task Main(string[] args)
    {
        var redisConnectionString = "127.0.0.1:6379";
        
        // Create a Task Client connected to Redis
        var taskClient = new TaskClient(TaskQueue.Redis(redisConnectionString));
        
        // Queue up a Sample Job
        await taskClient.TaskQueue.Enqueue(() => SampleJobFunction("Hello World!"));
        
        // Start the task listener, effectively turning this process into a worker.
        // NOTE: This will loop endlessly waiting for new tasks.
        await taskClient.Listen();
    }
    
    private static void SampleJobFunction(object value)
    {
        Console.WriteLine(value.ToString());
    }
}
```

[Read the Docs](https://brthor.github.io/Gofer.NET/) for more details.
 
