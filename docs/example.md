---
layout: default
title: Guided Example
nav_order: 5
description: "Gofer.NET is an easy C# API for distributed background tasks/jobs for .NET Core."
permalink: /example
---

## Example Project Guide

We will walk through setting up a project structure, deploying workers, queuing jobs, and watching them run.

### Project Source

The full source code for this example project can be [found on github.](https://github.com/brthor/Gofer.NET/tree/master/example)

### Prerequisites

 - [Docker](https://docs.docker.com/install/)
 - [.NET Core SDK](https://dotnet.microsoft.com/download)

This same guide should work on Linux, and on Windows. If you're using Windows there may be some extra steps not covered here to enable Linux Containers for Windows (LCOW).

### Start Redis

Open up a terminal (we'll be needing it for the rest of the guide too), and run the following to start a local redis instance:

```bash
$ docker run -d -p 127.0.0.1:6379:6379 redis:4-alpine
```

### Create the Project Structure

First we will create two projects, one for our primary codebase (usually a website), and the other for our workers.

#### Create Primary Project
{: .mt-6 .mb-3 }

```bash
$ mkdir primary
$ dotnet new console
$ dotnet add package -v 1.0.0-* Gofer.NET 
```

We also need to add the `<LangVersion>7.1</LangVersion>` tag to a `<PropertyGroup>` in `primary.csproj` to enable async main.

Open up `primary.csproj` and the result should look like this:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.2</TargetFramework>
    <LangVersion>7.1</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Gofer.NET" Version="1.0.0-*" />
  </ItemGroup>

</Project>
```

#### Change Primary Project's Code
{: .mt-6 .mb-3 }

Replace `Program.cs` with the following code:

```c#
using System;
using System.Threading.Tasks;
using Gofer.NET;

namespace primary
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var redisConnectionString = "127.0.0.1:6379";
            
            // Create a Task Client connected to Redis
            var taskClient = new TaskClient(TaskQueue.Redis(redisConnectionString));
            
            // Queue up a Sample Fire-And-Forget Job
            await taskClient.TaskQueue.Enqueue(() => WriteValue("Hello World!"));

            // Queue up a job to be run in 30 seconds
            await taskClient.TaskScheduler
                .AddScheduledTask(() => WriteValue("Scheduled Task!"), TimeSpan.FromSeconds(30));

            // Queue up a Recurring Job for every 10 seconds
            await taskClient.TaskScheduler
                .AddRecurringTask(() => WriteDate(), TimeSpan.FromSeconds(10), "my-recurring-job");
        }
        
        private static void WriteValue(object value)
        {
            Console.WriteLine(value.ToString());
        }

        private static void WriteDate()
        {
            Console.WriteLine(DateTime.UtcNow.ToString());
        }
    }
}
```

#### Create Worker Project
{: .mt-6 .mb-3 }

```bash
$ cd ..
$ mkdir worker
$ cd worker
$ dotnet new console
$ dotnet add reference ../primary
```

#### Add LangVersion Property
{: .mt-6 .mb-3 }

We also need to add the `<LangVersion>7.1</LangVersion>` tag to a `<PropertyGroup>` in `worker.csproj` to enable async main.

Open up `worker.csproj` and the result should look like this:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\primary\primary.csproj" />
  </ItemGroup>

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.2</TargetFramework>
    <LangVersion>7.1</LangVersion>
  </PropertyGroup>

</Project>
```

#### Change Worker Project's Code
{: .mt-6 .mb-3 }

Replace `Program.cs` with the following code:

```c#
using System;
using System.Threading.Tasks;
using Gofer.NET;

namespace worker
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var redisConnectionString = "127.0.0.1:6379";
            
            // Create a Task Client connected to Redis
            var taskClient = new TaskClient(TaskQueue.Redis(redisConnectionString));

            Console.WriteLine("Listening for Jobs...");

            // Endlessly listen for jobs
            await taskClient.Listen();
        }
    }
}
```

#### Results
{: .mt-6 .mb-3 }

Our project structure now looks like this:
![Example project in vscode.](./img/example-project-vs-code.jpg 'Example project in vscode.')

### Run It

First open up an extra terminal, we'll need one for the worker, and one for the primary project.

#### Running the Worker
{: .mt-6 .mb-3 }

In the extra terminal, navigate to the worker project and run it.
```bash
$ cd worker
$ dotnet run
```

Your terminal output should look like this:
```bash
$ dotnet run
Listening for Jobs...
```

#### Running the Primary Project
{: .mt-6 .mb-3 }

Make sure to leave the extra terminal with the worker running.

Go back to the first terminal, navigate back to the primary project, and run it:
```bash
$ cd ../primary
$ dotnet run
```

#### Viewing the Results
{: .mt-6 .mb-3 }

Go back to the worker terminal, and you should see tasks being run immediately.

Wait 30 seconds until you see the scheduled task run and congrats now you can expand this example project to fit your needs.
