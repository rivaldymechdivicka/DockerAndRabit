using Microsoft.EntityFrameworkCore;
using UserAppService.Models;

namespace UserAppService.Data
{
    public class UserAppServiceContext : DbContext
    {
        public UserAppServiceContext(DbContextOptions<UserAppServiceContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
