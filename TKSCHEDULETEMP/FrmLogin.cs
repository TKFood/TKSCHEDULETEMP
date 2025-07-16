using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Configuration;
using System.Reflection;
using TKITDLL;
using System.Net;
using System.Net.Sockets;

namespace TKSCHEDULETEMP
{
    public partial class FrmLogin : Form
    {
        public FrmLogin()
        {
            InitializeComponent();
        }

        #region BUTTON
        private void button1_Click(object sender, EventArgs e)
        {
            LOGIN();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
                return true;

            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion

        #region LOGIN
        public void LOGIN()
        {
            if (string.IsNullOrWhiteSpace(txt_UserName.Text) || string.IsNullOrWhiteSpace(txt_Password.Text))
            {
                MessageBox.Show("請輸入帳號、密碼");
                return;
            }

            try
            {
                // 解密連線字串
                Class1 TKID = new Class1();
                SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKMK"].ConnectionString);
                sqlsb.Password = TKID.Decryption(sqlsb.Password);
                sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

                using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                {
                    string sql = @"SELECT COUNT(*) FROM MNU_Login WHERE UserName=@username AND Password=@password";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@username", SqlDbType.NVarChar).Value = txt_UserName.Text.Trim();
                        cmd.Parameters.Add("@password", SqlDbType.NVarChar).Value = txt_Password.Text.Trim();

                        conn.Open();
                        int count = (int)cmd.ExecuteScalar();

                        List<string> IPAddress = GetHostIPAddress();
                        string result = (count == 1) ? "SUCCESS" : "FAIL";

                        ADDTKSYSLOGIN(MethodBase.GetCurrentMethod().DeclaringType.Namespace, txt_UserName.Text.Trim(), IPAddress[0], result);

                        if (count == 1)
                        {
                            FrmParent fm = new FrmParent(txt_UserName.Text.Trim());
                            fm.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("登入失敗!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("錯誤：" + ex.Message);
            }
        }

        public void ADDTKSYSLOGIN(string SYSTEMNAME, string USEDID, string USEDIP, string LOGIN)
        {
            SqlConnection sqlConn = new SqlConnection();
            SqlTransaction tran;
            SqlCommand cmd = new SqlCommand();
            int result;
            StringBuilder sbSql = new StringBuilder();


            //20210902密
            Class1 TKID = new Class1();//用new 建立類別實體
            SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKMK"].ConnectionString);

            //資料庫使用者密碼解密
            sqlsb.Password = TKID.Decryption(sqlsb.Password);
            sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

            String connectionString;
            sqlConn = new SqlConnection(sqlsb.ConnectionString);


            sqlConn.Close();
            sqlConn.Open();
            tran = sqlConn.BeginTransaction();

            sbSql.Clear();



            sbSql.AppendFormat(@" 
                                INSERT INTO [TKIT].[dbo].[TKSYSLOGIN]
                                ([SYSTEMNAME],[USEDDATES],[USEDID],[USEDIP],[LOGIN])
                                VALUES
                                (@SYSTEMNAME,@USEDDATES,@USEDID,@USEDIP,@LOGIN)
                                ");


            using (SqlConnection connection = new SqlConnection(sqlsb.ConnectionString))
            {
                SqlCommand command = new SqlCommand(sbSql.ToString(), connection);
                command.Parameters.AddWithValue("@SYSTEMNAME", SYSTEMNAME);
                command.Parameters.AddWithValue("@USEDDATES", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                command.Parameters.AddWithValue("@USEDID", USEDID);
                command.Parameters.AddWithValue("@USEDIP", USEDIP);
                command.Parameters.AddWithValue("@LOGIN", LOGIN);
                try
                {
                    connection.Open();
                    Int32 rowsAffected = command.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                finally
                {
                    sqlConn.Close();
                }
            }


        }

        // <summary>
        /// 取得本機 IP Address
        /// </summary>
        /// <returns></returns>
        private List<string> GetHostIPAddress()
        {
            List<string> lstIPAddress = new List<string>();
            IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ipa in IpEntry.AddressList)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    lstIPAddress.Add(ipa.ToString());
                    //MessageBox.Show(ipa.ToString());
                }

            }
            return lstIPAddress; // result: 192.168.1.17 ......
        }


        #endregion

        #region FUNCTION
        private void txt_Password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                LOGIN();
            }
        }

        #endregion
    }
}
