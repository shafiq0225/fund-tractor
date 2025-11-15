using Core.DTOs.Investment;
using Core.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.InvestmentRepo
{
    public interface IInvestmentRepository
    {
        Task<(bool Success, string Message, Investment? Data)> CreateInvestmentAsync(CreateInvestmentDto createDto, int createdByUserId, string investBy);
        Task<(bool Success, string Message, Investment? Data)> UpdateInvestmentAsync(int investmentId, UpdateInvestmentDto updateDto);
        Task<(bool Success, string Message, Investment? Data)> GetInvestmentByIdAsync(int investmentId);
        Task<(bool Success, string Message, List<Investment>? Data)> GetInvestmentsByInvestorAsync(int investorId);
        Task<(bool Success, string Message, List<Investment>? Data)> GetAllInvestmentsAsync();
        Task<(bool Success, string Message, List<Investment>? Data)> GetInvestmentsByStatusAsync(string status);
        Task<(bool Success, string Message)> DeleteInvestmentAsync(int investmentId);
        Task<(bool Success, string Message, List<Investment>? Data)> GetInvestmentsByApprovalStatusAsync(bool isApproved);
        Task<bool> InvestorExistsAsync(int investorId);
    }

}
