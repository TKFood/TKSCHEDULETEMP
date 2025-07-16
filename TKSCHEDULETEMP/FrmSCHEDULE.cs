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
        #endregion

        #region BUTTON

        private void button1_Click(object sender, EventArgs e)
        {
            DataTable dt = FIND_POSIP();
            if (dt != null)
            {
                MessageBox.Show(dt.Rows.Count.ToString());
            }
            else
            {
                MessageBox.Show("查無資料或查詢失敗");
            }
        }
        #endregion

      
    }
}
