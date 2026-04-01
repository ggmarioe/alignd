namespace Alignd.Application.Interfaces;

public interface IAdminTokenService
{
    string Generate(Guid roomId);
    bool   Validate(string token, Guid roomId);
}
