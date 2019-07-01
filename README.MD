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

## A Minimal Example

```c#
public static async Task Main(string[] args)
{
    var redisConnectionString = "127.0.0.1:6379";

    var taskQueue = TaskQueue.Redis(redisConnectionString);
    
    await taskQueue.Enqueue(() => Console.WriteLine("echo"));
    await taskQueue.ExecuteNext();
}
```
 
## Getting Started

[Read the Getting Started Guide.](https://brthor.github.io/Gofer.NET/#getting-started)
 
