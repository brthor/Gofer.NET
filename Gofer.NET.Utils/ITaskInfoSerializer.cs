namespace Gofer.NET.Utils
{
    public interface x
    {
        string Serialize(TaskInfo taskInfo);

        TaskInfo Deserialize(string taskInfoJsonString);
    }
}