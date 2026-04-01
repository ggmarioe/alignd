namespace Alignd.Application.Interfaces;

public interface IParticipantTokenService
{
    string Generate(Guid participantId, Guid roomId);
    (Guid participantId, Guid roomId)? Validate(string token);
}
