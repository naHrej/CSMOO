using System;
using CSMOO.Sessions;

namespace CSMOO.Sessions;

public record SessionInfo(Guid ClientGuid, IClientConnection Connection);

