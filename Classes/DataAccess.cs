using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
//using ABI.Windows.UI;

namespace CryptoPortfolioTracker
{
    public static class DataAccess
    {

        public async static void InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync("sqliteHotspots.db", CreationCollisionOption.OpenIfExists);
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "sqliteHotspots.db");
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                string sqLiteCommand = "CREATE TABLE IF NOT EXISTS Hotspot_Table (priority INTEGER, ssid NVARCHAR(2048), bssid NVARCHAR(2048), pw NVARCHAR(2048) NULL)";

                var createTable = new SqliteCommand(sqLiteCommand, db);

                createTable.ExecuteReader();
                //db.Open();

                sqLiteCommand = "CREATE TABLE IF NOT EXISTS PW_Table (bssid NVARCHAR(2048), pw NVARCHAR(2048) NULL)";

                createTable = new SqliteCommand(sqLiteCommand, db);

                createTable.ExecuteReader();

                sqLiteCommand = "CREATE TABLE IF NOT EXISTS Results_Table (date INTEGER, pingroundtime REAL)";

                createTable = new SqliteCommand(sqLiteCommand, db);

                createTable.ExecuteReader();


            }
        }   
    }
}
