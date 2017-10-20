namespace Thor.Tasks
{
    public class TaskQueueConfiguration
    {
        public string QueueName { get; set; }

        public static TaskQueueConfiguration Default()
        {
            return new TaskQueueConfiguration
            {
                QueueName = "Thor.Tasks.Default"
            };
        }
    }
}