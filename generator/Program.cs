using generator;

class Program
{


    static void Main()
    {

        var g = new Generator();
        var scalar_h = g.GenerateScalar_H();
        System.IO.File.WriteAllText("scalar.h", scalar_h);

        var vector_h = g.GenerateVector_H();
        System.IO.File.WriteAllText("vector.h", vector_h);

        var matrix_h = g.GenerateMatrix_H();
        System.IO.File.WriteAllText("matrix.h", matrix_h);

    }
}
