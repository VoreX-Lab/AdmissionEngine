using AdmissionEngine.API.Models.Entities;
using AdmissionEngine.API.Models.Enums;
using MiniExcelLibs;
using MongoDB.Driver;

namespace AdmissionEngine.API.Services
{
    public class DataSeeder
    {
        private readonly IMongoCollection<User> _usersCollection;

        public DataSeeder(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<User>("users");
        }

        public async Task SeedCandidatesFromExcelAsync(string excelFilePath)
        {
            if (!File.Exists(excelFilePath)) return;

            System.Diagnostics.Debug.WriteLine($"--> Parsing Excel dataset: {Path.GetFileName(excelFilePath)}...");
            var rows = MiniExcel.Query(excelFilePath, useHeaderRow: true);
            var candidatesToInsert = new List<User>();

            // Fetch existing registration numbers to avoid duplicate key exceptions
            var existingRegNos = (await _usersCollection
                .Find(u => u.JambRegNo != null)
                .Project(u => u.JambRegNo)
                .ToListAsync())
                .ToHashSet();

            foreach (IDictionary<string, object> row in rows)
            {
                var regNum = row["RG_NUM"]?.ToString()?.Trim() ?? string.Empty;

                // Skip if candidate already exists in MongoDB
                if (!string.IsNullOrEmpty(regNum) && existingRegNos.Contains(regNum))
                {
                    continue;
                }

                var candidate = new User
                {
                    JambRegNo = regNum,
                    FullName = row["RG_CANDNAME"]?.ToString() ?? string.Empty,
                    Role = "student",
                    Email = !string.IsNullOrEmpty(regNum)
                        ? $"{regNum.ToLower()}@jamb.gov.ng"
                        : $"{Guid.NewGuid():N}@candidate.com",
                    AppliedCourseName = row["CO_NAME"]?.ToString() ?? string.Empty,
                    JambScore = Convert.ToDouble(row["RG_AGGREGATE"] ?? 0),
                    Status = AdmissionStatus.Pending,
                    JambSubjects = new List<JambSubject>
            {
                new JambSubject { Name = "Use of English", Score = Convert.ToDouble(row["EngScore"] ?? 0) },
                new JambSubject { Name = row["Subject1"]?.ToString() ?? string.Empty, Score = Convert.ToDouble(row["RG_Sub1Score"] ?? 0) },
                new JambSubject { Name = row["Subject2"]?.ToString() ?? string.Empty, Score = Convert.ToDouble(row["RG_Sub2Score"] ?? 0) },
                new JambSubject { Name = row["Subject3"]?.ToString() ?? string.Empty, Score = Convert.ToDouble(row["RG_Sub3Score"] ?? 0) }
            }
                };

                candidatesToInsert.Add(candidate);
            }

            if (candidatesToInsert.Any())
            {
                await _usersCollection.InsertManyAsync(candidatesToInsert, new InsertManyOptions { IsOrdered = false });
                Console.WriteLine($"--> Successfully inserted {candidatesToInsert.Count} candidates from {Path.GetFileName(excelFilePath)}!");
            }
        }
    }
}