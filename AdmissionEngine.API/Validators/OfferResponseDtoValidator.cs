using AdmissionEngine.API.Models.DTOS;
using FluentValidation;

namespace AdmissionEngine.API.Validators
{
    public class OfferResponseDtoValidator : AbstractValidator<OfferResponseDto>
    {
        public OfferResponseDtoValidator()
        {
            RuleFor(x => x.StudentId)
               .NotEmpty().WithMessage("Student ID is required.")
               .Must(id => MongoDB.Bson.ObjectId.TryParse(id, out _))
               .WithMessage("Student ID must be a valid 24-character hexadecimal MongoDB ObjectId.");

            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required.");
        }
    }
}