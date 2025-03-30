using System.Collections.Generic;
using System.Threading.Tasks;
using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Interfaces
{
    public interface IRegroupApiService
    {
        Task<List<GroupResult>> GetGroupResultsAsync();
        Task<UserResponse?> GetGroupUsersAsync(string groupCodedName);
        Task<ContactResult?> GetContactAsync(string contactId, string contactName);
        Task CreateGroupAsync(GroupResult newGroup);
        Task AddContactToGroupsAsync(string email, List<string> groupNames);
        Task<bool> SendEmailAlertAsync(string subject, string body, List<string> emails, string fromEmail = "regroup@hotmail.com", string fromName = "Regroup");
        Task<List<ContactResult>> GetAllContactsAsync();
    }
} 