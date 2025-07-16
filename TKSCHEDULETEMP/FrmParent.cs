using System;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TKITDLL;

namespace TKSCHEDULETEMP
{
    public partial class FrmParent : Form
    {
        SqlConnection conn;
        MenuStrip MnuStrip;
        ToolStripMenuItem MnuStripItem;
        string UserName;
        DataTable dt = new DataTable();

        public FrmParent()
        {
            InitializeComponent();
        }

        public FrmParent(string txt_UserName)
        {
            InitializeComponent();
            UserName = txt_UserName;
        }


        private void FrmParent_Load(object sender, EventArgs e)
        {
            // To make this Form the Parent Form
            this.IsMdiContainer = true;

            //Creating object of MenuStrip class
            MnuStrip = new MenuStrip();

            //Placing the control to the Form
            this.Controls.Add(MnuStrip);

            //String connectionString;
            //connectionString = ConfigurationManager.ConnectionStrings["dbconn"].ConnectionString;
            //conn = new SqlConnection(connectionString);

            //20210902密
            //解密連線資訊
            Class1 TKID = new Class1();
            SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKMK"].ConnectionString);
            sqlsb.Password = TKID.Decryption(sqlsb.Password);
            sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

            using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
            {
                string sql = "SELECT MAINMNU, MENUPARVAL, STATUS FROM MNU_PARENT";

                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {                  
                    conn.Open();
                    da.Fill(dt);
                    
                }
            }

            foreach (DataRow dr in dt.Rows)
            {
                MnuStripItem = new ToolStripMenuItem(dr["MAINMNU"].ToString());
                SubMenu(MnuStripItem, dr["MENUPARVAL"].ToString(), UserName);
                MnuStrip.Items.Add(MnuStripItem);
            }
            // The Form.MainMenuStrip property determines the merge target.
            this.MainMenuStrip = MnuStrip;
        }

        public void SubMenu(ToolStripMenuItem mnu, string submenuCode, string userName)
        {
            try
            {
                // 解密連線資訊
                Class1 TKID = new Class1();
                SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKMK"].ConnectionString);
                sqlsb.Password = TKID.Decryption(sqlsb.Password);
                sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

                using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                {
                    string sql = @"
                                SELECT M.FRM_NAME 
                                FROM MNU_SUBMENU M
                                JOIN MNU_SUBMENULogin ML ON M.FRM_CODE = ML.FRM_CODE
                                WHERE ML.UserName = @UserName AND M.MENUPARVAL = @MenuParVal";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@UserName", SqlDbType.NVarChar).Value = userName;
                        cmd.Parameters.Add("@MenuParVal", SqlDbType.NVarChar).Value = submenuCode;

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            foreach (DataRow dr in dt.Rows)
                            {
                                string frmName = dr["FRM_NAME"].ToString();
                                ToolStripMenuItem subItem = new ToolStripMenuItem(frmName, null, new EventHandler(ChildClick));
                                mnu.DropDownItems.Add(subItem);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("載入子選單失敗：" + ex.Message);
            }
        }

        private void ChildClick(object sender, EventArgs e)
        {
            // MessageBox.Show(string.Concat("You have Clicked ", sender.ToString(), " Menu"), "Menu Items Event",MessageBoxButtons.OK, MessageBoxIcon.Information);

            String Seqtx = "SELECT FRM_CODE FROM MNU_SUBMENU WHERE FRM_NAME='" + sender.ToString() + "'";
            SqlDataAdapter datransaction = new SqlDataAdapter(Seqtx, conn);
            DataTable dtransaction = new DataTable();
            datransaction.Fill(dtransaction);
            //ADD USED LOG
            List<string> IPAddress = GetHostIPAddress();
            //MessageBox.Show(IPAddress[0].ToString());            
            TKSYSPRUSED(MethodBase.GetCurrentMethod().DeclaringType.Namespace, dtransaction.Rows[0]["FRM_CODE"].ToString(), sender.ToString(), UserName, IPAddress[0].ToString());


            Assembly frmAssembly = Assembly.LoadFile(Application.ExecutablePath);
            foreach (Type type in frmAssembly.GetTypes())
            {
                //MessageBox.Show(type.Name);
                if (type.BaseType == typeof(Form))
                {
                    if (type.Name == dtransaction.Rows[0][0].ToString())
                    {
                        Form frmShow = (Form)frmAssembly.CreateInstance(type.ToString());
                        // then when you want to close all of them simple call the below code

                        foreach (Form form in this.MdiChildren)
                        {
                            //form.Close();
                            //如果子視窗已經存在
                            if (form.Name == frmShow.Name)
                            {
                                //將該子視窗設為焦點
                                form.Focus();
                                return;
                            }
                        }

                        frmShow.MdiParent = this;
                        frmShow.WindowState = FormWindowState.Maximized;
                        //frmShow.ControlBox = false;
                        frmShow.Show();
                    }
                }
            }
        }

        private void FrmParent_FormClosed(object sender, FormClosedEventArgs e)
        {

            //=====偵測執行中的外部程式並關閉=====
            Process[] MyProcess = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            if (MyProcess.Length > 0)
                MyProcess[0].Kill(); //關閉執行中的程式

        }

        public void TKSYSPRUSED(string SYSTEMNAME, string PROGRAMCODE, string PROGRAMNAME, string USEDID, string USEDIP)
        {
            try
            {
                // 解密連線字串
                Class1 TKID = new Class1();
                SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["dbTKMK"].ConnectionString);
                sqlsb.Password = TKID.Decryption(sqlsb.Password);
                sqlsb.UserID = TKID.Decryption(sqlsb.UserID);

                using (SqlConnection conn = new SqlConnection(sqlsb.ConnectionString))
                {
                    string sql = @"
                                INSERT INTO [TKIT].[dbo].[TKSYSPRUSED]
                                ([SYSTEMNAME], [PROGRAMCODE], [PROGRAMNAME], [USEDDATES], [USEDID], [USEDIP])
                                VALUES
                                (@SYSTEMNAME, @PROGRAMCODE, @PROGRAMNAME, @USEDDATES, @USEDID, @USEDIP)
            ";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@SYSTEMNAME", SqlDbType.NVarChar).Value = SYSTEMNAME;
                        cmd.Parameters.Add("@PROGRAMCODE", SqlDbType.NVarChar).Value = PROGRAMCODE;
                        cmd.Parameters.Add("@PROGRAMNAME", SqlDbType.NVarChar).Value = PROGRAMNAME;
                        cmd.Parameters.Add("@USEDDATES", SqlDbType.NVarChar).Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                        cmd.Parameters.Add("@USEDID", SqlDbType.NVarChar).Value = USEDID;
                        cmd.Parameters.Add("@USEDIP", SqlDbType.NVarChar).Value = USEDIP;

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("寫入程式使用記錄失敗：" + ex.Message);
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
    }
}
