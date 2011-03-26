using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicIdentifier;

namespace Test
{
    class QueryTest
    {
        public static void Test(string dataBaseFile, string file, string correctName)
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.Load(dataBaseFile);

            //dataBase.QuestSigleFile(file, 500, 10, 0, correctName);
            dataBase.QuestRandomSigleFile(file, 500, 10, 4, correctName);
        }

        public static void TestRandom(string dataBaseFile, string file, string correctName)
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.Load(dataBaseFile);

            dataBase.RandomQuerySigleFile(file, 40, correctName);
        }
    }
}
