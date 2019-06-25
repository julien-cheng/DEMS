using System;
using System.Collections.Generic;
using System.Text;

namespace Documents.Clients.Manager.Models
{
    public class PatternRegex: ModelBase
    {
        public string Pattern { get; set; }
        /// <summary>
        /// Pattern matching for negation is harder in regex.  
        /// This flag will allow us to specify which regex's are allowed, vs regexs of things that aren't allowed.
        /// </summary>
        public bool IsAllowed { get; set; }
    }
}
