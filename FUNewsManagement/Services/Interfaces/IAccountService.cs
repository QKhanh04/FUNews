using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using DataAccessObjects;

namespace FUNewsManagement.Services
{
    public interface IAccountService
    {
        Task<ServiceResult<SystemAccount>> Login(String email, string password);
        Task<ServiceResult<SystemAccount>> LoginWithGoogleAsync(string googleId,
                                                                string email,
                                                                string name);
        Task<ServiceResult<bool>> UpdateAccount(SystemAccount account, short currentUserId);
        Task<ServiceResult<bool>> ChangePassword(short accountId, string oldPassword, string newPassword);

        Task<ViewModel.Account.AccountManagementViewModel> GetAccountManagementAsync(
            string? search,
            int pageNumber,
            int pageSize);

        Task<ServiceResult<bool>> AddAccount(SystemAccount account);
        Task<ServiceResult<bool>> DeleteAccount(short id);
        Task<ServiceResult<SystemAccount>> GetAccountById(short id);

    }
}

