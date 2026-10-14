using Npgsql;
using System.Data;
using System.Data.Common;
using System.Security.AccessControl;

namespace Test
{
    class Program
    {
        const string connString =
                    "Host=wotacoma.ru;" +
                    "Port=5090;" +
                    "Username=art_l;" +
                    "Password=827920;" +
                    "Database=art_l";
        static DbProviderFactory Factory = NpgsqlFactory.Instance;

        static DataTable GetAll()
        {
            using var conn = Factory.CreateConnection();
            conn!.ConnectionString = connString;
            conn.Open();

            using var command = conn.CreateCommand();
            command.CommandText =
                """
                SELECT * FROM students_lesson13
                """;

            using var adapter = Factory.CreateDataAdapter();
            adapter!.SelectCommand = command;

            DataTable table = new DataTable();
            adapter.Fill(table);
            ShowAll(table);
            Console.WriteLine("\n");

            return table;
        }

        static void ShowAll(DataTable table)
        {
            Console.WriteLine($"Всего строк: {table.Rows.Count}\n");

            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow row = table.Rows[i];
                if (row.RowState == DataRowState.Deleted)
                {
                    Console.WriteLine(
                        $"#{i,-3} {row["Id", DataRowVersion.Original],-3} - " +
                        $"{row["nickname", DataRowVersion.Original],-20} - " +
                        $"{row["first_name", DataRowVersion.Original],-20} - " +
                        $"{row["last_name", DataRowVersion.Original],-20} - " +
                        $"[{row.RowState}]"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"#{i,-3} {row["Id"],-3} - " +
                        $"{row["nickname"],-20} - " +
                        $"{row["first_name"],-20} - " +
                        $"{row["last_name"],-20} - " +
                        $"[{row.RowState}]"
                    );
                }
            }
            Console.WriteLine();
        }

        static DataTable AddRow(DataTable table)
        {
            var row = table.NewRow(); 
            
            row["nickname"] = "new_user";
            row["first_name"] = "Test";
            row["last_name"] = "User";

            Console.WriteLine($"Before add : {row.RowState}");

            table.Rows.Add(row);

            Console.WriteLine($"After add : {row.RowState}");
            Console.WriteLine($"[{row.RowState}] {row["nickname"]}");

            return table;
        }

        static DataTable ChangeStudent(DataTable table)
        {
            ShowAll(table);
            Console.WriteLine("Choose student to redact :");
            int choose = Convert.ToInt32(Console.ReadLine());
            var row = table.Rows[choose];

            if (row.RowState == DataRowState.Deleted)
            {
                Console.WriteLine("Нельзя изменить удалённую строку!\n");
                return table;
            }

            Console.Clear();

            Console.WriteLine
            (
            $"Current :\n" +
            $"Nickame - {row[1],-20}" +
            $"First Name - {row[2],-20}" +
            $"Last Name - {row[3],-20}"
            );

            Console.WriteLine("Enter new Nickname :\n");
            string nick = Console.ReadLine()!; 
            Console.WriteLine("Enter new FirstName :\n");
            string fname = Console.ReadLine()!; 
            Console.WriteLine("Enter new LastName :\n");
            string lname = Console.ReadLine()!;
            Console.Clear();


            Console.WriteLine($"Before update : {row.RowState}");
            row[1] = nick;
            row[2] = fname;
            row[3] = lname;
            Console.WriteLine($"After update : {row.RowState}");
            Console.WriteLine("\n\n");
            Console.WriteLine
            (
            $"Update :\n" +
            $"{row[1, DataRowVersion.Original],-20} => {row[1, DataRowVersion.Current],-20}\n" +
            $"{row[2, DataRowVersion.Original],-20} => {row[2, DataRowVersion.Current],-20}\n" +
            $"{row[3, DataRowVersion.Original],-20} => {row[3, DataRowVersion.Current],-20}\n"
            );

            return table;
        }

        static DataTable DeleteStudent(DataTable table)
        {
            ShowAll(table);
            Console.WriteLine("Choose student to delete :");
            int choose = Convert.ToInt32(Console.ReadLine());

            var row = table.Rows[choose];

            Console.Clear();

            Console.WriteLine($"\nBefore delete: {row.RowState}");

            row.Delete();

            Console.WriteLine($"After delete: {row.RowState}");
            Console.WriteLine($"Row marked to delete: {row["nickname", DataRowVersion.Original]}\n");

            return table;
        }

        static void Main()
        {
            DataTable table = new DataTable();
            while(true)
            {
                Console.WriteLine
                (
                """
                Choose action :
                1 - Get All
                2 - Show All (FOR CHECKING UPDATES)
                3 - Add Row
                4 - Change student
                5 - Delete student
                6 - Reject changes
                7 - Accept changes

                """
                );
                int choose = Convert.ToInt32(Console.ReadLine());
                Console.Clear();

                switch (choose)
                {
                    case 1:
                        table = GetAll();
                        break;
                    case 2:
                        ShowAll(table);
                        break;
                    case 3:
                        table = AddRow(table);
                        break;
                    case 4:
                        table = ChangeStudent(table);
                        break;
                    case 5:
                        table = DeleteStudent(table);
                        break;
                    case 6:
                        table.RejectChanges();
                        break;
                    case 7:
                        table.AcceptChanges();
                        break;
                }

                Console.WriteLine("\nPress any button to continue...");
                Console.ReadLine();
                Console.Clear();
            }
        }
    }
}