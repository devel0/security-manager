using System;
using System.IO;

namespace replace_token_with_file
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                System.Console.WriteLine($"required arguments : <token> <replace-with-token>");
                return 1;
            }

            using (var stream = Console.OpenStandardInput())
            {
                using (var sr = new StreamReader(stream)) 
                {
                    var s = sr.ReadToEnd();
                    System.Console.WriteLine(s.Replace(args[0], args[1]));
                }
            }            

            return 0;
        }
    }
}
