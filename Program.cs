using System.Text;

namespace AutoRranslateTextToCsv
{
    internal class Program
    {
        static string loc = Path.GetDirectoryName(AppContext.BaseDirectory) + "\\";

        const string InDir = "Input";
        const string OutDir = "Out";
        const string SrcDataFile = "_TextDictionary.csv";
        const string Ver = "0.1";
        static Dictionary<string, string> mDictSrcData;
        static void Main(string[] args)
        {
            string title = $"AutoRranslateTextToCsv Ver.{Ver} By  axibug.com";
            Console.Title = title;
            Console.WriteLine(title);


            if (!Directory.Exists(loc + InDir))
            {
                Console.WriteLine("Input文件不存在");
                Console.ReadLine();
                return;
            }

            if (!Directory.Exists(loc + OutDir))
            {
                Console.WriteLine("Out文件不存在");
                Console.ReadLine();
                return;
            }

            if (!File.Exists(loc + SrcDataFile))
            {
                Console.WriteLine($"{SrcDataFile}文件不存在");
                Console.ReadLine();
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Console.WriteLine($"开始加载译文数据源……");
            if (!LoadCsv(loc + SrcDataFile, out Dictionary<string, string> _dictSrcData))
            {
                Console.WriteLine($"译文数据源加载失败");
                Console.ReadLine();
                return;
            }

            mDictSrcData = _dictSrcData;
            Console.WriteLine($"译文数据源加载成功,共{_dictSrcData.Count}条数据");

            string[] files = FileHelper.GetDirFile(loc + InDir);
            Console.WriteLine($"Input中共{files.Length}个文件，是否处理? (y/n)");
            string yn = Console.ReadLine();
            if (yn.ToLower() != "y")
                return;
            
            int index = 0;
            int errcount = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string FileName = files[i].Substring(files[i].LastIndexOf("\\"));

                if (!FileName.ToLower().Contains(".csv"))
                {
                    continue;
                }
                index++;
                Console.WriteLine($">>>>>>>>>>>>>>开始处理 第{index}个文件  {FileName}<<<<<<<<<<<<<<<<<<<");

                if (DoReplaceRranslateFile(files[i], out string[] TempArr,out int DoneIndex))
                {
                    string newfileName = FileName;
                    string outstring = loc + OutDir + "\\" + newfileName;
                    File.WriteAllLines(outstring, TempArr);
                    Console.WriteLine($">>>>>>>>>>>>>>成功处理 第{index}个:{outstring} | 其中成功处理文本{DoneIndex}个");
                }
                else
                {
                    errcount++;
                    Console.WriteLine($">>>>>>>>>>>>>>处理失败 第{index}个");
                }
            }

            Console.WriteLine($"已处理{files.Length}个文件，其中{errcount}个失败");
            Console.ReadLine();
        }


        public static bool LoadCsv(string path, out Dictionary<string, string> _dictSrcData)
        {
            _dictSrcData = new Dictionary<string, string>();
            using (StreamReader sr = new StreamReader(path, Encoding.GetEncoding("gb2312")))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] values = line.Split(',');
                    if (values.Length >= 2)
                    {
                        string k = null;
                        string v = null;
                        k = values[0];
                        v = values[1];
                        if (string.IsNullOrEmpty(k) || string.IsNullOrEmpty(v))
                            continue;
                        if (string.Equals(k,v))
                            continue;
                        _dictSrcData[k] = v;
                    }
                }
            }
            return true;
        }

        public static bool DoReplaceRranslateFile(string path,out string[] TempArr,out int DoneNum)
        {
            DoneNum = 0;
            try
            {
                string[] StrArr = File.ReadAllLines(path, Encoding.GetEncoding("shift-jis"));
                TempArr = new string[StrArr.Length];
                for (int i = 0; i < StrArr.Length; i++)
                {
                    
                    string line = StrArr[i];
                    if (i == 0)
                    {
                        TempArr[i] = line;
                        continue;
                    }
                    string[] valueArr = StrArr[i].Split("\t");
                    if (valueArr.Length >= 2 && GetRranslateText(valueArr[2], out string Resultline))
                    {
                        string newline = "";
                        for (int j = 0; j < valueArr.Length; j++)
                        {
                            if (j == 2)
                                newline += EncodingConvert(Encoding.GetEncoding("gb2312"), Encoding.GetEncoding("shift-jis"), Resultline);
                            else
                                newline += valueArr[j];

                            if(j < valueArr.Length -1)
                                newline += "\t";

                            DoneNum++;
                        }
                        line = newline;
                    }
                    TempArr[i] = line;
                }

                return true;
            }
            catch(Exception ex)
            {
                TempArr = null;
                return false;
            }
        }
        public static string EncodingConvert(Encoding src, Encoding dst, string text)
        {
            var bytes = src.GetBytes(text);
            bytes = Encoding.Convert(src, dst, bytes);
            return dst.GetString(bytes);
        }

        public static bool GetRranslateText(string line, out string Resultline)
        {
            if (mDictSrcData.ContainsKey(line))
            {
                Resultline = mDictSrcData[line];
                return true;
            }
            Resultline = null;
            return false;
        }

    }
}