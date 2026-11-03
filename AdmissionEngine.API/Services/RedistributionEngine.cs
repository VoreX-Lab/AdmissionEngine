using AdmissionEngine.API.Models.Entities;
using AdmissionEngine.API.Services.Interfaces;

namespace AdmissionEngine.API.Services
{
    public class RedistributionEngine : IRedistributionEngine
    {
        public async Task<List<Recommendation>> RecommendAlternativeCoursesAsync(User candidate)
        {
            var rules = new List<AdmissionRule>();
            return await RecommendAlternativeCoursesAsync(candidate, rules);
        }

        public Task<List<Recommendation>> RecommendAlternativeCoursesAsync(User candidate, List<AdmissionRule> rules)
        {
            var recommendations = new List<Recommendation>();

            if (candidate == null || rules == null || !rules.Any())
                return Task.FromResult(recommendations);

            bool hasOLevelData = candidate.OlevelSittings != null && candidate.OlevelSittings.Any();

            // Extract O'Level passed subjects if available
            var candidateOLevelSubjects = hasOLevelData
                ? candidate.OlevelSittings
                    .Where(s => s?.Subjects != null)
                    .SelectMany(s => s.Subjects)
                    .Where(s => GetGradePoint(s.Grade) >= 5)
                    .Select(s => NormalizeSubjectName(s.Name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            // Extract UTME subject choices
            var candidateUtmeSubjects = candidate.JambSubjects?
                .Select(s => NormalizeSubjectName(s.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

            foreach (var rule in rules)
            {
                // Skip the primary candidate preference
                if (rule.CourseName.Equals(candidate.PreferredCourse, StringComparison.OrdinalIgnoreCase) ||
                    rule.CourseName.Equals(candidate.AppliedCourseName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check minimum UTME score requirement
                if (candidate.JambScore < rule.MinimumJambScore)
                    continue;

                // CAPACITY CHECK: Skip courses that are already full
                if (rule.AdmittedCount >= rule.MaximumCapacity)
                    continue;

                double matchPercentage = 0.0;
                string reason = string.Empty;

                if (hasOLevelData && rule.RequiredOLevelSubjects != null && rule.RequiredOLevelSubjects.Any())
                {
                    // O'Level evaluation route
                    int matchedSubjects = rule.RequiredOLevelSubjects
                        .Count(subject => candidateOLevelSubjects.Contains(NormalizeSubjectName(subject)));

                    matchPercentage = (double)matchedSubjects / rule.RequiredOLevelSubjects.Count * 100;
                    reason = $"Qualified based on UTME score ({candidate.JambScore}) and O'Level match ({matchPercentage:F0}%).";
                }
                else
                {
                    // UTME score & subject combination route (Fallback for missing O'Level data)
                    matchPercentage = 100.0; // Meets score requirement
                    reason = $"Qualified based on UTME score ({candidate.JambScore}). Pending O'Level submission.";
                }

                if (matchPercentage >= 50.0)
                {
                    recommendations.Add(new Recommendation
                    {
                        CourseId = string.IsNullOrEmpty(rule.CourseId) ? rule.Id : rule.CourseId,
                        CourseName = rule.CourseName,
                        MatchPercentage = Math.Round(matchPercentage, 2),
                        Reason = reason
                    });
                }
            }

            return Task.FromResult(recommendations.OrderByDescending(r => r.MatchPercentage).ToList());
        }

        private static string NormalizeSubjectName(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return string.Empty;

            var name = subject.Trim().ToLowerInvariant();

            if (name.Contains("math")) return "Mathematics";
            if (name.Contains("english")) return "English Language";

            return subject.Trim();
        }

        private static int GetGradePoint(string grade)
        {
            return grade?.ToUpper() switch
            {
                "A1" => 10,
                "B2" => 9,
                "B3" => 8,
                "C4" => 7,
                "C5" => 6,
                "C6" => 5,
                _ => 0
            };
        }
    }
}