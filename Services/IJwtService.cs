using WardrobeApi.Models;

namespace WardrobeApi.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
