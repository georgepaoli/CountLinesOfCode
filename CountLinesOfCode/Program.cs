using CommandLineArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountLinesOfCode {
    class Program {

        [Required]
        public string path = ".";

        [Required]
        public string pattern = "*.*";

        public void Start() {

            var searchPattern = pattern;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Searching in: {0}", path);
            Console.WriteLine("With pattern: {0}", searchPattern);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Working..");

            var files = new DirectoryInfo(path).GetFiles(searchPattern, SearchOption.AllDirectories);
            var totalLines = 0;

            foreach (FileInfo item in files) {
                totalLines += File.ReadAllLines(item.FullName).Count();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Total Files: {0}", files.Count());
            Console.WriteLine("Total Lines: {0}", totalLines);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Done!");
        }

        static void Main(string[] args) {

            try {
                ConsoleApp.FromCommandLineArgs<Program>(args).Start();
            }
            catch (ArgumentException) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Please, enter with parameters correctly");
            }
            catch (BindParamsException) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Please, enter with parameters correctly");
            }
            catch (DirectoryNotFoundException) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Directory not found. Please, enter name directory correctly.");
            }

            Console.ResetColor();
            Console.Read();
        }
    }
}
