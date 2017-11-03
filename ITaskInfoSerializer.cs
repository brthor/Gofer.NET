namespace Gofer.NET
{
    public interface ITaskInfoSerializer
    {
        string Serialize(TaskInfo taskInfo);

        TaskInfo Deserialize(string taskInfoJsonString);
    }
}