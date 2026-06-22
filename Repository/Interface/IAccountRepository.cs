using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObjects;

namespace Repository.Interface
{
    public interface IAccountRepository : IGenericRepository<SystemAccount, short>
    {
        Task<SystemAccount?> GetUserByEmail(string email);
        Task<SystemAccount?> GetByGoogleIdAsync(string googleId);
   
    }
}
