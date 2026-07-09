
using System.Text.Json.Serialization;

namespace FileMappingEngine.Lib.Models
{
    public class MappingSet
    {
        [JsonIgnore]
        public long? Id { get; set; }
        public string? Name { get; set; }
        public int HeaderRow { get; set; }

        public List<ActionStep> Steps { get; set; } = new();

        public FileDefinition? FileDefinition { get; set; }
    }
}
