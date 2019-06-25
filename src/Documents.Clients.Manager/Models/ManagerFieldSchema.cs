using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Documents.Clients.Manager.Models
{
    public class ManagerFieldBaseSchema : ModelBase
    {
        //public virtual string Type => "object";
        public string Title { get; set; }
        public string Description { get; set; }
        public string Placeholder { get; set; }
        public List<ManagerFieldSchemaValidator> Validators { get; set;}
        public Boolean? IsReadOnly { get; set; }
        public short? Order { get; set; }
        public string Format { get; set; }
        public int Columns { get; set; }
        public string AttributeStorageLocationKey { get; set; }
    }

    /// <summary>
    ///  Primitive types Number, String, Boolean
    /// </summary>
    public class ManagerFieldString : ManagerFieldBaseSchema
    {
        //public override string Type => "string";
        public List<EnumPair<string>> EnumList { get; set; }
        public string Default { get; set; }
    }

    public class ManagerFieldNumber<T> : ManagerFieldBaseSchema
    {
       // public override string Type => "number";
        public List<EnumPair<T>> EnumList { get; set; }
        // Here the default value could be an int, or a float, so we have to leave it as object
        public T Default { get; set; }
    }

    public class ManagerFieldDecimal : ManagerFieldNumber<decimal> { }
    public class ManagerFieldInt : ManagerFieldNumber<int> { }


    public class ManagerFieldBoolean : ManagerFieldBaseSchema
    {
        //public override string Type => "boolean";
        public Boolean Default { get; set; }
    }

    /// <summary>
    /// Complex types Array, and Object
    /// </summary>
    public class ManagerFieldArray : ManagerFieldBaseSchema
    {
        //public override string Type => "array";
        public Boolean? IsCollapsed { get; set; }
        public List<Object> Defaults { get; set; }
        public Dictionary<string, ManagerFieldBaseSchema> Properties { get; set; }
    }

    public class ManagerFieldObject : ManagerFieldBaseSchema
    {
       // public override string Type => "object";
        public Boolean? IsCollapsed { get; set; }
        public List<Object> Defaults { get; set; }
        public Dictionary<string, ManagerFieldBaseSchema> Properties { get; set; }
    }

    public class ManagerFieldNull : ManagerFieldBaseSchema
    {
    }

    /// <summary>
    /// Helpers around enumerations.
    /// </summary>
    public class EnumPair<T>
    {
        public T Key { get; set; }
        public string DisplayText { get; set; }
    }
}
