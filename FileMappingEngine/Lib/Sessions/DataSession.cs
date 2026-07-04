using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FileMappingEngine.Lib.Sessions
{
    public class DataSession
    {
        public FileState? File { get; set; }
        public DataState? Data { get; set; }
        public MappingSet MappingSet { get; set; } = new();
    }
}
