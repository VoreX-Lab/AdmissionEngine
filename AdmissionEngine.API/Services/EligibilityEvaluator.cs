using AdmissionEngine.API.Models.DTOS;
using AdmissionEngine.API.Models.Entities;
using AdmissionEngine.API.Models.Enums;
using AdmissionEngine.API.Services.Interfaces;

namespace AdmissionEngine.API.Services
{
    public class EligibilityEvaluator : IEligibilityEvaluator
    {
        private static readonly Dictionary<string, int> GradePoints = new(StringComparer.OrdinalIgnoreCase)
        {
            { "A1", 10 }, { "B2", 9 }, { "B3", 8 },
            { "C4", 7 },  { "C5", 6 }, { "C6", 5 },
            { "D7", 4 },  { "E8", 3 }, { "F9", 0 }
        };

        public EvaluationResultDto EvaluateCandidate(User candidate, AdmissionRule rule)
        {
            var result = new EvaluationResultDto();

            // 1. Evaluate UTME Score
            if (candidate.JambScore < rule.MinimumJambScore)
            {
                result.IsEligible = false;
                result.Status = AdmissionStatus.Rejected;
                result.Remarks.Add($"JAMB score ({candidate.JambScore}) is below required threshold ({rule.MinimumJambScore}).");
            }

            // 2. Evaluate O'Level Subjects
            var studentGrades = candidate.OlevelSittings
                .SelectMany(s => s.Subjects)
                .ToDictionary(s => s.Name, s => s.Grade, StringComparer.OrdinalIgnoreCase);

            foreach (var requiredSubject in rule.RequiredOLevelSubjects)
            {
                if (!studentGrades.TryGetValue(requiredSubject, out var grade) || GetGradePoint(grade) < 5) // Minimum C6
                {
                    result.IsEligible = false;
                    result.Status = AdmissionStatus.Rejected;
                    result.Remarks.Add($"Missing or insufficient grade for required O'Level subject: {requiredSubject}.");
                }
            }

            if (result.IsEligible)
            {
                result.Status = AdmissionStatus.AdmittedPrimary;
                result.Remarks.Add("Candidate meets all basic admission criteria.");
            }

            return result;
        }

        private static int GetGradePoint(string grade)
        {
            return GradePoints.TryGetValue(grade, out var point) ? point : 0;
        }
    }
}