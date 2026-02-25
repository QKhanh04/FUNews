using System;
using System.Collections.Generic;

namespace ViewModel.Account
{
    public class AccountManagementViewModel
    {
        public List<AccountItemViewModel> Accounts { get; set; } = new();
        public AccountStatsViewModel Stats { get; set; } = new();
        
        // Paging & Search
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }

    public class AccountItemViewModel
    {
        public short AccountId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int? Role { get; set; } 
        public string? RoleName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AccountStatsViewModel
    {
        public int TotalAccounts { get; set; }
        public int AdminCount { get; set; }
        public int StaffCount { get; set; }
        public int NewAccountsThisMonth { get; set; }
    }
}
