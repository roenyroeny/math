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
    class Matrix
    {
        public Vector vector;
        public int rows;
        public string TypeName
        {
            get
            {
                return $"{vector.TypeName}x{rows}";
            }
        }

        public int columns { get { return vector.components; } }


        public string Constructors
        {
            get
            {
                string s = "";
                // default ctor
                s += $"\t{TypeName}() = default;\n";

                s += $"\texplicit {TypeName}(";
                for (int i = 0; i < rows; i++)
                {
                    if (i != 0)
                        s += ", ";

                    s += $"{vector.TypeName} _{util.Components[i]}";
                }
                s += ") : ";
                for (int i = 0; i < rows; i++)
                {
                    if (i != 0)
                        s += ", ";

                    s += $"{util.Components[i]}(_{util.Components[i]})";
                }
                s += " {}\n";

                s += $"\texplicit {TypeName}(";
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        if (i != 0 || j != 0)
                            s += ", ";

                        s += $"{vector.scalar.TypeName} _{util.Components[i]}{util.Components[j]}";
                    }
                }
                s += ") : ";
                for (int i = 0; i < rows; i++)
                {
                    if (i != 0)
                        s += ", ";
                    s += $"{util.Components[i]}(";
                    for (int j = 0; j < columns; j++)
                    {
                        if (j != 0)
                            s += ", ";

                        s += $"_{util.Components[i]}{util.Components[j]}";
                    }
                    s += ")";
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
                s += $"\t\t{vector.TypeName} c[{rows}];\n";
                s += $"\t\tstruct {{ {vector.TypeName} ";
                for (int i = 0; i < rows; i++)
                {
                    if (i != 0)
                        s += ", ";
                    s += $"{util.Components[i]}";
                }
                s += "; };\n";
                return s + "\t};\n";
            }
        }

        string Row(string m, int i)
        {
            return $"{m}[{i}]";

            string s = $"{vector.TypeName}(";
            for (int j = 0; j < columns; j++)
            {
                if (j != 0)
                    s += ", ";
                s += $"{m}.{util.Components[i]}.{util.Components[j]}";
            }
            s += ")";
            return s;
        }
        string Col(string m, int i)
        {
            string s = $"{vector.TypeName}(";
            for (int j = 0; j < rows; j++)
            {
                if (j != 0)
                    s += ", ";
                s += $"{m}.{util.Components[j]}.{util.Components[i]}";
            }
            s += ")";
            return s;
        }

        string OperatorMul()
        {
            string s = $"\t/*not tested*/ friend {TypeName} operator *(const {TypeName}& a, const {TypeName}& b) {{ return {TypeName}(";
            for (int i = 0; i < rows; i++)
            {
                var r = Row("a", i);
                for (int j = 0; j < columns; j++)
                {
                    if (i != 0 || j != 0)
                        s += ", ";

                    var c = Col("b", i);
                    s += $"dot({r}, {c})";
                }
            }
            s += "); }";
            return s;
        }
        string OperatorIndex()
        {
            string s = $"\t{vector.TypeName} operator [] (int i) const {{ return c[i]; }}";
            return s;
        }
        public string Operators
        {
            get
            {
                StringWriter writer = new StringWriter();
                writer.WriteLine(OperatorIndex());
                writer.WriteLine(OperatorMul());

                return writer.ToString();
            }
        }

        string FunctionIdentity()
        {

            string s = $"\tstatic {TypeName} identity() {{ return {TypeName}(";

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (i != 0 || +j != 0)
                        s += ", ";

                    s += i == j ? "1" : "0";
                }
            }

            return s + "); }";
        }

        string FunctionDeterminant()
        {
            string s = $"static {vector.scalar.TypeName} determinant({TypeName} m) {{ return 0.0f; }}";
            return s;
        }

        string FunctionAdjoint()
        {
            string s = $"/*not tested*/ static {TypeName} adjoint({TypeName} a) {{ return {TypeName}(";

            s += "cross(a.y, a.z), cross(a.z, a.x), cross(a.x, a.y)";

            return s + "); }";
        }

        public string MemberFunctions
        {
            get
            {
                StringWriter writer = new StringWriter();
                writer.WriteLine(FunctionIdentity());

                return writer.ToString();
            }
        }
        public string Functions
        {
            get
            {
                StringWriter writer = new StringWriter();
                writer.WriteLine(FunctionDeterminant());
                if (rows == 3 && columns == 3)
                    writer.WriteLine(FunctionAdjoint());

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
                writer.Write(MemberFunctions);
                writer.WriteLine("};");
                writer.Write(Functions);
                return writer.ToString();
            }
        }

    }
}
