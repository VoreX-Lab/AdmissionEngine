using AdmissionEngine.API.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AdmissionEngine.API.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class Application
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public string TargetCourse { get; set; } = string.Empty;
        public AdmissionStatus Status { get; set; } = AdmissionStatus.Pending;
        public string OfferedCourse { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}