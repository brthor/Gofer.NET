---
layout: default
title: Jobs
nav_order: 3
description: "Gofer.NET is an easy C# API for distributed background tasks/jobs for .NET Core."
permalink: /jobs

explicit_nav_children:
    - url:  /jobs#fire-and-forget-jobs
      title: Fire-And-Forget Jobs
    - url:  /jobs#scheduled-jobs
      title: Scheduled Jobs
    - url:  /jobs#recurring-jobs
      title: Recurring Jobs

show_nav_children: false
---

## Job Functions

When you call `TaskQueue.Enqueue(...);`, the expression passed to `.Enqueue()` is parsed to extract the function (and arguments) you wish to execute on the worker. 

#### We Recommend Using Static Functions
{: .mt-6 .mb-3 }

To keep things simple, we recommend using `private` or `public` `static` functions with few or no arguments that are a part of your own codebase. Using `System` (built-in) functions (like `Console.WriteLine`) works in some cases, but in other cases doesn't seem to work at all.

```c#
public static async Task Main()
{
    var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
    
    await taskQueue.Enqueue(() => DoWork());
}

private static void DoWork()
{
    Console.WriteLine("Doing work...");
}
```

#### Non-Static Functions Will Run the Constructor
{: .mt-6 .mb-3 }

If the queued function is not `static`, the worker will create an instance of the class before executing the method, causing the constructor to be run. For this to work, there must be a Constructor with no arguments. If there is not a constructor with no arguments, an error will be thrown by the worker.

```c#
public class Program
{
    public Program()
    {
        Console.WriteLine("The constructor will be run for non-static queued functions");
    }

    public static async Task Main()
    {
        var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
        
        await taskQueue.Enqueue(() => DoWork());
    }

    private void DoWork()
    {
        Console.WriteLine("Doing work...");
    }
}
```

#### No Special Treatment for Async Functions
{: .mt-6 .mb-3 }

The queued function may be `async` and will behave as expected on the worker. Queuing `async` functions is the same as queuing non-async functions. Using `await` in the `.Enqueue()` method will not work.

```c#
// Enqueue async methods the same way as non-async.
await taskQueue.Enqueue(() => MyAsyncFunc());
```

```c#
// Bad, don't use async await in .Enqueue()
await taskQueue.Enqueue(async () => await MyAsyncFunc());
```


### Job Arguments

When you queue a job function to be run, the arguments passed in will be captured, serialized, persisted in Redis, and passed to the worker during execution time. 

#### Use Simple Arguments
{: .mt-6 .mb-3 }

For best performance, use simple argument types (like strings or integers) that won't take take up too many bytes. When passing database models, it's always a good practice to pass the id of the model, and re-fetch the data on the worker.

```c#
// You can pass literals as function arguments.
await taskQueue.Enqueue(() => Console.WriteLine("A string"));
```

```c#
// Or pass variables as function arguments.
var aString = "A string";
await taskQueue.Enqueue(() => Console.WriteLine(aString));
```

#### Nested Function Arguments are Run Immediately
{: .mt-6 .mb-3 }

Using a nested function argument will cause it to be run immediately, and it's return value passed onto the workers.
In the following example, `MyFunction` is run immediately (before the job is queued), and `"My String"` is passed to the workers as the argument to `Console.WriteLine` as a part of the job. 
```c#
Func<string> MyFunction = () => "My String";

// MyFunction() will be run immediately, not on the worker
await taskQueue.Enqueue(() => Console.WriteLine(MyFunction()));
```

### Idempotence

As a best practice, all jobs should be idempotent. Putting it simply, if a job is run more than once, everything should still be okay, and the results the same as if it were run once.

While it's not expected to run jobs multiple times, a failure in a worker node could cause this situation to arise.

This is a common best practice when using distributed job systems.

## Job Types

Jobs can be queued:
 - to be run immediately by the first available worker (one-off / fire-and-forget),
 - to be run only after a specific `DateTime` (scheduled),
 - or on a recurring basis (recurring)

### Fire-And-Forget Jobs

Fire-and-forget jobs are very simple, and are the same as have been demonstrated above.

 - Conceptually, it is just a function that is queued to be run on the workers.
 - These jobs cannot be canceled or updated once queued to run.

```c#
public class Program
{
    public static async Task Main()
    {
        var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
        
        await taskQueue.Enqueue(() => DoWork());
    }

    private static void DoWork()
    {
        Console.WriteLine("Doing work...");
    }
}
```

### Scheduled Jobs

Scheduled jobs are jobs that will only be run once, but not until at least a certain `DateTime`.

 - The `TaskScheduler` runs as a part of the worker, so at least one running worker is required for Scheduled Jobs execution times to be monitored.
 - If the task queue is filling faster than workers are running jobs, a scheduled job may be run after the specified DateTime, but never before it.
 - Incorrect system time set on the worker may cause unexpected run times.
 - Scheduling a job in the past will cause it to be run immediately (with a slight delay).

#### Set up Two ScheduledTasks
{: .mt-6 .mb-3 }

