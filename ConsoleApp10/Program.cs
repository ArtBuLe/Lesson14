using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Data.Common;

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

        const string tableName = "students_lesson14";

        static DbProviderFactory Factory = NpgsqlFactory.Instance;

        // ==================== ЗАГРУЗКА ====================
        static DataTable GetAll()
        {
            using var conn = Factory.CreateConnection();
            conn!.ConnectionString = connString;
            conn.Open();

            using var command = conn.CreateCommand();
            command.CommandText = $"SELECT * FROM {tableName}";

            using var adapter = Factory.CreateDataAdapter();
            adapter!.SelectCommand = command;

            DataTable table = new DataTable();
            adapter.Fill(table);

            Console.WriteLine($"Загружено строк: {table.Rows.Count}\n");
            ShowAll(table);

            return table;
        }

        // ==================== ОТОБРАЖЕНИЕ ====================
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
                        $"{row["nickname", DataRowVersion.Original],-15} - " +
                        $"{row["first_name", DataRowVersion.Original],-15} - " +
                        $"{row["last_name", DataRowVersion.Original],-15} - " +
                        $"{row["age", DataRowVersion.Original],-3} - " +
                        $"[{row.RowState}]"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"#{i,-3} {row["Id"],-3} - " +
                        $"{row["nickname"],-15} - " +
                        $"{row["first_name"],-15} - " +
                        $"{row["last_name"],-15} - " +
                        $"{row["age"],-3} - " +
                        $"[{row.RowState}]"
                    );
                }
            }
            Console.WriteLine();
        }

        // ==================== ДОБАВЛЕНИЕ ====================
        static DataTable AddRow(DataTable table)
        {
            var row = table.NewRow();

            Console.Write("Nickname   : "); row["nickname"] = Console.ReadLine()!;
            Console.Write("First Name : "); row["first_name"] = Console.ReadLine()!;
            Console.Write("Last Name  : "); row["last_name"] = Console.ReadLine()!;
            Console.Write("Age        : "); row["age"] = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine($"Before add : {row.RowState}");
            table.Rows.Add(row);
            Console.WriteLine($"After add  : {row.RowState}\n");

            return table;
        }

        // ==================== ИЗМЕНЕНИЕ ====================
        static DataTable ChangeStudent(DataTable table)
        {
            ShowAll(table);
            Console.Write("Choose student index to edit: ");
            int choose = Convert.ToInt32(Console.ReadLine());

            if (choose < 0 || choose >= table.Rows.Count)
            {
                Console.WriteLine("Неверный индекс!\n");
                return table;
            }

            var row = table.Rows[choose];
            if (row.RowState == DataRowState.Deleted)
            {
                Console.WriteLine("Нельзя изменить удалённую строку!\n");
                return table;
            }

            Console.WriteLine($"\nCurrent: {row["first_name"]} {row["last_name"]}, age = {row["age"]}");
            Console.Write("Enter new age: ");
            row["age"] = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine($"RowState после изменения: {row.RowState}\n");
            return table;
        }

        // ==================== УДАЛЕНИЕ ====================
        static DataTable DeleteStudent(DataTable table)
        {
            ShowAll(table);
            Console.Write("Choose student index to delete: ");
            int choose = Convert.ToInt32(Console.ReadLine());

            if (choose < 0 || choose >= table.Rows.Count)
            {
                Console.WriteLine("Неверный индекс!\n");
                return table;
            }

            var row = table.Rows[choose];
            if (row.RowState == DataRowState.Deleted)
            {
                Console.WriteLine("Строка уже помечена на удаление!\n");
                return table;
            }

            row.Delete();
            Console.WriteLine($"RowState после Delete(): {row.RowState}\n");
            return table;
        }

        // ==================== ЗАДАЧА 3: СОХРАНЕНИЕ В БД ====================
        static void SaveChanges(DataTable table)
        {
            var changes = table.GetChanges();
            if (changes == null)
            {
                Console.WriteLine("Нечего сохранять (нет изменений).\n");
                return;
            }

            Console.WriteLine("=== RowState ПЕРЕД сохранением ===");
            ShowAll(table);

            try
            {
                using var conn = (NpgsqlConnection)Factory.CreateConnection()!;
                conn.ConnectionString = connString;
                conn.Open();

                using var transaction = conn.BeginTransaction();

                using var adapter = (NpgsqlDataAdapter)Factory.CreateDataAdapter()!;

                adapter.SelectCommand = new NpgsqlCommand($"SELECT * FROM {tableName}", conn);
                adapter.SelectCommand.Transaction = transaction;

                adapter.InsertCommand = new NpgsqlCommand(
                    $"""
            INSERT INTO {tableName} (nickname, first_name, last_name, age)
            VALUES (@nick, @fname, @lname, @age)
            RETURNING id
            """, conn);
                adapter.InsertCommand.Transaction = transaction;

                adapter.InsertCommand.Parameters.Add(new NpgsqlParameter("@nick", NpgsqlDbType.Varchar)
                {
                    SourceColumn = "nickname"
                });
                adapter.InsertCommand.Parameters.Add(new NpgsqlParameter("@fname", NpgsqlDbType.Varchar)
                {
                    SourceColumn = "first_name"
                });
                adapter.InsertCommand.Parameters.Add(new NpgsqlParameter("@lname", NpgsqlDbType.Varchar)
                {
                    SourceColumn = "last_name"
                });
                adapter.InsertCommand.Parameters.Add(new NpgsqlParameter("@age", NpgsqlDbType.Integer)
                {
                    SourceColumn = "age"
                });

                var returnIdParam = new NpgsqlParameter("@id", NpgsqlDbType.Integer)
                {
                    Direction = ParameterDirection.Output,
                    SourceColumn = "Id"
                };
                adapter.InsertCommand.Parameters.Add(returnIdParam);
                adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.OutputParameters;

                adapter.UpdateCommand = new NpgsqlCommand(
                    $"""
            UPDATE {tableName}
            SET nickname = @nick, first_name = @fname, last_name = @lname, age = @age
            WHERE id = @id
            """, conn);
                adapter.UpdateCommand.Transaction = transaction;

                adapter.UpdateCommand.Parameters.Add(new NpgsqlParameter("@nick", NpgsqlDbType.Varchar)
                {
                    SourceColumn = "nickname"
                });
                adapter.UpdateCommand.Parameters.Add(new NpgsqlParameter("@fname", NpgsqlDbType.Varchar)
                {
                    SourceColumn = "first_name"
                });
                adapter.UpdateCommand.Parameters.Add(new NpgsqlParameter("@lname", NpgsqlDbType.Varchar)
                {
                    SourceColumn = "last_name"
                });
                adapter.UpdateCommand.Parameters.Add(new NpgsqlParameter("@age", NpgsqlDbType.Integer)
                {
                    SourceColumn = "age"
                });
                adapter.UpdateCommand.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Integer)
                {
                    SourceColumn = "Id",
                    SourceVersion = DataRowVersion.Original
                });

                adapter.DeleteCommand = new NpgsqlCommand(
                    $"DELETE FROM {tableName} WHERE id = @id", conn);
                adapter.DeleteCommand.Transaction = transaction;

                adapter.DeleteCommand.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Integer)
                {
                    SourceColumn = "Id",
                    SourceVersion = DataRowVersion.Original
                });

                // Выполняем Update
                int affected = adapter.Update(table);
                Console.WriteLine($"\n Успешно сохранено строк в БД: {affected}");

                transaction.Commit();
                Console.WriteLine("Транзакция зафиксирована.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n ОШИБКА сохранения: {ex.Message}");
                return;
            }

            Console.WriteLine("=== RowState ПОСЛЕ сохранения ===");
            ShowAll(table);

            VerifyInDatabase();
        }

        // Проверка, что данные реально записаны в PostgreSQL
        static void VerifyInDatabase()
        {
            Console.WriteLine("\n=== Проверка БД отдельным запросом ===");
            using var conn = Factory.CreateConnection();
            conn!.ConnectionString = connString;
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {tableName} ORDER BY id";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(
                    $"#{reader["id"],-3} - {reader["nickname"],-15} - " +
                    $"{reader["first_name"],-15} - {reader["last_name"],-15} - " +
                    $"age={reader["age"]}"
                );
            }
            Console.WriteLine();
        }

        // ==================== ЗАДАЧА 4: ФИЛЬТРАЦИЯ И СОРТИРОВКА ====================
        static void FilterAndSort(DataTable table)
        {
            if (table.Rows.Count == 0)
            {
                Console.WriteLine("Таблица пуста! Сначала загрузите данные.\n");
                return;
            }

            DataView view = new DataView(table);

            Console.WriteLine
            (
            """
            Фильтрация:
            1 - Возраст > 18
            2 - Возраст <= 18
            3 - Имя содержит 'а' (без учёта регистра)
            4 - Без фильтра
            0 - Назад
            """
            );
            int choice = Convert.ToInt32(Console.ReadLine());

            switch (choice)
            {
                case 1: view.RowFilter = "age > 18"; break;
                case 2: view.RowFilter = "age <= 18"; break;
                case 3: view.RowFilter = "first_name LIKE '%а%' OR first_name LIKE '%А%'"; break;
                case 4: view.RowFilter = ""; break;
                case 0: return;
                default: return;
            }

            Console.WriteLine("\nСортировка:");
            Console.WriteLine("1 - По имени (ASC)");
            Console.WriteLine("2 - По возрасту (DESC)");
            Console.WriteLine("3 - Без сортировки");
            int sortChoice = Convert.ToInt32(Console.ReadLine());

            switch (sortChoice)
            {
                case 1: view.Sort = "first_name ASC"; break;
                case 2: view.Sort = "age DESC"; break;
                case 3: view.Sort = ""; break;
            }

            Console.WriteLine($"\n=== Результат (строк в DataView: {view.Count}) ===");
            Console.WriteLine($"RowFilter : \"{view.RowFilter}\"");
            Console.WriteLine($"Sort      : \"{view.Sort}\"\n");

            foreach (DataRowView rowView in view)
            {
                DataRow row = rowView.Row;
                Console.WriteLine(
                    $"#{row["Id"],-3} - {row["first_name"],-15} - " +
                    $"{row["last_name"],-15} - age={row["age"],-3} - [{row.RowState}]"
                );
            }

            // ✅ Убеждаемся, что в DataTable строки не изменились
            Console.WriteLine($"\nВ исходной DataTable по-прежнему {table.Rows.Count} строк.");
            Console.WriteLine("DataView — это только ПРЕДСТАВЛЕНИЕ, БД и DataTable не изменены.\n");
        }

        // ==================== ЖУРНАЛ ИЗМЕНЕНИЙ ====================
        static void PrintChangeLog(DataTable table)
        {
            Console.WriteLine("=== Журнал изменений ===\n");
            bool hasChanges = false;

            foreach (DataRow row in table.Rows)
            {
                switch (row.RowState)
                {
                    case DataRowState.Modified:
                        hasChanges = true;
                        Console.WriteLine(
                            $"[MODIFIED] {row["first_name", DataRowVersion.Original]} " +
                            $"{row["last_name", DataRowVersion.Original]} (age={row["age", DataRowVersion.Original]}) → " +
                            $"{row["first_name", DataRowVersion.Current]} " +
                            $"{row["last_name", DataRowVersion.Current]} (age={row["age", DataRowVersion.Current]})"
                        );
                        break;

                    case DataRowState.Added:
                        hasChanges = true;
                        Console.WriteLine(
                            $"[ADDED]    {row["first_name"]} {row["last_name"]}, age={row["age"]}"
                        );
                        break;

                    case DataRowState.Deleted:
                        hasChanges = true;
                        Console.WriteLine(
                            $"[DELETED]  {row["first_name", DataRowVersion.Original]} " +
                            $"{row["last_name", DataRowVersion.Original]}, age={row["age", DataRowVersion.Original]}"
                        );
                        break;
                }
            }

            if (!hasChanges) Console.WriteLine("(нет изменений)");
            Console.WriteLine();
        }

        // ==================== ГЛАВНОЕ МЕНЮ ====================
        static void Main()
        {
            DataTable table = new DataTable();

            while (true)
            {
                Console.WriteLine
                (
                """
                Choose action :
                 1 - Get All (загрузка из БД)
                 2 - Show All
                 3 - Add Row
                 4 - Change student (возраст)
                 5 - Delete student
                 6 - Print Change Log
                 7 - RejectChanges
                 8 - AcceptChanges
                 9 - SAVE TO DB (Update + транзакция)
                10 - Verify in DB (отдельный SELECT)
                11 - Filter & Sort (DataView)
                 0 - Exit
                """
                );
                int choose = Convert.ToInt32(Console.ReadLine());
                Console.Clear();

                switch (choose)
                {
                    case 1: table = GetAll(); break;
                    case 2: ShowAll(table); break;
                    case 3: table = AddRow(table); break;
                    case 4: table = ChangeStudent(table); break;
                    case 5: table = DeleteStudent(table); break;
                    case 6: PrintChangeLog(table); break;
                    case 7:
                        table.RejectChanges();
                        Console.WriteLine("RejectChanges() выполнен.\n");
                        break;
                    case 8:
                        table.AcceptChanges();
                        Console.WriteLine("AcceptChanges() выполнен (локально, в БД не пишет!).\n");
                        break;
                    case 9: SaveChanges(table); break;
                    case 10: VerifyInDatabase(); break;
                    case 11: FilterAndSort(table); break;
                    case 0: return;
                }

                Console.WriteLine("\nPress any button to continue...");
                Console.ReadLine();
                Console.Clear();
            }
        }
    }
}