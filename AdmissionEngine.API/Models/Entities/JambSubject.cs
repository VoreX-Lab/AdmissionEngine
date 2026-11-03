using MongoDB.Bson.Serialization.Attributes;

namespace AdmissionEngine.API.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class JambSubject
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("score")]
        public double Score { get; set; }
    }
}