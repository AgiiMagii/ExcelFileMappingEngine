using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Interfaces
{
    public interface ISessionStore
    {
        DataSession Create(string ownerId);
        DataSession? Get(Guid sessionId, string ownerId);
        void Remove(Guid sessionId);
    }
}
