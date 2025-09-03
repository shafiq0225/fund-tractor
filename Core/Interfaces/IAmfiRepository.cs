using Core.Entities.AMFI;
using System;

namespace Core.Interfaces;

public interface IAmfiRepository
{
    Task ImportAmfiDataAsync(string rawData);
    //Task<bool> SetFundApprovalAsync(string fundId, bool isApproved);
    Task<(bool Success, string Message)> AddApprovedSchemeAsync(string fundName, string schemeId, bool isApproved);
    Task<(bool Success, string Message)> UpdateApprovedSchemeAsync(string fundName, string schemeId, bool isApproved);
    //Task<bool> SaveChangesAsync();

    Task<List<ApprovedData>> GetApprovedSchemesAsync();
    Task AddSchemeDetailsAsync(List<SchemeDetail> schemes);
}
