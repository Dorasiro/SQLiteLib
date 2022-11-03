using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteLib
{
    public class Table_Sqlite_sequence
    {
        public static readonly string TABLE_NAME = "sqlite_sequence";

        public static readonly string COLUMN_NAME = "name";
        public static readonly string COLUMN_SEQ = "seq";

        private Connector connector;

        public Table_Sqlite_sequence(Connector connector)
        {
            this.connector = connector;
        }

        /// <summary>
        /// 指定したテーブルで一番最後に使用されたIDを返す
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public async Task<long> GetLastUsedID(string targetTable)
        {
            var result = await connector.ExecuteReaderAsync(SQLiteWrapper.SQL_SelectAny(TABLE_NAME, new string[] { COLUMN_SEQ }, string.Format("{0} = \"{1}\"", COLUMN_NAME, targetTable))).ConfigureAwait(false);
            try
            {
                return Convert.ToInt64(result[0][COLUMN_SEQ]);
            }
            catch (InvalidCastException)
            {
                return 0;
            }
        }

        /// <summary>
        /// 指定したテーブルが存在するかを調べる
        /// </summary>
        /// <param name="targetTable"></param>
        /// <returns></returns>
        public async Task<bool> IsExistTableAsync(string targetTable)
        {
            try
            {
                var result = await connector.ExecuteScalarAsync(SQLiteWrapper.SQL_Count(TABLE_NAME, string.Format("{0} = \"{1}\"", COLUMN_NAME, targetTable))).ConfigureAwait(false);
                if ((int)result == 0)
                {
                    return false;
                }

                return true;
            }
            catch(SQLiteException)
            {
                return false;
            }
        }

        /// <summary>
        /// 指定したテーブルが存在するかを調べる
        /// </summary>
        /// <param name="targetTable"></param>
        /// <returns></returns>
        public bool IsExistTable(string targetTable)
        {
            try
            {
                var result = connector.ExecuteScalar(SQLiteWrapper.SQL_Count(TABLE_NAME, string.Format("{0} = \"{1}\"", COLUMN_NAME, targetTable)));
                if ((long)result == 0)
                {
                    return false;
                }

                return true;
            }
            catch(SQLiteException)
            {
                return false;
            }
        }

        /// <summary>
        /// 指定したテーブルの自動割り当てIDを0に戻す
        /// </summary>
        /// <param name="targetTable"></param>
        public async Task TableReCreate(string targetTable)
        {
            await connector.ExecuteNonQueryAsync(SQLiteWrapper.SQL_Update(TABLE_NAME, COLUMN_SEQ, "0")).ConfigureAwait(false);
        }

        /// <summary>
        /// テーブル新規作成時にそのテーブルを追加する
        /// </summary>
        /// <param name="tableName"></param>
        public void CreateTable(string tableName)
        {
            connector.ExecuteNonQuery(SQLiteWrapper.SQL_Insert(TABLE_NAME, new string[] { COLUMN_NAME, COLUMN_SEQ }, new string[] { tableName, "0" }));
        }
    }
}
