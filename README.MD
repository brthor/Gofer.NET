# Thor.Tasks
version: 0.0.1-alpha

![Build Status](https://travis-ci.org/brthor/Thor.Tasks.svg?branch=master)
![Myget Version Number](https://img.shields.io/myget/brthor/Thor.Tasks/YoloDev.Dnx.FSharp.svg

The simplest possible way to run background tasks in c#. 

Keeping the code simple promotes good practices that avoid pain in delegate serialization over the network.

TODO:
 - Task Chains
 - Support more delegate types (lambda expressions?)
 - Task Scheduler Tasks with arguments
 - Make things actually work distributed
 
## Getting Started

We recommend using the dotnet cli, to get started, but it's not a necessity.
Thor.Tasks is a `netstandard2.0` project, so you will need the 2.0.0 version of the dotnet cli.

```
$ dotnet --version
2.0.0
```
 
Getting started is easy. First create a new project:

`dotnet new console`

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

Finally, setup your Program.cs like so:

```c#
public class Program
{
    public static void Main(string[] args)
    {
        var redisConnectString = "..."

        var taskClient = new TaskClient(TaskQueue.Redis(redisConnectString));
        
        taskClient.TaskQueue.Enqueue(RunTaskNow);
        taskClient.TaskScheduler.AddScheduledTask(ScheduledTask, TimeSpan.FromSeconds(5), "scheduledTaskName");
        
        taskClient.Listen();
    }
    
    private static void RunTaskNow()
    {
        Console.WriteLine("Now!");
    }
    
    private static void ScheduledTask()
    {
        Console.WriteLine("Then!");
    }
}
```

TODO See complete docs.
 