
namespace FileMappingEngine.Lib.Models
{
    public class MappingSet
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public int HeaderRow { get; set; }

        public List<ActionStep> Steps { get; set; } = new();
    }
}
