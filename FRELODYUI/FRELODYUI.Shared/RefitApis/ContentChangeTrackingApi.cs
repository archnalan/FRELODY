using FRELODYSHRD.Models.ViewModels;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IContentChangeTrackingApi
    {
        [Get("/api/content-change-tracking/get-activity-since-last-login")]
        Task<IApiResponse<DashboardActivitySummary>> GetActivitySinceLastLogin([Query]string userId);
    }
}
