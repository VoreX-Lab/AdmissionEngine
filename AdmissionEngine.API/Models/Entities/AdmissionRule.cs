using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AdmissionEngine.API.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class AdmissionRule
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string CourseId => Id;

        [BsonElement("courseName")]
        public string CourseName { get; set; } = string.Empty;

        [BsonElement("minimumJambScore")]
        public double MinimumJambScore { get; set; }

        [BsonElement("requiredOLevelSubjects")]
        public List<string> RequiredOLevelSubjects { get; set; } = new();
        public int MaximumOLevelSittings { get; set; } = 2;
        [BsonElement("maximumCapacity")]
        public int MaximumCapacity { get; set; } = 100; // Default capacity limit
        [BsonElement("admittedCount")]
        public int AdmittedCount { get; set; } = 0;     // Live admitted count
    }
}