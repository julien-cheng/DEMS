using System;
using System.Collections.Generic;
using System.Text;

namespace Documents.Clients.Manager.Models
{
    public class ManagerFieldSchemaValidator
    {
        public const string REQUIRED = "required";
        public const string MIN_LENGTH = "minLength";
        public const string MAX_LENGTH = "maxLength";
        public const string MAX = "max";
        public const string MIN = "min";
        public const string GREATER_THAN = "greaterThan";
        public const string LESS_THAN = "lessThan";
        public const string PATTERN = "pattern";
        public const string UNIQUE = "unique";

        public static readonly string CUSTOM = "custom";
        public string Type { get; set; }
        public string Value { get; set; }
        public string ErrorMessage { get; set; }
    }
}
