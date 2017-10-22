# Thor.Tasks

![Build Status](https://travis-ci.org/brthor/Thor.Tasks.svg?branch=master)
![Myget Version Number](https://img.shields.io/myget/thor/v/Thor.Tasks.svg?color=green)

## What is this?
Inspired by celery for python, this is a distributed job runner for .NET Standard 2.0. 
Run individual jobs or scheduled jobs on multiple machines.

Here's a quick example using the same machine to execute jobs:
```c#
    public static void Main(string[] args)
    {
        var redisConnectString = "..."

        var taskQueue = TaskQueue.Redis(redisConnectString);
        
        taskQueue.Enqueue(() => RunTask("echo"));
        taskQueue.ExecuteNext();
    }
    
    private static void RunTask(string message)
    {
        Console.WriteLine(message);
    }
```

Currently requires a Redis backend to function, but other backends may be developed relatively easily.
 
## Getting Started
*Get started with background jobs for your web app in 1 minute* 

#### Install the dotnet cli
We recommend using the dotnet cli, to get started, but it's not a necessity.
Thor.Tasks is a `netstandard2.0` project, so you will need the 2.0.0 version of the dotnet cli.

```
$ dotnet --version
2.0.0
```
 
#### Create a Project
Getting started is easy. First create a new project:

`dotnet new console`

#### Add our NuGet Package to your project
Add the prerelease feed to your project. Your `NuGet.config` should look like this:
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Thor.Tasks Prerelease" value="https://www.myget.org/F/thor/api/v3/index.json" />
  </packageSources>
</configuration>
```

Then add the Thor.Tasks package:

`dotnet package add Thor.Tasks`

#### Put it all together
You can enqueue a function with any arguments that can be serialized. See examples below.
Setup your Program.cs to run the task client like so:

```c#
public class Program
{
    public static void Main(string[] args)
    {
        var redisConnectString = "..."

        var taskClient = new TaskClient(TaskQueue.Redis(redisConnectString));
        
        var aStr = "astring";
        taskClient.TaskQueue.Enqueue(() => RunTaskNow(aStr));
        
        var myCustom = new CustomClass {Value="Custom!"};
        taskClient.TaskQueue.Enqueue(() => RunTaskNow(myCustom));
        
        // Run a scheduled task at a defined interval
        taskClient.TaskScheduler.AddScheduledTask(ScheduledTask, TimeSpan.FromSeconds(5), "scheduledTaskName");
        
        // Start the task listener, effectively turning this node into a worker.
        // At least one listener is required for scheduled or enqueued jobs to run.
        taskClient.Listen();
    }
    
    private static void RunTaskNow(object value)
    {
        Console.WriteLine(value.ToString());
    }
    
    private static void ScheduledTask()
    {
        Console.WriteLine("Scheduled!");
    }
    
    private class CustomClass
    {
        public string Value { get; set; }
        
        public override string ToString()
        {
            return Value;
        }
    }
}
```
 