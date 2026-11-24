using FileMappingEngine.Lib.Interfaces;
using System;
using System.Collections.Concurrent;

namespace FileMappingEngine.Lib.Sessions
{
    public class InMemorySessionStore : ISessionStore
    {
        private readonly ConcurrentDictionary<Guid, DataSession> _sessions = new();

        public DataSession Create(string ownerId)
        {
            var session = new DataSession
            {
                SessionId = Guid.NewGuid(),
                OwnerId = ownerId
            };
            _sessions[session.SessionId] = session;
            return session;
        }

        public DataSession? Get(Guid sessionId, string ownerId)
        {
            return _sessions.TryGetValue(sessionId, out var session) && session.OwnerId == ownerId
                ? session
                : null;
        }

        public void Remove(Guid sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }
}
