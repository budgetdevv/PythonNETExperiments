using System;

namespace PythonNETExperiments
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine(new SalesforceBLIPImageCaptioning().Run("https://avatars.githubusercontent.com/u/74057874?v=4"));
        }
    }
}