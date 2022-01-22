using TimeReport.Domain.Entities;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Common.Interfaces;

public interface IItemsClient
{
    Task ItemAdded(User item);

    Task ItemDeleted(string id, string name);

    Task ImageUploaded(string id, string image);
}