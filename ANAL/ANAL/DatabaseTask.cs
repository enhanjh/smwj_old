using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;

namespace SMWJ
{
    public class DatabaseTask
    {
        private string strConn = "Server=10.0.1.7;Database=SMWJ;Uid=smwjwas;Pwd=qwer0802;Charset=utf8";

        public int InsertQuery(string query)
        {
            int rslt = 0;
            
            using (MySqlConnection conn = new MySqlConnection(strConn))
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(query, conn);

                rslt = cmd.ExecuteNonQuery();
            }

            return rslt;
        }

        
        public ArrayList SelectQuery(string query)
        {
            ArrayList rslt = new ArrayList();

            MySqlCommand cmd = null;
            MySqlDataReader rdr = null;
            
            using (MySqlConnection conn = new MySqlConnection(strConn))
            {
                conn.Open();

                cmd = new MySqlCommand(query, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Dictionary<string, string> temp = new Dictionary<string, string>();

                    for (int i = 0; i < rdr.FieldCount; i++)
                    {
                        temp.Add(rdr.GetName(i), rdr.GetString(i));
                        Debug.WriteLine("{0} : {1}", rdr.GetName(i), rdr.GetString(i));
                    }

                    rslt.Add(temp);
                }

                if (rdr != null)
                {
                    rdr.Close();
                }
            }

            return rslt;
        }
    }
}
