using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Data;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Specialized;

namespace TataMySqlConnection
{
    class MsqlConnection : IDisposable
    {
        static String ServerName = "10.20.10.61";//"localhost";
        static String username = "root";
        static String password = "srks4$";
        static String port = "3306";
        static String DB = "mazakdaqhistory";

        public MySqlConnection msqlConnection = new MySqlConnection("server = " + ServerName + ";userid = " + username + ";Password = " + password + ";database = " + DB + ";port = " + port + ";persist security info=False");

        public void open()
        {
            if (msqlConnection.State != System.Data.ConnectionState.Open)
                msqlConnection.Open();
        }

        public void close()
        {
            msqlConnection.Close();
        }

        void IDisposable.Dispose()
        { }

    }
}
