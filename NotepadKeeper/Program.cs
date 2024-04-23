using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace NotepadKeeper
{
    class Program
    {

        /* 
        已保存文件结构：
            大于0x80的数据：[Magic Header(3bytes)] [4th byte: unsaved/saved file] [5th byte: filePathStr length] [filePath string] [content length(1 or 2 bytes)] [05 01] [padding(53 bytes)] [content] [6 bytes]
            小于0x80的数据：[Magic Header(3bytes)] [4th byte: unsaved/saved file] [5th byte: filePathStr length] [filePath string] [content length(1 or 2 bytes)] [05 01] [padding(50 bytes)] [content] [6 bytes]
        
        文件存在中英混合的情况，需要额外处理中文（因为notepad的字符统计把中文按照3个字节来进行计算，但是保存是按照2字节进行保存的），我们需要自己手动计算[content length]：

            1. 确定[content]的字节区域范围：
                开始：3 + 1 + 1 + [filePathStr length] + [1 or 2 bytes] + 2 + [padding]
                结束：去掉倒数六个字节

            2. 确定[content]中包含多少个中文字节
            3. 求出真正的[content length]

        */

        // PrintFileContent：打印content内容

        public static void PrintFileContent(FileStream fileStreamInput, int header, int ender)
        {
            if (fileStreamInput == null)
                throw new ArgumentNullException(nameof(fileStreamInput));
            if (header < 0 || ender < 0)
                throw new ArgumentException("Header and ender must be non-negative.");
            if (header + ender >= fileStreamInput.Length)
                throw new ArgumentException("Header and ender combined are larger than the file length.");

            // 定位到 header 之后的开始位置

            header++;
            ender--;

            fileStreamInput.Seek(header, SeekOrigin.Begin);

            // 计算需要读取的有效字节数
            int effectiveLength = (int)(fileStreamInput.Length - header - ender);

            Console.WriteLine("PrintFileContent-effectiveLength: " + effectiveLength);

            if (effectiveLength <= 0)
            {
                Console.WriteLine("No data to read after adjusting for header and ender.");
                return;
            }

            // 读取有效字节
            byte[] buffer = new byte[effectiveLength];
            int bytesRead = fileStreamInput.Read(buffer, 0, effectiveLength);
            if (bytesRead < effectiveLength)
                throw new Exception("Could not read the expected amount of bytes.");

            // 将字节转换为 Unicode 字符串
            string mainContent = Encoding.Unicode.GetString(buffer);

            // 替换 CR 字符为 \r\n
            mainContent = mainContent.Replace("\u000d", "\r\n");

            // 打印内容到控制台
            Console.WriteLine("Main Content: " + mainContent);

#if DEBUG
            // 打印主内容的头字节以16进制形式
            string mainContentHex = BitConverter.ToString(buffer);
            Console.WriteLine("Main Content Bytes (Hex): " + mainContentHex);
#endif
        }
        // 计算表达式 a - c + b * c
        // c = 0x80

        public static int CalculateExpression(int a, int b, int c)
        {
            return (a - c) + (b * c);
        }

        // savedFile: 保存文件读取
        public static void savedFile(FileStream fileStreamInput)
        {
            // 指定要读取的文件路径
            try
            {
                // 使用FileStream打开文件
                using (FileStream fileStream = fileStreamInput)
                {
                    // 计算content前面多少个字节
                    int headerCalc = 3 + 1 + 1;

                    // 打印文件总字节数
                    Console.WriteLine("Total bytes in file: " + fileStream.Length);
                    // 读取第五个字节作为文件路径长度
                    int length = fileStream.ReadByte();

                    headerCalc += length * 2;
#if DEBUG
                    Console.WriteLine("FilePath Length: " + length);
#endif
                    length = length * 2;
                    // 读取指定长度的字节
                    byte[] filePath_buffer = new byte[length];
                    int bytesRead = fileStream.Read(filePath_buffer, 0, length);
                    if (bytesRead < length)
                    {
                        throw new Exception("File too short to read expected content.");
                    }

                    // 将字节转换为字符串
                    string filePath_content = Encoding.Unicode.GetString(filePath_buffer);
                    Console.WriteLine("File Name: " + filePath_content);

                    // 读取关于内容长度的两个字节，判断是否大于0x80
                    fileStream.Seek(5 + length, SeekOrigin.Begin);
                    int a = fileStream.ReadByte();
                    int b = fileStream.ReadByte();
                    int z = fileStream.ReadByte();

                    int contentLength;
                    long startPosition;

                    bool status;

#if DEBUG
                    Console.WriteLine($"a: {a}, b: {b}");
#endif 
                    if (b == 5 && z == 1)
                    {
#if DEBUG
                        Console.WriteLine("Content Length < 0x80");
#endif
                        contentLength = a;
                        startPosition = fileStream.Length - 6 - contentLength - 3;
                        status = true;
                        headerCalc += 2;
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine("Content Length > 0x80");
#endif
                        int c = 0x80;
                        int result = CalculateExpression(a, b, c);
                        contentLength = result;

                        status = false;
                        headerCalc += 1;
                    }

                    Console.WriteLine("Content Length: " + contentLength);


                        if (status) // 小于0x80，3为魔数
                        {
                            headerCalc += 50;
                        }
                        else // 大于 0x80
                        {
                            headerCalc += 53;
                        }

                    // content 前面的字节数量
                    int enderCalc = 6;
#if DEBUG
                    Console.WriteLine("headerCalc Length: " + headerCalc);
                    Console.WriteLine(" enderCalc Length: " + enderCalc); 
#endif
                    // 调用函数打印数据
                    PrintFileContent(fileStream, headerCalc, enderCalc);
                }
            }
            catch (Exception ex)
            {
                // 输出错误信息
                Console.WriteLine("Error reading file: " + ex.Message);
            }
        }


        // unsaved: 未保存文件读取
        public static void unsavedFile(FileStream fileStreamInput)
        {
            try
            {
                using (FileStream fileStream = fileStreamInput)
                {

                    // 未保存文件无文件名

                    // 确保文件长度足以进行读取
                    if (fileStream.Length < 20)  // 至少需要13 + 7 = 20个字节
                    {
                        Console.WriteLine("File is too short.");
                        return;
                    }

                    // 设置起始位置，从第13个字节开始读取（索引从0开始，所以是12）
                    fileStream.Seek(12, SeekOrigin.Begin);

                    // 计算要读取的字节数
                    int count = (int)fileStream.Length - 12 - 5;

                    // 创建缓冲区并读取数据
                    byte[] headerBytes = new byte[count];
                    int bytesRead = fileStream.Read(headerBytes, 0, count);

                    // 将字节转换为 Unicode 字符串并打印
                    string mainContent = Encoding.Unicode.GetString(headerBytes);
                    Console.WriteLine("Main Content: " + mainContent);
#if DEBUG
                    // 打印主内容的头字节以16进制形式
                    string mainContentHex = BitConverter.ToString(headerBytes);
                    Console.WriteLine("Main Content Bytes (Hex): " + mainContentHex);
#endif
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading file: " + ex.Message);
            }
        }


        // dealFileType: 处理文件类型 saved/unsaved
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
                    Console.WriteLine("----[ SAVED FILE  √]----");
                    savedFile(fileStream);
                }
                else
                {
                    Console.WriteLine("----[UNSAVED FILE ×]----");
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