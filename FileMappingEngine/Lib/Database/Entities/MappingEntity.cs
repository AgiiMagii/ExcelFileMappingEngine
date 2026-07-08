using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FileMappingEngine.Lib.Database.Entities
{
    public class MappingEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long FileId { get; set; }
        public required string Name { get; set; }
        public required string Data { get; set; } // JSON data as string
        public DateTime CreatedAt { get; set; }
    }
}
