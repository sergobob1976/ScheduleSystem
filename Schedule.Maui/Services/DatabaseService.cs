using SQLite;
using Schedule.Maui.Models;
using System.Diagnostics;

namespace Schedule.Maui.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _connection;
        private readonly string _dbPath;

        public DatabaseService()
        {
            // Шлях до бази у внутрішній пам'яті телефону/ПК
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "SchedData.db");
            //виводимо шлях для перевірки ТЕСТ ТЕСТ ТЕСТ можна прибрати
            Debug.WriteLine("DB PATH: " + _dbPath);
        }

        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_connection != null) return _connection;

            // 1. Перевіряємо, чи база вже скопійована
            if (!File.Exists(_dbPath))
            {
                // 2. Якщо ні — копіюємо її з Resources/Raw
                using var stream = await FileSystem.OpenAppPackageFileAsync("SchedData.db");
                using var newFile = File.Create(_dbPath);
                await stream.CopyToAsync(newFile);
            }

            _connection = new SQLiteAsyncConnection(_dbPath);
            return _connection;
        }

        // Метод для отримання списку груп (для вашого Picker)
        public async Task<List<Group>> GetGroupsAsync()
        {
            var db = await GetConnectionAsync();
            return await db.Table<Group>().ToListAsync();
        }

        // Метод для отримання базового розкладу для конкретної групи
        public async Task<List<BaseLesson>> GetBaseLessonsAsync(string groupName)
        {
            var db = await GetConnectionAsync();
            return await db.Table<BaseLesson>()
                           .Where(l => l.Group == groupName)
                           .ToListAsync();
        }
    }
}
