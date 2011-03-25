using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shazam;

namespace Test
{
    class QueryTest
    {
        public static void Test(string dataBaseFile, string file)
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.Load(dataBaseFile);

            dataBase.QuestSigleFile(file, 500, 10, 0);
            //dataBase.QuestSigleFile(file, 500, 10, 0);
        }

        public static void TestRandom(string dataBaseFile, string file)
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.Load(dataBaseFile);

            dataBase.RandomQuerySigleFile(file, 20);
        }
    }
}
