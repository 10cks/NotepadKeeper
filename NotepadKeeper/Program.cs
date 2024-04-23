using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace NotepadKeeper
{
    class Program
    {
        // 计算表达式 a-c + b*c
        public static int CalculateExpression(int a, int b, int c)
        {
            return (a - c) + (b * c);
        }

        public static void savedFile(FileStream fileStreamInput)
        {
            // 指定要读取的文件路径
            try
            {
                // 使用FileStream打开文件
                using (FileStream fileStream = fileStreamInput)
                {
                    // 读取第五个字节作为文件路径长度
                    int length = fileStream.ReadByte();
                    Console.WriteLine("FilePath Length: " + length);
                    length = length * 2;
                    // 读取指定长度的字节
                    byte[] filePath_buffer = new byte[length];
                    int bytesRead = fileStream.Read(filePath_buffer, 0, length);
                    if (bytesRead < length)
                    {
                        throw new Exception("File too short to read expected content.");
                    }

                    // 将字节转换为字符串
                    string filePath_content = Encoding.UTF8.GetString(filePath_buffer);
                    Console.WriteLine("File Name: " + filePath_content);

                    // 读取关于内容长度的两个字节，作为函数参数进行运算
                    fileStream.Seek(5 + length, SeekOrigin.Begin);
                    int a = fileStream.ReadByte();
                    int b = fileStream.ReadByte();
                    int contentLength;

#if DEBUG
                    Console.WriteLine($"a: {a}, b: {b}");
#endif 
                    if (b == 5)
                    {
                        Console.WriteLine("Content Length < 0x80");
                        contentLength = a;
                        Console.WriteLine("Content Length: " + contentLength);
                    }
                    else
                    {
                        Console.WriteLine("Content Length > 0x80");
                        int c = 0x80;
                        int result = CalculateExpression(a, b, c);
                        contentLength = result;
                        Console.WriteLine("Content Length: " + contentLength);
                    }

                    // 计算起始位置并读取内容
                    long startPosition = fileStream.Length - 6 - contentLength * 2;
                    if (startPosition < 0)
                    {
                        Console.WriteLine("Invalid content length, unable to read from specified position.");
                    }
                    else
                    {
                        fileStream.Seek(startPosition, SeekOrigin.Begin);
                        byte[] headerBytes = new byte[contentLength * 2];
                        int headerBytesRead = fileStream.Read(headerBytes, 0, headerBytes.Length);
                        if (headerBytesRead < headerBytes.Length)
                        {
                            throw new Exception("File too short to read expected header.");
                        }

                        // 将字节转换为字符串并打印
                        string mainContent = Encoding.UTF8.GetString(headerBytes);
                        Console.WriteLine("Main Content: " + mainContent);
                    }
                }
            }
            catch (Exception ex)
            {
                // 输出错误信息
                Console.WriteLine("Error reading file: " + ex.Message);
            }
        }

        public static void unsavedFile(FileStream fileStreamInput)
        {
            try
            {
                using (FileStream fileStream = fileStreamInput)
                {
                    // 确保文件长度足以进行读取
                    if (fileStream.Length < 20)  // 至少需要13 + 7 = 20个字节
                    {
                        Console.WriteLine("File is too short.");
                        return;
                    }

                    // 设置起始位置，从第13个字节开始读取（索引从0开始，所以是12）
                    fileStream.Seek(12, SeekOrigin.Begin);

                    // 计算要读取的字节数
                    int count = (int)fileStream.Length - 12 - 7;

                    // 创建缓冲区并读取数据
                    byte[] bytes = new byte[count];
                    int bytesRead = fileStream.Read(bytes, 0, count);

                    // 将字节转换为字符串
                    string content = Encoding.UTF8.GetString(bytes, 0, bytesRead);

                    // 打印结果
                    Console.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading file: " + ex.Message);
            }
        }

        public static void dealFileType(string filePath)
        {
            // 使用FileStream打开文件
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // 移动到第四个字节的位置
                fileStream.Seek(3, SeekOrigin.Begin);

                // 读取第四个字节
                int fourthByte = fileStream.ReadByte();

                // 根据第四个字节的值输出结果
                if (fourthByte == 1)
                {
                    Console.WriteLine("File State: Saved file");
                    savedFile(fileStream);
                }
                else
                {
                    Console.WriteLine("File State: Unsaved file");
                    unsavedFile(fileStream);
                }
            }
        }


        static void Main(string[] args)
        {

            // 获取当前用户的AppData\Local文件夹路径
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 指定要遍历的文件夹路径
            string directoryPath = $@"{appDataLocalPath}\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\LocalState\TabState";
            
            // 获取目录中所有的.bin文件
            string[] filePaths = Directory.GetFiles(directoryPath, "*.bin");

            // 定义一个GUID的正则表达式
            Regex guidRegex = new Regex(@"^[{(]?[0-9A-Fa-f]{8}[-]([0-9A-Fa-f]{4}[-]){3}[0-9A-Fa-f]{12}[)}]?$");

            // 遍历所有文件
            foreach (string filePath in filePaths)
            {
                // 获取文件名（不包括路径）
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                // 检查文件名是否符合GUID格式
                if (guidRegex.IsMatch(fileName))
                {
                    Console.WriteLine($"================================================");
                    Console.WriteLine($"Processing file: {filePath}");
                    dealFileType(filePath);
                }
            }
            Console.WriteLine($"================================================");
        }
    }
}