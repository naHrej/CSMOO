using System;
using CSMOO.Server.Session;

namespace CSMOO.Server.Session;

public record SessionInfo(Guid ClientGuid, IClientConnection Connection);
