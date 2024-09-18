using JobOnlineAPI.Models;
using Microsoft.AspNetCore.Builder;

namespace JobOnlineAPI.Repositories
{
    public interface IJobApplicationRepository
    {
        Task<int> AddJobApplicationAsync(JobApplication jobApplication);
    }
}
