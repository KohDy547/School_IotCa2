using CA2_Web.Data;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CA2_Web.Configurations
{
    public class AccessLevelRequirement : IAuthorizationRequirement
    {
        public int MinimumAccessLevel { get; set; }
        public AccessLevelRequirement(int requiredAccessLevel)
        {
            MinimumAccessLevel = requiredAccessLevel;
        }
    }
    public class AccessLevelHandler : AuthorizationHandler<AccessLevelRequirement>
    {
        private readonly ApplicationDbContext _ApplicationDbContext;
        public AccessLevelHandler(
            ApplicationDbContext ApplicationDbContext)
        {
            _ApplicationDbContext = ApplicationDbContext;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext inputContext, AccessLevelRequirement inputRequirement)
        {
            string currentUserId = inputContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            int currentUserAl = _ApplicationDbContext
                .UserProperties.ToArray()
                .Where(x => x.Id == currentUserId)
                .Select(x => x.AccessLevel)
                .First();

            if (currentUserAl >= inputRequirement.MinimumAccessLevel)
            {
                inputContext.Succeed(inputRequirement);
            }
            return Task.CompletedTask;
        }
    }
}