```c#
public class Program
{
    public static async Task Main()
    {
        var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
        var taskClient = new TaskClient(taskQueue);

        // Schedule a Task for 5 minutes in the future.
        await taskClient.TaskScheduler.AddScheduledTask(() => WriteDate(), TimeSpan.FromMinutes(5));

        // Schedule a Task to be run at a specific DateTime
        DateTime runTime = DateTime.UtcNow + TimeSpan.FromDays(1);
        await taskClient.TaskScheduler.AddScheduledTask(() => WriteDate(), runTime);
    }

    private void WriteDate()
    {
        Console.WriteLine(DateTime.UtcNow.ToString());
    }
}
```

#### Canceling a ScheduledTask
{: .mt-6 .mb-3 }

ScheduledTasks can be canceled using one of two methods, demonstrated in this example.

```c#
public class Program
{
    public static async Task Main()
    {
        var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
        var taskClient = new TaskClient(taskQueue);

        // Schedule a Task for 5 minutes in the future.
        var scheduledTask = await taskClient.TaskScheduler
            .AddScheduledTask(() => WriteDate(), TimeSpan.FromMinutes(5));

        // Method 1: Cancel with the ScheduledTask Object
        await taskClient.TaskScheduler.CancelScheduledTask(scheduledTask);

        // Method 2: Cancel with the ScheduledTask TaskKey
        await taskClient.TaskScheduler.CancelTask(scheduledTask.TaskKey);
    }

    private void WriteDate()
    {
        Console.WriteLine(DateTime.UtcNow.ToString());
    }
}
```


### Recurring Jobs

Recurring Jobs are set up to be run on an interval. You may specify a `TimeSpan` based interval, or use a 6-part crontab to specify the interval.

If there are no running workers, recurring tasks will not just stack up on the queue to be run later. At least one running worker is required to process recurring tasks. If the workers are down for a while, recurring tasks will run at the next interval when the workers are started back up, but won't run previous intervals.

Recurring tasks may be updated or canceled. They will continue to run until cancelled.

When you create a recurring task, you specify a unique name for that task. You will need this name to update or cancel the recurring task.

#### Set Up Two Recurring Tasks
{: .mt-6 .mb-3 }

```c#
public class Program
{
    public static async Task Main()
    {
        var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
        var taskClient = new TaskClient(taskQueue);

        // Print the Time every Five Minutes, using a TimeSpan interval
        await taskClient.TaskScheduler.AddRecurringTask(() => WriteDate(), 
            TimeSpan.FromMinutes(5), "five-minute-timespan");

        // Print the Time every Seven Minutes, using a Crontab interval
        await taskClient.TaskScheduler.AddRecurringTask(() => WriteDate(), 
            "0 */7 * * * *", "seven-minute-crontab");
    }

    private void WriteDate()
    {
        Console.WriteLine(DateTime.UtcNow.ToString());
    }
}
```

#### Update a Recurring Task
{: .mt-6 .mb-3 }

In this example, we set up a recurring task, demonstrate how it can be updated, then finally cancel it.

Anytime you call `.AddRecurringTask()` with an already existing task name, the existing task will be overwritten, stop running immediately, and the newly specified recurring task will take its place.

```c#
public class Program
{
    public static async Task Main()
    {
        var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
        var taskClient = new TaskClient(taskQueue);

        // Set up a Recurring Task
        await taskClient.TaskScheduler.AddRecurringTask(() => Write("hello!"), 
            TimeSpan.FromMinutes(5), "my-recurring-task");

        // Update the Interval
        await taskClient.TaskScheduler.AddRecurringTask(() => Write("hello!"), 
            TimeSpan.FromMinutes(7), "my-recurring-task");

        // Change the Interval to Crontab, Change function argument
        var recurringTask = await taskClient.TaskScheduler.AddRecurringTask(() => Write("crontab!"), 
            "0 */7 * * * *", "my-recurring-task");

        await taskClient.TaskScheduler.CancelRecurringTask(recurringTask);
    }

    private void Write(string value)
    {
        Console.WriteLine(value);
    }
}
```


#### Cancel a Recurring Task
{: .mt-6 .mb-3 }

Recurring Tasks can be canceled at any time after they are created, in one of three ways. All three ways are demonstrated in this example. 

It's important to note that when using the `TaskScheduler.CancelTask()` method, the argument is the `TaskKey` which is not the same as the task name you pass into `.AddRecurringTask()`. 

```c#
public class Program
{
    public static async Task Main()
    {
        var taskQueue = TaskQueue.Redis("127.0.0.1:6379");
        var taskClient = new TaskClient(taskQueue);

        // Set up a Recurring Task
        var recurringTask = await taskClient.TaskScheduler.AddRecurringTask(() => Write("hello!"), 
            TimeSpan.FromMinutes(5), "my-recurring-task");

        // Method 1: Cancel the RecurringTask with the RecurringTask object
        await taskClient.TaskScheduler.CancelRecurringTask(recurringTask);

        // Method 2:  Cancel the RecurringTask with the RecurringTask name
        await taskClient.TaskScheduler.CancelRecurringTask("my-recurring-task");

        // Method 3:  Cancel the RecurringTask with the RecurringTask.TaskKey
        //   NOTE: recurringTask.TaskKey is NOT the same as the task name "my-recurring-task"
        //         TaskScheduler.CancelTask() can also be used to cancel ScheduledTasks
        await taskClient.TaskScheduler.CancelTask(recurringTask.TaskKey);
    }

    private void Write(string value)
    {
        Console.WriteLine(value);
    }
}
```
