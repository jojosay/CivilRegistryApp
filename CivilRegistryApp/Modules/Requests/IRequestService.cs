using CivilRegistryApp.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Requests
{
    public interface IRequestService
    {
        Task<IEnumerable<DocumentRequest>> GetAllRequestsAsync();
        Task<DocumentRequest> GetRequestByIdAsync(int id);
        Task<IEnumerable<DocumentRequest>> GetRequestsByStatusAsync(string status);
        Task<DocumentRequest> AddRequestAsync(DocumentRequest request);
        Task UpdateRequestStatusAsync(int requestId, string status);
        Task DeleteRequestAsync(int id);
    }
}
