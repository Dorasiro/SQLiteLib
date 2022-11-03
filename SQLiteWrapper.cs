using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteLib
{
    /// <summary>
    /// 板にかかわらず１つのDBを使うのでstatic
    /// </summary>
    public class SQLiteWrapper
    {
        /// <summary>
        /// 指定した大きさの文字列型DbTypesの配列を返す
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private static DbType[] CreateStringDbTypes(int count)
        {
            var types = new DbType[count];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = DbType.String;
            }

            return types;
        }

        /// <summary>
        /// 指定したテーブルの特定カラムのみを取り出すselect文を実行。whereで条件を指定する
        /// select column from table where where
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static string SQL_SelectAny(string table, string[] columns, string where)
        {
            string cmd = "";

            // 全カラム対象の時
            if (columns.Length == 0)
            {
                cmd += "select * from " + table;
            }
            else
            {
                cmd += "select ";

                for (int i = 0; i < columns.Length - 1; i++)
                {
                    cmd += columns[i] + ",";
                }

                cmd += columns[columns.Length - 1] + " from " + table;
            }

            // whereを指定しない時
            if (where != "")
            {
                cmd += " where " + where;
            }

            return cmd;
        }

        /// <summary>
        /// 指定したテーブルの特定カラムのみを取り出すselect文を実行。whereで条件を指定する
        /// select column from table where where
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static string SQL_SelectAny(string table, string column, string where)
        {
            return SQL_SelectAny(table, new string[] { column }, where);
        }

        /// <summary>
        /// 指定したテーブルの特定カラムのみを取り出すselect文を実行
        /// select column from table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static string SQL_SelectAny(string table, string[] columns)
        {
            return SQL_SelectAny(table, columns, "");
        }

        /// <summary>
        /// 指定したテーブルの特定カラムのみを取り出すselect文を実行
        /// select column from table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public static string SQL_SelectAny(string table, string column)
        {
            return SQL_SelectAny(table, column, "");
        }

        /// <summary>
        /// 指定したテーブルの全カラムを対象とするselect文を実行。whereで条件を指定する
        /// select * from table where where
        /// </summary>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static string SQL_SelectAll(string table, string where)
        {
            return SQL_SelectAny(table, new string[] { }, where);
        }

        /// <summary>
        /// 指定したテーブルの全カラムを対象とするselect文を実行
        /// select * from table
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string SQL_SelectAll(string table)
        {
            return SQL_SelectAny(table, new string[] { }, "");
        }

        /// <summary>
        /// fTableにsTableを内部結合する
        /// select column from (fTable inner join sTable on on) where where
        /// </summary>
        /// <param name="fTable">結合元</param>
        /// <param name="sTable">結合先</param>
        /// <param name="columns">取得するカラム</param>
        /// <param name="on">一致条件</param>
        /// <param name="where">条件</param>
        /// <returns></returns>
        public static string SQL_InnerJoin(string fTable, string sTable, string[] columns, string on, string where)
        {
            string cmd = "";
            if (fTable == "" || sTable == "" || columns.Length == 0 || on == "")
            {
                throw new ArgumentException("引数を空にすることはできません");
            }

            cmd = "select ";

            for (int i = 0; i < columns.Length - 1; i++)
            {
                cmd += columns[i] + ",";
            }

            cmd += string.Format("{0} from ({1} inner join {2}", columns[columns.Length - 1], fTable, sTable);

            if (on != "")
            {
                cmd += " on " + on;
            }

            cmd += ")";

            if (where != "")
            {
                cmd += " where " + where;
            }

            return cmd;
        }

        /// <summary>
        /// fTableにsTableを内部結合する
        /// select column from (fTable inner join sTable on on)
        /// </summary>
        /// <param name="fTable">結合元</param>
        /// <param name="sTable">結合先</param>
        /// <param name="columns">取得するカラム</param>
        /// <param name="on">一致条件</param>
        /// <returns></returns>
        public static string SQL_InnerJoin(string fTable, string sTable, string[] columns, string on)
        {
            return SQL_InnerJoin(fTable, sTable, columns, on, "");
        }

        /// <summary>
        /// fTableにsTableとtTableをfOnの条件で内部結合したものsOnの条件で内部結合しwhereの条件で検索
        /// select columns from ((fTable inner join sTable on fOn) inner join tTable on sOn) where where
        /// </summary>
        /// <param name="fTable"></param>
        /// <param name="sTable"></param>
        /// <param name="tTable"></param>
        /// <param name="columns"></param>
        /// <param name="fOn"></param>
        /// <param name="sOn"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static string SQL_InnerJoin(string fTable, string sTable, string tTable, string[] columns, string fOn, string sOn, string where)
        {
            var part = string.Format("({0} inner join {1} on {2})", fTable, sTable, fOn);
            return SQL_InnerJoin(part, tTable, columns, sOn, where);
        }

        /// <summary>
        /// fTableにsTableとtTableをfOnの条件で内部結合したものsOnの条件で内部結合
        /// select columns from ((fTable inner join sTable on fOn) inner join tTable on sOn)
        /// </summary>
        /// <param name="fTable"></param>
        /// <param name="sTable"></param>
        /// <param name="tTable"></param>
        /// <param name="columns"></param>
        /// <param name="fOn"></param>
        /// <param name="sOn"></param>
        /// <returns></returns>
        public static string SQL_InnerJoin(string fTable, string sTable, string tTable, string[] columns, string fOn, string sOn)
        {
            return SQL_InnerJoin(fTable, sTable, tTable, columns, fOn, sOn, "");
        }

        /// <summary>
        /// テーブルの中に条件に一致するものが何件あるかを返す
        /// </summary>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static string SQL_Count(string table, string where)
        {
            string cmd = "";
            cmd = "select count(*) from " + table;

            if (where != "")
            {
                cmd += " where " + where;
            }

            return cmd;
        }

        /// <summary>
        /// 指定したテーブルの大きさをを返す
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string SQL_TableCount(string table)
        {
            return SQL_Count(table, "");
        }

        /// <summary>
        /// データをinsertする
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <param name="types"></param>
        /// <param name="values"></param>
        public static SQLiteLibCommandAddParam SQL_Insert(string table, string[] columns, DbType[] types, object[] values)
        {
            if (columns.Length != values.Length)
            {
                throw new ArgumentException("指定されたカラムの数と値の数が一致しません");
            }

            string cmd = "";
            cmd = "insert into " + table + " (";

            for (int i = 0; i < columns.Length - 1; i++)
            {
                cmd += columns[i] + ",";
            }

            cmd += columns[columns.Length - 1] + ") values (";

            for (int i = 0; i < columns.Length - 1; i++)
            {
                cmd += "@" + columns[i] + ",";
            }

            cmd += "@" + columns[columns.Length - 1] + ")";

            var paramList = new List<SQLiteParameter>();
            for (int i = 0; i < columns.Length; i++)
            {
                var param = new SQLiteParameter(columns[i], types[i])
                {
                    Value = values[i]
                };
                paramList.Add(param);
            }

            return new SQLiteLibCommandAddParam(cmd, paramList);
        }

        /// <summary>
        /// 型が文字列のデータをinsertする
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <param name="values"></param>
        public static SQLiteLibCommandAddParam SQL_Insert(string table, string[] columns, string[] values)
        {
            return SQL_Insert(table, columns, CreateStringDbTypes(columns.Length), values);
        }

        /// <summary>
        /// DbTypeを指定して複数カラムを対象にupdateを実行。
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <param name="types"></param>
        /// <param name="values"></param>
        /// <param name="where"></param>
        public static SQLiteLibCommandAddParam SQL_Update(string table, string[] columns, DbType[] types, object[] values, string where)
        {
            if (columns.Length != values.Length && columns.Length != types.Length)
            {
                throw new ArgumentException("指定されたカラムの数と値の数が一致しません");
            }

            var cmd = "";
            cmd = "update " + table + " set ";

            for (int i = 0; i < columns.Length - 1; i++)
            {
                cmd += columns[i] + "=@" + columns[i] + ",";
            }

            cmd += columns[columns.Length - 1] + "=@" + columns[columns.Length - 1];

            var paramList = new List<SQLiteParameter>();
            // パラメータを設定
            for (int i = 0; i < columns.Length; i++)
            {
                var param = new SQLiteParameter(columns[i], types[i])
                {
                    Value = values[i]
                };
                paramList.Add(param);
            }

            if (where != "")
            {
                cmd += " where " + where;
            }

            return new SQLiteLibCommandAddParam(cmd, paramList);
        }

        /// <summary>
        /// DbTypeを指定して複数カラムを対象にupdateを実行。whereなし
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <param name="types"></param>
        /// <param name="values"></param>
        public static SQLiteLibCommandAddParam SQL_Update(string table, string[] columns, DbType[] types, object[] values)
        {
            return SQL_Update(table, columns, types, values, "");
        }

        /// <summary>
        /// DbTypeを指定して1つのカラムを対象にupdateを実行。
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="where"></param>
        public static SQLiteLibCommandAddParam SQL_Update(string table, string column, DbType type, object value, string where)
        {
            return SQL_Update(table, new string[] { column }, new DbType[] { type }, new object[] { value }, where);
        }

        /// <summary>
        /// DbTypeを指定して1つのカラムを対象にupdateを実行。whereなし
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public static SQLiteLibCommandAddParam SQL_Update(string table, string column, DbType type, object value)
        {
            return SQL_Update(table, new string[] { column }, new DbType[] { type }, new object[] { value }, "");
        }

        /// <summary>
        /// updateを実行。指定した条件にあったものをすべて更新する
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <param name="types"></param>
        /// <param name="values"></param>
        public static SQLiteLibCommandAddParam SQL_Update(string table, string[] columns, string[] values, string where)
        {
            return SQL_Update(table, columns, CreateStringDbTypes(columns.Length), values, where);
        }

        /// <summary>
        /// updateを実行。テーブル内にある指定カラムのデータをすべて一括で書き換える
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <param name="values"></param>
        public static SQLiteLibCommandAddParam SQL_Update( string table, string[] columns, string[] values)
        {
            return SQL_Update(table, columns, values, "");
        }

        /// <summary>
        /// 単数のカラムを対象にupdateを実行。指定した条件にあったものを更新する。
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <param name="where"></param>
        public static SQLiteLibCommandAddParam SQL_Update(string table, string column, string value, string where)
        {
            return SQL_Update(table, new string[] { column }, new string[] { value }, where);
        }

        /// <summary>
        /// 単数のカラムを対象にupdateを実行。テーブル内にある指定カラムのデータをすべて一括で書き換える
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <param name="value"></param>
        public static SQLiteLibCommandAddParam SQL_Update(string table, string column, string value)
        {
            return SQL_Update(table, new string[] { column }, new string[] { value }, "");
        }

        /// <summary>
        /// 指定したテーブルの中身の全部消す
        /// </summary>
        /// <param name="table"></param>
        public static string SQL_Delete(string table)
        {
            return "DELETE FROM " + table;
        }

        /// <summary>
        /// 指定したテーブルの中身から条件に一致するものを消す
        /// </summary>
        /// <param name="table"></param>
        /// <param name="where"></param>
        public static string SQL_Delete(string table, string where)
        {
            return "DELETE FROM " + table + " where " + where;
        }

        public static string SQL_Vaccum(string table)
        {
            return "VACUUM";
        }

        /// <summary>
        /// where文に頻出するA="B"のようなものを簡単に書くためのメソッド
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CreateWhere(string column, string value)
        {
            return string.Format("{0}=\"{1}\"", column, value);
        }

        /// <summary>
        /// where文に頻出するA="B" AND のようなものを簡単に書くためのメソッド。続けて次の条件を書ける
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CreateWhereAnd(string column, string value)
        {
            return CreateWhere(column, value) + " AND ";
        }

        /// <summary>
        /// where文に頻出するA="B" OR のようなものを簡単に書くためのメソッド。続けて次の条件を書ける
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CreateWhereOr(string column, string value)
        {
            return CreateWhere(column, value) + " OR ";
        }
    }
}
