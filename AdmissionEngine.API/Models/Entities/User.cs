using AdmissionEngine.API.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace AdmissionEngine.API.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("fullName")]
        public string FullName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("role")]
        public string Role { get; set; } = "student";

        [BsonElement("appliedCourseName")]
        public string AppliedCourseName { get; set; } = string.Empty;

        // UTME / JAMB Fields
        [BsonElement("jambScore")]
        public double JambScore { get; set; }

        [BsonElement("jambRegNo")]
        public string JambRegNo { get; set; } = string.Empty;

        [BsonElement("jambSubjects")]
        public List<JambSubject> JambSubjects { get; set; } = new();

        // O'Level Sittings
        [BsonElement("olevelSittings")]
        public List<OLevelSitting> OlevelSittings { get; set; } = new();

        [BsonElement("status")]
        public AdmissionStatus Status { get; set; }

        [BsonElement("preferredCourse")]
        public string PreferredCourse { get; set; } = string.Empty;
    }
}