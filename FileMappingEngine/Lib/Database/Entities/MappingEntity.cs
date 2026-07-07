using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Database.Entities
{
    public class MappingEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long FileId { get; set; }
        public required string MappingName { get; set; }
        public required string JsonMapping { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
