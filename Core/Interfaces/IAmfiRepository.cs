using System;

namespace Core.Interfaces;

public interface IAmfiRepository
{
    Task ImportAmfiDataAsync(string rawData);
    Task<bool> SetFundApprovalAsync(string fundId, bool isApproved);
    Task<bool> SetSchemeApprovalAsync(string schemeId, bool isApproved);
    Task<bool> SaveChangesAsync();
}
