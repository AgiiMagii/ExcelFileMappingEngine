using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Database.Entities
{
    public class FileEntity
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? FingerPrint { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
