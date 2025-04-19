using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Modules.Auth;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Requests
{
    public class RequestService : IRequestService
    {
        private readonly IDocumentRequestRepository _requestRepository;
        private readonly IAuthenticationService _authService;

        public RequestService(
            IDocumentRequestRepository requestRepository,
            IAuthenticationService authService)
        {
            _requestRepository = requestRepository;
            _authService = authService;
        }

        public async Task<IEnumerable<DocumentRequest>> GetAllRequestsAsync()
        {
            try
            {
                // Get all requests with related entities included
                var requests = await _requestRepository.GetAllWithDetailsAsync();

                // If there are no requests, return an empty list
                if (requests == null)
                    return new List<DocumentRequest>();

                return requests;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving all document requests");
                // Return an empty list instead of throwing an exception
                return new List<DocumentRequest>();
            }
        }

        public async Task<DocumentRequest> GetRequestByIdAsync(int id)
        {
            try
            {
                return await _requestRepository.GetRequestWithDetailsAsync(id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving document request with ID {RequestId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DocumentRequest>> GetRequestsByStatusAsync(string status)
        {
            try
            {
                return await _requestRepository.GetRequestsByStatusAsync(status);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving document requests with status {Status}", status);
                throw;
            }
        }

        public async Task<DocumentRequest> AddRequestAsync(DocumentRequest request)
        {
            try
            {
                request.RequestDate = DateTime.Now;
                request.Status = "Pending";

                await _requestRepository.AddAsync(request);
                await _requestRepository.SaveChangesAsync();

                Log.Information("Document request added: {RequestorName} for document {DocumentId}",
                    request.RequestorName, request.RelatedDocumentId);

                return request;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding document request for document {DocumentId}", request.RelatedDocumentId);
                throw;
            }
        }

        public async Task UpdateRequestStatusAsync(int requestId, string status)
        {
            try
            {
                if (_authService.CurrentUser == null)
                    throw new UnauthorizedAccessException("User must be logged in to update request status");

                var request = await _requestRepository.GetByIdAsync(requestId);
                if (request == null)
                    throw new KeyNotFoundException($"Document request with ID {requestId} not found");

                request.Status = status;
                request.HandledBy = _authService.CurrentUser.UserId;

                await _requestRepository.UpdateAsync(request);
                await _requestRepository.SaveChangesAsync();

                Log.Information("Document request {RequestId} status updated to {Status} by user {Username}",
                    requestId, status, _authService.CurrentUser.Username);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating status for document request with ID {RequestId}", requestId);
                throw;
            }
        }

        public async Task DeleteRequestAsync(int id)
        {
            try
            {
                if (_authService.CurrentUser == null)
                    throw new UnauthorizedAccessException("User must be logged in to delete requests");

                if (!_authService.IsUserInRole("Admin"))
                    throw new UnauthorizedAccessException("Only administrators can delete requests");

                var request = await _requestRepository.GetByIdAsync(id);
                if (request == null)
                    throw new KeyNotFoundException($"Document request with ID {id} not found");

                await _requestRepository.DeleteAsync(request);
                await _requestRepository.SaveChangesAsync();

                Log.Information("Document request {RequestId} deleted by user {Username}",
                    id, _authService.CurrentUser.Username);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting document request with ID {RequestId}", id);
                throw;
            }
        }
    }


}
