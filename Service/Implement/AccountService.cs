using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Repository.Interface;
using Service.Interface;
using ViewModel.Account;

namespace Service.Implement
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;

        public AccountService(IAccountRepository accountRepository, IUnitOfWork unitOfWork, IConfiguration config)
        {
            _accountRepository = accountRepository;
            _unitOfWork = unitOfWork;
            _config = config;
        }

        public async Task<ServiceResult<SystemAccount>> Login(string email, string password)
        {
            var adminEmail = _config["AdminAccount:Email"]; 
            var adminPassword = _config["AdminAccount:Password"];

            if (!string.IsNullOrWhiteSpace(adminEmail)
                && !string.IsNullOrWhiteSpace(adminPassword)
                && string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase)
                && password == adminPassword)
            {
                var admin = new SystemAccount
                {
                    AccountId = 0,
                    AccountEmail = adminEmail,
                    AccountName = "Administrator",
                    AccountRole = (int)AccountRole.Admin
                };

                return ServiceResult<SystemAccount>.Ok(admin,
                    "Administrator login successful.");
            }

            var account = await _accountRepository.GetUserByEmail(email);

            if (account == null)
                return ServiceResult<SystemAccount>.Fail("Invalid email or password.");

            if (account.AccountPassword != password)
                return ServiceResult<SystemAccount>.Fail("Invalid email or password.");

            return ServiceResult<SystemAccount>.Ok(account, "Login successful.");
        }

        public async Task<ServiceResult<SystemAccount>> LoginWithGoogleAsync(
    string googleId,
    string email,
    string name)
        {
            // 1️⃣ Tìm theo GoogleId
            var account = await _accountRepository.GetByGoogleIdAsync(googleId);

            if (account != null)
                return ServiceResult<SystemAccount>.Ok(account, "Login By Google successfully!!");

            // Nếu chưa có GoogleId, check email
            account = await _accountRepository.GetUserByEmail(email);

            if (account != null)
            {
                // Link Google vào account cũ
                account.GoogleId = googleId;
                account.IsGoogleAccount = true;

                _accountRepository.Update(account);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult<SystemAccount>.Ok(account, "Linked to Google successfully!!");
            }

            // 3️⃣ Nếu không tồn tại -> tạo mới
            account = new SystemAccount
            {
                AccountEmail = email,
                AccountName = name,
                GoogleId = googleId,
                IsGoogleAccount = true,
                AccountRole = (int)AccountRole.Lecturer,
                CreatedAt = DateTime.UtcNow
            };

            await _accountRepository.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();


            return ServiceResult<SystemAccount>.Ok(account, "Create Account Google successfully!!");
        }
        public async Task<ServiceResult<SystemAccount>> GetAccountById(short id)
        {
            if (id == 0)
            {
                var adminEmail = _config["AdminAccount:Email"];
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    return ServiceResult<SystemAccount>.Ok(new SystemAccount
                    {
                        AccountId = 0,
                        AccountName = "Administrator",
                        AccountEmail = adminEmail,
                        AccountRole = (int)AccountRole.Admin
                    });
                }
            }

            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
                return ServiceResult<SystemAccount>.Fail("Account not found.");
            return ServiceResult<SystemAccount>.Ok(account);
        }

        public async Task<ServiceResult<bool>> UpdateAccount(SystemAccount account, short currentUserId)
        {
            if (account.AccountId == 0)
            {
                return ServiceResult<bool>.Fail("The configured administrator account cannot be edited from the database.");
            }

            var existing = await _accountRepository.GetByIdAsync(account.AccountId);
            if (existing == null)
                return ServiceResult<bool>.Fail("Account not found.");

            if (!string.IsNullOrWhiteSpace(account.AccountEmail))
            {
                var duplicated = await _accountRepository.GetUserByEmail(account.AccountEmail);
                if (duplicated != null && duplicated.AccountId != account.AccountId)
                {
                    return ServiceResult<bool>.Fail("An account with this email already exists.");
                }
            }

            // ADMIN RESTRICTIONS:
            // 1. Cannot change role to Admin (0) if not self or specifically allowed (user said only staff/lecturer)
            if (account.AccountRole == (int)AccountRole.Admin && account.AccountId != currentUserId)
            {
                return ServiceResult<bool>.Fail("You cannot promote other users to Administrator.");
            }

            // 2. Admin cannot change others' Email or Password
            if (account.AccountId != currentUserId)
            {
                // Only fail if a DIFFERENT email is explicitly provided (not null/empty from disabled field)
                if (!string.IsNullOrEmpty(account.AccountEmail) && existing.AccountEmail != account.AccountEmail)
                {
                    return ServiceResult<bool>.Fail("Administrators are not allowed to change other users' email addresses.");
                }

                // Verify if password is being changed (non-empty in the input model)
                if (!string.IsNullOrEmpty(account.AccountPassword))
                {
                    return ServiceResult<bool>.Fail("Administrators are not allowed to reset other users' passwords.");
                }
            }
            else
            {
                // If editing self, allow valid changes
                existing.AccountEmail = account.AccountEmail;
                if (!string.IsNullOrEmpty(account.AccountPassword))
                {
                    existing.AccountPassword = account.AccountPassword;
                }
            }

            existing.AccountName = account.AccountName;
            existing.AccountRole = account.AccountRole;

            _accountRepository.Update(existing);
            var result = await _unitOfWork.SaveChangesAsync();

            return result >= 0 
                ? ServiceResult<bool>.Ok(true, "Account updated successfully.")
                : ServiceResult<bool>.Fail("Failed to update account.");
        }

        public async Task<ServiceResult<bool>> ChangePassword(short accountId, string oldPassword, string newPassword)
        {
            if (accountId == 0)
                return ServiceResult<bool>.Fail("The configured administrator password must be changed in appsettings.json.");

            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
                return ServiceResult<bool>.Fail("Account not found.");

            if (account.AccountPassword != oldPassword)
                return ServiceResult<bool>.Fail("Incorrect current password.");

            account.AccountPassword = newPassword;
            _accountRepository.Update(account);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? ServiceResult<bool>.Ok(true, "Password changed successfully.")
                : ServiceResult<bool>.Fail("Failed to change password.");
        }

        public async Task<AccountManagementViewModel> GetAccountManagementAsync(
            string? search,
            int pageNumber,
            int pageSize)
        {
            var query = _accountRepository.GetAllAsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => 
                    (a.AccountName != null && a.AccountName.Contains(search)) ||
                    (a.AccountEmail != null && a.AccountEmail.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .Include(a => a.NewsArticles)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ViewModel.Account.AccountItemViewModel
                {
                    AccountId = a.AccountId,
                    FullName = a.AccountName,
                    Email = a.AccountEmail,
                    Role = a.AccountRole,
                    RoleName = a.AccountRole == (int)AccountRole.Admin ? "Administrator" : 
                               a.AccountRole == (int)AccountRole.Staff ? "Staff" : "Lecturer",
                    CreatedAt = a.CreatedAt,
                    CreatedNewsCount = a.NewsArticles.Count
                })
                .ToListAsync();

            // Stats
            var allAccounts = await _accountRepository.GetAllAsQueryable().ToListAsync();
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);

            return new ViewModel.Account.AccountManagementViewModel
            {
                Accounts = items,
                Stats = new ViewModel.Account.AccountStatsViewModel
                {
                    TotalAccounts = allAccounts.Count,
                    AdminCount = allAccounts.Count(a => a.AccountRole == (int)AccountRole.Admin),
                    StaffCount = allAccounts.Count(a => a.AccountRole == (int)AccountRole.Staff),
                    NewAccountsThisMonth = allAccounts.Count(a => a.CreatedAt >= thisMonthStart)
                },
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search
            };
        }

        public async Task<ServiceResult<bool>> AddAccount(SystemAccount account)
        {
            try
            {
                account.AccountEmail = account.AccountEmail?.Trim();
                account.AccountName = account.AccountName?.Trim();

                if (string.IsNullOrWhiteSpace(account.AccountEmail))
                {
                    return ServiceResult<bool>.Fail("Email is required.");
                }

                // Unique Email Check
                var existing = await _accountRepository.GetUserByEmail(account.AccountEmail);
                if (existing != null)
                {
                    return ServiceResult<bool>.Fail("An account with this email already exists.");
                }

                // Admin (0) is not allowed for regular creation via Management
                if (account.AccountRole == (int)AccountRole.Admin)
                {
                    return ServiceResult<bool>.Fail("You cannot create a new Administrator account.");
                }

                account.CreatedAt = DateTime.UtcNow;
                await _accountRepository.AddAsync(account);
                await _unitOfWork.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true, "Account created successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> DeleteAccount(short id)
        {
            try
            {
                if (id == 0)
                    return ServiceResult<bool>.Fail("The configured administrator account cannot be deleted.");

                var account = await _accountRepository.GetAllAsQueryable()
                    .Include(a => a.NewsArticles)
                    .FirstOrDefaultAsync(a => a.AccountId == id);

                if (account == null) return ServiceResult<bool>.Fail("Account not found.");

                if (account.NewsArticles.Any())
                {
                    return ServiceResult<bool>.Fail("Cannot delete this account because it has created news articles.");
                }
                
                _accountRepository.Remove(account);
                await _unitOfWork.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true, "Account deleted successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        public enum AccountRole
        {
            Admin = 0,
            Staff = 1,
            Lecturer = 2
        }
    }
}
