using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
namespace Mercader
{
    internal class DataRepository
    {
        private SQLiteConnection conn;
        string dbpath;

        public DataRepository(string dbpath)

        {
            dbpath = dbpath;
        }

        private void Init()

        {
            if (conn != null)
                return;
            
            conn = new SQLiteConnection(dbpath);
            conn.CreateTable<Person>();
            
            
        }

    }
}
