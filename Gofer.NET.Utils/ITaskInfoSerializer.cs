namespace Gofer.NET.Utils
{
    public interface ITaskInfoSerializer
    {
        string Serialize(TaskInfo taskInfo);

        TaskInfo Deserialize(string taskInfoJsonString);
    }
}