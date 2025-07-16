using System;
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
        public FrmSCHEDULE()
        {
            InitializeComponent();
        }

        #region FUNCTION
        private void FrmSCHEDULE_Load(object sender, EventArgs e)
        {

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
        /// 新增pos資料，到TKPOSTEMP
        /// </summary>
        /// <param name="DT"></param>
        public void ADD_TKPOSTEMP(DataTable DT)
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

                using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                {
                    SQL.AppendFormat(@"
                                    SELECT *
                                    FROM [192.168.1.131].[COSMOS_POS].dbo.POSTB WITH(NOLOCK)
                                    WHERE TB001 + TB002 + TB003 + TB006 + TB007 NOT IN 
                                    (
                                        SELECT TB001 + TB002 + TB003 + TB006 + TB007 
                                        FROM [TKPOSTEMP].[dbo].[POSTB]
                                    )
                                    AND TB001 >= '20250715'
                                        ");

                    using (SqlCommand cmd = new SqlCommand(SQL.ToString(), conn))
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            // Step 3: SqlBulkCopy 進行整批寫入
                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                            {
                                bulkCopy.DestinationTableName = "[TKPOSTEMP].[dbo].[POSTB]";
                                bulkCopy.BatchSize = 1000; // 每次一千筆
                                bulkCopy.BulkCopyTimeout = 60;

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
                            MessageBox.Show("無需寫入，無新資料。");
                        }
                    }
                }
            }
        }

        #endregion

        #region BUTTON

        private void button1_Click(object sender, EventArgs e)
        {
            DataTable DT = FIND_POSIP();
            if (DT != null)
            {               
                ADD_TKPOSTEMP(DT);
            }
            else
            {
                MessageBox.Show("查無資料或查詢失敗");
            }
        }
        #endregion

      
    }
}
