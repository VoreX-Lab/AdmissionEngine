using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace AdmissionEngine.API.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class OLevelSitting
    {
        [BsonElement("sittingNumber")]
        public int SittingNumber { get; set; }

        [BsonElement("examType")]
        public string ExamType { get; set; } = string.Empty; // e.g., WAEC, NECO

        [BsonElement("examYear")]
        public string ExamYear { get; set; } = string.Empty;

        [BsonElement("subjects")]
        public List<OLevelSubject> Subjects { get; set; } = new();
    }
}