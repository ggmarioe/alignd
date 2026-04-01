using Microsoft.AspNetCore.SignalR;

namespace Alignd.Infrastructure.Realtime;

/// <summary>
/// Marker hub used by SignalRRoomNotifier to obtain a typed IHubContext
/// without creating a dependency on the API layer.
/// VotingHub in the API project inherits from this class.
/// </summary>
public abstract class VotingHubMarker : Hub;
