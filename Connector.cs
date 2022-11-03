using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteLib
{
    public class Connector
    {
        /// <summary>
        /// SQLiteのファイル名
        /// </summary>
        private readonly string FileName;
        /// <summary>
        /// SQLiteのDB名
        /// </summary>
        private readonly string DbName;

        /// <summary>
        /// ログを記録するかどうか
        /// </summary>
        private readonly bool isLogging;

        public string crashReportDirectory;

        private SQLiteConnectionStringBuilder builder;

        /// <summary>
        /// ファイル名とDB名を指定するコンストラクタ
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dbName"></param>
        public Connector(string fileName, string dbName, bool isLogging, string crashReportDirectory)
        {
            this.FileName = fileName;
            this.DbName = dbName;
            this.isLogging = isLogging;
            this.crashReportDirectory = crashReportDirectory;

            builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = FileName;
            builder.BusyTimeout = 30;
            builder.DefaultTimeout = 60;
            builder.SyncMode = SynchronizationModes.Normal;
            builder.JournalMode = SQLiteJournalModeEnum.Wal;
            //b.DefaultIsolationLevel = System.Data.IsolationLevel.ReadCommitted;
        }

        /// <summary>
        /// コンストラクタで作成したビルダー情報からDBに接続する
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection ConnectToSQLite()
        {
            return new SQLiteConnection(builder.ConnectionString);
        }

        /// <summary>
        /// ExecuteNonQueryを実行する
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public void ExecuteNonQuery(string sqlCommand)
        {
            using (var con = ConnectToSQLite())
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch(SQLiteException)
                    {
                        Debug.WriteLine(sqlCommand);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// [非同期]ExecuteNonQueryを実行する
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task ExecuteNonQueryAsync(string sqlCommand)
        {
            sqlCommand = ReplaceDoubleQuote(sqlCommand);

            using (var con = ConnectToSQLite())
            {
                await con.OpenAsync().ConfigureAwait(false);

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;

                    await Task.Run(() =>
                    {
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (SQLiteException)
                        {
                            Debug.WriteLine(sqlCommand);
                            throw;
                        }
                    });
                }
            }
        }

        /// <summary>
        /// ExecuteNonQueryを実行する
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public void ExecuteNonQuery(SQLiteLibCommandAddParam cmdParam)
        {
            using (var con = ConnectToSQLite())
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = cmdParam.cmd;
                    try
                    {
                        foreach (var p in cmdParam.param)
                        {
                            cmd.Parameters.Add(p);
                        }

                        cmd.ExecuteNonQuery();
                    }
                    catch (SQLiteException)
                    {
                        Debug.WriteLine(cmdParam.cmd);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// [非同期]ExecuteNonQueryを実行する
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task ExecuteNonQueryAsync(SQLiteLibCommandAddParam cmdParam)
        {
            using (var con = ConnectToSQLite())
            {
                await con.OpenAsync().ConfigureAwait(false);

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = cmdParam.cmd;

                    await Task.Run(() =>
                    {
                        try
                        {
                            foreach(var p in cmdParam.param)
                            {
                                cmd.Parameters.Add(p);
                            }

                            cmd.ExecuteNonQuery();
                        }
                        catch (SQLiteException)
                        {
                            Debug.WriteLine(cmdParam.cmd);
                            throw;
                        }
                    });
                }
            }
        }

        /// <summary>
        /// ExecuteReaderを実行する
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public List<Dictionary<string, object>> ExecuteReader(string sqlCommand)
        {
            sqlCommand = ReplaceDoubleQuote(sqlCommand);

            var resultList = new List<Dictionary<string, object>>();

            using (var con = ConnectToSQLite())
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    try
                    {
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            var dict = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                dict[reader.GetName(i)] = reader[i];
                            }

                            resultList.Add(dict);
                        }

                        return resultList;
                    }
                    catch (SQLiteException)
                    {
                        Debug.WriteLine(sqlCommand);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// [非同期]ExecuteReaderを実行する
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task<List<Dictionary<string, object>>> ExecuteReaderAsync(string sqlCommand)
        {
            sqlCommand = ReplaceDoubleQuote(sqlCommand);

            var resultList = new List<Dictionary<string, object>>();

            using (var con = ConnectToSQLite())
            {
                await con.OpenAsync().ConfigureAwait(false);

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    await Task.Run(async () =>
                    {
                        try
                        {
                            var reader = cmd.ExecuteReader();

                            while (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                var dict = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    dict[reader.GetName(i)] = reader[i];
                                }

                                resultList.Add(dict);
                            }
                        }
                        catch (SQLiteException)
                        {
                            Debug.WriteLine(sqlCommand);
                            throw;
                        }
                    }).ConfigureAwait(false);
                    return resultList;
                }
            }
        }

        /// <summary>
        /// ExecuteScalarを実行する
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public object ExecuteScalar(string sqlCommand)
        {
            sqlCommand = ReplaceDoubleQuote(sqlCommand);

            using (var con = ConnectToSQLite())
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    try
                    {
                        return cmd.ExecuteScalar();
                    }
                    catch (SQLiteException)
                    {
                        Debug.WriteLine(sqlCommand);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// [非同期]ExecuteScalarを実行する
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(string sqlCommand)
        {
            sqlCommand = ReplaceDoubleQuote(sqlCommand);

            using (var con = ConnectToSQLite())
            {
                await con.OpenAsync().ConfigureAwait(false);

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;

                    return await Task.Run<object>(() =>
                    {
                        try
                        {
                            return cmd.ExecuteScalar();
                        }
                        catch(SQLiteException)
                        {
                            throw;
                        }
                    });
                }
            }
        }

        /// <summary>
        /// [非同期]引数で受け取ったstring配列から複数のsql文をExecuteNonQueryで実行する
        /// </summary>
        /// <param name="sqlCommandArray"></param>
        /// <returns></returns>
        public async Task BeginTransactionExecuteNoQuaryAsync(string[] sqlCommandArray)
        {
            for(int i=0;i<sqlCommandArray.Length;i++)
            {
                sqlCommandArray[i] = ReplaceDoubleQuote(sqlCommandArray[i]);
            }

            using (var con = ConnectToSQLite())
            {
                await con.OpenAsync().ConfigureAwait(false);

                await Task.Run(() =>
                {
                    using (var cmd = con.CreateCommand())
                    {
                        var trans = con.BeginTransaction();
                        try
                        {
                            foreach (var sqlCommand in sqlCommandArray)
                            {
                                cmd.CommandText = sqlCommand;
                                cmd.ExecuteNonQuery();
                            }

                            trans.Commit();
                        }
                        catch (Exception e)
                        {
                            foreach(var s in sqlCommandArray)
                            {
                                Debug.WriteLine(s + Environment.NewLine);
                            }
                            trans.Rollback();
                            OutputCrashReport(e);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// [非同期]引数で受け取ったリストから複数のsql文をExecuteNonQueryで実行する
        /// </summary>
        /// <param name="sqlCommandArray"></param>
        /// <returns></returns>
        public async Task BeginTransactionExecuteNoQuaryAsync(List<string> sqlCommandArray)
        {
            for (int i = 0; i < sqlCommandArray.Count; i++)
            {
                sqlCommandArray[i] = ReplaceDoubleQuote(sqlCommandArray[i]);
            }

            using (var con = ConnectToSQLite())
            {
                await con.OpenAsync().ConfigureAwait(false);

                await Task.Run(() =>
                {
                    using (var cmd = con.CreateCommand())
                    {
                        var trans = con.BeginTransaction();
                        try
                        {
                            foreach (var sqlCommand in sqlCommandArray)
                            {
                                cmd.CommandText = sqlCommand;
                                cmd.ExecuteNonQuery();
                            }

                            trans.Commit();
                        }
                        catch (Exception e)
                        {
                            foreach (var s in sqlCommandArray)
                            {
                                Debug.WriteLine(s + Environment.NewLine);
                            }
                            trans.Rollback();
                            OutputCrashReport(e);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 引数で受け取ったstring配列から複数のsql文をExecuteNonQueryで実行する
        /// </summary>
        /// <param name="sqlCommandArray"></param>
        /// <returns></returns>
        public void BeginTransactionExecuteNoQuary(string[] sqlCommandArray)
        {
            for (int i = 0; i < sqlCommandArray.Length; i++)
            {
                sqlCommandArray[i] = ReplaceDoubleQuote(sqlCommandArray[i]);
            }

            using (var con = ConnectToSQLite())
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    var trans = con.BeginTransaction();
                    try
                    {
                        foreach (var sqlCommand in sqlCommandArray)
                        {
                            cmd.CommandText = sqlCommand;
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        foreach (var s in sqlCommandArray)
                        {
                            Debug.WriteLine(s + Environment.NewLine);
                        }
                        trans.Rollback();
                        OutputCrashReport(e);
                    }
                }
            }
        }

        /// <summary>
        /// 引数で受け取ったリストから複数のsql文をExecuteNonQueryで実行する
        /// </summary>
        /// <param name="sqlCommandArray"></param>
        /// <returns></returns>
        public void BeginTransactionExecuteNoQuary(List<string> sqlCommandArray)
        {
            for (int i = 0; i < sqlCommandArray.Count; i++)
            {
                sqlCommandArray[i] = ReplaceDoubleQuote(sqlCommandArray[i]);
            }

            using (var con = ConnectToSQLite())
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    var trans = con.BeginTransaction();
                    try
                    {
                        foreach (var sqlCommand in sqlCommandArray)
                        {
                            cmd.CommandText = sqlCommand;
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        foreach (var s in sqlCommandArray)
                        {
                            Debug.WriteLine(s + Environment.NewLine);
                        }
                        trans.Rollback();
                        OutputCrashReport(e);
                    }
                }
            }
        }

        /// <summary>
        /// [非同期]引数で受け取ったIEnumerableなリストから複数のsql文をExecuteNonQueryで実行する
        /// </summary>
        /// <param name="sqlCommandArray"></param>
        /// <returns></returns>
        public async Task BeginTransactionExecuteNoQuaryAsync(List<SQLiteLibCommandAddParam> sqlCommandAddParam)
        {
            using (var con = ConnectToSQLite())
            {
                await con.OpenAsync().ConfigureAwait(false);

                await Task.Run(() =>
                {
                    using (var cmd = con.CreateCommand())
                    {
                        try
                        {
                            var trans = con.BeginTransaction();
                            try
                            {
                                foreach (var sqlCommand in sqlCommandAddParam)
                                {
                                    foreach (var p in sqlCommand.param)
                                    {
                                        cmd.Parameters.Add(p);
                                    }

                                    cmd.CommandText = sqlCommand.cmd;
                                    cmd.ExecuteNonQuery();
                                }

                                trans.Commit();
                            }
                            catch (Exception e)
                            {
                                foreach (var s in sqlCommandAddParam)
                                {
                                    Debug.WriteLine(s + Environment.NewLine);
                                }
                                trans.Rollback();
                                OutputCrashReport(e);
                            }
                        }
                        catch (SQLiteException e)
                        {
                            // ErrorCode(5)はdatabase is locked
                            if (e.ErrorCode != 5)
                            {
                                // 別のエラーの時は投げる
                                throw e;
                            }
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 引数で受け取ったstring配列から複数のsql文をExecuteNonQueryで実行する
        /// </summary>
        /// <param name="sqlCommandArray"></param>
        /// <returns></returns>
        public void BeginTransactionExecuteNoQuary(List<SQLiteLibCommandAddParam> sqlCommandAddParam)
        {
            using (var con = ConnectToSQLite())
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    var trans = con.BeginTransaction();
                    try
                    {
                        foreach (var sqlCommand in sqlCommandAddParam)
                        {
                            foreach (var p in sqlCommand.param)
                            {
                                cmd.Parameters.Add(p);
                            }

                            cmd.CommandText = sqlCommand.cmd;
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        foreach (var s in sqlCommandAddParam)
                        {
                            Debug.WriteLine(s.cmd + Environment.NewLine);
                        }
                        trans.Rollback();
                        OutputCrashReport(e);
                    }
                }
            }
        }

        /// <summary>
        /// [非同期]引数で受け取ったIEnumerableなリストから複数のsql文をExecuteNonQueryで実行する
        /// </summary>
        /// <param name="sqlCommandArray"></param>
        /// <returns></returns>
        public async Task BeginTransactionExecuteNoQuaryAsync(SQLiteLibCommandAddParam[] sqlCommandAddParam)
        {
            using (var con = ConnectToSQLite())
            {
                await con.OpenAsync().ConfigureAwait(false);

                await Task.Run(() =>
                {
                    using (var cmd = con.CreateCommand())
                    {
                        using (var trans = con.BeginTransaction())
                        {
                            try
                            {
                                foreach (var sqlCommand in sqlCommandAddParam)
                                {
                                    foreach (var p in sqlCommand.param)
                                    {
                                        cmd.Parameters.Add(p);
                                    }

                                    cmd.CommandText = sqlCommand.cmd;
                                    cmd.ExecuteNonQuery();
                                }

                                trans.Commit();
                            }
                            catch (Exception e)
                            {
                                foreach (var s in sqlCommandAddParam)
                                {
                                    Debug.WriteLine(s + Environment.NewLine);
                                }
                                trans.Rollback();
                                OutputCrashReport(e);
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 引数で受け取ったstring配列から複数のsql文をExecuteNonQueryで実行する
        /// </summary>
        /// <param name="sqlCommandArray"></param>
        /// <returns></returns>
        public void BeginTransactionExecuteNoQuary(SQLiteLibCommandAddParam[] sqlCommandAddParam)
        {
            using (var con = ConnectToSQLite())
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    var trans = con.BeginTransaction();
                    try
                    {
                        foreach (var sqlCommand in sqlCommandAddParam)
                        {
                            foreach (var p in sqlCommand.param)
                            {
                                cmd.Parameters.Add(p);
                            }

                            cmd.CommandText = sqlCommand.cmd;
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        foreach (var s in sqlCommandAddParam)
                        {
                            Debug.WriteLine(s.cmd + Environment.NewLine);
                        }
                        trans.Rollback();
                        OutputCrashReport(e);
                    }
                }
            }
        }

        /// <summary>
        /// 引数に指定したExceptionのクラッシュレポートを生成する
        /// </summary>
        /// <param name="e"></param>
        public void OutputCrashReport(Exception e)
        {
            var nowDate = DateTime.Now;
            var path = Path.Combine(crashReportDirectory, "CrashReport-" + nowDate.Year.ToString() + nowDate.Month.ToString() + nowDate.Day.ToString() + nowDate.Hour.ToString() +
               nowDate.Minute.ToString() + nowDate.Second.ToString() + nowDate.Millisecond.ToString() + "_" + e.GetHashCode().ToString() + ".txt");

            using (var sw = new StreamWriter(path))
            {
                sw.WriteLine("[Message]" + Environment.NewLine + e.Message);
                sw.WriteLine("[Source]" + Environment.NewLine + e.Source);
                sw.WriteLine("[Stacktrace]" + Environment.NewLine + e.StackTrace);
            }
        }

        /// <summary>
        /// SQLコマンドの中に"が含まれていた場合'に置き換える
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public string ReplaceDoubleQuote(string sqlCommand)
        {
            return sqlCommand.Replace("\"", "'");
        }
    }
}
