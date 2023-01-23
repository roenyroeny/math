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
    }

    public class Vector
    {
        public Scalar scalar;
        public int components;
        public string TypeName
        {
            get
            {
                return $"{scalar.TypeName}_{components}";
            }
        }

        public string[] Components
        {
            get
            {
                switch (components)
                {
                    case 1:
                        return new string[] { "x" };
                    case 2:
                        return new string[] { "x", "y" };
                    case 3:
                        return new string[] { "x", "y", "z" };
                    case 4:
                        return new string[] { "x", "y", "z", "w" };
                    default:
                        return new string[] { "UNKNOWN" };
                }
            }
        }

        public string Constructors
        {
            get
            {
                string s = "";
                // default ctor
                s += $"\t{TypeName}() = default;\n";

                s += $"\texplicit {TypeName}({scalar.TypeName} v) : ";
                for (int i = 0; i < components; i++)
                {
                    if (i != 0)
                        s += ", ";
                    s += $"{Components[i]}(v)";
                }
                s += " {}\n";
                s += $"\texplicit {TypeName}(";
                for (int i = 0; i < components; i++)
                {
                    if (i != 0)
                        s += ", ";

                    s += $"{scalar.TypeName} _{Components[i]}";
                }
                s += ") : ";
                for (int i = 0; i < components; i++)
                {
                    if (i != 0)
                        s += ", ";

                    s += $"{Components[i]}(_{Components[i]})";
                }
                s += " {}\n";


                return s;
            }
        }

        public string Members
        {
            get
            {
                string s = "\tunion\n\t{\t\n";
                s += $"\t\t{scalar.TypeName} c[{components}];\n";
                s += $"\t\tstruct {{ {scalar.TypeName} ";
                for (int i = 0; i < components; i++)
                {
                    if (i != 0)
                        s += ", ";
                    s += $"{Components[i]}";
                }
                s += "; };\n";
                return s + "\t};\n";
            }
        }

        string PerCompFunc(string func, string func2)
        {
            string s = $"static {TypeName} {func}({TypeName} a) ";
            s += "{";
            s += $" return {TypeName}(";
            for (int i = 0; i < components; i++)
            {
                if (i != 0)
                    s += ", ";

                s += $"{func2}(a.{Components[i]})";
            }
            s += "); }";
            return s;
        }
        string PerCompFunc2(string func, string func2)
        {
            string s = $"static {TypeName} {func}({TypeName} a, {TypeName} b) ";
            s += "{";
            s += $" return {TypeName}(";
            for (int i = 0; i < components; i++)
            {
                if (i != 0)
                    s += ", ";

                s += $"{func2}(a.{Components[i]}, b.{Components[i]})";
            }
            s += "); }";
            return s;
        }
        string OperatorIndex()
        {
            string s = $"\t{scalar.TypeName} operator [] (int i) const {{ return c[i]; }}";
            return s;
        }

        string OperatorPerComponent(string o, bool emitScalar=false)
        {
            string s = "";
            s += $"\tfriend {TypeName} operator {o} (const {TypeName}& a, const {TypeName}& b)" + " {";
            s += $" return {TypeName}(";
            for (int i = 0; i < components; i++)
            {
                if (i != 0)
                    s += ", ";

                s += $"a.{Components[i]} {o} b.{Components[i]}";
            }
            s += "); }\n";

            s += $"\tvoid operator {o}= (const {TypeName}& b)" + " { (*this) = (*this) + b; }";
            if(emitScalar)
            {
                s += $"\n\tfriend {TypeName} operator {o} (const {TypeName}& a, const {scalar.TypeName}& b)" + " {";
                s += $" return {TypeName}(";
                for (int i = 0; i < components; i++)
                {
                    if (i != 0)
                        s += ", ";

                    s += $"a.{Components[i]} {o} b";
                }
                s += "); }";
            }

            return s;
        }

        string OperatorCompare(string o, string c)
        {
            string s = "";
            s += $"\tfriend bool operator {o} (const {TypeName}& a, const {TypeName}& b)" + " {";
            s += $" return ";
            for (int i = 0; i < components; i++)
            {
                if (i != 0)
                    s += $" {c} ";

                s += $"a.{Components[i]} {o} b.{Components[i]}";
            }
            s += "; }";

            return s;
        }

        string OperatorNegate()
        {
            string s = $"\t{TypeName} operator -() const {{ return {TypeName}(";
            for (int i = 0; i < components; i++)
            {
                if (i != 0)
                    s += ", ";
                s += $"{Components[i]}";
            }
            s += "); }";
            return s;
        }

        string Dot()
        {
            string s = $"static {scalar.TypeName} dot({TypeName} a, {TypeName} b) {{ return ";

            for (int i = 0; i < components; i++)
            {
                if (i != 0)
                    s += " + ";

                s += $"a.{Components[i]} * b.{Components[i]}";
            }

            return s + "; }";
        }
        string Length()
        {
            string sqrt = scalar.bitness == 32 ? "sqrtf" : "sqrt";
            return $"static {scalar.TypeName} length({TypeName} v) {{ return {sqrt}(dot(v, v)); }}";
        }
        string Normalize()
        {
            return $"static {TypeName} normalize({TypeName} v) {{ return v / length(v); }}";
        }

        public string Functions
        {
            get
            {
                StringWriter writer = new StringWriter();
                if (scalar.Signed)
                    writer.WriteLine(PerCompFunc("abs", "abs"));
                writer.WriteLine(PerCompFunc2("min", "std::min"));
                writer.WriteLine(PerCompFunc2("max", "std::max"));
                writer.WriteLine(Dot());
                if (scalar.type == Scalar.Type.Float)
                {
                    writer.WriteLine(Length());
                    writer.WriteLine(Normalize());
                }

                return writer.ToString();
            }
        }
        public string Operators
        {
            get
            {
                StringWriter writer = new StringWriter();
                writer.WriteLine(OperatorIndex());
                if (scalar.Signed)
                    writer.WriteLine(OperatorNegate());
                writer.WriteLine(OperatorPerComponent("+"));
                writer.WriteLine(OperatorPerComponent("-"));
                writer.WriteLine(OperatorPerComponent("*", true));
                writer.WriteLine(OperatorPerComponent("/", true));
                writer.WriteLine(OperatorCompare("<", "&&"));
                writer.WriteLine(OperatorCompare(">", "&&"));
                writer.WriteLine(OperatorCompare("<=", "&&"));
                writer.WriteLine(OperatorCompare(">=", "&&"));
                writer.WriteLine(OperatorCompare("==", "&&"));
                writer.WriteLine(OperatorCompare("!=", "||"));

                return writer.ToString();
            }
        }

        public string Declaration
        {
            get
            {
                StringWriter writer = new StringWriter();
                writer.WriteLine($"struct {TypeName}");
                writer.WriteLine("{");
                writer.Write(Members);
                writer.Write(Constructors);
                writer.Write(Operators);
                writer.WriteLine("};");
                writer.Write(Functions);
                return writer.ToString();
            }
        }
    }

    public class Generator
    {
        List<Scalar> baseTypes = new List<Scalar>();
        List<Vector> Vectors = new List<Vector>();
        public Generator()
        {
            baseTypes.Add(new Scalar { type = Scalar.Type.Int, bitness = 16 });
            baseTypes.Add(new Scalar { type = Scalar.Type.Int, bitness = 32 });
            baseTypes.Add(new Scalar { type = Scalar.Type.Int, bitness = 64 });

            baseTypes.Add(new Scalar { type = Scalar.Type.Uint, bitness = 16 });
            baseTypes.Add(new Scalar { type = Scalar.Type.Uint, bitness = 32 });
            baseTypes.Add(new Scalar { type = Scalar.Type.Uint, bitness = 64 });

            // baseTypes.Add(new Scalar { type = Scalar.Type.Float, bitness = 16 });
            baseTypes.Add(new Scalar { type = Scalar.Type.Float, bitness = 32 });
            baseTypes.Add(new Scalar { type = Scalar.Type.Float, bitness = 64 });

            foreach (var v in baseTypes)
            {
                Vectors.Add(new Vector { scalar = v, components = 2 });
                Vectors.Add(new Vector { scalar = v, components = 3 });
                Vectors.Add(new Vector { scalar = v, components = 4 });
            }
        }

        public string GenerateScalar_H()
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("#ifndef SCALAR_H");
            writer.WriteLine("#define SCALAR_H");
            writer.WriteLine("#include <stdint.h>");
            foreach (var t in baseTypes)
                writer.WriteLine($"typedef {t.StdIntTypeName} {t.TypeName};");

            writer.WriteLine("#endif");
            return writer.ToString();
        }

        public string GenerateVector_H()
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("#ifndef VECTOR_H");
            writer.WriteLine("#define VECTOR_H");
            writer.WriteLine("#include \"scalar.h\"");
            writer.WriteLine("#include <cstdlib>");
            writer.WriteLine("#include <algorithm>");
            foreach (var t in Vectors)
                writer.WriteLine(t.Declaration);

            writer.WriteLine("#endif");
            return writer.ToString();
        }
    }
}
