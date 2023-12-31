﻿using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoRranslateTextToCsv
{
    internal class Program
    {
        static string loc = Path.GetDirectoryName(AppContext.BaseDirectory) + "\\";

        const string InDir = "Input";
        const string OutDir = "Out";
        const string SrcDataFile = "_TextDictionary.csv";
        const string Ver = "0.5";
        static Dictionary<string, string> mDictSrcData;
        static Dictionary<char, char> DictTongJia = new Dictionary<char, char>();
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

                if (DoReplaceRranslateFile(files[i], out string[] TempArr,out int DoneIndex, out string[] ErrArr))
                {
                    string newfileName = FileName;
                    string outstring = loc + OutDir + "\\" + newfileName;
                    File.WriteAllLines(outstring, TempArr);
                    string err_outstring = loc + OutDir + "\\" + newfileName+"未找到译文的列.csv";
                    File.WriteAllLines(err_outstring, ErrArr);
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
                        if (v.Length == 1)
                        {
                            //如果单字英数
                            if(IsNatural_Number(v))
                                continue;
                        }

                        _dictSrcData[k] = TongJiaZi(v);
                    }
                }
            }
            return true;
        }

        public static bool IsNatural_Number(string str)
        {
            System.Text.RegularExpressions.Regex reg1 = new System.Text.RegularExpressions.Regex(@"^[A-Za-z0-9]+$");
            return reg1.IsMatch(str);
        }


        public static bool DoReplaceRranslateFile(string path,out string[] TempArr,out int DoneNum,out string[] ErrArr)
        {
            DoneNum = 0;
            List<string> ErrList = new List<string>();
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
                    if (valueArr.Length >= 2)
                    {
                        if (!GetRranslateText(valueArr[2], out string Resultline))
                        {
                            //没有找到译文
                            ErrList.Add(line);
                        }
                        else
                        {
                            string newline = "";
                            for (int j = 0; j < valueArr.Length; j++)
                            {
                                if (j == 3)
                                    newline += EncodingConvert(Encoding.GetEncoding("gb2312"), Encoding.GetEncoding("shift-jis"), Resultline);
                                else
                                    newline += valueArr[j];

                                if (j < valueArr.Length - 1)
                                    newline += "\t";

                                DoneNum++;
                            }
                            line = newline;
                        }
                    }
                    TempArr[i] = line;
                }

                ErrArr = ErrList.ToArray();
                return true;
            }
            catch(Exception ex)
            {
                ErrArr = null;
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

        public static string TongJiaZi(string Src)
        {
            string str = Src
                            .Replace("<TAB>", "\t") // Replace tab
                            .Replace("<CLINE>", "\r\n") // Replace carriage return
                            .Replace("<NLINE>", "\n") // Replace new line.Replace("擊", "撃")
                            .Replace("步", "歩")
                            .Replace("每", "毎")
                            .Replace("貓", "猫")
                            .Replace("值", "値")
                            .Replace("說", "説")
                            .Replace("姬", "姫")
                            .Replace("產", "産")
                            .Replace("銳", "鋭")
                            .Replace("內", "内")
                            .Replace("錄", "録")
                            .Replace("狀", "状")
                            .Replace("歷", "歴")
                            .Replace("焰", "焔")
                            .Replace("黃", "黄")
                            .Replace("絕", "絶")
                            .Replace("啟", "起")
                            .Replace("檯", "台")
                            .Replace("溫", "温")
                            .Replace("骷", "髑")
                            .Replace("醃", "奄")
                            .Replace("嗎", "嘛")
                            .Replace("繳", "交")
                            .Replace("伙", "夥")
                            .Replace("剝", "剥")
                            .Replace("髓", "髄")
                            .Replace("蔥", "葱")
                            .Replace("阱", "井")
                            .Replace("‧", "・")
                            .Replace("夥伴喵", "夥伴猫")
                            .Replace("喵卡奄壺", "猫卡奄壺")
                            .Replace("喵夾克", "猫夾克")
                            .Replace("喵拖鞋", "猫拖鞋")
                            .Replace("喵", "Ｎｙａ")
                            .Replace("蚱蜢", "虫乍虫孟")
                            .Replace("吧", "叭")
                            .Replace("頭盔", "頭鎧")
                            .Replace("武士盔", "兜")
                            .Replace("盔甲", "鎧甲")
                            .Replace("腳", "脚")
                            .Replace("沉", "沈")
                            .Replace("網咖", "NetCafe")
                            .Replace("噗吱", "Pugi")
                            .Replace("噗", "Pu")
                            .Replace("吱", "gi")
                            .Replace("桌", "卓")
                            .Replace("夠", "垢")
                            .Replace("禱", "祷")
                            .Replace("歧", "岐")
                            .Replace("查", "査")
                            .Replace("—", "ー")
                            .Replace("辦", "辧")
                            .Replace("舉?", "舉辧")
                            .Replace("踢", "蹴")
                            .Replace("瞄", "描")
                            .Replace("喔", "哦")
                            .Replace("卡", "卞")
                            .Replace("牠", "他")
                            .Replace("樁", "椿")
                            .Replace("?容", "内容")
                            .Replace("?明", "説明")
                            .Replace("撐", "挺")
                            .Replace("幫", "幇")
                            .Replace("啦", "拉")
                            .Replace("嗯", "恩")
                            .Replace("哪", "吶")
                            .Replace("呢", "吶")
                            .Replace("份", "分")
                            .Replace("鍊", "錬")
                            .Replace("偷", "偸")
                            .Replace("啊", "阿")
                            .Replace("奶", "乃")
                            .Replace("眾", "衆")
                            .Replace("徵", "徴")
                            .Replace("烤", "火考")
                            .Replace("哎", "艾")
                            .Replace("跑", "足包")
                            .Replace("睏", "目困")
                            .Replace("咧", "口尼")
                            .Replace("餵", "食畏")
                            .Replace("閱", "閲")
                            .Replace("傢", "家")
                            .Replace("俱", "具")
                            .Replace("糰", "團")
                            .Replace("漩", "旋")
                            .Replace("咦", "姨")
                            .Replace("瞧", "看")
                            .Replace("麵", "麺")
                            .Replace("淚", "涙")
                            .Replace("眶", "框")
                            .Replace("咻", "嗅")
                            .Replace("咕", "Gu")
                            .Replace("囉", "了哦")
                            .Replace("暱", "匿")
                            .Replace("唉", "艾")
                            .Replace("盯", "叮")
                            .Replace("均勻", "平均")
                            .Replace("戶", "戸")
                            .Replace("她", "他")
                            .Replace("汙", "汚")
                            .Replace("汛", "迅")
                            .Replace("污", "汚")
                            .Replace("佈", "布")
                            .Replace("佔", "占")
                            .Replace("您", "イ尓")
                            .Replace("你", "イ尓")
                            .Replace("刨", "鉋")
                            .Replace("吞", "呑")
                            .Replace("吵", "口少")
                            .Replace("呃", "咢")
                            .Replace("囤", "屯")
                            .Replace("垃", "拉")
                            .Replace("圾", "及")
                            .Replace("圾", "及")
                            .Replace("妒", "妬")
                            .Replace("朵", "朶")
                            .Replace("剁", "朶リ")
                            .Replace("呣", "口母")
                            .Replace("呦", "口幼")
                            .Replace("戾", "戻")
                            .Replace("剎", "刹")
                            .Replace("磺", "黄")
                            .Replace("碳", "炭")
                            .Replace("鈮", "金尼")
                            .Replace("鉆", "鑽")
                            .Replace("虛", "虚")
                            .Replace("嗨", "ｈｉ")
                            .Replace("歲", "歳")
                            .Replace("溼", "湿")
                            .Replace("蜓", "廷")
                            .Replace("鉻", "咯")
                            .Replace("銬", "金考")
                            .Replace("嘿", "ｈｅｙ")
                            .Replace("噁", "額")
                            .Replace("樑", "梁")
                            .Replace("緣", "縁")
                            .Replace("蔥", "葱")
                            .Replace("鋁", "金呂")
                            .Replace("鋯", "鎬")
                            .Replace("鋰", "理")
                            .Replace("噩", "惡")
                            .Replace("噯", "艾")
                            .Replace("噹", "当")
                            .Replace("撿", "拾")
                            .Replace("螞", "馬")
                            .Replace("錳", "猛")
                            .Replace("錶", "表")
                            .Replace("頰", "頬")
                            .Replace("謢", "護")
                            .Replace("鍥", "契")
                            .Replace("蟬", "蝉")
                            .Replace("繫", "繋")
                            .Replace("鏢", "標")
                            .Replace("鬍", "胡")
                            .Replace("龐", "厖")
                            .Replace("曬", "晒")
                            .Replace("僱", "雇")
                            .Replace("嗆", "搶")
                            .Replace("鈉", "納")
                            .Replace("鈦", "金太")
                            .Replace("鄉", "郷")
                            .Replace("煸", "煽")
                            .Replace("痹", "痺")
                            .Replace("馱", "駄")
                            .Replace("銻", "金弟")
                            .Replace("銼", "措")
                            .Replace("糗", "米臭")
                            .Replace("螃", "虫旁")
                            .Replace("糙", "操")
                            .Replace("嚕", "魯")
                            .Replace("嚙", "噛")
                            .Replace("繡", "繍")
                            .Replace("軀", "躯")
                            .Replace("鎦", "硫")
                            .Replace("雞", "鶏")
                            .Replace("鏽", "錆")
                            .Replace("蠟", "蝋")
                            .Replace("鐮", "鎌")
                            .Replace("鐳", "金雷")
                            .Replace("囉", "羅")
                            .Replace("鱷", "鰐")
                            .Replace("釷", "金土")
                            .Replace("釹", "金女")
                            .Replace("啷", "朗")
                            .Replace("喔", "哦")
                            .Replace("喲", "ｙｏ")
                            .Replace("嗓", "桑")
                            .Replace("嗥", "皐")
                            .Replace("嗦", "索")
                            .Replace("渴", "渇")
                            .Replace("萊", "莱")
                            .Replace("韌", "靱")
                            .Replace("嚨", "隆")
                            .Replace("鎵", "金家")
                            .Replace("糬", "米署")
                            .Replace("麴", "麹")
                            .Replace("鐨", "金費")
                            .Replace("囊", "嚢")

            #region 700字饱和通假字
                            /*
.Replace("时", "時")
.Replace("戏", "戲")
.Replace("厅", "庁")
.Replace("门", "門")
.Replace("为", "為")
.Replace("间", "間")
.Replace("键", "鍵")
.Replace("盘", "鍵")
.Replace("开", "開")
.Replace("积", "積")
.Replace("换", "換")
.Replace("试", "試")
.Replace("节", "節")
.Replace("转", "轉")
.Replace("发", "發")
.Replace("这", "這")
.Replace("个", "個")
.Replace("删", "刪")
.Replace("哥", "哥")
.Replace("喂", "畏")
.Replace("击", "撃")
.Replace("战", "戰")
.Replace("务", "務")
.Replace("报", "報")
.Replace("勵", "励")
.Replace("聆", "聆")
.Replace("揭", "掲")
.Replace("洽", "洽")
.Replace("她", "他")
.Replace("繼", "繼")
.Replace("攀", "攀")
.Replace("隸", "隸")
.Replace("惘", "惘")
.Replace("鈕", "鈕")
.Replace("聊", "聊")
.Replace("层", "層")
.Replace("您", "イ尓")
.Replace("咳", "咳")
.Replace("崔", "摧")
.Replace("糟", "糟")
.Replace("强", "強")
.Replace("歧", "岐")
.Replace("猎", "獵")
.Replace("团", "團")
.Replace("况", "況")
.Replace("变", "変")
.Replace("类", "類")
.Replace("动", "動")
.Replace("帮", "幇")
.Replace("邮", "郵")
.Replace("减", "減")
.Replace("页", "頁")
.Replace("标", "標")
.Replace("摇", "搖")
.Replace("杆", "杆")
.Replace("单", "單")
.Replace("视", "視")
.Replace("图", "図")
.Replace("輯", "輯")
.Replace("签", "簽")
.Replace("滚", "滾")
.Replace("钮", "鈕")
.Replace("链", "鏈")
.Replace("导", "導")
.Replace("员", "員")
.Replace("显", "顯")
.Replace("设", "設")
.Replace("项", "項")
.Replace("搜", "搜")
.Replace("测", "測")
.Replace("级", "級")
.Replace("截", "截")
.Replace("场", "場")
.Replace("过", "過")
.Replace("拟", "擬")
.Replace("斩", "斬")
.Replace("撈", "勞")
.Replace("蹲", "頓")
.Replace("准", "准")
.Replace("复", "復")
.Replace("离", "離")
.Replace("戳", "戳")
.Replace("樁", "庄")
.Replace("臺", "台")
.Replace("冲", "冲")
.Replace("註", "注")
.Replace("覽", "覧")
.Replace("嗯", "恩")
.Replace("敘", "敘")
.Replace("哦", "哦")
.Replace("囉", "羅")
.Replace("唉", "艾")
.Replace("呀", "呀")
.Replace("籌", "籌")
.Replace("梯", "涕")
.Replace("吩", "分")
.Replace("咐", "附")
.Replace("滂", "膀")
.Replace("沱", "陀")
.Replace("聰", "聰")
.Replace("臥", "臥")
.Replace("迪", "迪")
.Replace("儂", "濃")
.Replace("迈", "邁")
.Replace("无", "无")
.Replace("詢", "訓")
.Replace("搏", "搏")
.Replace("鞏", "鞏")
.Replace("奠", "典")
.Replace("餵", "畏")
.Replace("舖", "鋪")
.Replace("賺", "賺")
.Replace("姓", "姓")
.Replace("躺", "堂")
.Replace("蔬", "疏")
.Replace("奕", "亦")
.Replace("慵", "慵")
.Replace("懶", "懶")
.Replace("遁", "遯")
.Replace("唔", "唔")
.Replace("邀", "邀")
.Replace("儘", "盾")
.Replace("沮", "沮")
.Replace("咦", "姨")
.Replace("哇", "哇")
.Replace("咧", "列")
.Replace("鄙", "鄙")
.Replace("措", "措")
.Replace("枉", "枉")
.Replace("哈", "哈")
.Replace("虧", "虧")
.Replace("陪", "培")
.Replace("嘛", "嘛")
.Replace("哼", "亨")
.Replace("肯", "肯")
.Replace("哎", "艾")
.Replace("睏", "困")
.Replace("憊", "備")
.Replace("扯", "撤")
.Replace("僵", "僵")
.Replace("懦", "懦")
.Replace("歉", "歉")
.Replace("贏", "贏")
.Replace("頰", "夾")
.Replace("鬧", "閙")
.Replace("羞", "羞")
.Replace("耍", "刷")
.Replace("衷", "衷")
.Replace("懊", "懊")
.Replace("豈", "豈")
.Replace("剿", "剿")
.Replace("罷", "罷")
.Replace("庸", "庸")
.Replace("辜", "辜")
.Replace("轍", "徹")
.Replace("餒", "妥")
.Replace("揹", "背")
.Replace("搞", "鎬")
.Replace("膛", "螳")
.Replace("抬", "擡")
.Replace("吶", "吶")
.Replace("誡", "戒")
.Replace("萊", "莱")
.Replace("莎", "砂")
.Replace("帖", "貼")
.Replace("瑟", "瑟")
.Replace("苻", "付")
.Replace("寇", "冦")
.Replace("踹", "揣")
.Replace("拚", "絣")
.Replace("榜", "榜")
.Replace("瀏", "琉")
.Replace("鏢", "標")
.Replace("暱", "匿")
.Replace("样", "樣")
.Replace("喏", "諾")
.Replace("檔", "档")
.Replace("砰", "平")
.Replace("篩", "篩")
.Replace("窄", "窄")
.Replace("陋", "漏")
.Replace("姊", "姉")
.Replace("勒", "勒")
.Replace("靶", "巴")
.Replace("擒", "擒")
.Replace("坦", "坦")
.Replace("氓", "芒")
.Replace("瀧", "朧")
.Replace("罐", "欟")
.Replace("吵", "操")
.Replace("嚕", "魯")
.Replace("嘎", "ｇａ")
.Replace("仗", "仗")
.Replace("啷", "郎")
.Replace("碟", "諜")
.Replace("鏘", "将")
.Replace("嗶", "畢")
.Replace("釁", "釁")
.Replace("慘", "慚")
.Replace("恕", "恕")
.Replace("趟", "淌")
.Replace("眨", "扎")
.Replace("叮", "叮")
.Replace("嚀", "鈴")
.Replace("嗦", "索")
.Replace("瀾", "瀾")
.Replace("徬", "傍")
.Replace("笨", "笨")
.Replace("忽", "忽")
.Replace("攸", "悠")
.Replace("陌", "陌")
.Replace("廳", "庁")
.Replace("娟", "娟")
.Replace("窩", "窩")
.Replace("嘖", "責")
.Replace("饋", "饋")
.Replace("呿", "怯")
.Replace("嗄", "嚇")
.Replace("欸", "欸")
.Replace("跌", "跌")
.Replace("咪", "米")
.Replace("敞", "敞")
.Replace("漓", "璃")
.Replace("勃", "勃")
.Replace("伶", "令")
.Replace("矜", "矜")
.Replace("陶", "陶")
.Replace("啥", "撒")
.Replace("孰", "孰")
.Replace("扛", "抗")
.Replace("砍", "坎")
.Replace("呣", "姆")
.Replace("嬰", "嬰")
.Replace("潘", "潘")
.Replace("倆", "兩")
.Replace("愣", "楞")
.Replace("论", "論")
.Replace("寞", "寞")
.Replace("侈", "侈")
.Replace("恭", "恭")
.Replace("爸", "父")
.Replace("媽", "媽")
.Replace("吻", "吻")
.Replace("窘", "窮")
.Replace("掙", "争")
.Replace("扎", "紮")
.Replace("啞", "呀")
.Replace("渥", "沃")
.Replace("茲", "茲")
.Replace("哆", "咄")
.Replace("渙", "喚")
.Replace("彆", "鼈")
.Replace("芬", "紛")
.Replace("喘", "喘")
.Replace("噯", "艾")
.Replace("汀", "叮")
.Replace("蒂", "帝")
.Replace("弗", "弗")
.Replace("喬", "巧")
.Replace("妮", "泥")
.Replace("淮", "淮")
.Replace("嘻", "喜")
.Replace("鋪", "鋪")
.Replace("囤", "飩")
.Replace("逛", "洸")
.Replace("吝", "吝")
.Replace("嗇", "嗇")
.Replace("晉", "晉")
.Replace("错", "錯")
.Replace("误", "誤")
.Replace("码", "碼")
.Replace("碼", "碼")
.Replace("咖", "喀")
.Replace("涉", "渉")
.Replace("籐", "藤")
.Replace("鉋", "鉋")
.Replace("埃", "挨")
.Replace("狄", "狄")
.Replace("茉", "茉")
.Replace("婭", "亞")
.Replace("琪", "淇")
.Replace("翰", "翰")
.Replace("薑", "薑")
.Replace("嘉", "伽")
.Replace("郡", "郡")
.Replace("咘", "卜")
.Replace("佛", "佛")
.Replace("李", "李")
.Replace("檢", "檢")
.Replace("酩", "銘")
.Replace("酊", "叮")
.Replace("柯", "顆")
.Replace("腥", "腥")
.Replace("襯", "襯")
.Replace("衫", "衫")
.Replace("褲", "庫")
.Replace("鞋", "鞋")
.Replace("堡", "堡")
.Replace("刪", "刪")
.Replace("讯", "迅")
.Replace("桿", "杆")
.Replace("逗", "逗")
.Replace("勸", "勸")
.Replace("齋", "哉")
.Replace("瘦", "痩")
.Replace("翹", "翹")
.Replace("决", "决")
.Replace("廓", "廓")
.Replace("訕", "山")
.Replace("痊", "全")
.Replace("矇", "蒙")
.Replace("螫", "螫")
.Replace("妳", "祢")
.Replace("腸", "腸")
.Replace("誆", "框")
.Replace("宏", "宏")
.Replace("檯", "台")
.Replace("惰", "墮")
.Replace("躬", "弓")
.Replace("义", "義")
.Replace("擠", "擠")
.Replace("捐", "捐")
.Replace("辭", "辭")
.Replace("灵", "靈")
.Replace("爷", "爺")
.Replace("货", "貨")
.Replace("车", "車")
.Replace("头", "頭")
.Replace("龙", "龍")
.Replace("鹰", "鷹")
.Replace("达", "達")
.Replace("掀", "掀")
.Replace("胖", "胖")
.Replace("框", "框")
.Replace("踐", "践")
.Replace("睿", "鋭")
.Replace("糜", "弥")
.Replace("睜", "争")
.Replace("屆", "届")
.Replace("倪", "尼")
.Replace("迭", "疊")
.Replace("孽", "涅")
.Replace("坡", "坡")
.Replace("扶", "撫")
.Replace("蝟", "梶")
.Replace("辮", "謂")
.Replace("淘", "萄")
.Replace("垰", "喀")
.Replace("疤", "巴")
.Replace("廠", "廠")
.Replace("鄰", "鄰")
.Replace("获", "獲")
.Replace("泙", "平")
.Replace("现", "現")
.Replace("广", "广")
.Replace("举", "舉")
.Replace("说", "説")
.Replace("关", "關")
.Replace("于", "于")
.Replace("讨", "討")
.Replace("辉", "輝")
.Replace("带", "帯")
.Replace("锁", "鎖")
.Replace("识", "識")
.Replace("僱", "顧")
.Replace("糗", "嚊")
.Replace("攔", "攬")
.Replace("魷", "尤")
.Replace("款", "款")
.Replace("嘔", "嘔")
.Replace("租", "租")
.Replace("羹", "羹")
.Replace("煸", "遍")
.Replace("卫", "衛")
.Replace("焗", "局")
.Replace("啡", "非")
.Replace("肅", "肅")
.Replace("叼", "貂")
.Replace("逾", "踰")
.Replace("纹", "紋")
.Replace("梆", "幇")
.Replace("刨", "庖")
.Replace("餃", "餃")
.Replace("嬸", "審")
.Replace("产", "產")
.Replace("异", "異")
.Replace("态", "態")
.Replace("领", "領")
.Replace("维", "維")
.Replace("护", "護")
.Replace("澆", "澆")
.Replace("寥", "寥")
.Replace("进", "進")
.Replace("传", "傳")
.Replace("纽", "鈕")
.Replace("经", "經")
.Replace("矩", "矩")
.Replace("猖", "猖")
.Replace("獗", "厥")
.Replace("逮", "逮")
.Replace("检", "檢")
.Replace("嚷", "壤")
.Replace("潢", "湟")
.Replace("搗", "搗")
.Replace("蹋", "踏")
.Replace("卿", "卿")
.Replace("騷", "騷")
.Replace("滯", "滯")
.Replace("詐", "詐")
.Replace("搥", "錘")
.Replace("鉆", "鑽")
.Replace("睹", "睹")
.Replace("悚", "悚")
.Replace("抄", "操")
.Replace("踴", "蛹")
.Replace("皺", "皺")
.Replace("熬", "熬")
.Replace("貿", "貿")
.Replace("覓", "覓")
.Replace("痹", "痺")
.Replace("猴", "喉")
.Replace("贖", "贖")
.Replace("諱", "諱")
.Replace("淹", "淹")
.Replace("渺", "渺")
.Replace("瑣", "瑣")
.Replace("疏", "疏")
.Replace("暸", "瞭")
.Replace("叔", "叔")
.Replace("坨", "陀")
.Replace("聾", "隆")
.Replace("拯", "拯")
.Replace("潭", "潭")
.Replace("寨", "砦")
.Replace("悽", "凄")
.Replace("洲", "洲")
.Replace("噪", "躁")
.Replace("軼", "軼")
.Replace("趨", "驅")
.Replace("瀰", "弥")
.Replace("摔", "蟀")
.Replace("扬", "揚")
.Replace("寻", "尋")
.Replace("迹", "迹")
.Replace("杳", "杳")
.Replace("裙", "裙")
.Replace("舍", "舍")
.Replace("囂", "囂")
.Replace("虞", "虞")
.Replace("賠", "賠")
.Replace("鱷", "顎")
.Replace("餚", "肴")
.Replace("贊", "贊")
.Replace("昭", "昭")
.Replace("捎", "哨")
.Replace("黨", "黨")
.Replace("溼", "湿")
.Replace("撩", "繚")
.Replace("佔", "占")
.Replace("政", "政")
.Replace("彥", "諺")
.Replace("匡", "框")
.Replace("塌", "榻")
.Replace("咿", "姨")
.Replace("蛤", "哈")
.Replace("蝌", "蝌")
.Replace("蚪", "蚪")
.Replace("袍", "袍")
.Replace("篷", "蓬")
.Replace("罗", "羅")
.Replace("宾", "繽")
.Replace("碱", "鹸")
.Replace("躡", "涅")
.Replace("蝸", "渦")
.Replace("蚰", "蚰")
.Replace("鋤", "鋤")
.Replace("蔚", "蔚")
.Replace("蠶", "蠶")
.Replace("釦", "扣")
.Replace("鱉", "鼈")
.Replace("辟", "辟")
.Replace("莓", "莓")
.Replace("孕", "孕")
.Replace("菁", "青")
.Replace("蘑", "磨")
.Replace("梨", "梨")
.Replace("鱈", "鱈")
.Replace("胺", "安")
.Replace("胭", "臙")
.Replace("壘", "壘")
.Replace("抒", "抒")
.Replace("榍", "屑")
.Replace("鯰", "鯰")
.Replace("穆", "穆")
.Replace("謠", "謠")
.Replace("铠", "鎧")
.Replace("沁", "沁")
.Replace("边", "辺")
.Replace("脸", "臉")
.Replace("银", "銀")
.Replace("鱼", "魚")
.Replace("呱", "呱")
.Replace("縝", "鎮")
.Replace("愈", "愈")
.Replace("壳", "殼")
.Replace("褶", "褶")
.Replace("颶", "颶")
.Replace("函", "函")
.Replace("鵝", "鵝")
.Replace("鶇", "鶇")
.Replace("淤", "淤")
.Replace("陵", "陵")
.Replace("囀", "囀")
.Replace("缸", "缸")
.Replace("喊", "喊")
.Replace("窒", "窒")
.Replace("皋", "皋")
.Replace("蟬", "嬋")
.Replace("蟋", "蟋")
.Replace("蟀", "蟀")
.Replace("戾", "歴")
.Replace("赭", "赭")
.Replace("櫧", "儲")
.Replace("磺", "黄")
.Replace("峭", "峭")
.Replace("諫", "諫")
.Replace("拱", "拱")
.Replace("鐳", "雷")
.Replace("亙", "亙")
.Replace("庇", "庇")
.Replace("騁", "騁")
.Replace("遨", "遨")
.Replace("墳", "墳")
.Replace("艦", "艦")
.Replace("簇", "簇")
.Replace("諭", "諭")
.Replace("櫛", "櫛")
.Replace("蘗", "蘗")
.Replace("瀨", "頼")
.Replace("祇", "祇")
.Replace("吆", "ｙｏ")
.Replace("祿", "祿")
.Replace("醞", "允")
.Replace("魘", "魘")
.Replace("甸", "甸")
.Replace("韜", "韜")
.Replace("匿", "匿")
.Replace("薊", "薊")
.Replace("薺", "薺")
.Replace("朶", "朶")
.Replace("洸", "洸")
.Replace("喇", "喇")
.Replace("叭", "叭")
.Replace("崗", "崗")
.Replace("哨", "哨")
.Replace("刑", "刑")
.Replace("顱", "顱")
.Replace("剌", "剌")
.Replace("匕", "匕")
.Replace("閘", "閘")
.Replace("珥", "珥")
.Replace("鉻", "絡")
.Replace("袓", "祖")
.Replace("瑕", "瑕")
.Replace("鏟", "産")
.Replace("囍", "憙")
.Replace("猝", "猝")
.Replace("嚎", "豪")
.Replace("葫", "葫")
.Replace("蘆", "蘆")
.Replace("坎", "坎")
.Replace("扒", "叭")
.Replace("劊", "刽")
.Replace("濺", "濺")
.Replace("夷", "夷")
.Replace("晰", "晰")
.Replace("檞", "解")
.Replace("燎", "燎")
.Replace("颱", "颱")
.Replace("閥", "閥")
.Replace("璣", "机")
.Replace("釉", "釉")
.Replace("鋰", "理")
.Replace("鈷", "鈷")
.Replace("硃", "洙")
.Replace("殞", "殞")
.Replace("縹", "縹")
.Replace("緲", "緲")
.Replace("輓", "輓")
.Replace("娑", "娑")
.Replace("闢", "闢")
.Replace("駿", "駿")
.Replace("釷", "肚")
.Replace("慓", "慓")
.Replace("鑣", "標")
.Replace("汪", "汪")
.Replace("毗", "毘")
.Replace("誅", "誅")
.Replace("拐", "拐")
.Replace("卉", "卉")
.Replace("嚓", "擦")
.Replace("惶", "惶")
.Replace("陛", "陛")
.Replace("壕", "壕")
.Replace("藪", "藪")
.Replace("挾", "挾")
.Replace("繆", "繆")
.Replace("岬", "岬")
.Replace("碾", "碾")
.Replace("嗥", "嚆")
.Replace("堰", "堰")
.Replace("譏", "譏")
.Replace("諷", "諷")
.Replace("耿", "耿")
.Replace("濛", "濛")
.Replace("蕊", "蘂")
.Replace("腎", "腎")
.Replace("鴟", "鴟")
.Replace("檳", "檳")
.Replace("箴", "箴")
.Replace("汛", "迅")
.Replace("磅", "磅")
.Replace("汐", "汐")
.Replace("愴", "愴")
.Replace("嶼", "嶼")
.Replace("戮", "戮")
.Replace("啼", "啼")
.Replace("煞", "殺")
.Replace("懣", "懣")
.Replace("窪", "窪")
.Replace("篤", "篤")
.Replace("戍", "戍")
.Replace("嚙", "涅")
.Replace("癡", "痴")
.Replace("噩", "惡")
.Replace("鋁", "呂")
.Replace("錳", "猛")
.Replace("鈣", "丐")
.Replace("鴻", "鴻")
.Replace("樑", "梁")
.Replace("鎳", "聶")
.Replace("歿", "歿")
.Replace("蟒", "蠎")
.Replace("櫺", "櫺")
.Replace("壤", "壌")
.Replace("蔻", "寇")
.Replace("鰲", "鰲")
.Replace("琦", "竒")
.Replace("倜", "啼")
.Replace("儻", "儻")
.Replace("蕭", "蕭")
.Replace("邈", "渺")
.Replace("槓", "槓")
.Replace("洰", "巨")
.Replace("辯", "辯")
.Replace("灣", "灣")
.Replace("鴿", "鴿")
.Replace("嗷", "嗷")
.Replace("賈", "賈")
.Replace("唇", "唇")
.Replace("嗩", "鎖")
.Replace("皚", "皚")
.Replace("絳", "絳")
.Replace("戈", "戈")
.Replace("几", "几")
.Replace("剮", "刮")
.Replace("凔", "蒼")
.Replace("溪", "溪")
.Replace("址", "址")
.Replace("悻", "幸")
.Replace("翟", "曲")
.Replace("愿", "愿")
.Replace("财", "財")
.Replace("悬", "懸")
.Replace("难", "難")
.Replace("烦", "煩")
.Replace("恼", "惱")
.Replace("谢", "謝")
.Replace("绝", "絶")
.Replace("马", "馬")
.Replace("嘮", "勞")
.Replace("銬", "拷")
.Replace("釵", "釵")
.Replace("凯", "凱")
.Replace("尔", "爾")
.Replace("乌", "烏")
.Replace("诺", "諾")
.Replace("爱", "愛")
.Replace("库", "庫")
.Replace("亚", "亞")
.Replace("恤", "恤")
.Replace("臍", "臍")
.Replace("尿", "尿")
.Replace("紮", "紮")
.Replace("趴", "爬")
.Replace("鶩", "鶩")
.Replace("曠", "曠")
.Replace("跤", "較")
.Replace("辧", "辧")
.Replace("尓", "尓")
.Replace("幇", "幇")
.Replace("奄", "奄")
.Replace("偸", "偸")
.Replace("廷", "廷")
.Replace("署", "署")
.Replace("烽", "烽")
.Replace("債", "債")
.Replace("澀", "澀")
.Replace("檸", "檸")
.Replace("簣", "簣")
.Replace("邏", "邏")*/
            #endregion

            #region 400字基础通假
.Replace("时", "時")
.Replace("戏", "戲")
.Replace("厅", "庁")
.Replace("门", "門")
.Replace("为", "為")
.Replace("间", "間")
.Replace("键", "鍵")
.Replace("盘", "鍵")
.Replace("开", "開")
.Replace("积", "積")
.Replace("换", "換")
.Replace("试", "試")
.Replace("节", "節")
.Replace("转", "轉")
.Replace("发", "發")
.Replace("这", "這")
.Replace("删", "刪")
.Replace("哥", "哥")
.Replace("击", "撃")
.Replace("战", "戰")
.Replace("务", "務")
.Replace("报", "報")
.Replace("勵", "励")
.Replace("揭", "掲")
.Replace("她", "他")
.Replace("攀", "攀")
.Replace("鈕", "鈕")
.Replace("聊", "聊")
.Replace("层", "層")
.Replace("您", "イ尓")
.Replace("咳", "咳")
.Replace("崔", "摧")
.Replace("糟", "糟")
.Replace("强", "強")
.Replace("猎", "獵")
.Replace("团", "團")
.Replace("变", "変")
.Replace("类", "類")
.Replace("动", "動")
.Replace("帮", "幇")
.Replace("邮", "郵")
.Replace("减", "減")
.Replace("页", "頁")
.Replace("标", "標")
.Replace("摇", "搖")
.Replace("杆", "杆")
.Replace("单", "單")
.Replace("视", "視")
.Replace("图", "図")
.Replace("輯", "輯")
.Replace("签", "簽")
.Replace("滚", "滾")
.Replace("钮", "鈕")
.Replace("链", "鏈")
.Replace("导", "導")
.Replace("员", "員")
.Replace("显", "顯")
.Replace("设", "設")
.Replace("项", "項")
.Replace("搜", "搜")
.Replace("测", "測")
.Replace("级", "級")
.Replace("截", "截")
.Replace("场", "場")
.Replace("过", "過")
.Replace("拟", "擬")
.Replace("斩", "斬")
.Replace("撈", "勞")
.Replace("蹲", "頓")
.Replace("准", "准")
.Replace("复", "復")
.Replace("离", "離")
.Replace("戳", "戳")
.Replace("臺", "台")
.Replace("冲", "冲")
.Replace("註", "注")
.Replace("覽", "覧")
.Replace("嗯", "恩")
.Replace("哦", "哦")
.Replace("囉", "羅")
.Replace("唉", "艾")
.Replace("呀", "呀")
.Replace("籌", "籌")
.Replace("吩", "分")
.Replace("咐", "附")
.Replace("滂", "膀")
.Replace("沱", "陀")
.Replace("聰", "聰")
.Replace("迪", "迪")
.Replace("儂", "濃")
.Replace("迈", "邁")
.Replace("无", "无")
.Replace("詢", "訓")
.Replace("搏", "搏")
.Replace("奠", "典")
.Replace("餵", "畏")
.Replace("舖", "鋪")
.Replace("賺", "賺")
.Replace("姓", "姓")
.Replace("蔬", "疏")
.Replace("奕", "亦")
.Replace("懶", "懶")
.Replace("唔", "唔")
.Replace("邀", "邀")
.Replace("儘", "盾")
.Replace("沮", "沮")
.Replace("哇", "哇")
.Replace("鄙", "鄙")
.Replace("措", "措")
.Replace("哈", "哈")
.Replace("虧", "虧")
.Replace("陪", "培")
.Replace("嘛", "嘛")
.Replace("肯", "肯")
.Replace("哎", "艾")
.Replace("憊", "備")
.Replace("扯", "撤")
.Replace("懦", "懦")
.Replace("歉", "歉")
.Replace("贏", "贏")
.Replace("鬧", "閙")
.Replace("羞", "羞")
.Replace("衷", "衷")
.Replace("懊", "懊")
.Replace("庸", "庸")
.Replace("轍", "徹")
.Replace("餒", "妥")
.Replace("吶", "吶")
.Replace("誡", "戒")
.Replace("萊", "莱")
.Replace("莎", "砂")
.Replace("帖", "貼")
.Replace("苻", "付")
.Replace("榜", "榜")
.Replace("瀏", "琉")
.Replace("样", "樣")
.Replace("檔", "档")
.Replace("窄", "窄")
.Replace("陋", "漏")
.Replace("擒", "擒")
.Replace("坦", "坦")
.Replace("氓", "芒")
.Replace("瀧", "朧")
.Replace("鏘", "将")
.Replace("釁", "釁")
.Replace("慘", "慚")
.Replace("恕", "恕")
.Replace("叮", "叮")
.Replace("嗦", "索")
.Replace("笨", "笨")
.Replace("忽", "忽")
.Replace("攸", "悠")
.Replace("陌", "陌")
.Replace("廳", "庁")
.Replace("窩", "窩")
.Replace("嘖", "責")
.Replace("饋", "饋")
.Replace("呿", "怯")
.Replace("嗄", "嚇")
.Replace("欸", "欸")
.Replace("跌", "跌")
.Replace("咪", "米")
.Replace("敞", "敞")
.Replace("漓", "璃")
.Replace("勃", "勃")
.Replace("伶", "令")
.Replace("陶", "陶")
.Replace("啥", "撒")
.Replace("扛", "抗")
.Replace("砍", "坎")
.Replace("倆", "兩")
.Replace("论", "論")
.Replace("寞", "寞")
.Replace("侈", "侈")
.Replace("窘", "窮")
.Replace("掙", "争")
.Replace("扎", "紮")
.Replace("啞", "呀")
.Replace("渥", "沃")
.Replace("茲", "茲")
.Replace("哆", "咄")
.Replace("渙", "喚")
.Replace("芬", "紛")
.Replace("噯", "艾")
.Replace("汀", "叮")
.Replace("蒂", "帝")
.Replace("弗", "弗")
.Replace("喬", "巧")
.Replace("妮", "泥")
.Replace("嘻", "喜")
.Replace("鋪", "鋪")
.Replace("囤", "飩")
.Replace("错", "錯")
.Replace("误", "誤")
.Replace("码", "碼")
.Replace("碼", "碼")
.Replace("咖", "喀")
.Replace("籐", "藤")
.Replace("鉋", "鉋")
.Replace("埃", "挨")
.Replace("婭", "亞")
.Replace("琪", "淇")
.Replace("嘉", "伽")
.Replace("佛", "佛")
.Replace("李", "李")
.Replace("檢", "檢")
.Replace("酩", "銘")
.Replace("酊", "叮")
.Replace("柯", "顆")
.Replace("腥", "腥")
.Replace("衫", "衫")
.Replace("褲", "庫")
.Replace("鞋", "鞋")
.Replace("堡", "堡")
.Replace("刪", "刪")
.Replace("讯", "迅")
.Replace("桿", "杆")
.Replace("勸", "勸")
.Replace("齋", "哉")
.Replace("瘦", "痩")
.Replace("翹", "翹")
.Replace("决", "决")
.Replace("廓", "廓")
.Replace("訕", "山")
.Replace("痊", "全")
.Replace("矇", "蒙")
.Replace("螫", "螫")
.Replace("妳", "祢")
.Replace("腸", "腸")
.Replace("誆", "框")
.Replace("宏", "宏")
.Replace("檯", "台")
.Replace("惰", "墮")
.Replace("躬", "弓")
.Replace("义", "義")
.Replace("擠", "擠")
.Replace("灵", "靈")
.Replace("爷", "爺")
.Replace("货", "貨")
.Replace("车", "車")
.Replace("头", "頭")
.Replace("龙", "龍")
.Replace("达", "達")
.Replace("掀", "掀")
.Replace("框", "框")
.Replace("踐", "践")
.Replace("睿", "鋭")
.Replace("糜", "弥")
.Replace("睜", "争")
.Replace("屆", "届")
.Replace("倪", "尼")
.Replace("迭", "疊")
.Replace("孽", "涅")
.Replace("坡", "坡")
.Replace("扶", "撫")
.Replace("蝟", "梶")
.Replace("辮", "謂")
.Replace("淘", "萄")
.Replace("垰", "喀")
.Replace("获", "獲")
.Replace("泙", "平")
.Replace("现", "現")
.Replace("广", "广")
.Replace("举", "舉")
.Replace("说", "説")
.Replace("关", "關")
.Replace("于", "于")
.Replace("讨", "討")
.Replace("辉", "輝")
.Replace("带", "帯")
.Replace("锁", "鎖")
.Replace("识", "識")
.Replace("僱", "顧")
.Replace("款", "款")
.Replace("嘔", "嘔")
.Replace("租", "租")
.Replace("煸", "遍")
.Replace("卫", "衛")
.Replace("焗", "局")
.Replace("啡", "非")
.Replace("肅", "肅")
.Replace("纹", "紋")
.Replace("梆", "幇")
.Replace("刨", "庖")
.Replace("产", "產")
.Replace("异", "異")
.Replace("态", "態")
.Replace("领", "領")
.Replace("维", "維")
.Replace("护", "護")
.Replace("寥", "寥")
.Replace("进", "進")
.Replace("传", "傳")
.Replace("纽", "鈕")
.Replace("经", "經")
.Replace("矩", "矩")
.Replace("獗", "厥")
.Replace("检", "檢")
.Replace("搗", "搗")
.Replace("騷", "騷")
.Replace("搥", "錘")
.Replace("鉆", "鑽")
.Replace("悚", "悚")
.Replace("抄", "操")
.Replace("踴", "蛹")
.Replace("皺", "皺")
.Replace("熬", "熬")
.Replace("覓", "覓")
.Replace("痹", "痺")
.Replace("猴", "喉")
.Replace("渺", "渺")
.Replace("疏", "疏")
.Replace("暸", "瞭")
.Replace("坨", "陀")
.Replace("聾", "隆")
.Replace("拯", "拯")
.Replace("寨", "砦")
.Replace("洲", "洲")
.Replace("噪", "躁")
.Replace("趨", "驅")
.Replace("瀰", "弥")
.Replace("扬", "揚")
.Replace("寻", "尋")
.Replace("迹", "迹")
.Replace("裙", "裙")
.Replace("舍", "舍")
.Replace("囂", "囂")
.Replace("虞", "虞")
.Replace("賠", "賠")
.Replace("鱷", "顎")
.Replace("餚", "肴")
.Replace("贊", "贊")
.Replace("昭", "昭")
.Replace("捎", "哨")
.Replace("黨", "黨")
.Replace("溼", "湿")
.Replace("撩", "繚")
.Replace("佔", "占")
.Replace("政", "政")
.Replace("彥", "諺")
.Replace("匡", "框")
.Replace("塌", "榻")
.Replace("咿", "姨")
.Replace("蛤", "哈")
.Replace("蝌", "蝌")
.Replace("蚪", "蚪")
.Replace("篷", "蓬")
.Replace("罗", "羅")
.Replace("宾", "繽")
.Replace("躡", "涅")
.Replace("蝸", "渦")
.Replace("鋤", "鋤")
.Replace("釦", "扣")
.Replace("孕", "孕")
.Replace("菁", "青")
.Replace("蘑", "磨")
.Replace("壘", "壘")
.Replace("沁", "沁")
.Replace("银", "銀")
.Replace("愈", "愈")
.Replace("褶", "褶")
.Replace("颶", "颶")
.Replace("喊", "喊")
.Replace("騁", "騁")
.Replace("遨", "遨")
.Replace("朶", "朶")
.Replace("叭", "叭")
.Replace("哨", "哨")
.Replace("刑", "刑")
.Replace("顱", "顱")
.Replace("匕", "匕")
.Replace("珥", "珥")
.Replace("坎", "坎")
.Replace("劊", "刽")
.Replace("堰", "堰")
.Replace("耿", "耿")
.Replace("啼", "啼")
.Replace("癡", "痴")
.Replace("嗷", "嗷")
.Replace("唇", "唇")
.Replace("戈", "戈")
.Replace("几", "几")
.Replace("溪", "溪")
.Replace("址", "址")
.Replace("愿", "愿")
.Replace("难", "難")
.Replace("烦", "煩")
.Replace("恼", "惱")
.Replace("谢", "謝")
.Replace("马", "馬")
.Replace("凯", "凱")
.Replace("尔", "爾")
.Replace("乌", "烏")
.Replace("诺", "諾")
.Replace("爱", "愛")
.Replace("库", "庫")
.Replace("亚", "亞")
.Replace("恤", "恤")
.Replace("尿", "尿")
.Replace("紮", "紮")
.Replace("幇", "幇")
.Replace("奄", "奄")
.Replace("偸", "偸")
.Replace("廷", "廷")
.Replace("署", "署")
.Replace("烽", "烽")
.Replace("債", "債")
            #endregion
                            ;

            string temp = "";

            
            for (int j = 0; j < str.Length; j++)
            {
                char c = str[j];
                if (DictTongJia.ContainsKey(c) && DictTongJia[c] != c)
                    c = DictTongJia[c];

                temp += c;
            }

            return temp;
        }

        
    }
}