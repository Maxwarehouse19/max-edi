using System;

namespace max_edi
{
    public class EDILine
    {
        public int LineNumber { get; set; }
        public string LineText { get; set; }
        public string[] Values { get; set; }
        public string Type { get; set; }
    }
}
