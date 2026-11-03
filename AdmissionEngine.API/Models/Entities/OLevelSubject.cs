using MongoDB.Bson.Serialization.Attributes;

namespace AdmissionEngine.API.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class OLevelSubject
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("grade")]
        public string Grade { get; set; } = string.Empty;
    }
}