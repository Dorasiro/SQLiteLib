using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteLib
{
    public class Logger
    {
        /// <summary>
        /// ファイルストリームを個別に取得するとロックが競合するのでstaticにして共通のものを使う
        /// </summary>
        private static StreamWriter writer;

        /// <summary>
        /// セッション管理用のIDを作成するために使う
        /// </summary>
        private static Random random = new Random();

        /// <summary>
        /// セッション管理用のIDに使われる文字
        /// </summary>
        private static readonly char[] SessionChars = new char[62];

        /// <summary>
        /// 追加したタグを格納する
        /// </summary>
        private List<string> tags;

        private static AsyncLock asyncLock = new AsyncLock();

        /// <summary>
        /// writerを初期化するタイプ初期化子
        /// </summary>
        static Logger()
        {
            writer = new StreamWriter("database_log.txt");
            SessionChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
        }

        /// <summary>
        /// 何もしないデフォルトコンストラクタ
        /// </summary>
        public Logger()
        {
            tags = new List<string>();
        }

        /// <summary>
        /// タグを1つ追加するコンストラクタ
        /// </summary>
        /// <param name="tag"></param>
        public Logger(string tag)
        {
            tags = new List<string>();
            AddTag(tag);
        }

        /// <summary>
        /// タグを2つ追加するコンストラクタ
        /// </summary>
        /// <param name="tag"></param>
        public Logger(string tag1, string tag2) : this(tag1)
        {
            AddTag(tag2);
        }

        /// <summary>
        /// タグを3つ追加するコンストラクタ
        /// </summary>
        /// <param name="tag"></param>
        public Logger(string tag1, string tag2, string tag3) : this(tag1, tag2)
        {
            AddTag(tag3);
        }

        /// <summary>
        /// タグを4つ追加するコンストラクタ
        /// </summary>
        /// <param name="tag"></param>
        public Logger(string tag1, string tag2, string tag3, string tag4) : this(tag1, tag2, tag3)
        {
            AddTag(tag4);
        }

        /// <summary>
        /// タグを複数追加するコンストラクタ
        /// </summary>
        /// <param name="tag"></param>
        public Logger(IEnumerable<string> tags)
        {
            foreach(var tag in tags)
            {
                AddTag(tag);
            }
        }

        /// <summary>
        /// [tag]のようなタグをログの先頭に追加する
        /// </summary>
        /// <param name="tag"></param>
        public void AddTag(string tag)
        {
            // タグの数がが1以上で最後のタグと同じタグを追加するのを防ぐ
            if(tags.Count > 0 && tags[tags.Count - 1] == tag)
            {
                return;
            }

            tags.Add("[" + tag + "]");
        }

        /// <summary>
        /// ユニークなIDを作成してタグを作成する
        /// </summary>
        /// <returns>作成されたID</returns>
        public string AddSessionTag()
        {
            var id = new string(Enumerable.Repeat(SessionChars, 5).Select(s => s[random.Next(s.Length)]).ToArray());
            AddTag(id);
            return id;
        }

        /// <summary>
        /// タグを全部消す
        /// </summary>
        public void ClearTag()
        {
            tags.Clear(); ;
        }

        /// <summary>
        /// 末尾のタグを消す
        /// </summary>
        public void RemoveLastTag()
        {
            if(tags.Count > 0)
            {
                tags.RemoveAt(tags.Count - 1);
            }
        }

        /// <summary>
        /// messageをログに出力する
        /// </summary>
        /// <param name="message"></param>
        public void WriteLog(string message)
        {
            string tag = "";
            foreach(var t in tags)
            {
                tag += t;
            }

            while(true)
            {
                try
                {
                    writer.WriteLine(DateTime.Now.ToString() + " " + tag);
                    writer.WriteLine("   " + message + Environment.NewLine);
                    writer.Flush();

                    break;
                }
                catch (InvalidOperationException)
                {
                    // 何もせずリトライ
                }
            }
        }

        /// <summary>
        /// [非同期]messageをログに出力する
        /// </summary>
        /// <param name="message"></param>
        public async Task WriteLogAsync(string message)
        {
            string tag = "";
            var tagsCopy = tags.ToArray();
            foreach (var t in tagsCopy)
            {
                tag += t;
            }

            using (await asyncLock.LockAsync().ConfigureAwait(false))
            {
                try
                {
                    await writer.WriteLineAsync(DateTime.Now.ToString() + " " + tag).ConfigureAwait(false);
                    await writer.WriteLineAsync("   " + message + Environment.NewLine).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // 書き込み中にアプリが終了された場合にでる
                    // 仕方ないので放置
                }
                catch (ObjectDisposedException)
                {
                    // 書き込み中にアプリが終了された場合にでる
                    // 仕方ないので放置
                }
            }
            
        }

        /// <summary>
        /// Loggerのコピーを渡す
        /// 参照で渡して使用すると非同期処理の時に勝手に変更される可能性がある
        /// </summary>
        /// <returns></returns>
        public Logger Clone()
        {
            // 参照型のフィールドは参照がコピーされるらしいので同じ中身で別のListに作り直す
            var tagsClone = new List<string>();
            for(int i=0;i<tags.Count-1;i++)
            {
                tagsClone.Add(tags[i]);
            }

            // 複製を作ってTagsを入れ替える
            var clone = (Logger)MemberwiseClone();
            clone.tags = tagsClone;

            return clone;
        }

        /// <summary>
        /// Loggerを閉じる
        /// </summary>
        public static void Close()
        {
            while(true)
            {
                try
                {
                    writer.Close();

                    break;
                }
                catch(InvalidOperationException)
                {
                    // 終わるまで繰り返す
                }
            }
        }
    }
}
