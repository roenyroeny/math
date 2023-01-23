using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Xml.Schema;
using System.Security.Policy;

namespace generator
{
    public class Scalar
    {
        public enum Type
        {
            Float,
            Int,
            Uint,
        }

        public Type type;
        public int bitness;
        public bool Signed { get { return type != Type.Uint; } }

        public string Prefix
        {
            get
            {
                switch (type)
                {
                    case Type.Int:
                        return "i";
                    case Type.Uint:
                        return "u";
                    case Type.Float:
                        return "f";
                    default:
                        return "UNKNOWN";
                }
            }
        }

        public string TypeName
        {
            get
            {
                return $"{Prefix}{bitness}";
            }
        }

        public string StdIntTypeName
        {
            get
            {
                switch (type)
                {
                    case Type.Int:
                        return $"int{bitness}_t";
                    case Type.Uint:
                        return $"uint{bitness}_t";
                    case Type.Float:
                        switch (bitness)
                        {
                            case 16:
                                return $"half";
                            case 32:
                                return $"float";
                            case 64:
                                return $"double";
                            default:
                                return "UNKNOWN";
                        }
                    default:
                        return "UNKNOWN";
                }
            }
        }

        public string Declaration
        {
            get
            {
                return $"typedef {StdIntTypeName} {TypeName};";
            }
        }
    }
}
