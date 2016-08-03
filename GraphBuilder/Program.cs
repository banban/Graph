using System.Collections.Generic;

namespace DBGraph
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, DBEntryDescriptor> dbObjects = new Dictionary<string, DBEntryDescriptor>();
            DBHelper dbh = new DBHelper();
            YedHelper yeh = new YedHelper();
            if (args.Length >= 1)
            {
                yeh.YedOutputFilePath = args[0];
            }
            if (args.Length >= 2)
            {
                dbh.Request = args[1];
            }
            if (args.Length >= 3)
            {
                dbh.DBConnectionString = args[2];
            }
            dbh.LoadData(ref dbObjects);
            yeh.Build(dbObjects);
        }
    }
}
