using Core.DTOs;
using Core.Entities.AMFI;

namespace Core.Interfaces;

public interface IAmfiRepository
{
    Task ImportAmfiDataAsync(string rawData);
    Task ImportAmfiDataFromExcelAsync(byte[] excelData);
    Task<(bool Success, string Message)> AddApprovedSchemeAsync(ApprovedSchemeDto addSchemeDto);
    Task<(bool Success, string Message)> UpdateApprovedSchemeAsync(string fundId, string schemeId, bool isApproved);
    Task<(bool Success, string Message)> UpdateApprovedFundAsync(string fundName, bool isApproved);
    Task<List<ApprovedData>> GetApprovedSchemesAsync();
    Task<(bool Success, string Message, List<ApprovedData>? Data)> GetSchemesListAsync();
    Task AddSchemeDetailsAsync(List<SchemeDetail> schemes);
    Task<(bool Success, string Message, List<SchemeDetail>? Data)> GetSchemesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<(bool Success, string Message, List<SchemeDetail>? schemeDetails)> GetSchemePerformance(string schemeCode);
    SchemePerformanceResponse TransformToPerformanceResponse(List<SchemeDetail> schemeDetails);
}
