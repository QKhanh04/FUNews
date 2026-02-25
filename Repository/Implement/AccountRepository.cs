using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repository.Interface;

namespace Repository.Implement
{
    public class AccountRepository : GenericRepository<SystemAccount, short>, IAccountRepository
    {
        public AccountRepository(FUNewsContext context) : base(context)
        {
        }
        public async Task<SystemAccount?> GetUserByEmail(string email)
        {
            return await _context.SystemAccounts
                .FirstOrDefaultAsync(a => a.AccountEmail == email);
        }

        public async Task<SystemAccount?> GetByGoogleIdAsync(string googleId)
        {
            if (string.IsNullOrWhiteSpace(googleId))
                return null;

            return await _context.SystemAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.GoogleId == googleId);
        }
    }
}
