using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace SqlServerManager.Core.QueryEngine
{
    public class QueryHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SqlQuery { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public string Database { get; set; } = string.Empty;
        public string ConnectionName { get; set; } = string.Empty;
        public int RowsAffected { get; set; }
        public bool IsSuccessful { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class QueryFavorite
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string SqlQuery { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUsed { get; set; } = DateTime.Now;
        public int UsageCount { get; set; } = 0;
    }

    public class QueryHistoryManager
    {
        private readonly string _historyFilePath;
        private readonly string _favoritesFilePath;
        private readonly int _maxHistoryEntries;
        private List<QueryHistory> _historyCache;
        private List<QueryFavorite> _favoritesCache;

        public QueryHistoryManager(string dataDirectory = null, int maxHistoryEntries = 1000)
        {
            var appDataDir = dataDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SqlServerManager");

            Directory.CreateDirectory(appDataDir);

            _historyFilePath = Path.Combine(appDataDir, "query_history.json");
            _favoritesFilePath = Path.Combine(appDataDir, "query_favorites.json");
            _maxHistoryEntries = maxHistoryEntries;

            LoadData();
        }

        #region History Management

        public void SaveQuery(string sql, DateTime executed, TimeSpan duration, 
            string database = "", string connectionName = "", int rowsAffected = 0, 
            bool isSuccessful = true, string errorMessage = "")
        {
            if (string.IsNullOrWhiteSpace(sql)) return;

            var historyItem = new QueryHistory
            {
                SqlQuery = sql.Trim(),
                ExecutedAt = executed,
                Duration = duration,
                Database = database,
                ConnectionName = connectionName,
                RowsAffected = rowsAffected,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage
            };

            _historyCache.Insert(0, historyItem);

            // Maintain max history entries
            if (_historyCache.Count > _maxHistoryEntries)
            {
                _historyCache = _historyCache.Take(_maxHistoryEntries).ToList();
            }

            SaveHistoryToFile();
        }

        public List<QueryHistory> GetRecentQueries(int count = 50)
        {
            return _historyCache.Take(count).ToList();
        }

        public List<QueryHistory> SearchHistory(string searchTerm, bool includeErrors = false)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetRecentQueries();

            return _historyCache
                .Where(h => (includeErrors || h.IsSuccessful) &&
                           (h.SqlQuery.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            h.Database.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .Take(100)
                .ToList();
        }

        public void ClearHistory()
        {
            _historyCache.Clear();
            SaveHistoryToFile();
        }

        public void DeleteHistoryItem(string id)
        {
            _historyCache.RemoveAll(h => h.Id == id);
            SaveHistoryToFile();
        }

        #endregion

        #region Favorites Management

        public void AddToFavorites(string name, string sql, string description = "", string category = "General")
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sql))
                return;

            // Check if already exists
            var existing = _favoritesCache.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.SqlQuery = sql.Trim();
                existing.Description = description;
                existing.Category = category;
            }
            else
            {
                var favorite = new QueryFavorite
                {
                    Name = name,
                    SqlQuery = sql.Trim(),
                    Description = description,
                    Category = category
                };

                _favoritesCache.Add(favorite);
            }

            SaveFavoritesToFile();
        }

        public List<QueryFavorite> GetFavoriteQueries()
        {
            return _favoritesCache.OrderBy(f => f.Category).ThenBy(f => f.Name).ToList();
        }

        public Dictionary<string, List<QueryFavorite>> GetFavoritesByCategory()
        {
            return _favoritesCache
                .GroupBy(f => f.Category)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.Name).ToList());
        }

        public QueryFavorite GetFavoriteById(string id)
        {
            return _favoritesCache.FirstOrDefault(f => f.Id == id);
        }

        public QueryFavorite GetFavoriteByName(string name)
        {
            return _favoritesCache.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void UpdateFavoriteUsage(string id)
        {
            var favorite = GetFavoriteById(id);
            if (favorite != null)
            {
                favorite.LastUsed = DateTime.Now;
                favorite.UsageCount++;
                SaveFavoritesToFile();
            }
        }

        public void DeleteFavorite(string id)
        {
            _favoritesCache.RemoveAll(f => f.Id == id);
            SaveFavoritesToFile();
        }

        public void RenameFavorite(string id, string newName)
        {
            var favorite = GetFavoriteById(id);
            if (favorite != null && !string.IsNullOrWhiteSpace(newName))
            {
                favorite.Name = newName;
                SaveFavoritesToFile();
            }
        }

        public List<string> GetCategories()
        {
            return _favoritesCache.Select(f => f.Category).Distinct().OrderBy(c => c).ToList();
        }

        #endregion

        #region File Operations

        private void LoadData()
        {
            _historyCache = LoadHistoryFromFile();
            _favoritesCache = LoadFavoritesFromFile();
        }

        private List<QueryHistory> LoadHistoryFromFile()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    return JsonSerializer.Deserialize<List<QueryHistory>>(json) ?? new List<QueryHistory>();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - start with empty history
                Console.WriteLine($"Error loading query history: {ex.Message}");
            }

            return new List<QueryHistory>();
        }

        private List<QueryFavorite> LoadFavoritesFromFile()
        {
            try
            {
                if (File.Exists(_favoritesFilePath))
                {
                    var json = File.ReadAllText(_favoritesFilePath);
                    return JsonSerializer.Deserialize<List<QueryFavorite>>(json) ?? new List<QueryFavorite>();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - start with empty favorites
                Console.WriteLine($"Error loading query favorites: {ex.Message}");
            }

            return new List<QueryFavorite>();
        }

        private void SaveHistoryToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(_historyCache, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving query history: {ex.Message}");
            }
        }

        private void SaveFavoritesToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(_favoritesCache, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_favoritesFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving query favorites: {ex.Message}");
            }
        }

        #endregion

        #region Statistics

        public Dictionary<string, object> GetStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalHistoryEntries"] = _historyCache.Count,
                ["TotalFavorites"] = _favoritesCache.Count,
                ["SuccessfulQueries"] = _historyCache.Count(h => h.IsSuccessful),
                ["FailedQueries"] = _historyCache.Count(h => !h.IsSuccessful),
                ["AverageExecutionTime"] = _historyCache.Where(h => h.IsSuccessful).Average(h => h.Duration.TotalMilliseconds),
                ["MostUsedFavorite"] = _favoritesCache.OrderByDescending(f => f.UsageCount).FirstOrDefault()?.Name ?? "None",
                ["CategoriesCount"] = GetCategories().Count
            };

            return stats;
        }

        #endregion
    }
}
