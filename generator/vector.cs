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
                    s += $"{util.Components[i]}(v)";
                }
                s += " {}\n";
                s += $"\texplicit {TypeName}(";
                for (int i = 0; i < components; i++)
                {
                    if (i != 0)
                        s += ", ";

                    s += $"{scalar.TypeName} _{util.Components[i]}";
                }
                s += ") : ";
                for (int i = 0; i < components; i++)
                {
                    if (i != 0)
                        s += ", ";

                    s += $"{util.Components[i]}(_{util.Components[i]})";
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
                    s += $"{util.Components[i]}";
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

                s += $"{func2}(a.{util.Components[i]})";
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

                s += $"{func2}(a.{util.Components[i]}, b.{util.Components[i]})";
            }
            s += "); }";
            return s;
        }
        string OperatorIndex()
        {
            string s = $"\t{scalar.TypeName} operator [] (int i) const {{ return c[i]; }}";
            return s;
        }

        string OperatorPerComponent(string o, bool emitScalar = false)
        {
            string s = "";
            s += $"\tfriend {TypeName} operator {o} (const {TypeName}& a, const {TypeName}& b)" + " {";
            s += $" return {TypeName}(";
            for (int i = 0; i < components; i++)
            {
                if (i != 0)
                    s += ", ";

                s += $"a.{util.Components[i]} {o} b.{util.Components[i]}";
            }
            s += "); }\n";

            s += $"\tvoid operator {o}= (const {TypeName}& b)" + " { (*this) = (*this) + b; }";
            if (emitScalar)
            {
                s += $"\n\tfriend {TypeName} operator {o} (const {TypeName}& a, const {scalar.TypeName}& b)" + " {";
                s += $" return {TypeName}(";
                for (int i = 0; i < components; i++)
                {
                    if (i != 0)
                        s += ", ";

                    s += $"a.{util.Components[i]} {o} b";
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

                s += $"a.{util.Components[i]} {o} b.{util.Components[i]}";
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
                s += $"-{util.Components[i]}";
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

                s += $"a.{util.Components[i]} * b.{util.Components[i]}";
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
        string Cross()
        {
            return $"static {TypeName} cross({TypeName} a, {TypeName} b) {{ return {TypeName}( a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }}";
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
                if (components == 3)
                    writer.WriteLine(Cross());

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
}
