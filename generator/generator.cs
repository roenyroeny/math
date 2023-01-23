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
    public class Generator
    {
        List<Scalar> baseTypes = new List<Scalar>();
        List<Vector> Vectors = new List<Vector>();
        List<Matrix> Matrices = new List<Matrix>();
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

            Matrices.Add(new Matrix { vector = Vectors[19], rows = 3 });
            Matrices.Add(new Matrix { vector = Vectors[20], rows = 4 });
        }

        public string GenerateScalar_H()
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("#ifndef SCALAR_H");
            writer.WriteLine("#define SCALAR_H");
            writer.WriteLine("#include <stdint.h>");
            foreach (var t in baseTypes)
                writer.WriteLine(t.Declaration);

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

        public string GenerateMatrix_H()
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("#ifndef MATRIX_H");
            writer.WriteLine("#define MATRIX_H");
            writer.WriteLine("#include \"vector.h\"");
            foreach (var t in Matrices)
                writer.WriteLine(t.Declaration);

            writer.WriteLine("#endif");
            return writer.ToString();
        }
    }
}
