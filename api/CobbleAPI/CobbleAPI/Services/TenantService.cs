using CobbleAPI.Interfaces;

namespace CobbleAPI.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _contextAccessor;
    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _contextAccessor = httpContextAccessor;
    }
    public Guid GetCurrentTenantId()
    {
        var tenantClaim = _contextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantClaim)) return Guid.Empty;

        return Guid.Parse(tenantClaim);
    }
}
