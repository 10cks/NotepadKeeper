using System;
using System.IO;
using System.Text;

namespace NotepadKeeper
{
    class NotepadPP
    {
        public static void get()
        {

            Console.WriteLine($"\n----[NOTEPAD ++]----\n");

            // 动态获取当前Windows用户的用户名
            string username = Environment.UserName;

            // 根据用户名设置目录路径
            string directoryPath = $@"C:\Users\{username}\AppData\Roaming\Notepad++\backup";

            // 检查目录是否存在
            if (Directory.Exists(directoryPath))
            {
                // 获取目录中的所有文件
                string[] files = Directory.GetFiles(directoryPath);
                
                Console.WriteLine($"Total files: {files.Length}");

                // 遍历所有文件
                foreach (string file in files)
                {
                    // 读取每个文件的内容
                    string content = File.ReadAllText(file, Encoding.UTF8);
                    Console.WriteLine($"Reading file: {file}");
                    Console.WriteLine(content);
                    Console.WriteLine("--------------------------------");
                }
            }
            else
            {
                Console.WriteLine($"The directory {directoryPath} does not exist.");
            }

#if DEBUG
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(); 
#endif
        }
    }
}