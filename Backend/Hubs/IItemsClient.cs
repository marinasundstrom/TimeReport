using TimeReport.Data;

namespace TimeReport.Hubs;

public interface IItemsClient
{
    Task ItemAdded(User item);

    Task ItemDeleted(string id, string name);

    Task ImageUploaded(string id, string image);
}