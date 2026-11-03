using AdmissionEngine.API.Models.DTOS;
using AdmissionEngine.API.Models.Entities;
using AdmissionEngine.API.Models.Enums;
using AdmissionEngine.API.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AdmissionEngine.API.Services
{
    public class AdmissionEngineService : IAdmissionEngineService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<AdmissionRule> _rulesCollection;
        private readonly IRedistributionEngine _redistributionEngine;

        public AdmissionEngineService(
            IMongoDatabase database,
            IRedistributionEngine redistributionEngine)
        {
            _usersCollection = database.GetCollection<User>("users");
            _rulesCollection = database.GetCollection<AdmissionRule>("admissionrules");
            _redistributionEngine = redistributionEngine;
        }

        public async Task<EvaluationResultDto> EvaluateApplicantAsync(string studentId)
        {
            // 1. Fetch Student Record
            FilterDefinition<User> userFilter = ObjectId.TryParse(studentId, out var parsedObjectId)
                ? Builders<User>.Filter.Eq("_id", parsedObjectId)
                : Builders<User>.Filter.Eq(u => u.Id, studentId);

            var candidate = await _usersCollection.Find(userFilter).FirstOrDefaultAsync();

            if (candidate == null)
            {
                return new EvaluationResultDto
                {
                    StudentId = studentId,
                    IsEligible = false,
                    Status = AdmissionStatus.Rejected,
                    TargetCourse = "",
                    Remarks = new List<string> { "Candidate not found." }
                };
            }

            string targetCourse = !string.IsNullOrWhiteSpace(candidate.PreferredCourse)
                ? candidate.PreferredCourse
                : "Computer Science";

            var allRules = await _rulesCollection.Find(_ => true).ToListAsync();
            var targetRule = allRules.FirstOrDefault(r =>
                r.CourseName.Equals(targetCourse, StringComparison.OrdinalIgnoreCase));

            var remarks = new List<string>();
            bool isEligible = true;

            if (targetRule == null)
            {
                remarks.Add($"No admission criteria rule found for course '{targetCourse}'.");
                isEligible = false;
            }
            else
            {
                // Check Minimum UTME Cut-off
                if (candidate.JambScore < targetRule.MinimumJambScore)
                {
                    isEligible = false;
                    remarks.Add($"UTME score ({candidate.JambScore}) is below required cutoff ({targetRule.MinimumJambScore}) for {targetCourse}.");
                }

                // Extract passed O'Level subjects (C6 or higher)
                var candidatePassedSubjects = candidate.OlevelSittings?
                    .Where(s => s?.Subjects != null)
                    .SelectMany(s => s.Subjects)
                    .Where(s => GetGradePoint(s.Grade) >= 5)
                    .Select(s => NormalizeSubjectName(s.Name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

                var missingSubjects = targetRule.RequiredOLevelSubjects
                    .Where(sub => !candidatePassedSubjects.Contains(NormalizeSubjectName(sub)))
                    .ToList();

                if (missingSubjects.Any())
                {
                    isEligible = false;
                    remarks.Add($"Missing required O'Level pass (C6+) in: {string.Join(", ", missingSubjects)}.");
                }

                // CAPACITY CHECK: Candidate passed academically, but department capacity is full
                if (isEligible && targetRule.AdmittedCount >= targetRule.MaximumCapacity)
                {
                    isEligible = false;
                    remarks.Clear();
                    remarks.Add($"You met the academic cut-off for {targetCourse}, but maximum student capacity ({targetRule.MaximumCapacity}) has been reached.");
                }
            }

            // Generate alternative recommendations if ineligible (or if capacity was reached)
            var alternatives = !isEligible
                ? await _redistributionEngine.RecommendAlternativeCoursesAsync(candidate, allRules)
                : new List<Recommendation>();

            if (isEligible)
            {
                remarks.Add($"Candidate meets all academic criteria for {targetCourse}.");
            }

            return new EvaluationResultDto
            {
                StudentId = studentId,
                IsEligible = isEligible,
                Status = isEligible ? AdmissionStatus.Pending : AdmissionStatus.Rejected,
                TargetCourse = targetCourse,
                Remarks = remarks,
                RecommendedCourses = alternatives
            };
        }

        public async Task<string> RespondToCourseOfferAsync(OfferResponseDto responseDto)
        {
            // 1. Fetch Candidate Record
            FilterDefinition<User> userFilter = ObjectId.TryParse(responseDto.StudentId, out var userObjectId)
                ? Builders<User>.Filter.Eq("_id", userObjectId)
                : Builders<User>.Filter.Eq(u => u.Id, responseDto.StudentId);

            var candidate = await _usersCollection.Find(userFilter).FirstOrDefaultAsync();
            if (candidate == null)
            {
                return "Candidate record not found.";
            }

            // 2. Prevent Accepting Multiple Admissions
            if (responseDto.Accept && candidate.Status == AdmissionStatus.OfferAccepted)
            {
                return "You have already accepted an admission offer and cannot accept another course.";
            }

            if (!responseDto.Accept && candidate.Status == AdmissionStatus.OfferDeclined)
            {
                return "You have already declined this admission offer.";
            }

            // 3. Process Acceptance with Capacity Check
            if (responseDto.Accept)
            {
                FilterDefinition<AdmissionRule> ruleFilter = ObjectId.TryParse(responseDto.CourseId, out var courseObjectId)
                    ? Builders<AdmissionRule>.Filter.Eq("_id", courseObjectId)
                    : Builders<AdmissionRule>.Filter.Eq(r => r.Id, responseDto.CourseId);

                var courseRule = await _rulesCollection.Find(ruleFilter).FirstOrDefaultAsync();

                if (courseRule == null)
                {
                    return "Selected course rule not found.";
                }

                // Capacity Guard: Stop attempts to bypass evaluation or overfill quota
                if (courseRule.AdmittedCount >= courseRule.MaximumCapacity)
                {
                    return $"No more space available in {courseRule.CourseName}. The maximum capacity ({courseRule.MaximumCapacity}) has been reached.";
                }

                // Increment quota atomically for this course
                var ruleUpdate = Builders<AdmissionRule>.Update.Inc(r => r.AdmittedCount, 1);
                await _rulesCollection.UpdateOneAsync(ruleFilter, ruleUpdate);

                // Update candidate status and update preferred course to the accepted offer
                var userUpdate = Builders<User>.Update
                    .Set(u => u.Status, AdmissionStatus.OfferAccepted)
                    .Set(u => u.PreferredCourse, courseRule.CourseName);

                await _usersCollection.UpdateOneAsync(userFilter, userUpdate);

                return $"Congratulations! You have successfully accepted your admission offer for {courseRule.CourseName}.";
            }

            // Handle Offer Decline
            var declineUpdate = Builders<User>.Update.Set(u => u.Status, AdmissionStatus.OfferDeclined);
            await _usersCollection.UpdateOneAsync(userFilter, declineUpdate);

            return "You have declined the admission offer.";
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