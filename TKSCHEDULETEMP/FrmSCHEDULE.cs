﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Linq;
using TKITDLL;
using System.Text.RegularExpressions;


namespace TKSCHEDULETEMP
{
    public partial class FrmSCHEDULE : Form
    {
        int CommandTimeout = 60;
        int BulkCopyTimeout = 60;

        public FrmSCHEDULE()
        {
            InitializeComponent();
        }

        #region FUNCTION
        private void FrmSCHEDULE_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            timer1.Interval = 1000 * 60; // 1 分鐘
            timer1.Start();
        }

        /// <summary>
        /// 定期執行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            //每分鐘執行1次

            //轉入POS資料到TKPOSTEMP
            try
            {
                DataTable DT = FIND_POSIP();
                if (DT != null)
                {
                    ADD_TKPOSTEMP_POSTA(DT);
                    ADD_TKPOSTEMP_POSTB(DT);
                    ADD_TKPOSTEMP_POSTC(DT);

                    //MessageBox.Show("完成");
                }
            }
            catch { }
        }

        public DataTable FIND_POSIP()
        {
            StringBuilder SQL = new StringBuilder();
            try
            {
                // 解密連線字串
                Class1 TKID = new Class1();
                SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKSCHEDULETEMP"].ConnectionString);
                sqlsb.Password = TKID.Decryption(sqlsb.Password);
                sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

                using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                {
                    SQL.AppendFormat(@"
                                     SELECT 
                                     [IP]
                                    ,[DBNAME]
                                    FROM [TKSCHEDULETEMP].[dbo].[POSIP]
                                    ");
               
                    using (SqlCommand cmd = new SqlCommand(SQL.ToString(), conn))
                    {
                        //cmd.Parameters.Add("@ACCOUNT", SqlDbType.NVarChar).Value = account;

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            return dt.Rows.Count > 0 ? dt : null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("查詢失敗：" + ex.Message);
                return null;
            }
        }
        /// <summary>
        /// 新增POSTA資料，到TKPOSTEMP
        /// </summary>
        /// <param name="DT"></param>
        public void ADD_TKPOSTEMP_POSTA(DataTable DT)
        {
            StringBuilder SQL = new StringBuilder();

            foreach (DataRow DR in DT.Rows)
            {
                string IP = DR["IP"].ToString();
                string DBNAME = DR["DBNAME"].ToString();

                // 解密連線字串
                Class1 TKID = new Class1();
                SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKSCHEDULETEMP"].ConnectionString);
                sqlsb.Password = TKID.Decryption(sqlsb.Password);
                sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

                SQL.Clear();

                try
                {
                    if (IsLinkedServerAlive(IP, DBNAME))
                    {
                        using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                        {
                            SQL.AppendFormat(@"
                                    SELECT *
                                    FROM [{0}].[{1}].dbo.POSTA WITH(NOLOCK)
                                    WHERE TA001 + TA002 + TA003 + TA006  NOT IN 
                                    (
                                        SELECT TA001 + TA002 + TA003 + TA006
                                        FROM [TKPOSTEMP].[dbo].[POSTA] WITH(NOLOCK)
                                    )
                                    AND TA001 >= '20250716'
                                        ", IP, DBNAME);

                            using (SqlCommand cmd = new SqlCommand(SQL.ToString(), conn))
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                cmd.CommandTimeout = CommandTimeout; // 查詢 timeout
                                DataTable dt = new DataTable();
                                da.Fill(dt);

                                if (dt.Rows.Count > 0)
                                {
                                    // Step 3: SqlBulkCopy 進行整批寫入
                                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                                    {
                                        bulkCopy.DestinationTableName = "[TKPOSTEMP].[dbo].[POSTA]";
                                        bulkCopy.BatchSize = 1000; // 每次一千筆
                                        bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                                        // 自動對應欄位（欄位名稱相同會自動對應）
                                        foreach (DataColumn col in dt.Columns)
                                        {
                                            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                        }

                                        conn.Open();
                                        bulkCopy.WriteToServer(dt);
                                        conn.Close();

                                        //MessageBox.Show($"成功寫入 {dt.Rows.Count} 筆資料！");
                                    }
                                }
                                else
                                {
                                    //MessageBox.Show("無需寫入，無新資料。");
                                }
                            }
                        }
                    }
                }
                catch (Exception EX)
                {
                    MessageBox.Show(EX.ToString());
                }
                finally
                { }
            }
        }

        /// <summary>
        /// 新增POSTB資料，到TKPOSTEMP
        /// </summary>
        /// <param name="DT"></param>
        public void ADD_TKPOSTEMP_POSTB(DataTable DT)
        {
            StringBuilder SQL = new StringBuilder();

            foreach (DataRow DR in DT.Rows)
            {
                string IP = DR["IP"].ToString();
                string DBNAME = DR["DBNAME"].ToString();

                // 解密連線字串
                Class1 TKID = new Class1();
                SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKSCHEDULETEMP"].ConnectionString);
                sqlsb.Password = TKID.Decryption(sqlsb.Password);
                sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

                SQL.Clear();

                try
                {
                    if (IsLinkedServerAlive(IP, DBNAME))
                    {
                        using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                        {
                            SQL.AppendFormat(@"
                                    SELECT *
                                    FROM [{0}].[{1}].dbo.POSTB WITH(NOLOCK)
                                    WHERE TB001 + TB002 + TB003 + TB006 + TB007 NOT IN 
                                    (
                                        SELECT TB001 + TB002 + TB003 + TB006 + TB007 
                                        FROM [TKPOSTEMP].[dbo].[POSTB] WITH(NOLOCK)
                                    )
                                    AND TB001 >= '20250716'
                                        ", IP, DBNAME);

                            using (SqlCommand cmd = new SqlCommand(SQL.ToString(), conn))
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                cmd.CommandTimeout = CommandTimeout; // 查詢 timeout
                                DataTable dt = new DataTable();
                                da.Fill(dt);

                                if (dt.Rows.Count > 0)
                                {
                                    // Step 3: SqlBulkCopy 進行整批寫入
                                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                                    {
                                        bulkCopy.DestinationTableName = "[TKPOSTEMP].[dbo].[POSTB]";
                                        bulkCopy.BatchSize = 1000; // 每次一千筆
                                        bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                                        // 自動對應欄位（欄位名稱相同會自動對應）
                                        foreach (DataColumn col in dt.Columns)
                                        {
                                            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                        }

                                        conn.Open();
                                        bulkCopy.WriteToServer(dt);
                                        conn.Close();

                                        //MessageBox.Show($"成功寫入 {dt.Rows.Count} 筆資料！");
                                    }
                                }
                                else
                                {
                                    //MessageBox.Show("無需寫入，無新資料。");
                                }
                            }
                        }
                    }
                }
                catch (Exception EX)
                {
                    MessageBox.Show(EX.ToString());
                }
                finally
                { }                      
            }
        }

        /// <summary>
        /// 新增POSTC資料，到TKPOSTEMP
        /// </summary>
        /// <param name="DT"></param>
        public void ADD_TKPOSTEMP_POSTC(DataTable DT)
        {
            StringBuilder SQL = new StringBuilder();

            foreach (DataRow DR in DT.Rows)
            {
                string IP = DR["IP"].ToString();
                string DBNAME = DR["DBNAME"].ToString();

                // 解密連線字串
                Class1 TKID = new Class1();
                SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKSCHEDULETEMP"].ConnectionString);
                sqlsb.Password = TKID.Decryption(sqlsb.Password);
                sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

                SQL.Clear();
                try
                {
                    if (IsLinkedServerAlive(IP, DBNAME))
                    {
                        using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                        {
                            SQL.AppendFormat(@"
                                    SELECT *
                                    FROM [{0}].[{1}].dbo.POSTC WITH(NOLOCK)
                                    WHERE TC001 + TC002 + TC003 + TC006  NOT IN 
                                    (
                                        SELECT TC001 + TC002 + TC003 + TC006
                                        FROM [TKPOSTEMP].[dbo].[POSTC] WITH(NOLOCK)
                                    )
                                    AND TC001 >= '20250716'
                                        ", IP, DBNAME);

                            using (SqlCommand cmd = new SqlCommand(SQL.ToString(), conn))
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                cmd.CommandTimeout = CommandTimeout; // 查詢 timeout
                                DataTable dt = new DataTable();
                                da.Fill(dt);

                                if (dt.Rows.Count > 0)
                                {
                                    // Step 3: SqlBulkCopy 進行整批寫入
                                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                                    {
                                        bulkCopy.DestinationTableName = "[TKPOSTEMP].[dbo].[POSTC]";
                                        bulkCopy.BatchSize = 1000; // 每次一千筆
                                        bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                                        // 自動對應欄位（欄位名稱相同會自動對應）
                                        foreach (DataColumn col in dt.Columns)
                                        {
                                            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                        }

                                        conn.Open();
                                        bulkCopy.WriteToServer(dt);
                                        conn.Close();

                                        //MessageBox.Show($"成功寫入 {dt.Rows.Count} 筆資料！");
                                    }
                                }
                                else
                                {
                                    //MessageBox.Show("無需寫入，無新資料。");
                                }
                            }
                        }
                    }
                }
                catch (Exception EX)
                {
                    MessageBox.Show(EX.ToString());
                }
                finally
                { }
            }
        }

        //測試linkedserver是否連線
        public bool IsLinkedServerAlive(string linkedServer, string dbName)
        {
            string testSql = $@"SELECT TOP 1 1 FROM [{linkedServer}].[{dbName}].dbo.POSTB WITH(NOLOCK)";

            try
            {
                // 使用主機本機 SQL Server 連線
                Class1 TKID = new Class1();
                SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKPOSTEMP"].ConnectionString);
                sqlsb.Password = TKID.Decryption(sqlsb.Password);
                sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

                using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(testSql, conn))
                {
                    cmd.CommandTimeout = 3; // 最多等 3 秒
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region BUTTON

        private void button1_Click(object sender, EventArgs e)
        {
            DataTable DT = FIND_POSIP();
            if (DT != null)
            {
                ADD_TKPOSTEMP_POSTA(DT);
                ADD_TKPOSTEMP_POSTB(DT);
                ADD_TKPOSTEMP_POSTC(DT);

                MessageBox.Show("完成");
            }
            else
            {
                MessageBox.Show("查無資料或查詢失敗");
            }
        }

        #endregion

     
    }
}
