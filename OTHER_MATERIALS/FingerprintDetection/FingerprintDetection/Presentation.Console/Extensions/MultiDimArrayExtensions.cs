namespace Presentation.Console.Extensions;

public static class MultiDimArrayExtensions
{
    public static TU[,] FormatArray<T, TU>(this T[,] array, Func<T, TU> formatter)
    {
        var rows = array.GetLength(0);
        var columns = array.GetLength(1);
        var result = new TU[rows, columns];

        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < columns; j++)
            {
                result[i, j] = formatter(array[i, j]);
            }
        }

        return result;
    }
}